#!/usr/bin/env bash
# Walks through the Todo API: registration, bearer login, per-user isolation and validation.
# Start the API first with: dotnet run --project TodoApi
set -euo pipefail

BASE_URL="${BASE_URL:-http://localhost:5080}"
PASSWORD='Passw0rd!'
RUN_ID="$(date +%s)"
BODY=''

step() { printf '\n\033[1;34m== %s\033[0m\n' "$*"; }

# api EXPECTED_STATUS METHOD PATH [TOKEN] [JSON_BODY]
# Prints the exchange, stores the response body in $BODY and fails on an unexpected status.
api() {
  local expected=$1 method=$2 path=$3 token=${4:-} body=${5:-}
  local args=(-s -X "$method" -w '\n%{http_code}' "$BASE_URL$path")
  if [[ -n $token ]]; then args+=(-H "Authorization: Bearer $token"); fi
  if [[ -n $body ]]; then args+=(-H 'Content-Type: application/json' -d "$body"); fi

  local response status
  response=$(curl "${args[@]}")
  status=${response##*$'\n'}
  BODY=${response%"$status"}
  BODY=${BODY%$'\n'}

  printf '%s %s -> %s\n' "$method" "$path" "$status"
  if [[ -n $BODY ]]; then jq . <<<"$BODY" 2>/dev/null || echo "$BODY"; fi
  if [[ $status != "$expected" ]]; then
    echo "Expected HTTP $expected but got $status" >&2
    exit 1
  fi
}

credentials() { jq -nc --arg email "$1" --arg password "$PASSWORD" '{$email, $password}'; }

register_and_login() {
  api 200 POST /auth/register '' "$(credentials "$1")"
  api 200 POST /auth/login '' "$(credentials "$1")"
}

step "Register and log in alice and bob (bearer tokens, no cookies)"
register_and_login "alice-$RUN_ID@example.com"
ALICE_TOKEN=$(jq -r .accessToken <<<"$BODY")
ALICE_REFRESH=$(jq -r .refreshToken <<<"$BODY")
register_and_login "bob-$RUN_ID@example.com"
BOB_TOKEN=$(jq -r .accessToken <<<"$BODY")

step "Anonymous requests are rejected"
api 401 GET /todos

step "Alice creates todos"
api 201 POST /todos "$ALICE_TOKEN" '{"title":"Buy milk","dueDate":"2099-12-24"}'
MILK_ID=$(jq -r .id <<<"$BODY")
api 201 POST /todos "$ALICE_TOKEN" '{"title":"Write report","isComplete":false}'
REPORT_ID=$(jq -r .id <<<"$BODY")

step "Invalid payloads return a validation problem"
api 400 POST /todos "$ALICE_TOKEN" '{"title":""}'
api 400 POST /todos "$ALICE_TOKEN" "$(jq -nc '{title: ("x" * 201)}')"
api 400 POST /todos "$ALICE_TOKEN" '{"title":"Pay rent","dueDate":"2020-01-01"}'

step "Each user only sees their own todos"
api 200 GET /todos "$ALICE_TOKEN"
api 200 GET /todos "$BOB_TOKEN"

step "Bob cannot read, change or delete alice's todo"
api 404 GET "/todos/$MILK_ID" "$BOB_TOKEN"
api 404 PUT "/todos/$MILK_ID" "$BOB_TOKEN" '{"title":"Hijacked","isComplete":true}'
api 404 DELETE "/todos/$MILK_ID" "$BOB_TOKEN"

step "Alice updates and deletes her todos"
api 204 PUT "/todos/$MILK_ID" "$ALICE_TOKEN" '{"title":"Buy oat milk","isComplete":true}'
api 200 GET "/todos/$MILK_ID" "$ALICE_TOKEN"
api 204 DELETE "/todos/$REPORT_ID" "$ALICE_TOKEN"
api 200 GET /todos "$ALICE_TOKEN"

step "Refresh the access token and query the account"
api 200 POST /auth/refresh '' "$(jq -nc --arg refreshToken "$ALICE_REFRESH" '{$refreshToken}')"
ALICE_TOKEN=$(jq -r .accessToken <<<"$BODY")
api 200 GET /auth/manage/info "$ALICE_TOKEN"

step "All checks passed"
