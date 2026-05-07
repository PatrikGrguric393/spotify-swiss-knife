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

## Relations

- `Albums (1) -> (0..*) Tracks` via `Tracks.AlbumId`
- `Playlists (1) -> (0..*) PlaylistTrackEntries` via `PlaylistTrackEntries.PlaylistId`
- `Tracks (1) -> (0..*) PlaylistTrackEntries` via `PlaylistTrackEntries.TrackId`
- `Albums (0..*) <-> (0..*) Artists` via `AlbumArtists`
- `Tracks (0..*) <-> (0..*) Artists` via `TrackArtists`
