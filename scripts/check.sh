#!/usr/bin/env bash
# check.sh — Run SkillLedger quality gates
#
# Usage:
#   ./scripts/check.sh              # run all checks in parallel (backend + web)
#   ./scripts/check.sh backend      # backend only
#   ./scripts/check.sh web          # web (frontend) only

set -euo pipefail

REPO_ROOT="$(git rev-parse --show-toplevel)"
TRACK="${1:-all}"

TMPDIR_CHECK=$(mktemp -d)
trap 'rm -rf "$TMPDIR_CHECK"' EXIT

run_backend() {
  local log="$TMPDIR_CHECK/backend.log"
  {
    echo ""
    echo "══════════════════════════════════════════════"
    echo " Backend checks (.NET)"
    echo "══════════════════════════════════════════════"

    local failed=0

    echo ""
    echo "▶ Build (type check)..."
    (cd "$REPO_ROOT" && dotnet build --nologo -v minimal -warnaserror:false) \
      && echo "  ✓ Build passed" \
      || { echo "  ✗ Build FAILED"; failed=1; }

    echo ""
    echo "▶ Tests (unit + integration)..."
    (cd "$REPO_ROOT" && dotnet test --nologo --logger "console;verbosity=minimal" \
      --filter "Category!=E2E") \
      && echo "  ✓ Tests passed" \
      || { echo "  ✗ Tests FAILED"; failed=1; }

    return $failed
  } > "$log" 2>&1
}

run_web() {
  local log="$TMPDIR_CHECK/web.log"
  {
    echo ""
    echo "══════════════════════════════════════════════"
    echo " Web checks (Next.js)"
    echo "══════════════════════════════════════════════"

    local failed=0

    echo ""
    echo "▶ Lint..."
    (cd "$REPO_ROOT/web" && yarn lint) \
      && echo "  ✓ Lint passed" \
      || { echo "  ✗ Lint FAILED"; failed=1; }

    echo ""
    echo "▶ Type check..."
    (cd "$REPO_ROOT/web" && yarn typecheck) \
      && echo "  ✓ Type check passed" \
      || { echo "  ✗ Type check FAILED"; failed=1; }

    echo ""
    echo "▶ Tests..."
    (cd "$REPO_ROOT/web" && yarn test --watchAll=false --passWithNoTests --forceExit) \
      && echo "  ✓ Tests passed" \
      || { echo "  ✗ Tests FAILED"; failed=1; }

    echo ""
    echo "▶ Build..."
    (cd "$REPO_ROOT/web" && yarn build) \
      && echo "  ✓ Build passed" \
      || { echo "  ✗ Build FAILED"; failed=1; }

    return $failed
  } > "$log" 2>&1
}

case "$TRACK" in
  backend)
    run_backend
    cat "$TMPDIR_CHECK/backend.log"
    RESULT=$?
    ;;
  web)
    run_web
    cat "$TMPDIR_CHECK/web.log"
    RESULT=$?
    ;;
  all)
    run_backend &
    PID_BE=$!
    run_web &
    PID_WEB=$!

    RESULT=0
    wait "$PID_BE" || RESULT=1
    wait "$PID_WEB" || RESULT=1

    for track in backend web; do
      [ -f "$TMPDIR_CHECK/$track.log" ] && cat "$TMPDIR_CHECK/$track.log"
    done
    ;;
  *)
    echo "Unknown track: $TRACK"
    echo "Usage: $0 [backend|web|all]"
    exit 1
    ;;
esac

echo ""
if [[ $RESULT -eq 0 ]]; then
  echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
  echo " ✓ All checks passed — safe to commit"
  echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
  exit 0
else
  echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
  echo " ✗ Checks FAILED — fix issues before committing"
  echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
  exit 1
fi
