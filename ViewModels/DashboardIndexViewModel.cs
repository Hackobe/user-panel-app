using UserPanelApp.Models;

namespace UserPanelApp.ViewModels;

public class DashboardIndexViewModel
{
    public CreateNoteViewModel NewNote { get; set; } = new();

    public List<UserNote> Notes { get; set; } = new();
}