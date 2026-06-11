## MVC Routes

### Core

| URL      | Controller         | Akcija                            | View(s)                                       |
| -------- | ------------------ | --------------------------------- | --------------------------------------------- |
| /        | HomeController     | Index                             | Shared/_Layout.cshtml, Home/Index.cshtml      |
| /about   | AboutController    | Index                             | Shared/_Layout.cshtml, About/Index.cshtml     |
| /login   | LoginController    | Index                             | Shared/_Layout.cshtml, Login/Index.cshtml     |
| /shuffle | ShuffleController  | GET -> Index, POST -> Index(...)  | Shared/_Layout.cshtml, Shuffle/Index.cshtml   |
| /search  | SearchController   | GET -> Index(q)                   | — (JSON)                                       |

### Local account — `AccountController` (`/account`)

| URL                       | Controller        | Akcija                                          | View(s)                                           |
| ------------------------- | ----------------- | ----------------------------------------------- | ------------------------------------------------- |
| /account/register         | AccountController | GET -> Register, POST -> Register(...)          | Shared/_Layout.cshtml, Account/Register.cshtml    |
| /account/login            | AccountController | GET -> Login, POST -> Login(...)                | Shared/_Layout.cshtml, Account/Login.cshtml       |
| /account/logout           | AccountController | POST -> Logout                                  | — (redirect to /)                                 |
| /account/denied           | AccountController | Denied                                          | Shared/_Layout.cshtml, Account/Denied.cshtml      |
| /account/users            | AccountController | Users `[Admin]`                                 | Shared/_Layout.cshtml, Account/Users.cshtml       |
| /account/users/{id}/edit  | AccountController | GET -> EditUser, POST -> EditUser(...) `[Admin]` | Shared/_Layout.cshtml, Account/EditUser.cshtml    |
| /account/users/{id}/delete| AccountController | POST -> DeleteUser `[Admin]`                    | — (redirect to /account/users)                    |

> Role assignment is performed through `POST /account/users/{id}/edit` (the user edit form), not a separate endpoint.

### Spotify OAuth — `SpotifyAuthController` (`/auth`)

| URL           | Controller           | Akcija                               | View(s)                                          |
| ------------- | -------------------- | ------------------------------------ | ------------------------------------------------ |
| /auth/login   | SpotifyAuthController | Login                                | — (redirect to Spotify authorization URL)        |
| /auth/callback| SpotifyAuthController | Callback                             | — (redirect to /auth/confirm)                    |
| /auth/confirm | SpotifyAuthController | GET -> Confirm, POST -> ConfirmPost  | Shared/_Layout.cshtml, SpotifyAuth/Confirm.cshtml |
| /auth/logout  | SpotifyAuthController | POST -> Logout                       | — (redirect to /)                                |

### Library — `/lib`

Library list controllers (`Tracks`, `Albums`, `Artists`, `Playlists`) carry a class-level `[Authorize(Roles = "Admin,Editor")]`; the index, search, cover, and validation actions are `[AllowAnonymous]`, so listing and search are public while create/edit/delete require the `Admin` or `Editor` role.

The `search` actions return JSON and filter the cached library by name/metadata (case-insensitive substring); the date and duration query parameters narrow the result set further.

