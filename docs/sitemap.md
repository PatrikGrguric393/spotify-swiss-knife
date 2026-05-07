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