# Workspace Copilot Instructions

## UI/UX

- For any request involving UI or UX changes, always invoke the UI/UX subagent before making edits.
- This includes: CSS styling, JS code, layout changes, responsive behavior, visual polish, HTML structure, Razor view UI, UI/UX decisions, and any other UI-related tasks.
- Do not implement UI changes directly in the default agent unless the user explicitly says to skip the UI/UX subagent.
- After the UI/UX subagent returns, apply the recommended edits and keep changes scoped to the UI layer unless asked otherwise.
- In the final response for UI tasks, state that the UI/UX subagent was used.
- If the UI/UX subagent is unavailable, explain that clearly and proceed with a minimal fallback implementation.

## Spotify API usage

- For any request involving Spotify API endpoints, requests, responses, OAuth scopes and flows, or any other Spotify API-related tasks, use the spotify-web-api skill as the authoritative source of truth.
- Do not rely on memory, examples from blogs, or inferred endpoint shapes for Spotify API work.
- Always verify request parameters, request bodies, response payloads, status codes, and OAuth scopes against the spotify-web-api skill before suggesting or writing code.