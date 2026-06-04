using Microsoft.AspNetCore.Mvc;
using spotify_swiss_knife.Models;
using spotify_swiss_knife.Models.FormModels;
using spotify_swiss_knife.Services;

namespace spotify_swiss_knife.Controllers;

[Route("playlists")]
public class PlaylistController : Controller
{
	private readonly PlaylistRepository _playlistRepository;

	public PlaylistController(PlaylistRepository playlistRepository)
	{
		_playlistRepository = playlistRepository;
	}

	[HttpGet("create")]
	public IActionResult Create()
	{
		return View(new PlaylistCreateModel());
	}

	[HttpPost("create")]
	[ValidateAntiForgeryToken]
	public IActionResult Create(PlaylistCreateModel model)
	{
		if (ModelState.IsValid)
		{
			var playlist = new Playlist
			{
				Id = Guid.NewGuid().ToString(),
				Name = model.Name,
				Description = model.Description
			};

			return RedirectToAction("Index", "Library", new { section = "Playlists" });
		}

		return View(model);
	}

	[HttpGet("{id}/edit")]
	public IActionResult Edit(string id)
	{
		var playlist = _playlistRepository.GetById(id);
		if (playlist == null)
		{
			return NotFound();
		}

		var model = new PlaylistEditModel
		{
			Id = playlist.Id,
			Name = playlist.Name,
			Description = playlist.Description
		};

		return View(model);
	}

	[HttpPost("{id}/edit")]
	[ValidateAntiForgeryToken]
	public IActionResult Edit(string id, PlaylistEditModel model)
	{
		if (id != model.Id)
		{
			ModelState.AddModelError("", "Playlist ID mismatch");
			return View(model);
		}

		if (ModelState.IsValid)
		{
			var playlist = _playlistRepository.GetById(id);
			if (playlist == null)
			{
				return NotFound();
			}

			playlist.Name = model.Name;
			playlist.Description = model.Description;

			return RedirectToAction("Index", "Library", new { section = "Playlists" });
		}

		return View(model);
	}

	[HttpPost("{id}/delete")]
	[ValidateAntiForgeryToken]
	public IActionResult Delete(string id)
	{
		var playlist = _playlistRepository.GetById(id);
		if (playlist == null)
		{
			return NotFound();
		}

		return RedirectToAction("Index", "Library", new { section = "Playlists" });
	}
}
