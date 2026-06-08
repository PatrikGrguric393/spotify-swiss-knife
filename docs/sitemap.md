## MVC Routes

### Core

| URL          | Controller         | Akcija                                               | View(s)                                              |
| ------------ | ------------------ | ---------------------------------------------------- | ---------------------------------------------------- |
| /            | HomeController     | Index                                                | Shared/_Layout.cshtml, Home/Index.cshtml             |
| /description | AboutController    | Index                                                | Shared/_Layout.cshtml, About/Index.cshtml            |
| /login       | LoginController    | Index                                                | Shared/_Layout.cshtml, Login/Index.cshtml            |
| /shuffle     | ServicesController | GET -> ShufflePlaylist, POST -> ShufflePlaylist(...) | Shared/_Layout.cshtml, Services/ShufflePlaylist.cshtml |
| /search      | SearchController   | Index                                                | — (JSON)                                             |

### Local account — `AccountController` (`/account`)

| URL                       | Controller        | Akcija                                          | View(s)                                           |
| ------------------------- | ----------------- | ----------------------------------------------- | ------------------------------------------------- |
| /account/register         | AccountController | GET -> Register, POST -> Register(...)          | Shared/_Layout.cshtml, Account/Register.cshtml    |
| /account/login            | AccountController | GET -> Login, POST -> Login(...)                | Shared/_Layout.cshtml, Account/Login.cshtml       |
| /account/logout           | AccountController | POST -> Logout                                  | — (redirect to /)                                 |
| /account/denied           | AccountController | Denied                                          | Shared/_Layout.cshtml, Account/Denied.cshtml      |
| /account/users            | AccountController | Users `[Admin]`                                 | Shared/_Layout.cshtml, Account/Users.cshtml       |
| /account/users/role       | AccountController | POST -> SetRole `[Admin]`                       | — (redirect to /account/users)                    |
| /account/users/{id}/edit  | AccountController | GET -> EditUser, POST -> EditUser(...) `[Admin]` | Shared/_Layout.cshtml, Account/EditUser.cshtml    |
| /account/users/{id}/delete| AccountController | POST -> DeleteUser `[Admin]`                    | — (redirect to /account/users)                    |

### Spotify OAuth — `AuthController` (`/auth`)

| URL           | Controller     | Akcija                               | View(s)                                       |
| ------------- | -------------- | ------------------------------------ | --------------------------------------------- |
| /auth/login   | AuthController | Login                                | — (redirect to Spotify authorization URL)     |
| /auth/callback| AuthController | Callback                             | — (redirect to /auth/confirm)                 |
| /auth/confirm | AuthController | GET -> Confirm, POST -> ConfirmPost  | Shared/_Layout.cshtml, Auth/Confirm.cshtml    |
| /auth/logout  | AuthController | POST -> Logout                       | — (redirect to /)                             |

### Library — `/lib`

| URL                       | Controller         | Akcija                                                    | View(s)                                               |
| ------------------------- | ------------------ | --------------------------------------------------------- | ----------------------------------------------------- |
| /lib                      | LibraryController  | Index                                                     | — (redirect to /lib/songs)                            |
| /lib/songs                | SongsController    | Songs                                                     | Shared/_Layout.cshtml, Songs/Songs.cshtml             |
| /lib/songs/create         | SongsController    | GET -> CreateSong, POST -> CreateSongPost `[Admin,Editor]`| Shared/_Layout.cshtml, Songs/CreateSong.cshtml        |
| /lib/songs/edit/{id}      | SongsController    | GET -> EditSong, POST -> EditSongPost `[Admin,Editor]`    | Shared/_Layout.cshtml, Songs/EditSong.cshtml          |
| /lib/songs/delete/{id}    | SongsController    | GET -> DeleteSong, POST -> DeleteSongConfirmed `[Admin,Editor]` | Shared/_Layout.cshtml, Songs/DeleteSong.cshtml  |
| /lib/songs/search         | SongsController    | SearchSongs                                               | — (JSON)                                              |
| /lib/albums               | AlbumsController   | Albums                                                    | Shared/_Layout.cshtml, Albums/Albums.cshtml           |
| /lib/albums/create        | AlbumsController   | GET -> CreateAlbum, POST -> CreateAlbumPost `[Admin,Editor]` | Shared/_Layout.cshtml, Albums/CreateAlbum.cshtml   |
| /lib/albums/edit/{id}     | AlbumsController   | GET -> EditAlbum, POST -> EditAlbumPost `[Admin,Editor]`  | Shared/_Layout.cshtml, Albums/EditAlbum.cshtml        |
| /lib/albums/delete/{id}   | AlbumsController   | GET -> DeleteAlbum, POST -> DeleteAlbumConfirmed `[Admin,Editor]` | Shared/_Layout.cshtml, Albums/DeleteAlbum.cshtml |
| /lib/albums/search        | AlbumsController   | SearchAlbums                                              | — (JSON)                                              |
| /lib/artists              | ArtistsController  | Artists                                                   | Shared/_Layout.cshtml, Artists/Artists.cshtml         |
| /lib/artists/create       | ArtistsController  | GET -> CreateArtist, POST -> CreateArtist(...) `[Admin,Editor]` | Shared/_Layout.cshtml, Artists/CreateArtist.cshtml |
| /lib/artists/edit/{id}    | ArtistsController  | GET -> EditArtist, POST -> EditArtistPost `[Admin,Editor]`| Shared/_Layout.cshtml, Artists/EditArtist.cshtml      |
| /lib/artists/delete/{id}  | ArtistsController  | GET -> DeleteArtist, POST -> DeleteArtistConfirmed `[Admin,Editor]` | Shared/_Layout.cshtml, Artists/DeleteArtist.cshtml |
| /lib/artists/search       | ArtistsController  | SearchArtists                                             | — (JSON)                                              |
| /lib/artists/validate-name| ArtistsController  | ValidateArtistName                                        | — (JSON)                                              |
| /lib/playlists            | PlaylistsController| Playlists                                                 | Shared/_Layout.cshtml, Playlists/Playlists.cshtml     |
| /lib/playlists/create     | PlaylistsController| GET -> CreatePlaylist, POST -> CreatePlaylistPost `[Admin,Editor]` | Shared/_Layout.cshtml, Playlists/CreatePlaylist.cshtml |
| /lib/playlists/edit/{id}  | PlaylistsController| GET -> EditPlaylist, POST -> EditPlaylistPost `[Admin,Editor]` | Shared/_Layout.cshtml, Playlists/EditPlaylist.cshtml |
| /lib/playlists/delete/{id}| PlaylistsController| GET -> DeletePlaylist, POST -> DeletePlaylistConfirmed `[Admin,Editor]` | Shared/_Layout.cshtml, Playlists/DeletePlaylist.cshtml |
| /lib/playlists/search     | PlaylistsController| SearchPlaylists                                           | — (JSON)                                              |

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

