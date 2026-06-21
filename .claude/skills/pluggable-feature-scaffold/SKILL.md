---
name: pluggable-feature-scaffold
description: >
  A SoftwareInventory projektben minden új pluggable funkció (provider) scaffoldingjához.
  Triggerelj, ha:
  - A felhasználó új feature-t vagy új providert kér implementálni (pl. "implement Elasticsearch provider",
    "add OAuth auth provider", "create email notification provider")
  - Valaki kéri az interfész, NullProvider, DI extension, config schema és dokumentációs stub
    együttes generálását
  - Feature flag konfigurációs kulcs hozzáadása szükséges
  Soha ne generálj csak implementációt interfész és NullProvider nélkül!
---

# Pluggable Feature Scaffold Skill

Ez a skill a SoftwareInventory projekt pluggable architektúra-mintájának betartásával
generálja egy új feature vagy provider teljes scaffoldjait.

A specifikáció szerint (P1, P2 alapelv) **minden** pluggable komponenshez az alábbiakat
kell egyszerre létrehozni:

1. Interfész (`Core/Interfaces/`)
2. NullProvider
3. Éles implementáció váz
4. DI regisztrációs extension (`Api/Extensions/`)
5. Konfigurációs kulcs (`appsettings.json` schema)
6. Dokumentációs stub-ok (`docs/developer/`, `docs/user/`)

---

## Workflow

### 1. lépés – Azonosítás

Azonosítsd:

- A feature domain-nevét (pl. `Notification`, `Search`, `DocumentStore`)
- Az interfész fő metódusait a meglévő domain alapján
- A konfiguráció szekció nevét (pl. `Features:Notifications`)
- Az első éles provider nevét (pl. `EmailNotificationProvider`)

### 2. lépés – Interfész generálása

Hely: `src/AppInventory.Core/Interfaces/I{Domain}Provider.cs`

```csharp
// src/AppInventory.Core/Interfaces/I{Domain}Provider.cs

/// <summary>
/// {Domain} provider absztrakció. Implementációk: {NullProvider}, {FirstProvider}.
/// Konfigurálható: Features:{Domain}:Provider
/// </summary>
public interface I{Domain}Provider
{
    /// <summary>Jelzi, hogy a provider elérhető és aktív-e.</summary>
    bool IsAvailable { get; }

    // TODO: domain-specifikus metódusok
    Task<{ResultType}> {MainOperation}Async(
        {ParameterType} request,
        CancellationToken ct = default);
}

// Input/output rekord típusok ugyanebben a fájlban, ha egyszerűek:
public record {ResultType}(/* mezők */)
{
    public static {ResultType} Unavailable() => new(/* üres/default értékek */);
}
```

### 3. lépés – NullProvider generálása

Hely: `src/AppInventory.Infrastructure/{Domain}/Null{Domain}Provider.cs`

```csharp
// src/AppInventory.Infrastructure/{Domain}/Null{Domain}Provider.cs

/// <summary>
/// Placeholder implementáció, ha a {domain} funkció ki van kapcsolva (Features:{Domain}:Enabled = false).
/// Soha nem dob kivételt; üres/not-available választ ad.
/// </summary>
internal sealed class Null{Domain}Provider : I{Domain}Provider
{
    public bool IsAvailable => false;

    public Task<{ResultType}> {MainOperation}Async(
        {ParameterType} request,
        CancellationToken ct)
        => Task.FromResult({ResultType}.Unavailable());
}
```

### 4. lépés – Éles provider váz generálása

Hely: `src/AppInventory.Infrastructure/{Domain}/{FirstProviderName}.cs`

```csharp
// src/AppInventory.Infrastructure/{Domain}/{FirstProviderName}.cs

internal sealed class {FirstProviderName} : I{Domain}Provider
{
    private readonly ILogger<{FirstProviderName}> _logger;
    // TODO: szükséges függőségek (pl. DbContext, HttpClient, IOptions<...>)

    public {FirstProviderName}(ILogger<{FirstProviderName}> logger /* , ... */)
    {
        _logger = logger;
        // TODO: függőségek hozzárendelése
    }

    public bool IsAvailable => true;

    public async Task<{ResultType}> {MainOperation}Async(
        {ParameterType} request,
        CancellationToken ct)
    {
        // TODO: implementáció
        throw new NotImplementedException();
    }
}
```

### 5. lépés – DI regisztrációs extension generálása

Hely: `src/AppInventory.Api/Extensions/{Domain}ServiceExtensions.cs`

