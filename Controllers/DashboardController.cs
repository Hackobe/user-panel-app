using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserPanelApp.Data;
using UserPanelApp.Models;
using UserPanelApp.ViewModels;

namespace UserPanelApp.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly AppDbContext _db;

    public DashboardController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = GetCurrentUserId();

        var notes = await _db.UserNotes
            .Where(note => note.AppUserId == userId)
            .OrderByDescending(note => note.CreatedAt)
            .ToListAsync();

        return View(new DashboardIndexViewModel
        {
            Notes = notes
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddNote([Bind(Prefix = "NewNote")] CreateNoteViewModel model)
    {
        var userId = GetCurrentUserId();

        if (!ModelState.IsValid)
        {
            var notes = await _db.UserNotes
                .Where(note => note.AppUserId == userId)
                .OrderByDescending(note => note.CreatedAt)
                .ToListAsync();

            return View("Index", new DashboardIndexViewModel
            {
                NewNote = model,
                Notes = notes
            });
        }

        _db.UserNotes.Add(new UserNote
        {
            AppUserId = userId,
            Title = model.Title,
            Content = model.Content,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private int GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.Parse(value!);
    }
}