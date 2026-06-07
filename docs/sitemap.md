| URL            | Controller         | Akcija                                               | View(s)                                         |
| -------------- | ------------------ | ---------------------------------------------------- | ----------------------------------------------- |
| /              | HomeController     | Index                                                | Shared/_Layout.cshtml, Home/Index.cshtml        |
| /description   | AboutController    | Index                                                | Shared/_Layout.cshtml, About/Index.cshtml       |
| /lib           | LibraryController  | Index                                                | Shared/_Layout.cshtml, Library/Songs.cshtml     |
| /lib/songs     | LibraryController  | Songs                                                | Shared/_Layout.cshtml, Library/Songs.cshtml     |
| /lib/albums    | LibraryController  | Albums                                               | Shared/_Layout.cshtml, Library/Albums.cshtml    |
| /lib/artists   | LibraryController  | Artists                                              | Shared/_Layout.cshtml, Library/Artists.cshtml   |
| /lib/playlists | LibraryController  | Playlists                                            | Shared/_Layout.cshtml, Library/Playlists.cshtml |
| /shuffle       | ServicesController | GET -> ShufflePlaylist, POST -> ShufflePlaylist(...) | Shared/_Layout.cshtml, ShufflePlaylist.cshtml   |

## API Reference

REST API controllers inherit from `ApiControllerBase` (`[ApiController]`). All routes are prefixed with `api/` and exchange JSON DTOs.

**Authorization** mirrors the MVC `/lib` controllers: each controller requires the `Admin` or `Editor` role (`[Authorize(Roles = "Admin,Editor")]`), while the read endpoints (`GET` list and `GET /{id}`) are `[AllowAnonymous]`. Create/update/delete therefore require an authenticated Admin or Editor.

### Albums — `AlbumsApiController` (`api/albums`)

| Method | Route             | Akcija     | Request body     | Responses                          |
| ------ | ----------------- | ---------- | ---------------- | ---------------------------------- |
| GET    | /api/albums?q=    | GetAll     | —                | 200 `IEnumerable<AlbumSummaryDto>` |
| GET    | /api/albums/{id}  | GetById    | —                | 200 `AlbumDto`, 404                |
| POST   | /api/albums       | Create     | `AlbumCreateDto` | 201 `AlbumDto`, 400, 422           |
| PUT    | /api/albums/{id}  | Update     | `AlbumUpdateDto` | 200 `AlbumDto`, 400, 404, 422      |
| DELETE | /api/albums/{id}  | Delete     | —                | 204, 404                           |

### Tracks — `TracksApiController` (`api/tracks`)

| Method | Route             | Akcija     | Request body     | Responses                          |
| ------ | ----------------- | ---------- | ---------------- | ---------------------------------- |
| GET    | /api/tracks?q=    | GetAll     | —                | 200 `IEnumerable<TrackSummaryDto>` |
| GET    | /api/tracks/{id}  | GetById    | —                | 200 `TrackDto`, 404                |
| POST   | /api/tracks       | Create     | `TrackCreateDto` | 201 `TrackDto`, 400, 404           |
| PUT    | /api/tracks/{id}  | Update     | `TrackUpdateDto` | 200 `TrackDto`, 404                |
| DELETE | /api/tracks/{id}  | Delete     | —                | 204, 404                           |

### Playlists — `PlaylistsApiController` (`api/playlists`)

| Method | Route                | Akcija | Request body        | Responses                             |
| ------ | -------------------- | ------ | ------------------- | ------------------------------------- |
| GET    | /api/playlists?q=    | GetAll | —                   | 200 `IEnumerable<PlaylistSummaryDto>` |
| GET    | /api/playlists/{id}  | GetById| —                   | 200 `PlaylistDto`, 404                |
| POST   | /api/playlists       | Create | `PlaylistCreateDto` | 201 `PlaylistDto`, 400, 422           |
| PUT    | /api/playlists/{id}  | Update | `PlaylistUpdateDto` | 200 `PlaylistDto`, 404, 422           |
| DELETE | /api/playlists/{id}  | Delete | —                   | 204, 404                              |

### Artists — `ArtistsApiController` (`api/artists`)

Supports soft delete; GET endpoints accept an optional `includeDeleted` query parameter.

| Method | Route                            | Akcija     | Request body      | Responses                           |
| ------ | -------------------------------- | ---------- | ----------------- | ----------------------------------- |
| GET    | /api/artists?q=&includeDeleted=  | GetAll     | —                 | 200 `IEnumerable<ArtistSummaryDto>` |
| GET    | /api/artists/{id}?includeDeleted=| GetById    | —                 | 200 `ArtistDto`, 404                |
| POST   | /api/artists                     | Create     | `ArtistCreateDto` | 201 `ArtistDto`, 400, 422           |
| PUT    | /api/artists/{id}                | Update     | `ArtistUpdateDto` | 200 `ArtistDto`, 404, 422           |
| DELETE | /api/artists/{id}                | SoftDelete | —                 | 204, 404                            |