```csharp
// src/AppInventory.Api/Extensions/{Domain}ServiceExtensions.cs

public static class {Domain}ServiceExtensions
{
    public static IServiceCollection Add{Domain}Provider(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var section = configuration.GetSection("Features:{Domain}");

        if (!section.GetValue<bool>("Enabled"))
        {
            services.AddSingleton<I{Domain}Provider, Null{Domain}Provider>();
            return services;
        }

        var provider = section.GetValue<string>("Provider")
            ?? throw new InvalidOperationException(
                "Features:{Domain}:Provider is required when {domain} is enabled.");

        return provider switch
        {
            "{FirstProviderKey}" => services.AddScoped<I{Domain}Provider, {FirstProviderName}>(),
            // Jövőbeli providerek ide kerülnek:
            // "{SecondProviderKey}" => services.AddScoped<I{Domain}Provider, {SecondProviderName}>(),
            _ => throw new InvalidOperationException(
                $"Unknown {domain} provider: '{provider}'. " +
                $"Valid values: {FirstProviderKey}")
        };
    }
}
```

**Program.cs-be kötelező bekerül:**

```csharp
builder.Services.Add{Domain}Provider(builder.Configuration);
```

### 6. lépés – Konfigurációs kulcs séma

Az `appsettings.json`-ban kötelező felvenni:

```json
"Features": {
  "{Domain}": {
    "Enabled": true,
    "Provider": "{FirstProviderKey}"
  }
}
```

Az `appsettings.Development.json`-ban és `appsettings.Production.json`-ban is felvenni
a megfelelő értékekkel.

### 7. lépés – Feature API végpont frissítése

A `/api/v1/config/features` endpoint visszatérési típusát frissíteni kell az új kulccsal:

```csharp
// FeatureConfigController.cs - bővíteni az új domain-nel
"{camelCaseDomain}": new { enabled = featureManager.IsEnabled("{Domain}"), provider = config[...] }
```

### 8. lépés – Dokumentációs stub-ok

Kötelező fájlok létrehozása:

**`docs/developer/{domain}/overview.md`**

```markdown
# {Domain} – Fejlesztői áttekintés

## Architektúra

TODO: rövid leírás, mire való ez a provider

## Interfész

`I{Domain}Provider` – `src/AppInventory.Core/Interfaces/I{Domain}Provider.cs`

Fő metódus: `{MainOperation}Async`

## Elérhető implementációk

| Provider kulcs       | Osztály                | Leírás                     |
| -------------------- | ---------------------- | -------------------------- |
| (disabled)           | `Null{Domain}Provider` | Feature kikapcsolt állapot |
| `{FirstProviderKey}` | `{FirstProviderName}`  | TODO: leírás               |

## Provider csere menete

1. `appsettings.json`-ban `Features:{Domain}:Provider` értékét átírni
2. Ha az új provider egyedi konfigurációt igényel, felvenni a szekciót
3. Restart
```

**`docs/developer/{domain}/configuration.md`**

```markdown
# {Domain} – Konfigurációs referencia

## Konfigurációs kulcsok

| Kulcs                             | Típus  | Alapértelmezett | Leírás                                   |
| --------------------------------- | ------ | --------------- | ---------------------------------------- |
| `Features:{Domain}:Enabled`       | bool   | `false`         | Feature bekapcsolása                     |
| `Features:{Domain}:Provider`      | string | —               | Provider neve (kötelező ha Enabled=true) |
| TODO: provider-specifikus kulcsok |        |                 |                                          |

## Példa konfiguráció

TODO: konkrét példa
```

**`docs/user/{domain-lowercase}.md`**

```markdown
# {Felhasználóbarát feature-cím}

TODO: mit csinál ez a funkció, hogyan érhető el, mikor érdemes használni.

Technikai részletek nélkül – ez felhasználói dokumentáció.
```

---

## Ellenőrző lista generálás után

A scaffold elkészülte után ellenőrizd, hogy megvan-e minden:

- [ ] `I{Domain}Provider` interfész a Core projektben
- [ ] `Null{Domain}Provider` az Infrastructure projektben
- [ ] `{FirstProviderName}` váz az Infrastructure projektben
- [ ] `Add{Domain}Provider` DI extension az Api projektben
- [ ] `Program.cs` frissítve a regisztrációval
- [ ] `appsettings.json` frissítve az új szekcióval
- [ ] `/api/v1/config/features` endpoint frissítve
- [ ] `docs/developer/{domain}/overview.md` stub létrehozva
- [ ] `docs/developer/{domain}/configuration.md` stub létrehozva
- [ ] `docs/user/{domain}.md` stub létrehozva

---

## Példa – Notification feature scaffold

A `{Domain}` = `Notification`, `{FirstProviderKey}` = `Email`,
`{FirstProviderName}` = `EmailNotificationProvider` értékekkel alkalmazva:

```csharp
// Core/Interfaces/INotificationProvider.cs
public interface INotificationProvider
{
    bool IsAvailable { get; }
    Task SendAsync(NotificationMessage message, CancellationToken ct = default);
}

public record NotificationMessage(string To, string Subject, string Body, NotificationChannel Channel);
public enum NotificationChannel { Email, Teams, Slack }
```

```json
// appsettings.json részlet
"Features": {
  "Notifications": {
    "Enabled": false,
    "Provider": "Null"
  }
}
```