| URL                       | Controller         | Akcija                                                    | View(s)                                               |
| ------------------------- | ------------------ | --------------------------------------------------------- | ----------------------------------------------------- |
| /lib                      | LibraryController  | Index                                                     | — (redirect to /lib/tracks)                           |
| /lib/tracks               | TracksController   | Index                                                     | Shared/_Layout.cshtml, Tracks/Index.cshtml            |
| /lib/tracks/create        | TracksController   | GET -> Create, POST -> CreatePost `[Admin,Editor]`        | Shared/_Layout.cshtml, Tracks/Create.cshtml           |
| /lib/tracks/edit/{id}     | TracksController   | GET -> Edit, POST -> EditPost `[Admin,Editor]`            | Shared/_Layout.cshtml, Tracks/Edit.cshtml             |
| /lib/tracks/delete/{id}   | TracksController   | GET -> Delete, POST -> DeleteConfirmed `[Admin,Editor]`   | Shared/_Layout.cshtml, Tracks/Delete.cshtml           |
| /lib/tracks/search        | TracksController   | SearchTracks(q, durationMin, durationMax)                 | — (JSON)                                              |
| /lib/albums               | AlbumsController   | Index                                                     | Shared/_Layout.cshtml, Albums/Index.cshtml            |
| /lib/albums/create        | AlbumsController   | GET -> Create, POST -> CreatePost `[Admin,Editor]`        | Shared/_Layout.cshtml, Albums/Create.cshtml           |
| /lib/albums/edit/{id}     | AlbumsController   | GET -> Edit, POST -> EditPost `[Admin,Editor]`            | Shared/_Layout.cshtml, Albums/Edit.cshtml             |
| /lib/albums/delete/{id}   | AlbumsController   | GET -> Delete, POST -> DeleteConfirmed `[Admin,Editor]`   | Shared/_Layout.cshtml, Albums/Delete.cshtml           |
| /lib/albums/cover/{id}    | AlbumsController   | AlbumCover                                                | — (image stream)                                      |
| /lib/albums/search        | AlbumsController   | SearchAlbums(q, dateFrom, dateTo)                         | — (JSON)                                              |
| /lib/artists              | ArtistsController  | Index                                                     | Shared/_Layout.cshtml, Artists/Index.cshtml           |
| /lib/artists/create       | ArtistsController  | GET -> Create, POST -> Create(...) `[Admin,Editor]`       | Shared/_Layout.cshtml, Artists/Create.cshtml          |
| /lib/artists/edit/{id}    | ArtistsController  | GET -> Edit, POST -> EditPost `[Admin,Editor]`            | Shared/_Layout.cshtml, Artists/Edit.cshtml            |
| /lib/artists/delete/{id}  | ArtistsController  | GET -> Delete, POST -> DeleteConfirmed `[Admin,Editor]`   | Shared/_Layout.cshtml, Artists/Delete.cshtml          |
| /lib/artists/search       | ArtistsController  | SearchArtists(q)                                          | — (JSON)                                              |
| /lib/artists/validate-name| ArtistsController  | ValidateArtistName(q, excludeId)                          | — (JSON)                                              |
| /lib/playlists            | PlaylistsController| Index                                                     | Shared/_Layout.cshtml, Playlists/Index.cshtml         |
| /lib/playlists/create     | PlaylistsController| GET -> Create, POST -> CreatePost `[Admin,Editor]`        | Shared/_Layout.cshtml, Playlists/Create.cshtml        |
| /lib/playlists/edit/{id}  | PlaylistsController| GET -> Edit, POST -> EditPost `[Admin,Editor]`            | Shared/_Layout.cshtml, Playlists/Edit.cshtml          |
| /lib/playlists/delete/{id}| PlaylistsController| GET -> Delete, POST -> DeleteConfirmed `[Admin,Editor]`   | Shared/_Layout.cshtml, Playlists/Delete.cshtml        |
| /lib/playlists/search     | PlaylistsController| SearchPlaylists(q, dateFrom, dateTo)                      | — (JSON)                                              |

### Scheduled shuffles — `SchedulesController` (`/schedules`)

Every action requires a connected Spotify account (the `SpotifyConnect` scheme is checked in-action; callers without it are redirected to login).

| URL                      | Controller         | Akcija                            | View(s)                                          |
| ------------------------ | ------------------ | --------------------------------- | ------------------------------------------------ |
| /schedules               | SchedulesController | Index                             | Shared/_Layout.cshtml, Schedules/Index.cshtml    |
| /schedules/create        | SchedulesController | GET -> Create, POST -> Create(...)| Shared/_Layout.cshtml, Schedules/Create.cshtml   |
| /schedules/{id}/toggle   | SchedulesController | POST -> Toggle                    | — (redirect to /schedules)                       |
| /schedules/{id}/delete   | SchedulesController | POST -> Delete                    | — (redirect to /schedules)                       |

Due schedules are executed out-of-band by the `ShuffleSchedulerService` hosted background service (60-second tick), not by an HTTP route.

### Files — `FilesController` (`/files`) — requires `[Authorize]`

| URL                  | Controller      | Akcija              | View(s)                                      |
| -------------------- | --------------- | ------------------- | -------------------------------------------- |
| /files               | FilesController | Index               | Shared/_Layout.cshtml, Files/Index.cshtml    |
| /files/list          | FilesController | List                | — (JSON)                                     |
| /files/upload        | FilesController | POST -> Upload      | — (JSON)                                     |
| /files/download/{id} | FilesController | Download            | — (file stream)                              |
| /files/{id}          | FilesController | DELETE -> Delete    | — (JSON)                                     |

## API Reference

REST API controllers inherit from `ApiControllerBase` (`[ApiController]`). All routes are prefixed with `api/` and exchange JSON DTOs.

**Authentication:** The `api/` surface authenticates exclusively via **JWT bearer tokens** (scheme `Bearer`); the Identity (`SSKAuth`) cookie is no longer accepted on `api/` routes. Obtain a token from `POST /api/auth/token` and send it as the `Authorization: Bearer <access_token>` header. Tokens are HS256-signed with a key generated on first run and persisted in the `JwtSigningKeys` table.

**Authorization:** GET endpoints (`GetAll` and `GetById`) are `[AllowAnonymous]` and need no token. POST/PUT/DELETE endpoints require a bearer token whose holder has the `Admin` or `Editor` role — unauthenticated callers receive `401`, authenticated callers without the required role receive `403`.

