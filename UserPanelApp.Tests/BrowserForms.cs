using System.Net;
using System.Text.RegularExpressions;

namespace UserPanelApp.Tests;

internal static class BrowserForms
{
    public static async Task<HttpResponseMessage> PostAsync(
        HttpClient browser, string formPage, string action, Dictionary<string, string> fields)
    {
        using var form = await browser.GetAsync(formPage);
        Assert.Equal(HttpStatusCode.OK, form.StatusCode);
        var html = await form.Content.ReadAsStringAsync();
        var token = Regex.Match(html,
            "<input[^>]*name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(token.Success, "The real MVC form must include an anti-forgery token.");
        fields["__RequestVerificationToken"] = WebUtility.HtmlDecode(token.Groups[1].Value);
        return await browser.PostAsync(action, new FormUrlEncodedContent(fields));
    }

    public static async Task RegisterAsync(HttpClient browser, string email, string password)
    {
        using var response = await PostAsync(browser, "/Account/Register", "/Account/Register", new()
        {
            ["Email"] = email,
            ["Password"] = password
        });
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Account/Login", response.Headers.Location?.OriginalString);
    }

    public static async Task LoginAsync(HttpClient browser, string email, string password)
    {
        using var response = await PostAsync(browser, "/Account/Login", "/Account/Login", new()
        {
            ["Email"] = email,
            ["Password"] = password
        });
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Dashboard", response.Headers.Location?.OriginalString);
        Assert.Contains(response.Headers.GetValues("Set-Cookie"), cookie => cookie.StartsWith(".AspNetCore.Cookies="));
    }

    public static async Task AddNoteAsync(HttpClient browser, string title, string content)
    {
        using var response = await PostAsync(browser, "/Dashboard", "/Dashboard/AddNote", new()
        {
            ["NewNote.Title"] = title,
            ["NewNote.Content"] = content
        });
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Dashboard", response.Headers.Location?.OriginalString);
    }
}
