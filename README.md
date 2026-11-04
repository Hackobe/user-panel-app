# User Panel

A small ASP.NET Core MVC portfolio application with cookie authentication, BCrypt password hashing, User/Admin roles and private notes. HTTP integration tests exercise the real login flow, access control and CSRF protection against an isolated SQLite database.

[View GitHub Actions runs](https://github.com/Hackobe/user-panel-app/actions/workflows/dotnet.yml)

Edukacyjna aplikacja ASP.NET Core MVC: rejestracja i logowanie, prywatne notatki oraz lista użytkowników dostępna administratorowi. Projekt portfolio, bez deklaracji wdrożenia komercyjnego.

## Dostępne funkcje

- Rejestracja konta z rolą `User`, logowanie i wylogowanie przez cookies.
- Hashowanie i weryfikacja haseł za pomocą BCrypt.
- Tworzenie notatek oraz lista notatek bieżącego użytkownika.
- Panel `Admin` z listą kont, ich ról i dat utworzenia.
- Walidacja formularzy i ochrona operacji POST tokenem anty-CSRF.
- Opcjonalne utworzenie administratora z lokalnej konfiguracji przy starcie.

Notatki nie mają operacji edycji, usuwania ani osobnej strony szczegółów. Administrator przegląda listę kont; nie ma panelu zarządzania rolami ani dostępu do wszystkich notatek.

## Technologie i organizacja kodu

.NET 8 / C#, ASP.NET Core MVC, Entity Framework Core 8, SQLite, BCrypt.Net-Next, Bootstrap oraz xUnit i `Microsoft.AspNetCore.Mvc.Testing`.

- `Controllers/AccountController.cs` — rejestracja, logowanie, cookie z identyfikatorem i rolą użytkownika.
- `Controllers/DashboardController.cs` — odczyt i dodawanie notatek; właściciel pochodzi z uwierzytelnionego cookie.
- `Controllers/AdminController.cs` — dostęp ograniczony przez `[Authorize(Roles = "Admin")]`.
- `Models/`, `ViewModels/`, `Views/` — encje, modele formularzy oraz widoki Razor.
- `Data/` i `Migrations/` — kontekst EF Core, relacja użytkownik–notatki, unikalny email i migracje.
- `UserPanelApp.Tests/` — testy HTTP rzeczywistej aplikacji i odczyt bazy w celu weryfikacji zapisu.

## Wymagania

- .NET SDK 8.0 (sam runtime nie wystarczy).
- Git i dostęp do NuGet na czas przywracania pakietów/narzędzi.
- Przeglądarka. Osobny serwer bazy danych ani zewnętrzne usługi nie są potrzebne.

## Uruchomienie od zera

```bash
git clone https://github.com/Hackobe/user-panel-app.git
cd user-panel-app
dotnet --version
dotnet tool restore
dotnet restore UserPanelApp.sln
dotnet build UserPanelApp.sln --configuration Release --no-restore
dotnet ef database update --project UserPanelApp.csproj
dotnet run --project UserPanelApp.csproj --launch-profile http
```

Otwórz [http://localhost:5113](http://localhost:5113). Profil `http` działa lokalnie w środowisku `Development`. Zatrzymanie aplikacji: `Ctrl+C`.

Manifest `.config/dotnet-tools.json` zapewnia lokalne narzędzie `dotnet-ef` w wersji zgodnej z EF Core. Polecenie migracji tworzy plik `userpanel.db` w katalogu projektu. Aplikacja również stosuje oczekujące migracje przy starcie. Baza nie jest przechowywana w Git. Dla odrębnej bazy można ustawić `ConnectionStrings__DefaultConnection`, np. na `Data Source=/pełna/ścieżka/demo.db` (katalog musi istnieć).

Załóż konto na `/Account/Register`, zaloguj się na `/Account/Login`, a następnie dodaj notatkę w `/Dashboard`. Konto z rejestracji zawsze otrzymuje rolę `User`. Domyślnie żaden administrator nie jest tworzony.

### Opcjonalny lokalny administrator

Projekt ma już `UserSecretsId`. W poniższych poleceniach zastąp wartości w nawiasach własnymi danymi demonstracyjnymi. Nie wpisuj prawdziwego hasła używanego w innym serwisie.

```bash
dotnet user-secrets set "SeedAdmin:Email" "<lokalny-email-admina>" --project UserPanelApp.csproj
dotnet user-secrets set "SeedAdmin:Password" "<lokalne-haslo-demonstracyjne>" --project UserPanelApp.csproj
dotnet run --project UserPanelApp.csproj --launch-profile http
```

Po zalogowaniu nowego administratora otwórz `/Admin`. Seeder tworzy konto tylko wtedy, gdy podany email jeszcze nie istnieje; nie podnosi uprawnień istniejącego użytkownika i nie zmienia jego hasła. `user-secrets` to lokalna konfiguracja deweloperska poza repozytorium, nie magazyn produkcyjny ani szyfrowana ochrona sekretów. Nie publikuj danych logowania. Usunięcie wpisów konfiguracyjnych nie usuwa już utworzonego konta z bazy.

```bash
dotnet user-secrets remove "SeedAdmin:Email" --project UserPanelApp.csproj
dotnet user-secrets remove "SeedAdmin:Password" --project UserPanelApp.csproj
```

## Testy i CI

Uruchom w katalogu repozytorium te same polecenia, których używa workflow:

```bash
dotnet restore UserPanelApp.sln
dotnet build UserPanelApp.sln --configuration Release --no-restore
dotnet test UserPanelApp.sln --configuration Release --no-build
```

Testy uruchamiają aplikację przez `WebApplicationFactory<Program>` / TestServer. Każdy test otrzymuje osobny plik SQLite w katalogu tymczasowym, rzeczywiste migracje, odrębne cookies klientów i tymczasowy provider Data Protection. Po teście baza jest usuwana. Środowisko `Testing` nie wczytuje deweloperskich user-secrets; konfiguracja seedera jest zastępowana, a hasła demonstracyjne generowane w pamięci. Nie jest używana lokalna baza `userpanel.db`, zewnętrzna usługa ani atrapowa tożsamość.

Sprawdzane są:

- przekierowanie niezalogowanego klienta do logowania przy odczycie `/Dashboard` i `/Admin` oraz brak możliwości zapisania notatki;
- rzeczywista rejestracja, logowanie i cookie uwierzytelnienia;
- izolacja notatek dwóch użytkowników, również przy bezpośrednim żądaniu z cudzym identyfikatorem w URL i przy ponownym wyświetleniu błędnego formularza;
- próba przesłania cudzego `AppUserId` i identyfikatora notatki do `AddNote`: cudza notatka pozostaje bez zmian, nowa należy do zalogowanego klienta;
- odmowa dostępu zwykłego użytkownika do `/Admin` i ignorowanie próby nadania sobie roli `Admin` przez formularz rejestracji;
- dostęp zalogowanego administratora do listy kont bez ujawniania hashy haseł;
- odrzucenie uwierzytelnionego POST bez tokena anty-CSRF.

Cookie authentication dla tych stron MVC zwraca **302 do logowania lub AccessDenied**, nie 401/403 JSON API. Testy nie podążają automatycznie za tymi przekierowaniami. Nie ma testów zmiany/usuwania cudzych notatek, ponieważ takie operacje nie istnieją; test przekazania cudzego ID dotyczy istniejących tras listowania i dodawania.

Workflow [`.github/workflows/dotnet.yml`](.github/workflows/dotnet.yml) wykonuje restore, build i test na .NET 8 po `push` i `pull_request`, bez prywatnych sekretów. Wyniki poszczególnych uruchomień są dostępne w [GitHub Actions](https://github.com/Hackobe/user-panel-app/actions/workflows/dotnet.yml).

## Ograniczenia wersji demonstracyjnej

- To mała aplikacja edukacyjna, bez resetowania hasła, potwierdzania emaila, MFA i ograniczania liczby prób logowania.
- Rola jest zapisywana w cookie przy logowaniu; nie ma mechanizmu natychmiastowego unieważnienia wszystkich sesji po zmianie roli.
- SQLite i migracje przy starcie upraszczają lokalne demo. Projekt nie demonstruje wdrożenia wieloinstancyjnego, kopii zapasowych ani monitoringu produkcyjnego.
- Brak stronicowania listy kont i notatek; testy HTTP nie zastępują testów przeglądarkowych ani audytu bezpieczeństwa.
- Lokalny profil HTTP służy do demonstracji. Publiczne wdrożenie wymaga osobnej konfiguracji HTTPS, bezpiecznych kluczy Data Protection i zarządzania sekretami.

## Pytania do omówienia

Dlaczego BCrypt zamiast szybkiego SHA-256 do haseł? Czym różni się uwierzytelnienie od autoryzacji? Dlaczego ukrycie linku `Admin` nie zabezpiecza endpointu? Dlaczego właściciela notatki odczytujemy z claims zamiast z formularza? Co chroni token anty-CSRF i dlaczego test musi przesłać go razem z cookie?
