# Cloudflare Tunnel Requirements — DayKeeper Service

## Overview

DayKeeper is self-hosted on a local k3d Kubernetes cluster.
The Android app needs public internet access to reach the
backend. A **Cloudflare Tunnel** provides this without
exposing the host network or opening inbound firewall ports.

The tunnel runs `cloudflared` inside the cluster, which establishes an outbound-only
connection to Cloudflare's edge. Cloudflare then proxies public requests through
the tunnel to the backend service.

## Public Hostname

```text
api.daykeeper.example.com
```

Replace with the actual domain registered in Cloudflare DNS.

## Routes to Expose

| Route           | Purpose                       | Notes                        |
| --------------- | ----------------------------- | ---------------------------- |
| `/graphql`      | GraphQL API (Android app)     | Primary interactive endpoint |
| `/api/*`        | REST endpoints                | Includes `/api/helloworld`   |
| `/sync/push`    | Offline-first sync (upload)   | Planned                      |
| `/sync/pull`    | Offline-first sync (download) | Planned                      |
| `/health/live`  | Liveness probe                | Cloudflare health monitoring |
| `/health/ready` | Readiness probe               | Cloudflare health monitoring |

### Excluded Routes

| Route              | Reason                                       |
| ------------------ | -------------------------------------------- |
| `/scalar/v1`       | Interactive API docs — dev-only, not in prod |
| `/openapi/v1.json` | OpenAPI spec — dev-only, not in prod         |

These routes are gated behind `IsDevelopment()` in
`Program.cs` and are not served in the production container,
so no tunnel-level blocking is needed.

## TLS Termination

```text
Client ──HTTPS──▶ Cloudflare Edge
  ──HTTP──▶ cloudflared ──HTTP──▶ Service (port 8080)
```

- **Public side:** Cloudflare terminates TLS. Use Full (strict) mode if an origin
  certificate is configured, or Full mode with Cloudflare's default.
- **Internal side:** The tunnel connects to the backend over
  plain HTTP on port 8080. The cluster network is trusted
  (single-node k3d on encrypted host storage).
- **No origin TLS certificate is required** for the initial setup since `cloudflared`
  uses an outbound tunnel (not a traditional reverse proxy with inbound TLS).

## Tunnel → Service Connectivity

### Phase 1: Direct to API (current)

Kong is not yet deployed. The tunnel points directly to the API service:

```text
cloudflared ──▶ daykeeper-api.daykeeper.svc:8080
```

Tunnel ingress rule:

```yaml
ingress:
  - hostname: api.daykeeper.example.com
    service: http://daykeeper-api.daykeeper.svc.cluster.local:8080
  - service: http_status:404
```

### Phase 2: Through Kong Gateway (future)

When Kong is deployed, the tunnel target changes to Kong's proxy port:

```text
cloudflared ──▶ kong-proxy.daykeeper.svc:80
```

Kong then routes to `daykeeper-api` based on path matching. The tunnel config
simplifies to a single upstream; routing logic moves to Kong.

## Header Requirements

Cloudflare injects headers that the application should trust:

| Header              | Value                             |
| ------------------- | --------------------------------- |
| `CF-Connecting-IP`  | Original client IP                |
| `X-Forwarded-For`   | Client IP chain                   |
| `X-Forwarded-Proto` | `https` (original protocol)       |
| `CF-Ray`            | Cloudflare request ID (debugging) |

**Action required:** Configure ASP.NET Core `ForwardedHeaders` middleware to trust
these headers so `HttpContext.Connection.RemoteIpAddress` reflects the real client:

```csharp
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});
```

This is a code change for a future task — not part of this document's scope.

## Network Policy Implications

The existing network policies in `deploy/k8s/base/networkpolicy.yaml` are
**already compatible** with a Cloudflare Tunnel deployment:

| Rule                        | Status | Why                                              |
| --------------------------- | ------ | ------------------------------------------------ |
| Ingress on port 8080 (TCP)  | ✅     | `cloudflared` connects to API on this port       |
| Egress on port 443 (TCP)    | ✅     | `cloudflared` needs outbound HTTPS to Cloudflare |
| Egress on port 53 (UDP/TCP) | ✅     | DNS resolution for Cloudflare endpoints          |

If `cloudflared` runs as a **separate deployment**
(not a sidecar), it needs its own network policy allowing:

- Egress to `0.0.0.0/0` on port 443 (Cloudflare edge)
- Egress to `daykeeper-api` on port 8080
- Egress DNS (port 53)

## Service Type Change

The current `daykeeper-api` service is type `LoadBalancer`:

```yaml
# deploy/k8s/base/service.yaml
spec:
  type: LoadBalancer
```

Once the tunnel is the sole entry point, change to `ClusterIP` to avoid exposing
the service directly on the host network:

```yaml
spec:
  type: ClusterIP
```

This is a manifest change for a future task.

## Deployment Notes

### cloudflared Deployment Options

| Option              | Pros                            | Cons                              |
| ------------------- | ------------------------------- | --------------------------------- |
| Separate Deployment | Independent scaling, clear RBAC | Extra manifest, network policy    |
| Sidecar in API pod  | Shares network namespace        | Coupled lifecycle, resource waste |

**Recommendation:** Separate Deployment — keeps concerns isolated and allows
independent restarts.

### Tunnel Credential

`cloudflared` authenticates with a tunnel token
(or credentials file). Store it as a Kubernetes Secret:

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: cloudflare-tunnel-token
  namespace: daykeeper
type: Opaque
stringData:
  token: "<tunnel-token-from-cloudflare-dashboard>"
```

Generate the token via:

```bash
cloudflared tunnel create daykeeper
```

### Resource Estimates

`cloudflared` is lightweight:

- CPU: 50m request / 200m limit
- Memory: 64Mi request / 128Mi limit

### Health Check Configuration

Configure Cloudflare to monitor tunnel health using:

- **Health check URL:** `/health/ready`
- **Expected status:** 200
- **Interval:** 60 seconds
