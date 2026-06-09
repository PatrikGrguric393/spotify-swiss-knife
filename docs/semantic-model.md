# Database Semantic Model

## Tables (Entity Classes)

### Albums (`Album`)
- PK: `Id:string`
- Columns:
	- `Id:string`
	- `AlbumType:string`
	- `TotalTracks:int`
	- `Name:string`
	- `ReleaseDate:string`
	- `ReleaseDatePrecision:string`
	- `Label:string`
	- `Popularity:int`
	- `ExternalUrls_Spotify:string` (owned)
- Related tables:
	- `AlbumImages` via `Images`
	- `Tracks` via `TrackList` (1-*; FK `Tracks.AlbumId`, nullable)
	- `AlbumArtists` (M-M join with `Artists`)

### Artists (`Artist`)
- PK: `Id:string`
- Columns:
	- `Id:string`
	- `Name:string`
	- `ExternalUrls_Spotify:string` (owned)
- Related tables:
	- `AlbumArtists` (M-M join with `Albums`)
	- `TrackArtists` (M-M join with `Tracks`)

### Tracks (`Track`)
- PK: `Id:string`
- Columns:
	- `Id:string`
	- `Name:string`
	- `DiscNumber:int`
	- `TrackNumber:int`
	- `DurationMs:int`
	- `IsLocal:bool`
	- `AlbumId:string?` (FK -> `Albums.Id`)
	- `ExternalUrls_Spotify:string` (owned)
- Related tables:
	- `TrackImages` via `Images`
	- `TrackArtists` (M-M join with `Artists`)
	- `PlaylistTrackEntries` (1-* from `Tracks` to `PlaylistTrackEntries`)

### Playlists (`Playlist`)
- PK: `Id:string`
- Columns:
	- `Id:string`
	- `Name:string`
	- `Description:string`
	- `SnapshotId:string`
	- `LastShuffled:DateTime?`
	- `ExternalUrls_Spotify:string` (owned)
	- `Owner_DisplayName:string?` (owned)
	- `Owner_ExternalUrls_Spotify:string` (owned)
- Related tables:
	- `PlaylistImages` via `Images`
	- `PlaylistTrackEntries` via `TrackEntries` (1-*)

### PlaylistTrackEntries (`PlaylistTrackEntry`)
- PK: `Id:int`
- Columns:
	- `Id:int`
	- `PlaylistId:string` (FK -> `Playlists.Id`)
	- `TrackId:string` (FK -> `Tracks.Id`)
	- `SortOrder:int`

## Owned Collection Tables

### AlbumImages (owned `Image`)
- PK: `(AlbumId, Id)`
- Columns:
	- `AlbumId:string` (FK -> `Albums.Id`)
	- `Id:int`
	- `Url:string`
	- `Height:int?`
	- `Width:int?`

### TrackImages (owned `Image`)
- PK: `(TrackId, Id)`
- Columns:
	- `TrackId:string` (FK -> `Tracks.Id`)
	- `Id:int`
	- `Url:string`
	- `Height:int?`
	- `Width:int?`

### PlaylistImages (owned `Image`)
- PK: `(PlaylistId, Id)`
- Columns:
	- `PlaylistId:string` (FK -> `Playlists.Id`)
	- `Id:int`
	- `Url:string`
	- `Height:int?`
	- `Width:int?`

## Many-to-Many Join Tables

### AlbumArtists
- PK: `(AlbumId, ArtistId)`
- Columns:
	- `AlbumId:string` (FK -> `Albums.Id`)
	- `ArtistId:string` (FK -> `Artists.Id`)

### TrackArtists
- PK: `(ArtistId, TrackId)`
- Columns:
	- `ArtistId:string` (FK -> `Artists.Id`)
	- `TrackId:string` (FK -> `Tracks.Id`)

## Auth & Token Tables

### SpotifyTokens (`SpotifyToken`)
- PK: `Id:int`
- Columns:
	- `Id:int`
	- `SpotifyUserId:string` (unique index; Spotify account id, **not** the ASP.NET Identity user id)
	- `AccessToken:string`
	- `RefreshToken:string`
	- `ExpiresAt:DateTimeOffset`

