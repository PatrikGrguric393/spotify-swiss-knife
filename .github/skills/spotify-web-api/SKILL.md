---
name: spotify-web-api
description: "Use when working with Spotify Web API OpenAPI schema, generating or reviewing Spotify API clients, mapping endpoints, request/response models, OAuth scopes, pagination, search, library, playback, or handling deprecated and current Spotify resource shapes."
---

# Spotify Web API Schema Skill

Use the Spotify Web API OpenAPI schema as the source of truth for any Spotify API work. Prefer the schema at stored in the references/open-api-schema.yaml file over memory, examples from blogs, or inferred endpoint shapes.

## Use This Skill When

- Implementing or reviewing Spotify Web API clients, SDKs, adapters, or tests
- Translating Spotify endpoints into typed models or service methods
- Checking request parameters, request bodies, response payloads, and status codes
- Verifying OAuth scopes or playback-library permissions
- Handling pagination, relinking, union types, or deprecated Spotify fields
- Migrating from older Spotify endpoints to current replacements

## Working Rules

1. Start from the target resource family or tag group.
2. Prefer current endpoints and current response fields over deprecated ones.
3. Match request and response shapes exactly, including nullable fields and oneOf unions.
4. Treat Spotify IDs, URIs, href links, and external_urls as distinct concepts.
5. Preserve unknown enum-like strings and extensible reason fields instead of rejecting them.
6. Verify required OAuth scopes before suggesting or writing code.
7. Handle 401, 403, 404, 429, and 204 responses explicitly when the schema defines them.

## Resource Map

- Albums: album metadata, album tracks, and saved albums
- Artists: artist metadata, related artists, top tracks, and discography
- Audiobooks: audiobook metadata, chapters, and saved audiobooks
- Categories: browse categories and featured playlists
- Chapters: audiobook chapter metadata and paging
- Episodes: podcast episode metadata and saved episodes
- Genres: recommendation seed genres
- Library: saved-item APIs, contains checks, and follow/save migrations
- Markets: available markets
- Player: playback state, queue, devices, transfer, skip, seek, repeat, shuffle, and volume
- Playlists: playlist metadata, items, snapshots, and cover images
- Search: multi-type search results
- Shows: show metadata and show episodes
- Tracks: track metadata, audio features, audio analysis, and recommendations
- Users: profile, public profile, followers, and top items

## Schema Patterns To Preserve

- Full objects versus simplified objects are distinct models and should stay separate.
- Offset paging uses href, items, limit, next, offset, previous, and total.
- Cursor paging uses href, items, limit, next, cursors, and total.
- oneOf unions are common for track or episode, and sometimes artist or track.
- Common nested objects include external_urls, external_ids, images, followers, restrictions, resume_point, context, device, and copyrights.
- Date and time values are typically ISO 8601 strings, while playback timestamps may be integer milliseconds.
- release_date must be paired with release_date_precision where present.
- Many response fields can be null, omitted, or deprecated but still returned.
- Playlist and playback flows often use polymorphic item shapes that must respect the type discriminator.

## Deprecation Guidance

- Prefer /me/library and /playlists/{playlist_id}/items over older save, follow, or playlist-tracks endpoints when the schema provides both.
- Prefer items over deprecated playlist tracks fields.
- Keep deprecated fields in parsers so existing Spotify responses still deserialize cleanly.
- Treat deprecated popularity, genre, and preview fields as legacy compatibility fields rather than primary data sources.

## OAuth Scope Hints

- Playback control: user-modify-playback-state, user-read-playback-state, user-read-currently-playing, user-read-recently-played
- Library access: user-library-read, user-library-modify
- Follow access: user-follow-read, user-follow-modify
- Profile access: user-read-private, user-read-email
- Playlist access: playlist-read-private, playlist-read-collaborative, playlist-modify-public, playlist-modify-private
- User playback position: user-read-playback-position
- Playlist cover upload: ugc-image-upload
- Premium-only playback endpoints should still be treated as Premium-gated even if that constraint is only described in endpoint docs.

## Endpoint Selection Guidance

When asked to build a feature, choose the endpoint family that matches the resource and use the current operationId and response shape from the schema.

- Use current library endpoints for save, remove, and contains workflows.
- Use playlist items endpoints for reading or mutating playlist contents.
- Use current player endpoints for playback state, queue, transport, and device control.
- Use search for broad discovery across albums, artists, playlists, tracks, shows, episodes, and audiobooks.
- Use recommendations only when seed-based generation is explicitly requested.

## Response-Handling Checklist

- Validate error payloads using status and message.
- Return or map 204 responses without expecting a body.
- Follow next or cursors.after until the collection is exhausted.
- Treat null items in playlist, search, and library results as valid and expected.
- Check available_markets, is_playable, restrictions, and linked_from before surfacing playability.
- Preserve unknown reason values in restriction objects.
- Account for market relinking when a user asks for playable content in a specific country.

## If You Are Writing Code

- Generate typed models from the schema when possible.
- Keep separate models for simplified and full objects.
- Model unions explicitly instead of flattening them into generic maps.
- Add tests for paging, null handling, deprecation fallbacks, and scope failures.
- Localize schema mapping in a small adapter layer so future endpoint changes are easy to absorb.

## Practical Workflow

1. Identify the endpoint, tag, and operationId.
2. Read the parameters, request body, security section, and response codes.
3. Check whether the schema marks any fields or endpoints as deprecated.
4. Map the data model precisely, including optional, nullable, and union fields.
5. Add handling for authorization, pagination, rate limits, and content availability.
6. Verify the result against the schema before considering the task complete.

## Default Answer Style For Spotify Tasks

When answering a Spotify API task, be explicit about:

- the endpoint chosen
- the required scopes
- the response type
- any deprecated fields or replacement endpoints
- any assumptions about market, pagination, or Premium playback
