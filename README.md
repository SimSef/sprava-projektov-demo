# Správa projektov – Testovací príklad

- Čas realizácie: ~5 hodiny

## Spustenie

1. Naklonovať repozitár
```bash
git clone https://github.com/SimSef/sprava-projektov-demo.git
```

2. Prejsť do priečinka
```bash
cd sprava-projektov-demo
```

3. Build Docker image
```bash
docker build -t sprava-projektov:local .
```

4. Spustiť kontajner
```bash
docker run --rm -it -p 8080:8080 --name sprava-projektov sprava-projektov:local
```

5. Otvoriť aplikáciu: http://localhost:8080/login

## Prehľad
- Používame: .NET 9
- Použitá technológia: Blazor Web App (`dotnet new blazor`)
- Dôvod voľby: celé UI aj logika v C#, bez miešania JS, jednoduchšia autentifikácia (cookies, roly), všetko beží na serveri – rýchlejšia a bezpečnejšia implementácia.

## Autentifikácia a oprávnenia
- Cookie-based autentifikácia s rolami: `Admin` a `User`.
- Oprávnenia:
  - Admin: vytvárať, upravovať a mazať projekty.
  - User: len prehliadať zoznam projektov.
- Prihlasovacie údaje sú zobrazené priamo na stránke prihlásenia (`/login`).
- Odhlásenie: tlačidlo „Odhlásiť sa“ v ľavom paneli dole (vymaže auth cookie).

## Úložisko a konfigurácia
- Údaje sú v XML súboroch (bez DB):
  - Projekty: `SpravaProjektov/Data/XML/projects.xml` (Windows-1250)
  - Používatelia: `SpravaProjektov/Data/XML/users.xml`
- Konfigurácia (cesty k XML): `SpravaProjektov/config/app.config.xml`, načítané cez `AddXmlFile(...)`.
- Podpora kódovania Windows-1250: registrovaný `System.Text.Encoding.CodePages` provider kvôli súboru `projects.xml`.

## Architektúra (3 vrstvy)
- Prezentačná: `SpravaProjektov/Presentation` (stránky, layouty, komponenty)
- Aplikačná: `SpravaProjektov/Application` (rozhrania, modely/doména, konfigurácia)
- Dátová: `SpravaProjektov/Data/XML` (XML modely + repository implementácie)
- Vrstvy sú oddelené cez rozhrania (`IProjectRepository`, `IAuthRepository`) a DI, aby sa dalo jednoducho nahradiť XML iným úložiskom (DB/REST/Cloud).

## Validácia, ergonómia a štýl
- Validácia vstupov cez `EditForm` + `DataAnnotations` (napr. `Presentation/Pages/Login.razor`, `Presentation/Pages/Projects.razor`).
- Štýl: Bootstrap + drobný custom CSS; konzistentné rozloženie, jasné popisky, zrozumiteľné chybové hlášky.

## Logovanie
- Použité `ILogger<>` v kľúčových častiach (auth, repos, stránky); logujú sa udalosti na úrovniach Debug/Information/Warning/Error pre jednoduchšie ladenie.

## Poznámky k rozšíriteľnosti
- Vymeniteľné úložisko cez DI a rozhrania; presun na DB vyžaduje len novú implementáciu repository.