### RefreshTokens (`RefreshToken`)
- PK: `Id:int`
- Columns:
	- `Id:int`
	- `TokenHash:string` (unique index; SHA-256 hash of the raw refresh token — the raw value is never stored)
	- `UserId:string` (index; ASP.NET Identity user id the token was issued to)
	- `ExpiresAt:DateTimeOffset`
	- `CreatedAt:DateTimeOffset`
	- `RevokedAt:DateTimeOffset?` (set when rotated out on refresh or explicitly revoked)

### JwtSigningKeys (`JwtSigningKey`)
- PK: `Id:int`
- Columns:
	- `Id:int`
	- `Purpose:string` (unique index; keeps exactly one active key row)
	- `KeyMaterial:string` (base64-encoded raw HMAC-SHA256 key bytes, generated on first run)
	- `CreatedAt:DateTimeOffset`

## Application Tables

### ScheduledShuffles (`ScheduledShuffle`)
- PK: `Id:int`
- Columns:
	- `Id:int`
	- `UserId:string` (index; Spotify account id, links logically to `SpotifyTokens.SpotifyUserId`)
	- `PlaylistId:string`
	- `PlaylistName:string`
	- `RandomnessLevel:int` (enum `ShuffleRandomnessLevel`)
	- `CronExpression:string` (standard 5-field cron, UTC)
	- `IsEnabled:bool`
	- `LastRunAt:DateTimeOffset?`
	- `NextRunAt:DateTimeOffset?`
	- `CreatedAt:DateTimeOffset`

### UserFiles (`UserFile`)
- PK: `Id:int`
- Columns:
	- `Id:int`
	- `UserId:string` (FK -> `AspNetUsers.Id`, cascade delete)
	- `OriginalFileName:string`
	- `StoredFileName:string`
	- `ContentType:string`
	- `FileSize:long`
	- `UploadedAt:DateTime`

## Identity & Infrastructure Tables

### AspNetUsers (`AppUser`)
- PK: `Id:string`
- Extends ASP.NET Identity `IdentityUser` (base columns: `UserName`, `NormalizedUserName`, `Email`, `NormalizedEmail`, `EmailConfirmed`, `PasswordHash`, `SecurityStamp`, `ConcurrencyStamp`, `PhoneNumber`, `PhoneNumberConfirmed`, `TwoFactorEnabled`, `LockoutEnd`, `LockoutEnabled`, `AccessFailedCount`).
- Custom columns:
	- `FirstName:string?`
	- `LastName:string?`
	- `DateOfBirth:DateOnly?`
	- `OIB:string` (required, 11 digits)
	- `JMBAG:string` (required, 10 digits)
- Companion Identity tables (standard schema, provided by `IdentityDbContext`): `AspNetRoles`, `AspNetUserRoles`, `AspNetUserClaims`, `AspNetUserLogins`, `AspNetUserTokens`, `AspNetRoleClaims`.

### DataProtectionKeys (`DataProtectionKey`)
- PK: `Id:int`
- Columns:
	- `Id:int`
	- `FriendlyName:string?`
	- `Xml:string?`
- Managed by ASP.NET Data Protection (`PersistKeysToDbContext`); stores the app's key ring (including the cookie/antiforgery keys).

## Relations

- `Albums (1) -> (0..*) Tracks` via `Tracks.AlbumId`
- `Playlists (1) -> (0..*) PlaylistTrackEntries` via `PlaylistTrackEntries.PlaylistId`
- `Tracks (1) -> (0..*) PlaylistTrackEntries` via `PlaylistTrackEntries.TrackId`
- `Albums (0..*) <-> (0..*) Artists` via `AlbumArtists`
- `Tracks (0..*) <-> (0..*) Artists` via `TrackArtists`
- `AspNetUsers (1) -> (0..*) UserFiles` via `UserFiles.UserId` (FK, cascade)
- `AspNetUsers (1) -> (0..*) RefreshTokens` via `RefreshTokens.UserId` (logical; indexed, no enforced FK)
- `SpotifyTokens (1) -> (0..*) ScheduledShuffles` via `ScheduledShuffles.UserId` matching `SpotifyTokens.SpotifyUserId` (logical; indexed, no enforced FK)
