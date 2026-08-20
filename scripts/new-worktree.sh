#!/usr/bin/env bash
# new-worktree.sh — Create a fully-wired SkillLedger worktree
#
# Usage:
#   ./scripts/new-worktree.sh <branch-name>
#   ./scripts/new-worktree.sh feat/my-feature
#   ./scripts/new-worktree.sh fix/credit-transfer
#
# Creates .worktrees/<slug>/ with:
#   - Isolated git worktree on <branch-name>
#   - Copied .env files for all sub-projects
#   - dotnet restore for backend
#   - yarn install for web
#   - git hooks wired up

set -euo pipefail

REPO_ROOT="$(git rev-parse --show-toplevel)"
WORKTREES_DIR="$REPO_ROOT/.worktrees"

# ── Validate argument ────────────────────────────────────────────────────────
if [[ $# -lt 1 ]]; then
  echo "Usage: $0 <branch-name>"
  echo "  e.g. $0 feat/my-feature"
  exit 1
fi

BRANCH="$1"

if ! git check-ref-format --branch "$BRANCH" &>/dev/null; then
  echo "Error: '$BRANCH' is not a valid git branch name."
  exit 1
fi

# Convert branch name to safe directory slug (replace / and . with -)
SLUG="${BRANCH//\//-}"
SLUG="${SLUG//./-}"
WORKTREE_PATH="$WORKTREES_DIR/$SLUG"

if [[ -d "$WORKTREE_PATH" ]]; then
  echo "Error: Worktree '$WORKTREE_PATH' already exists."
  exit 1
fi

echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo " Creating worktree: $SLUG"
echo " Branch:            $BRANCH"
echo " Path:              $WORKTREE_PATH"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

mkdir -p "$WORKTREES_DIR"

# ── Create worktree ──────────────────────────────────────────────────────────
if git show-ref --verify --quiet "refs/heads/$BRANCH"; then
  echo ""
  echo "▶ Branch '$BRANCH' exists — checking it out in new worktree..."
  git worktree add "$WORKTREE_PATH" "$BRANCH"
else
  echo ""
  echo "▶ Creating new branch '$BRANCH' from main..."
  git worktree add "$WORKTREE_PATH" -b "$BRANCH" main
fi

# ── Copy .env files ──────────────────────────────────────────────────────────
echo ""
echo "▶ Copying .env files..."

copy_env() {
  local src_dir="$1"
  local dst_dir="$2"
  for f in "$src_dir"/.env*; do
    [[ -f "$f" ]] || continue
    filename="$(basename "$f")"
    if [[ "$filename" == *.example || "$filename" == *.template ]]; then
      continue  # skip templates — they're in source control
    fi
    dest="$dst_dir/$filename"
    if [[ ! -f "$dest" ]]; then
      cp "$f" "$dest"
      echo "  copied $filename → $dst_dir/$filename"
    fi
  done
}

copy_env "$REPO_ROOT"                               "$WORKTREE_PATH"
copy_env "$REPO_ROOT/web"                            "$WORKTREE_PATH/web"
copy_env "$REPO_ROOT/src/SkillLedger.Api"            "$WORKTREE_PATH/src/SkillLedger.Api"

# ── Backend — dotnet restore ─────────────────────────────────────────────────
echo ""
echo "▶ Restoring .NET dependencies..."
(cd "$WORKTREE_PATH" && dotnet restore --nologo -v minimal) \
  && echo "  dotnet restore OK" \
  || echo "  ⚠ dotnet restore failed — run manually: dotnet restore"

# ── Frontend — yarn install ───────────────────────────────────────────────────
echo ""
echo "▶ Installing web yarn packages..."
(cd "$WORKTREE_PATH/web" && yarn install --frozen-lockfile) \
  && echo "  web yarn install OK" \
  || echo "  ⚠ web yarn install failed — run manually: cd web && yarn install"

# ── Wire git hooks ───────────────────────────────────────────────────────────
echo ""
echo "▶ Configuring git hooks..."
(cd "$WORKTREE_PATH" && git config core.hooksPath "$REPO_ROOT/.githooks")
echo "  hooksPath → $REPO_ROOT/.githooks"

# ── Done ─────────────────────────────────────────────────────────────────────
echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo " ✓ Worktree ready at: $WORKTREE_PATH"
echo ""
echo " Next steps:"
echo "   cd $WORKTREE_PATH"
echo "   # ... do your work ..."
echo "   ./scripts/check.sh          # run quality gates"
echo "   git add <files> && git commit -m 'type(scope): description'"
echo ""
echo " When done:"
echo "   git worktree remove $WORKTREE_PATH"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
