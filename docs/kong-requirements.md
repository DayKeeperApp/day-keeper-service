# Kong Gateway Requirements — DayKeeper Service

> **Status**: Placeholder for Phase 2 integration with central Kong gateway.
> **Last updated**: 2026-03-08

## Overview

DayKeeper Phase 1 uses Cloudflare Tunnel connecting directly to the API service.
Phase 2 introduces a central Kong gateway between Cloudflare and the API:

```text
Internet → Cloudflare Edge (TLS) → cloudflared → kong-proxy.daykeeper.svc:80 → daykeeper-api:8080
```

This document specifies the routes, plugins, and configuration that the Kong
gateway needs to proxy traffic to the DayKeeper service.

See also: [Cloudflare Tunnel Requirements](cloudflare-tunnel-requirements.md)

## Service & Upstream

| Setting  | Value                                       |
| -------- | ------------------------------------------- |
| Name     | `daykeeper-api`                             |
| Host     | `daykeeper-api.daykeeper.svc.cluster.local` |
| Port     | `8080`                                      |
| Protocol | `http` (TLS terminates at Cloudflare edge)  |
| Retries  | `2`                                         |

## Routes

### Application Routes

| Route Pattern           | Methods     | Notes                          |
| ----------------------- | ----------- | ------------------------------ |
| `/graphql`              | POST        | Hot Chocolate GraphQL endpoint |
| `/api/v1/sync/push`     | POST        | Cursor-based sync upload       |
| `/api/v1/sync/pull`     | POST        | Cursor-based sync download     |
| `/api/v1/attachments`   | POST        | File upload (multipart)        |
| `/api/v1/attachments/*` | GET, DELETE | Download, metadata, delete     |

### Health Check Routes

| Route Pattern   | Methods | Notes                             |
| --------------- | ------- | --------------------------------- |
| `/health/live`  | GET     | Liveness probe (always 200)       |
| `/health/ready` | GET     | Readiness probe (checks database) |

### Excluded Routes

The following routes are dev-only and must **not** be exposed through Kong in
production:

- `/scalar/v1` — Interactive API documentation
- `/openapi/v1.json` — OpenAPI specification

## Plugins

### Rate Limiting

Apply the `rate-limiting` plugin globally on DayKeeper routes.

| Setting        | Suggested Value | Notes                                   |
| -------------- | --------------- | --------------------------------------- |
| Minute limit   | 120             | Per IP, adjust based on usage data      |
| Hour limit     | 3600            | Per IP                                  |
| Policy         | `local`         | Use `redis` for multi-node Kong         |
| Fault tolerant | `true`          | Allow traffic if rate-limit store fails |

Health check routes (`/health/*`) should be excluded from rate limiting.

### CORS

Apply the `cors` plugin to match the current application-level CORS policy.

| Setting         | Value   | Notes                                    |
| --------------- | ------- | ---------------------------------------- |
| Origins         | `*`     | Restrict to known origins for production |
| Methods         | All     | GET, POST, PUT, PATCH, DELETE, OPTIONS   |
| Headers         | All     | `Authorization`, `Content-Type`, etc.    |
| Exposed headers | —       | Default                                  |
| Credentials     | `false` | Required when origins = `*`              |
| Max age         | `3600`  | Preflight cache in seconds               |

> **Production note**: Replace `origins: *` with explicit allowed origins
> (e.g., mobile app deep-link domains, admin dashboard URL) before going live.

### Request Size Limiting

Apply the `request-size-limiting` plugin to the attachment upload route.

| Setting                | Value       | Route                        |
| ---------------------- | ----------- | ---------------------------- |
| `allowed_payload_size` | `10`        | `/api/v1/attachments` (POST) |
| `size_unit`            | `megabytes` |                              |

This matches the application-level limit configured in `AttachmentsController`
(`[RequestSizeLimit(10 * 1024 * 1024)]`).

All other routes can use Kong's default body size limit (typically 8 KB or
higher depending on deployment).

### Request Termination (Optional)

In production, use the `request-termination` plugin to block dev-only routes:

| Route              | Status Code | Message   |
| ------------------ | ----------- | --------- |
| `/scalar/v1`       | 404         | Not found |
| `/openapi/v1.json` | 404         | Not found |

## Active Health Checks

Configure Kong upstream health checks to align with the Kubernetes probes
defined in `deploy/k8s/base/deployment.yaml`.

| Setting                   | Value           |
| ------------------------- | --------------- |
| Health check path         | `/health/live`  |
| Interval                  | `15` seconds    |
| Healthy threshold         | `1` success     |
| Unhealthy threshold       | `3` failures    |
| Timeout                   | `3` seconds     |
| HTTP statuses (healthy)   | `200`           |
| HTTP statuses (unhealthy) | `429, 500, 503` |

## Future Considerations

- **Authentication plugin** — When authentication moves from the application
  layer to the gateway, configure the `jwt` or `openid-connect` plugin with
  Firebase Auth token validation.
- **Logging** — Add `http-log` or `tcp-log` plugin for centralized request
  logging and observability.
- **IP Restriction** — If the gateway should only accept traffic from Cloudflare
  IPs, add the `ip-restriction` plugin with Cloudflare's published IP ranges.
- **Response transformation** — Add security headers (`X-Content-Type-Options`,
  `X-Frame-Options`, `Strict-Transport-Security`) if not already set by
  Cloudflare.
