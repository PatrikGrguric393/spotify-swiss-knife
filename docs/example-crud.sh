#!/usr/bin/env bash
#
# End-to-end example that exercises the local-library CRUD API against a running
# instance. It walks one resource (artists) through its full lifecycle and then
# checks the authorization rules, covering every scenario the API tests assert:
#
#   1. GET all            -> 200 + collection
#   2. GET by id (exists) -> 200 + record
#   3. GET by id (missing)-> 404
#   4. POST (valid)       -> 201 Created
#   5. POST (invalid)     -> 400 Bad Request
#   6. PUT (exists)       -> 200 + updated record
#   7. PUT (missing)      -> 404
#   8. DELETE (exists)    -> 204 No Content
#   9. DELETE (missing)   -> 404
#  10. Authorization      -> 401 (no token), 403 (insufficient role)
#
# The same request shapes apply to /api/albums, /api/tracks and /api/playlists;
# see docs/example-commands.md for their payloads.
#
# Requires: bash, curl, jq. Override any setting via the environment, e.g.
#   BASE=http://localhost:5000 ./docs/example-crud.sh

set -uo pipefail

BASE="${BASE:-https://ssk.grghomelab.me}"
ADMIN_USER="${ADMIN_USER:-admin@ssk.grghomelab.me}"
ADMIN_PASS="${ADMIN_PASS:-Password1admin}"
EDITOR_USER="${EDITOR_USER:-editor@ssk.grghomelab.me}"
EDITOR_PASS="${EDITOR_PASS:-Password1editor}"

ART="/api/artists"
pass=0
fail=0

# Performs a request and sets the globals STATUS (HTTP code) and BODY (response body).
#   api METHOD PATH [TOKEN] [JSON_BODY]
api() {
  local method=$1 path=$2 token=${3:-} body=${4:-}
  local args=(-sS -X "$method" "$BASE$path" -H 'Accept: application/json' -w '\n%{http_code}')
  [[ -n $token ]] && args+=(-H "Authorization: Bearer $token")
  [[ -n $body ]] && args+=(-H 'Content-Type: application/json' -d "$body")
  local out
  out=$(curl "${args[@]}")
  STATUS=${out##*$'\n'}
  BODY=${out%$'\n'*}
}

# Compares the last STATUS against an expected code.
#   expect EXPECTED_STATUS "label"
expect() {
  if [[ $STATUS == "$1" ]]; then
    printf '  \033[32m✓\033[0m %s -> %s\n' "$2" "$STATUS"
    pass=$((pass + 1))
  else
    printf '  \033[31m✗\033[0m %s -> got %s, expected %s\n' "$2" "$STATUS" "$1"
    [[ -n $BODY ]] && printf '      %s\n' "$BODY"
    fail=$((fail + 1))
  fi
}

# Logs in through /api/auth/token and echoes the bearer access token.
#   get_token USER PASS
get_token() {
  curl -sS -X POST "$BASE/api/auth/token" \
    -H 'Content-Type: application/json' \
    -d "{\"username\":\"$1\",\"password\":\"$2\"}" | jq -r '.access_token // empty'
}

echo "Base: $BASE"

# --- Authenticate -----------------------------------------------------------
ADMIN_TOKEN=$(get_token "$ADMIN_USER" "$ADMIN_PASS")
EDITOR_TOKEN=$(get_token "$EDITOR_USER" "$EDITOR_PASS")
if [[ -z $ADMIN_TOKEN || -z $EDITOR_TOKEN ]]; then
  echo "Could not obtain tokens — check credentials and that $BASE is reachable." >&2
  exit 1
fi

MISSING_ID="00000000-0000-0000-0000-000000000000"
NAME="Example Artist $$"   # $$ keeps the name unique per run so re-runs don't 422

echo
echo "== Read =="
# 1. GET all -> 200 + collection
api GET "$ART"
expect 200 "GET $ART (list all)"
[[ $STATUS == 200 ]] && echo "      $(echo "$BODY" | jq 'length') artist(s) in library"

# 3. GET by id (missing) -> 404
api GET "$ART/$MISSING_ID"
expect 404 "GET $ART/<missing>"

echo
echo "== Create =="
# 5. POST invalid (no name) -> 400
api POST "$ART" "$ADMIN_TOKEN" '{"spotifyUrl":null}'
expect 400 "POST $ART (missing name)"

# 4. POST valid -> 201 Created
api POST "$ART" "$ADMIN_TOKEN" "{\"name\":\"$NAME\",\"spotifyUrl\":null}"
expect 201 "POST $ART (valid)"
ID=$(echo "$BODY" | jq -r '.id')
echo "      created id: $ID"

# 2. GET by id (exists) -> 200 + record
api GET "$ART/$ID"
expect 200 "GET $ART/$ID"

echo
echo "== Update =="
# 6. PUT existing -> 200 + updated record
api PUT "$ART/$ID" "$ADMIN_TOKEN" "{\"name\":\"$NAME (edited)\",\"spotifyUrl\":null}"
expect 200 "PUT $ART/$ID"

# 7. PUT missing -> 404
api PUT "$ART/$MISSING_ID" "$ADMIN_TOKEN" '{"name":"Ghost","spotifyUrl":null}'
expect 404 "PUT $ART/<missing>"

echo
echo "== Authorization =="
# 10a. Write without a token -> 401
api POST "$ART" "" "{\"name\":\"Nope\",\"spotifyUrl\":null}"
expect 401 "POST $ART (no token)"

# 10b. DELETE as Editor -> 403 (DELETE is Admin-only; Editor may only POST/PUT)
api DELETE "$ART/$ID" "$EDITOR_TOKEN"
expect 403 "DELETE $ART/$ID (editor)"

echo
echo "== Delete =="
# 8. DELETE existing as Admin -> 204
api DELETE "$ART/$ID" "$ADMIN_TOKEN"
expect 204 "DELETE $ART/$ID (admin)"

# 9. DELETE missing -> 404
api DELETE "$ART/$MISSING_ID" "$ADMIN_TOKEN"
expect 404 "DELETE $ART/<missing>"

echo
echo "Passed: $pass  Failed: $fail"
[[ $fail -eq 0 ]]
