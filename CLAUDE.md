# Project Instructions

## UI/UX

- For any request involving UI or UX changes, always invoke the `uiux` subagent before making edits.
- This includes: CSS styling, JS code, layout changes, responsive behavior, visual polish, HTML structure, Razor view UI, UI/UX decisions, and any other UI-related tasks.
- Do not implement UI changes directly in the main agent unless the user explicitly says to skip the `uiux` subagent.
- After the `uiux` subagent returns, apply the recommended edits and keep changes scoped to the UI layer unless asked otherwise.
- In the final response for UI tasks, state that the `uiux` subagent was used.
- If the `uiux` subagent is unavailable, explain that clearly and proceed with a minimal fallback implementation.

## Spotify API usage

- For any request involving Spotify API endpoints, requests, responses, OAuth scopes and flows, or any other Spotify API-related tasks, use the `spotify-web-api` skill as the authoritative source of truth.
- Do not rely on memory, examples from blogs, or inferred endpoint shapes for Spotify API work.
- Always verify request parameters, request bodies, response payloads, status codes, and OAuth scopes against the `spotify-web-api` skill before suggesting or writing code.

## Spotify API testing

- Spotify API functionality cannot be tested locally.
- Do not attempt to run, simulate, or verify Spotify API calls as part of task completion.
- All testing of Spotify API related functionality must be offloaded to the developer.
