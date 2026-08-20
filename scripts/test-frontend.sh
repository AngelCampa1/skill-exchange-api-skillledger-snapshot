#!/bin/bash
# Simple wrapper to run all frontend (Next.js) tests

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
exec "$SCRIPT_DIR/run-tests.sh" --frontend "$@"