**Search (`?q=`):** The collection `GetAll` endpoints accept an optional `q` term. A match is returned when the entity's **name contains `q`** (case-insensitive substring) **or** the entity's **`Id` equals `q` exactly** (case-insensitive). This lets the same endpoint serve both free-text searches and exact-ID lookups; passing a known resource id returns just that resource.

### Auth — `AuthApiController` (`api/auth`)

All endpoints are `[AllowAnonymous]`. `TokenResponseDto` is `{ token_type, access_token, expires_in, refresh_token }`. The access token is short-lived (`Jwt:AccessTokenMinutes`, default 60); `refresh` rotates the refresh token (the presented one is revoked and a new pair issued).

| Method | Route             | Akcija  | Request body                          | Responses                          |
| ------ | ----------------- | ------- | ------------------------------------- | ---------------------------------- |
| POST   | /api/auth/token   | Token   | `TokenRequestDto` (username, password)| 200 `TokenResponseDto`, 400, 401   |
| POST   | /api/auth/refresh | Refresh | `RefreshRequestDto` (refresh_token)   | 200 `TokenResponseDto`, 400, 401   |
| POST   | /api/auth/revoke  | Revoke  | `RefreshRequestDto` (refresh_token)   | 204                                |

### Albums — `AlbumsApiController` (`api/albums`)

`GET /api/albums?q=` matches by album name (substring) or exact album id.

| Method | Route             | Akcija     | Request body     | Responses                          |
| ------ | ----------------- | ---------- | ---------------- | ---------------------------------- |
| GET    | /api/albums?q=    | GetAll     | —                | 200 `IEnumerable<AlbumListDto>`    |
| GET    | /api/albums/{id}  | GetById    | —                | 200 `AlbumDetailDto`, 404          |
| POST   | /api/albums       | Create     | `AlbumCreateDto` | 201 `AlbumDetailDto`, 400, 401, 403, 422     |
| PUT    | /api/albums/{id}  | Update     | `AlbumUpdateDto` | 200 `AlbumDetailDto`, 400, 401, 403, 404, 422|
| DELETE | /api/albums/{id}  | Delete     | —                | 204, 401, 403, 404                           |

### Tracks — `TracksApiController` (`api/tracks`)

`GET /api/tracks?q=` matches by track name (substring) or exact track id.

| Method | Route             | Akcija     | Request body     | Responses                          |
| ------ | ----------------- | ---------- | ---------------- | ---------------------------------- |
| GET    | /api/tracks?q=    | GetAll     | —                | 200 `IEnumerable<TrackListDto>`    |
| GET    | /api/tracks/{id}  | GetById    | —                | 200 `TrackDetailDto`, 404          |
| POST   | /api/tracks       | Create     | `TrackCreateDto` | 201 `TrackDetailDto`, 400, 401, 403, 404     |
| PUT    | /api/tracks/{id}  | Update     | `TrackUpdateDto` | 200 `TrackDetailDto`, 400, 401, 403, 404     |
| DELETE | /api/tracks/{id}  | Delete     | —                | 204, 401, 403, 404                           |

### Playlists — `PlaylistsApiController` (`api/playlists`)

`GET /api/playlists?q=` matches by playlist name (substring) or exact playlist id.

| Method | Route                | Akcija | Request body        | Responses                             |
| ------ | -------------------- | ------ | ------------------- | ------------------------------------- |
| GET    | /api/playlists?q=    | GetAll | —                   | 200 `IEnumerable<PlaylistListDto>`    |
| GET    | /api/playlists/{id}  | GetById| —                   | 200 `PlaylistDetailDto`, 404          |
| POST   | /api/playlists       | Create | `PlaylistCreateDto` | 201 `PlaylistDetailDto`, 400, 401, 403, 422     |
| PUT    | /api/playlists/{id}  | Update | `PlaylistUpdateDto` | 200 `PlaylistDetailDto`, 400, 401, 403, 404, 422|
| DELETE | /api/playlists/{id}  | Delete | —                   | 204, 401, 403, 404                              |

### Artists — `ArtistsApiController` (`api/artists`)

Supports soft delete; GET endpoints accept an optional `includeDeleted` query parameter. `GET /api/artists?q=` matches by artist name (substring) or exact artist id.

| Method | Route                            | Akcija     | Request body      | Responses                           |
| ------ | -------------------------------- | ---------- | ----------------- | ----------------------------------- |
| GET    | /api/artists?q=&includeDeleted=  | GetAll     | —                 | 200 `IEnumerable<ArtistListDto>`    |
| GET    | /api/artists/{id}?includeDeleted=| GetById    | —                 | 200 `ArtistDetailDto`, 404          |
| POST   | /api/artists                     | Create     | `ArtistCreateDto` | 201 `ArtistDetailDto`, 400, 401, 403, 422     |
| PUT    | /api/artists/{id}                | Update     | `ArtistUpdateDto` | 200 `ArtistDetailDto`, 400, 401, 403, 404, 422|
| DELETE | /api/artists/{id}                | Delete     | —                 | 204, 401, 403, 404                            |
