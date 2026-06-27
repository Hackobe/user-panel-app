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
```

Aplikacja uruchamia się lokalnie, na przykład pod adresem:
```bash
http://localhost:5113
```

### Użytkownik testowy
Zwykłego użytkownika można utworzyć przez stronę:
```bash
/Account/Register
```
Nowy użytkownik dostaje rolę User.

### Konto administratora
Administrator jest tworzony lokalnie przy starcie aplikacji na podstawie user-secrets.
Dane przykładowe:
```bash
Email: admin@example.com
Hasło: Admin12345!
```
To dane tylko do lokalnego testowania.

### Hashowanie haseł
Hasła są hashowane przez BCrypt.Net-Next.
Kod zapisu hasła znajduje się w:
```bash
Controllers/AccountController.cs
```
Przy rejestracji używana jest metoda:
```bash
BCrypt.Net.BCrypt.HashPassword(...)
```
Przy logowaniu hasło jest sprawdzane przez:
```bash
BCrypt.Net.BCrypt.Verify(...)
```
### Uwierzytelnianie
Konfiguracja cookie authentication znajduje się w:
```bash
Program.cs
```
Aplikacja używa:
```bash
AddAuthentication(...)
AddCookie(...)
HttpContext.SignInAsync(...)
HttpContext.SignOutAsync(...)
```
### Autoryzacja
Panel użytkownika jest zabezpieczony przez:
```bash
[Authorize]
```
w pliku:
```bash
Controllers/DashboardController.cs
```
Panel administratora jest zabezpieczony przez:
```bash
[Authorize(Roles = "Admin")]
```
w pliku:
```bash
Controllers/AdminController.cs
```
## Odpowiedzi na pytania
### Dlaczego nie wolno przechowywać haseł w postaci jawnej?
Po wycieku bazy danych ktoś mógłby od razu odczytać hasła użytkowników i użyć ich w tej albo innej aplikacji.
### Dlaczego sam SHA-256 nie jest dobrym wyborem do haseł?
SHA-256 jest zbyt szybki. Atakujący może bardzo szybko sprawdzać ogromną liczbę haseł. Do haseł lepsze są mechanizmy takie jak BCrypt, które są wolniejsze i mają wbudowaną sól.
### Po co używa się soli?
Sól powoduje, że dwa identyczne hasła mają inne hashe. Utrudnia to używanie gotowych tablic hashy.
### Czym różni się sól od pieprzu?
Sól jest zwykle zapisywana razem z hashem w bazie. Pieprz to dodatkowy sekret trzymany poza bazą, na przykład w konfiguracji środowiska.
### Czym różni się uwierzytelnienie od autoryzacji?
Uwierzytelnienie sprawdza, kim jest użytkownik. Autoryzacja sprawdza, czy ten użytkownik ma dostęp do konkretnego zasobu.
### Dlaczego ukrycie linku w widoku nie wystarcza jako zabezpieczenie?
Bo użytkownik może wpisać adres ręcznie. Zabezpieczenie musi być po stronie kontrolera, na przykład przez [Authorize].
### Dlaczego komunikat „nie ma takiego użytkownika” przy logowaniu może być problemem?
Bo pozwala sprawdzać, które adresy email istnieją w systemie. Bezpieczniej pokazywać ogólny komunikat, np. „Nieprawidłowy email lub hasło”.