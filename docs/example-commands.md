# Example API commands

Copy/paste `curl` snippets for the local-library CRUD API. They assume `bash`, `curl`, and `jq`.

`GET` endpoints are anonymous. Writes (`POST`/`PUT`) require an **Admin** or **Editor** token; `DELETE` requires **Admin**. Replace `THE_ID` with a real id (the `GET` lists return ids).

For a runnable end-to-end walk through the full CRUD lifecycle and the authorization rules, see [`example-crud.sh`](example-crud.sh):

```bash
# Defaults target https://ssk.grghomelab.me; override via env vars.
BASE=https://ssk.grghomelab.me ./docs/example-crud.sh
```

## Setup

```bash
BASE=https://ssk.grghomelab.me

# Authenticate and capture a bearer token for the write/delete calls below.
# Admin can do everything (including DELETE).
TOKEN=$(curl -s -X POST "$BASE/api/auth/token" \
  -H 'Content-Type: application/json' \
  -d '{"username":"admin@ssk.grghomelab.me","password":"Password1admin"}' | jq -r .access_token)
echo "$TOKEN"

# Optional: an Editor token can create/update (POST/PUT) but cannot DELETE (-> 403).
# TOKEN=$(curl -s -X POST "$BASE/api/auth/token" \
#   -H 'Content-Type: application/json' \
#   -d '{"username":"editor@ssk.grghomelab.me","password":"Password1editor"}' | jq -r .access_token)
```

## Artists

```bash
# Read (anonymous)
curl -s "$BASE/api/artists" | jq                       # list all
curl -s "$BASE/api/artists?q=radiohead" | jq           # search by name, or pass an id for an exact match
curl -s "$BASE/api/artists/THE_ID" | jq                # single artist
curl -s "$BASE/api/artists/THE_ID?includeDeleted=true" | jq   # include soft-deleted

# Create (Admin/Editor) -> 201 Created
curl -s -X POST "$BASE/api/artists" \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d '{"name":"Radiohead","spotifyUrl":"https://open.spotify.com/artist/4Z8W4fKeB5YxbusRsdQVPb"}' | jq

# Update (Admin/Editor)
curl -s -X PUT "$BASE/api/artists/THE_ID" \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d '{"name":"Radiohead (edited)","spotifyUrl":"https://open.spotify.com/artist/4Z8W4fKeB5YxbusRsdQVPb"}' | jq

# Delete (Admin) -> 204 No Content (soft-delete; row is flagged, not removed)
curl -i -X DELETE "$BASE/api/artists/THE_ID" -H "Authorization: Bearer $TOKEN"
```

## Albums

```bash
# Read (anonymous)
curl -s "$BASE/api/albums" | jq                        # list all
curl -s "$BASE/api/albums?q=kid+a" | jq                # search by name, or pass an id for an exact match
curl -s "$BASE/api/albums/THE_ID" | jq                 # single album (includes its tracks)

# Create (Admin/Editor) -> 201 Created
# artistIds/trackIds are optional; any id that doesn't exist -> 404.
curl -s -X POST "$BASE/api/albums" \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d '{"name":"Kid A","albumType":"album","releaseDate":"2000-10-02","label":"Parlophone","popularity":75,"spotifyUrl":"https://open.spotify.com/album/6GjwtEZcfenmOf6l18N7T7","artistIds":[],"trackIds":[]}' | jq

# Update (Admin/Editor)
curl -s -X PUT "$BASE/api/albums/THE_ID" \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d '{"name":"Kid A (remaster)","albumType":"album","releaseDate":"2000-10-02","label":"XL","popularity":80,"artistIds":[],"trackIds":[]}' | jq

# Delete (Admin) -> 204 No Content
curl -i -X DELETE "$BASE/api/albums/THE_ID" -H "Authorization: Bearer $TOKEN"
```

## Tracks

```bash
# Read (anonymous)
curl -s "$BASE/api/tracks" | jq                        # list all
curl -s "$BASE/api/tracks?q=idioteque" | jq            # search by name, or pass an id for an exact match
curl -s "$BASE/api/tracks/THE_ID" | jq                 # single track

# Create (Admin/Editor) -> 201 Created
# albumId is optional; a non-existent albumId or artistId -> 404.
curl -s -X POST "$BASE/api/tracks" \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d '{"name":"Idioteque","durationMs":230000,"discNumber":1,"trackNumber":8,"isLocal":false,"spotifyUrl":"https://open.spotify.com/track/3SVAN3Bodjm7Lpv6jABax9","albumId":null,"artistIds":[]}' | jq

# Update (Admin/Editor)
curl -s -X PUT "$BASE/api/tracks/THE_ID" \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d '{"name":"Idioteque (live)","durationMs":245000,"discNumber":1,"trackNumber":8,"isLocal":false,"albumId":null,"artistIds":[]}' | jq

# Delete (Admin) -> 204 No Content
curl -i -X DELETE "$BASE/api/tracks/THE_ID" -H "Authorization: Bearer $TOKEN"
```

## Playlists

```bash
# Read (anonymous)
curl -s "$BASE/api/playlists" | jq                     # list all
curl -s "$BASE/api/playlists?q=workout" | jq           # search by name, or pass an id for an exact match
curl -s "$BASE/api/playlists/THE_ID" | jq              # single playlist (tracks returned in order)

# Create (Admin/Editor) -> 201 Created
# trackIds is optional and defines track order.
curl -s -X POST "$BASE/api/playlists" \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d '{"name":"Workout Mix","description":"High-energy set","ownerDisplayName":"pg","spotifyUrl":"https://open.spotify.com/playlist/37i9dQZF1DX76Wlfdnj7AP","trackIds":[]}' | jq

# Update (Admin/Editor)
curl -s -X PUT "$BASE/api/playlists/THE_ID" \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d '{"name":"Workout Mix v2","description":"Updated set","ownerDisplayName":"pg","trackIds":[]}' | jq

# Delete (Admin) -> 204 No Content
curl -i -X DELETE "$BASE/api/playlists/THE_ID" -H "Authorization: Bearer $TOKEN"
```

## Auth helpers

```bash
# Exchange a refresh token for a new access token.
curl -s -X POST "$BASE/api/auth/refresh" -H 'Content-Type: application/json' \
  -d '{"refreshToken":"YOUR_REFRESH_TOKEN"}' | jq

# Revoke a refresh token.
curl -i -X POST "$BASE/api/auth/revoke" -H 'Content-Type: application/json' \
  -d '{"refreshToken":"YOUR_REFRESH_TOKEN"}'
```
