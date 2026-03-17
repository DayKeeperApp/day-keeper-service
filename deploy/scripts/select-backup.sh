#!/usr/bin/env bash
# select-backup.sh
#
# Interactive picker for database backup files stored in /var/backups/daykeeper/.
# Displays a numbered list of available backups (newest first) and prompts the
# user to select one. Prints the selected filename to stdout for use by the
# calling Taskfile task (deploy:db-restore).
#
# Usage: BACKUP_FILE=$(./deploy/scripts/select-backup.sh)
set -euo pipefail

BACKUP_DIR="/var/backups/daykeeper"

# ── Styling ──────────────────────────────────────────────────────
B="\033[1m"    # Bold
D="\033[2m"    # Dim
R="\033[0m"    # Reset
C="\033[36m"   # Cyan
G="\033[32m"   # Green
RED="\033[31m" # Red
HR="───────────────────────────────────────────────────────────────"

# Collect backup files sorted newest-first
mapfile -t BACKUPS < <(ls -t "$BACKUP_DIR"/daykeeper_*.dump 2>/dev/null)

if [ ${#BACKUPS[@]} -eq 0 ]; then
    echo -e "${RED}${B}No backups found in ${BACKUP_DIR}${R}" >&2
    exit 1
fi

echo "" >&2
echo -e "  ${D}${HR}${R}" >&2
echo -e "  ${B}${C}Database Backups${R}  ${D}(${#BACKUPS[@]} available)${R}" >&2
echo -e "  ${D}${HR}${R}" >&2
echo "" >&2

for i in "${!BACKUPS[@]}"; do
    FILE=$(basename "${BACKUPS[$i]}")
    SIZE=$(du -h "${BACKUPS[$i]}" | cut -f1)

    # Extract date parts from filename: daykeeper_YYYYMMDD_HHMMSS.dump
    FDATE="${FILE#daykeeper_}"
    FDATE="${FDATE%.dump}"
    FYEAR="${FDATE:0:4}"
    FMON="${FDATE:4:2}"
    FDAY="${FDATE:6:2}"
    FHOUR="${FDATE:9:2}"
    FMIN="${FDATE:11:2}"
    FSEC="${FDATE:13:2}"
    PRETTY_DATE="${FYEAR}-${FMON}-${FDAY} ${FHOUR}:${FMIN}:${FSEC}"

    NUM=$(printf "%2d" "$((i + 1))")

    if [ "$i" -eq 0 ]; then
        echo -e "    ${B}${G}${NUM})${R}  ${B}${FILE}${R}  ${D}│${R}  ${SIZE}  ${D}│${R}  ${PRETTY_DATE}  ${G}${B}(latest)${R}" >&2
    else
        echo -e "    ${D}${NUM})${R}  ${FILE}  ${D}│${R}  ${SIZE}  ${D}│${R}  ${PRETTY_DATE}" >&2
    fi
done

echo "" >&2
echo -e "  ${D}${HR}${R}" >&2

while true; do
    echo "" >&2
    printf "  ${B}Select backup${R} ${D}[1-%d]${R} ${D}(q to cancel):${R} " "${#BACKUPS[@]}" >&2
    read -r CHOICE
    if [ "$CHOICE" = "q" ] || [ "$CHOICE" = "Q" ]; then
        echo -e "  ${D}Cancelled.${R}" >&2
        exit 1
    fi
    if [[ "$CHOICE" =~ ^[0-9]+$ ]] && [ "$CHOICE" -ge 1 ] && [ "$CHOICE" -le ${#BACKUPS[@]} ]; then
        SELECTED=$(basename "${BACKUPS[$((CHOICE - 1))]}")
        echo -e "  ${G}${B}Selected:${R} ${SELECTED}" >&2
        echo "$SELECTED"
        exit 0
    fi
    echo -e "  ${RED}Invalid selection.${R}" >&2
done
