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

	/// <summary>
	/// Display form to create a new playlist
	/// </summary>
	[HttpGet("create")]
	public IActionResult Create()
	{
		return View(new PlaylistCreateModel());
	}

	/// <summary>
	/// Handle form submission for creating a new playlist
	/// </summary>
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

			// Note: In a real app, you would persist this to the database
			// _context.Playlists.Add(playlist);
			// _context.SaveChanges();

			return RedirectToAction("Index", "Library", new { section = "Playlists" });
		}

		// Return form with validation errors
		return View(model);
	}

	/// <summary>
	/// Display form to edit an existing playlist
	/// </summary>
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

	/// <summary>
	/// Handle form submission for editing a playlist
	/// </summary>
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

			// Note: In a real app, you would persist this
			// _context.SaveChanges();

			return RedirectToAction("Index", "Library", new { section = "Playlists" });
		}

		return View(model);
	}

	/// <summary>
	/// Delete a playlist
	/// </summary>
	[HttpPost("{id}/delete")]
	[ValidateAntiForgeryToken]
	public IActionResult Delete(string id)
	{
		var playlist = _playlistRepository.GetById(id);
		if (playlist == null)
		{
			return NotFound();
		}

		// Note: In a real app, you would delete this from the database
		// _context.Playlists.Remove(playlist);
		// _context.SaveChanges();

		return RedirectToAction("Index", "Library", new { section = "Playlists" });
	}
}
