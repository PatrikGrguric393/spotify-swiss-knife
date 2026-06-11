BASE=https://ssk.grghomelab.me

TOKEN=$(curl -s -X POST "$BASE/api/auth/token" \
  -H 'Content-Type: application/json' \
  -d '{"username":"admin@ssk.local","password":"Admin123!"}' | jq -r .access_token)
echo "$TOKEN"

curl -s "$BASE/api/artists" | jq
curl -s "$BASE/api/artists?q=radiohead" | jq

curl -s "$BASE/api/artists/THE_ID" | jq

curl -s -X PUT "$BASE/api/artists/THE_ID" \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d '{"name":"Radiohead (edited)","spotifyUrl":"https://open.spotify.com/artist/4Z8W4fKeB5YxbusRsdQVPb"}' | jq

curl -i -X DELETE "$BASE/api/artists/THE_ID" -H "Authorization: Bearer $TOKEN"

curl -s -X POST "$BASE/api/auth/refresh" -H 'Content-Type: application/json' \
  -d '{"refreshToken":"YOUR_REFRESH_TOKEN"}' | jq
curl -i -X POST "$BASE/api/auth/revoke" -H 'Content-Type: application/json' \
  -d '{"refreshToken":"YOUR_REFRESH_TOKEN"}'

