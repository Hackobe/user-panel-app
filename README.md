# UserPanelApp

Prosta aplikacja ASP.NET Core MVC do rejestracji, logowania, prywatnych notatek i panelu administratora.

## Uruchomienie aplikacji

```bash
dotnet restore
dotnet user-secrets init
dotnet user-secrets set 'SeedAdmin:Email' 'admin@example.com'
dotnet user-secrets set 'SeedAdmin:Password' 'Admin12345!'
dotnet ef database update
dotnet run