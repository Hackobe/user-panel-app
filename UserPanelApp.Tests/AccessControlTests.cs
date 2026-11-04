using System.Net;
using Microsoft.EntityFrameworkCore;

namespace UserPanelApp.Tests;

public sealed class AccessControlTests : IDisposable
{
    private readonly UserPanelFactory _factory = new();
    private const string UserA = "alice@example.test";
    private const string UserB = "bob@example.test";
    private const string PrivateTitle = "Private note belonging to Bob";
    private const string PrivateContent = "Only Bob should see this demo content.";

    [Theory]
    [InlineData("/Dashboard")]
    [InlineData("/Admin")]
    public async Task AnonymousUserIsRedirectedToLogin(string path)
    {
        using var browser = _factory.CreateBrowser();
        using var response = await browser.GetAsync(path);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("https://localhost/Account/Login?ReturnUrl=", response.Headers.Location?.OriginalString);
        Assert.DoesNotContain("<table", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task AnonymousUserCannotCreateANote()
    {
        using var browser = _factory.CreateBrowser();
        using var response = await browser.PostAsync("/Dashboard/AddNote", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["NewNote.Title"] = "Anonymous note",
            ["NewNote.Content"] = "Should not be saved"
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("https://localhost/Account/Login?ReturnUrl=", response.Headers.Location?.OriginalString);
        Assert.Equal(0, await _factory.ReadDatabaseAsync(db => db.UserNotes.CountAsync()));
    }

    [Theory]
    [InlineData("/Dashboard")]
    [InlineData("/Dashboard/Index/{0}?appUserId={1}&userId={1}")]
    [InlineData("/Dashboard?id={0}&appUserId={1}&userId={1}")]
    public async Task UserSeesOnlyOwnNotesEvenWithForeignIdentifiers(string path)
    {
        using var bob = await RegisterAndLoginAsync(UserB);
        await BrowserForms.AddNoteAsync(bob, PrivateTitle, PrivateContent);
        using var alice = await RegisterAndLoginAsync(UserA);
        await BrowserForms.AddNoteAsync(alice, "Alice's note", "Alice's content");
        var foreignNote = await _factory.ReadDatabaseAsync(db => db.UserNotes.SingleAsync(note => note.Title == PrivateTitle));

        using var response = await alice.GetAsync(string.Format(path, foreignNote.Id, foreignNote.AppUserId));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Contains("Alice's note", html);
        Assert.Contains("Alice's content", html);
        Assert.DoesNotContain(PrivateTitle, html);
        Assert.DoesNotContain(PrivateContent, html);

        // Positive control: Bob's note was created and can actually be displayed.
        var bobHtml = await bob.GetStringAsync("/Dashboard");
        Assert.Contains(PrivateTitle, bobHtml);
        Assert.Contains(PrivateContent, bobHtml);
    }

    [Fact]
    public async Task InvalidNoteFormDoesNotLeakAnotherUsersNotes()
    {
        using var bob = await RegisterAndLoginAsync(UserB);
        await BrowserForms.AddNoteAsync(bob, PrivateTitle, PrivateContent);
        using var alice = await RegisterAndLoginAsync(UserA);
        await BrowserForms.AddNoteAsync(alice, "Alice note", "Alice content");

        using var response = await BrowserForms.PostAsync(alice, "/Dashboard", "/Dashboard/AddNote", new()
        {
            ["NewNote.Title"] = "",
            ["NewNote.Content"] = "Invalid without a title"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Contains("Tytuł jest wymagany.", html);
        Assert.Contains("Alice note", html);
        Assert.DoesNotContain(PrivateTitle, html);
        Assert.DoesNotContain(PrivateContent, html);
        Assert.Equal(2, await _factory.ReadDatabaseAsync(db => db.UserNotes.CountAsync()));
    }

    [Fact]
    public async Task ForgedOwnerAndNoteIdentifiersCannotChangeAnotherUsersNote()
    {
        using var bob = await RegisterAndLoginAsync(UserB);
        await BrowserForms.AddNoteAsync(bob, PrivateTitle, PrivateContent);
        var foreignNote = await _factory.ReadDatabaseAsync(db => db.UserNotes.SingleAsync());
        using var alice = await RegisterAndLoginAsync(UserA);

        using var response = await BrowserForms.PostAsync(alice, "/Dashboard",
            $"/Dashboard/AddNote/{foreignNote.Id}?appUserId={foreignNote.AppUserId}", new()
        {
            ["NewNote.Title"] = "Forged ownership attempt",
            ["NewNote.Content"] = "This must become Alice's own note",
            ["NewNote.AppUserId"] = foreignNote.AppUserId.ToString(),
            ["AppUserId"] = foreignNote.AppUserId.ToString(),
            ["NewNote.Id"] = foreignNote.Id.ToString(),
            ["Id"] = foreignNote.Id.ToString()
        });

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var notes = await _factory.ReadDatabaseAsync(db => db.UserNotes.OrderBy(note => note.Id).ToListAsync());
        Assert.Equal(2, notes.Count);
        Assert.Equal(foreignNote.Id, notes[0].Id);
        Assert.Equal(foreignNote.AppUserId, notes[0].AppUserId);
        Assert.Equal(PrivateTitle, notes[0].Title);
        Assert.Equal(PrivateContent, notes[0].Content);
        var aliceId = await _factory.ReadDatabaseAsync(db => db.AppUsers.Where(user => user.Email == UserA).Select(user => user.Id).SingleAsync());
        Assert.Equal(aliceId, notes[1].AppUserId);
        Assert.NotEqual(foreignNote.Id, notes[1].Id);
    }

    [Fact]
    public async Task UserCannotReadAdminPageByDirectRequest()
    {
        using var browser = await RegisterAndLoginAsync(UserA);
        using var response = await browser.GetAsync("/Admin/Index");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("https://localhost/Account/AccessDenied?ReturnUrl=", response.Headers.Location?.OriginalString);
        Assert.DoesNotContain(UserA, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task RegistrationCannotGrantAdminRole()
    {
        using var browser = _factory.CreateBrowser();
        using var registration = await BrowserForms.PostAsync(browser, "/Account/Register", "/Account/Register", new()
        {
            ["Email"] = UserA,
            ["Password"] = _factory.Password,
            ["Role"] = "Admin"
        });
        Assert.Equal(HttpStatusCode.Redirect, registration.StatusCode);
        Assert.Equal("User", await _factory.ReadDatabaseAsync(db => db.AppUsers.Select(user => user.Role).SingleAsync()));

        await BrowserForms.LoginAsync(browser, UserA, _factory.Password);
        using var response = await browser.GetAsync("/Admin");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("https://localhost/Account/AccessDenied", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task AdminCanReadUserListAfterRealLogin()
    {
        using var factory = new UserPanelFactory(seedAdmin: true);
        using var browser = factory.CreateBrowser();
        await BrowserForms.RegisterAsync(browser, UserA, factory.Password);
        await BrowserForms.LoginAsync(browser, UserPanelFactory.AdminEmail, factory.Password);

        using var response = await browser.GetAsync("/Admin");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Panel admina", html);
        Assert.Contains(UserA, html);
        Assert.Contains(UserPanelFactory.AdminEmail, html);
        Assert.Contains("Admin", html);
        var users = await factory.ReadDatabaseAsync(db => db.AppUsers.ToListAsync());
        foreach (var user in users)
            Assert.DoesNotContain(user.PasswordHash, html);
    }

    [Fact]
    public async Task AuthenticatedPostWithoutAntiForgeryTokenIsRejected()
    {
        using var browser = await RegisterAndLoginAsync(UserA);
        using var response = await browser.PostAsync("/Dashboard/AddNote", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["NewNote.Title"] = "Missing token",
            ["NewNote.Content"] = "Should not be saved"
        }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await _factory.ReadDatabaseAsync(db => db.UserNotes.CountAsync()));
    }

    private async Task<HttpClient> RegisterAndLoginAsync(string email)
    {
        var browser = _factory.CreateBrowser();
        await BrowserForms.RegisterAsync(browser, email, _factory.Password);
        await BrowserForms.LoginAsync(browser, email, _factory.Password);
        return browser;
    }

    public void Dispose() => _factory.Dispose();
}