**Authorization:** GET endpoints (`GetAll` and `GetById`) are `[AllowAnonymous]`. POST/PUT/DELETE endpoints require `[Authorize(Roles = "Admin,Editor")]` — unauthenticated callers receive `401`, authenticated callers without the required role receive `403`.

### Albums — `AlbumsApiController` (`api/albums`)

| Method | Route             | Akcija     | Request body     | Responses                          |
| ------ | ----------------- | ---------- | ---------------- | ---------------------------------- |
| GET    | /api/albums?q=    | GetAll     | —                | 200 `IEnumerable<AlbumSummaryDto>` |
| GET    | /api/albums/{id}  | GetById    | —                | 200 `AlbumDto`, 404                |
| POST   | /api/albums       | Create     | `AlbumCreateDto` | 201 `AlbumDto`, 400, 401, 403, 422           |
| PUT    | /api/albums/{id}  | Update     | `AlbumUpdateDto` | 200 `AlbumDto`, 400, 401, 403, 404, 422      |
| DELETE | /api/albums/{id}  | Delete     | —                | 204, 401, 403, 404                           |

### Tracks — `TracksApiController` (`api/tracks`)

| Method | Route             | Akcija     | Request body     | Responses                          |
| ------ | ----------------- | ---------- | ---------------- | ---------------------------------- |
| GET    | /api/tracks?q=    | GetAll     | —                | 200 `IEnumerable<TrackSummaryDto>` |
| GET    | /api/tracks/{id}  | GetById    | —                | 200 `TrackDto`, 404                |
| POST   | /api/tracks       | Create     | `TrackCreateDto` | 201 `TrackDto`, 400, 401, 403, 404           |
| PUT    | /api/tracks/{id}  | Update     | `TrackUpdateDto` | 200 `TrackDto`, 401, 403, 404                |
| DELETE | /api/tracks/{id}  | Delete     | —                | 204, 401, 403, 404                           |

### Playlists — `PlaylistsApiController` (`api/playlists`)

| Method | Route                | Akcija | Request body        | Responses                             |
| ------ | -------------------- | ------ | ------------------- | ------------------------------------- |
| GET    | /api/playlists?q=    | GetAll | —                   | 200 `IEnumerable<PlaylistSummaryDto>` |
| GET    | /api/playlists/{id}  | GetById| —                   | 200 `PlaylistDto`, 404                |
| POST   | /api/playlists       | Create | `PlaylistCreateDto` | 201 `PlaylistDto`, 400, 401, 403, 422           |
| PUT    | /api/playlists/{id}  | Update | `PlaylistUpdateDto` | 200 `PlaylistDto`, 401, 403, 404, 422           |
| DELETE | /api/playlists/{id}  | Delete | —                   | 204, 401, 403, 404                              |

### Artists — `ArtistsApiController` (`api/artists`)

Supports soft delete; GET endpoints accept an optional `includeDeleted` query parameter.

| Method | Route                            | Akcija     | Request body      | Responses                           |
| ------ | -------------------------------- | ---------- | ----------------- | ----------------------------------- |
| GET    | /api/artists?q=&includeDeleted=  | GetAll     | —                 | 200 `IEnumerable<ArtistSummaryDto>` |
| GET    | /api/artists/{id}?includeDeleted=| GetById    | —                 | 200 `ArtistDto`, 404                |
| POST   | /api/artists                     | Create     | `ArtistCreateDto` | 201 `ArtistDto`, 400, 401, 403, 422           |
| PUT    | /api/artists/{id}                | Update     | `ArtistUpdateDto` | 200 `ArtistDto`, 401, 403, 404, 422           |
| DELETE | /api/artists/{id}                | SoftDelete | —                 | 204, 401, 403, 404                            |
