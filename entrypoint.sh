#!/bin/sh
set -e

# Build connection string from individual DB_* vars when the full string isn't provided.
if [ -z "${ConnectionStrings__SpotifyDbContext:-}" ] \
    && [ -n "${DB_HOST:-}" ] \
    && [ -n "${DB_USER:-}" ] \
    && [ -n "${DB_PASSWORD:-}" ]; then
    export ConnectionStrings__SpotifyDbContext="\
Host=${DB_HOST};\
Port=${DB_PORT:-5432};\
Database=${DB_NAME:-ssk};\
Username=${DB_USER};\
Password=${DB_PASSWORD};\
SSL Mode=${DB_SSL_MODE:-Require};\
Trust Server Certificate=${DB_TRUST_SERVER_CERTIFICATE:-true}"
fi

# Map flat SPOTIFY_* vars to the ASP.NET Core config hierarchy.
[ -n "${SPOTIFY_CLIENT_ID:-}" ]     && export Spotify__ClientId="${SPOTIFY_CLIENT_ID}"
[ -n "${SPOTIFY_CLIENT_SECRET:-}" ] && export Spotify__ClientSecret="${SPOTIFY_CLIENT_SECRET}"
[ -n "${SPOTIFY_REDIRECT_URI:-}" ]  && export Spotify__RedirectUri="${SPOTIFY_REDIRECT_URI}"

exec dotnet spotify-swiss-knife.dll
