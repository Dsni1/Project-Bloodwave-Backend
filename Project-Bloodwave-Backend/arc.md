# 🏗️ Project Bloodwave Backend - API Architecture Guide

## 📋 Tartalomjegyzék
1. [Architektúra Áttekintése](#architektúra-áttekintése)
2. [DTO-k (Data Transfer Objects)](#dto-k)
3. [Extension Methods](#extension-methods)
4. [Service Layer](#service-layer)
5. [Controllers](#controllers)
6. [Adatbázis & Entity Framework](#adatbázis--entity-framework)
7. [Authentication & Authorization](#authentication--authorization)
8. [Best Practices](#best-practices)

---

## 🎯 Architektúra Áttekintése

### Rétegek Szerkezete
```
┌─────────────────────────────────────┐
│         CLIENT (Frontend)           │
└────────────────┬────────────────────┘
                 │ HTTP/REST
┌────────────────▼────────────────────┐
│      Controllers (API Endpoints)    │
│  - Request validation               │
│  - JWT token ellenőrzés             │
│  - Response formatting              │
└────────────────┬────────────────────┘
                 │ Service Interface
┌────────────────▼────────────────────┐
│    Services (Business Logic)        │
│  - Adatfeldolgozás                  │
│  - Üzleti logika                    │
│  - Validációk                       │
└────────────────┬────────────────────┘
                 │ Entity Framework
┌────────────────▼────────────────────┐
│    Repository (EF DbContext)        │
│  - Adatbázis operációk              │
│  - LINQ queries                     │
└────────────────┬────────────────────┘
                 │ SQL
┌────────────────▼────────────────────┐
│    Database (SQL Server/SQLite)     │
└─────────────────────────────────────┘
```

---

## 📦 DTO-k (Data Transfer Objects)

### Mi a DTO?
A DTO egy **adatcsomag**, amely az API és a kliens között utazik. Nem tároljuk az adatbázisban, hanem csak a kommunikációra használjuk.

### Miért van szükség rá?
- 🔒 **Biztonság** - Nem küldjük ki a szenzitív adatokat (jelszavak, token-ek)
- 📊 **Rugalmasság** - Az adatbázis szerkezete független az API-tól
- 🎯 **Szeparáció** - Az Entity modelleket nem tesszük közvetlen elérhetővé
- ⚡ **Teljesítmény** - Csak szükséges mezőket küldünk

### Praktikus Példa

#### ❌ ROSSZ (Entity közvetlenül)
```csharp
public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string PasswordHash { get; set; }  // ⚠️ Szenzitív!
    public string Email { get; set; }
    public DateTime CreatedAt { get; set; }
}

// API vissza adja az egész User-t - BAJ!
return Ok(user);  // Jelszó is jön!
```

#### ✅ HELYES (DTO-val)
```csharp
public class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    // PasswordHash nincs itt!
}

// API vissza adja csak a szükséges adatokat
var userDto = new UserDto 
{ 
    Id = user.Id,
    Username = user.Username,
    Email = user.Email
};
return Ok(userDto);
```

### A Projekt DTO-i

#### 1️⃣ **PlayerStatsDto**
```csharp
public class PlayerStatsDto
{
    public int UserId { get; set; }
    public int TotalKills { get; set; }
    public int HighestLevel { get; set; }
    public int TotalMatches { get; set; }
    public double AverageScore { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```
**Használat:** GET `/api/player/stats` - Játékos statisztikáinak lekérdezése

---

#### 2️⃣ **MatchDto**
```csharp
public class MatchDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int Time { get; set; }
    public int Level { get; set; }
    public int MaxHealth { get; set; }
    public string Weapon1 { get; set; }
    public string Weapon2 { get; set; }
    public string Weapon3 { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<int> ItemIds { get; set; }
}
```
**Használat:** GET `/api/player/matches` - Egy játékos összes meccse

---

#### 3️⃣ **CreateMatchDto**
```csharp
public class CreateMatchDto
{
    [Required]
    public int Time { get; set; }
    
    [Required]
    public int Level { get; set; }
    
    [Required]
    public int MaxHealth { get; set; }
    
    public string Weapon1 { get; set; }
    public string Weapon2 { get; set; }
    public string Weapon3 { get; set; }
    
    public List<int> ItemIds { get; set; }
}
```
**Használat:** POST `/api/player/match` - Új meccs létrehozása
**Eltérés a MatchDto-tól:** Nincs `Id`, `UserId`, `CreatedAt` (ezek automatikusan generálódnak)

---

#### 4️⃣ **LeaderboardEntryDto**
```csharp
public class LeaderboardEntryDto
{
    public int Rank { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; }
    public int TotalKills { get; set; }
    public int HighestLevel { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```
**Használat:** GET `/api/player/leaderboard` - Globális rangsor

---

#### 5️⃣ **AuthDto-k (Autentifikáció)**
```csharp
public class RegisterDto
{
    [Required, MinLength(3)]
    public string Username { get; set; }
    
    [Required, EmailAddress]
    public string Email { get; set; }
    
    [Required, MinLength(8)]
    public string Password { get; set; }
}

public class LoginDto
{
    [Required]
    public string Email { get; set; }
    
    [Required]
    public string Password { get; set; }
}

public class AuthResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public string Token { get; set; }  // JWT
    public UserDto User { get; set; }
}
```

---

## 🔧 Extension Methods

### Mi az Extension Method?
Egy metódus, amely egy **már létező osztályhoz** adunk hozzá extra funkcionalitást, anélkül hogy módosítanánk az eredeti osztályt.

### Szintaxis
```csharp
public static ReturnType MethodName(this TargetClass parameter)
{
    // logika
}
```

### A Projekt Extension Methodjai

#### 1️⃣ **ControllerExtensions.cs**
```csharp
public static class ControllerExtensions
{
    /// <summary>
    /// JWT token-ből kinyeri a userId-t és validálja
    /// </summary>
    public static ActionResult ValidateAndGetUserId(
        this ControllerBase controller, 
        out int userId)
    {
        userId = 0;
        
        var userIdClaim = controller.User
            .FindFirst(ClaimTypes.NameIdentifier);
        
        if (userIdClaim == null)
            return controller.Unauthorized(
                new { message = "User ID not found in token" });
        
        if (!int.TryParse(userIdClaim.Value, out userId))
            return controller.BadRequest(
                new { message = "Invalid user ID format" });
        
        return null;  // Nincs hiba
    }
}
```

**Hogyan Használjuk a Controllerben?**
```csharp
[HttpGet("stats")]
public async Task<ActionResult<PlayerStatsDto>> GetStats()
{
    // Extension method hívása
    var validationError = this.ValidateAndGetUserId(out int userId);
    
    if (validationError != null)
        return validationError;  // Hiba esetén azonnal vissza
    
    // userId már validált és parsed
    var stats = await _playerService.GetPlayerStatsAsync(userId);
    return Ok(stats);
}
```

**Előnyei:**
- ✅ Kódismétlődés elkerülése
- ✅ Tiszta, olvasható kód
- ✅ Egy helyről kezelhető a validáció

---

#### 2️⃣ **ServiceExtensions.cs**
```csharp
public static class ServiceExtensions
{
    /// <summary>
    /// Regisztrálja az összes Service-t a DI container-ben
    /// </summary>
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPlayerService, PlayerService>();
        
        // JWT konfigurálása
        var jwtSettings = configuration.GetSection("JwtSettings");
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.ASCII.GetBytes(jwtSettings["SecretKey"])),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    RequireExpirationTime = true
                };
            });
        
        // CORS konfigurálása
        services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", builder =>
            {
                builder.WithOrigins("http://localhost:3000")
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });
        
        return services;
    }
}
```

**Hogyan Használjuk a Program.cs-ben?**
```csharp
var builder = WebApplicationBuilder.CreateBuilder(args);

// Extension method hívása
builder.Services.AddApplicationServices(builder.Configuration);

var app = builder.Build();

// ...
```

**Előnyei:**
- ✅ Program.cs nagyon tiszta marad
- ✅ Minden konfigurálás egy helyen
- ✅ Könnyű tesztelni
- ✅ Újrafelhasználható

---

## 🛠️ Service Layer

### Mi a Service?
A **Service** tartalmazza az üzleti logikát. Az olyan operációkat, amelyeket az adatok manipulálásához szükséges.

### Service vs Controller
```
Controller     → "Mit csináljon a felhasználó?"
                 (HTTP request kezelése, routing)

Service        → "Hogyan csináljon?"
                 (Üzleti logika, validációk, számítások)

Repository     → "Honnan szerzem az adatokat?"
                 (Adatbázis queries)
```

### IPlayerService Interface

```csharp
public interface IPlayerService
{
    Task<PlayerStatsDto> GetPlayerStatsAsync(int userId);
    Task<MatchDto> CreateMatchAsync(int userId, CreateMatchDto createMatchDto);
    Task<List<MatchDto>> GetAllMatchesAsync(int userId);
    Task<MatchDto> GetMatchByIdAsync(int matchId, int userId);
    Task<List<LeaderboardEntryDto>> GetLeaderboardAsync(int limit);
}
```

**Miért interface?**
- 🔄 Dependency Injection
- 🧪 Könnyebb tesztelni (mock service-ek)
- 🔒 Contract - garancia az implementációra

---

### PlayerService Implementáció

#### Fő Metódusok

##### 1️⃣ **GetPlayerStatsAsync**
```csharp
public async Task<PlayerStatsDto> GetPlayerStatsAsync(int userId)
{
    var stats = await _context.PlayerStats
        .FirstOrDefaultAsync(ps => ps.UserId == userId);
    
    if (stats == null)
        return null;
    
    return new PlayerStatsDto
    {
        UserId = stats.UserId,
        TotalKills = stats.TotalKills,
        HighestLevel = stats.HighestLevel,
        TotalMatches = stats.TotalMatches,
        AverageScore = stats.TotalMatches > 0 
            ? stats.TotalKills / (double)stats.TotalMatches 
            : 0,
        UpdatedAt = stats.UpdatedAt
    };
}
```

**Logika:**
1. Lekérdez az adatbázisból
2. Null check (ha nincs stats)
3. Entity → DTO konvertálás
4. Átlagérték kiszámítása

---

##### 2️⃣ **CreateMatchAsync**
```csharp
public async Task<MatchDto> CreateMatchAsync(
    int userId, 
    CreateMatchDto createMatchDto)
{
    // 1. Új Match entity létrehozása
    var match = new Match
    {
        UserId = userId,
        Time = createMatchDto.Time,
        Level = createMatchDto.Level,
        MaxHealth = createMatchDto.MaxHealth,
        Weapon1 = createMatchDto.Weapon1,
        Weapon2 = createMatchDto.Weapon2,
        Weapon3 = createMatchDto.Weapon3,
        CreatedAt = DateTime.UtcNow
    };
    
    // 2. Item linkek létrehozása
    if (createMatchDto.ItemIds?.Any() == true)
    {
        match.MatchItems = createMatchDto.ItemIds
            .Select(itemId => new MatchItem 
            { 
                ItemId = itemId 
            })
            .ToList();
    }
    
    // 3. Mentés az adatbázisba
    _context.Matches.Add(match);
    await _context.SaveChangesAsync();
    
    // 4. Entity → DTO konvertálás
    return MapMatchToDto(match);
}
```

**Lépések:**
1. DTO-ból Entity létrehozása
2. Relációk beállítása (ItemIds)
3. Adatbázisba mentés
4. DTO visszaadása

---

##### 3️⃣ **GetLeaderboardAsync**
```csharp
public async Task<List<LeaderboardEntryDto>> GetLeaderboardAsync(int limit)
{
    var leaderboard = await _context.PlayerStats
        .OrderByDescending(ps => ps.TotalKills)           // Elsősorban kills
        .ThenByDescending(ps => ps.HighestLevel)          // Másodsorban level
        .Take(limit)
        .Select(ps => new LeaderboardEntryDto
        {
            Rank = 0,  // Később számítjuk
            UserId = ps.UserId,
            Username = ps.User.Username,
            TotalKills = ps.TotalKills,
            HighestLevel = ps.HighestLevel,
            UpdatedAt = ps.UpdatedAt
        })
        .ToListAsync();
    
    // Rank számítása
    for (int i = 0; i < leaderboard.Count; i++)
    {
        leaderboard[i].Rank = i + 1;
    }
    
    return leaderboard;
}
```

**Logika:**
1. PlayerStats lekérdezése JOINS-sal (User username-hez)
2. Rendezés: Kills → Level
3. Limit: Top N
4. Entity → DTO konvertálás
5. Rank hozzáadása

---

## 🎮 Controllers

### Mi a Controller?
A **Controller** a HTTP requestek belépési pontja. Az olyan metódusok, amely útvonalakat (routes) definiálnak.

### PlayerController Struktúra

```csharp
[ApiController]
[Route("api/[controller]")]        // /api/player
[Authorize]                         // JWT token szükséges
public class PlayerController : ControllerBase
{
    private readonly IPlayerService _playerService;
    
    public PlayerController(IPlayerService playerService) 
        => _playerService = playerService;
    
    // Endpointok...
}
```

**Dekorációk magyarázata:**
- `[ApiController]` - Ez egy API controller (automatikus validáció)
- `[Route("api/[controller]")]` - Alap útvonal
- `[Authorize]` - Összes endpoint JWT-vel védett (alapértelmezett)

---

### Endpoints Részletezése

#### 1️⃣ **GET /api/player/stats**
```csharp
[HttpGet("stats")]
public async Task<ActionResult<PlayerStatsDto>> GetStats()
{
    // 1. JWT validáció
    var validationError = this.ValidateAndGetUserId(out int userId);
    if (validationError != null)
        return validationError;
    
    // 2. Service hívása
    var stats = await _playerService.GetPlayerStatsAsync(userId);
    
    // 3. Null check
    if (stats == null)
        return NotFound(new { message = "Player stats not found" });
    
    // 4. Válasz
    return Ok(stats);
}
```

**HTTP Request:**
```
GET /api/player/stats HTTP/1.1
Authorization: Bearer <JWT_TOKEN>
```

**Válasz (200 OK):**
```json
{
  "userId": 5,
  "totalKills": 250,
  "highestLevel": 45,
  "totalMatches": 25,
  "averageScore": 10.0,
  "updatedAt": "2026-02-19T10:30:00Z"
}
```

---

#### 2️⃣ **POST /api/player/match**
```csharp
[HttpPost("match")]
public async Task<ActionResult<MatchDto>> CreateMatch(
    [FromBody] CreateMatchDto createMatchDto)
{
    // 1. DTO validáció (DataAnnotations)
    if (!ModelState.IsValid)
        return BadRequest(ModelState);
    
    // 2. JWT validáció
    var validationError = this.ValidateAndGetUserId(out int userId);
    if (validationError != null)
        return validationError;
    
    // 3. Service hívása
    var match = await _playerService.CreateMatchAsync(userId, createMatchDto);
    
    // 4. 201 Created válasz
    return CreatedAtAction(nameof(GetMatch), 
        new { matchId = match.Id }, 
        match);
}
```

**HTTP Request:**
```
POST /api/player/match HTTP/1.1
Authorization: Bearer <JWT_TOKEN>
Content-Type: application/json

{
  "time": 1500,
  "level": 42,
  "maxHealth": 150,
  "weapon1": "Sword",
  "weapon2": "Shield",
  "weapon3": "Lightning",
  "itemIds": [1, 2, 3]
}
```

**Válasz (201 Created):**
```json
{
  "id": 123,
  "userId": 5,
  "time": 1500,
  "level": 42,
  "maxHealth": 150,
  "weapon1": "Sword",
  "weapon2": "Shield",
  "weapon3": "Lightning",
  "createdAt": "2026-02-19T10:30:00Z",
  "itemIds": [1, 2, 3]
}
```

---

#### 3️⃣ **GET /api/player/leaderboard**
```csharp
[HttpGet("leaderboard")]
[AllowAnonymous]  // ← JWT token NEM szükséges!
public async Task<ActionResult<List<LeaderboardEntryDto>>> GetLeaderboard(
    [FromQuery] int limit = 100)
{
    // 1. Limit validáció
    if (limit <= 0 || limit > 1000)
        limit = 100;
    
    // 2. Service hívása
    var leaderboard = await _playerService.GetLeaderboardAsync(limit);
    
    // 3. Válasz
    return Ok(leaderboard);
}
```

**HTTP Request:**
```
GET /api/player/leaderboard?limit=50 HTTP/1.1
```

**Válasz (200 OK):**
```json
[
  {
    "rank": 1,
    "userId": 5,
    "username": "ProGamer",
    "totalKills": 1250,
    "highestLevel": 87,
    "updatedAt": "2026-02-19T10:30:00Z"
  },
  {
    "rank": 2,
    "userId": 3,
    "username": "NinjaPlayer",
    "totalKills": 1100,
    "highestLevel": 82,
    "updatedAt": "2026-02-19T09:15:00Z"
  }
]
```

---

## 💾 Adatbázis & Entity Framework

### Entity Relationships

```
┌──────────────┐         ┌──────────────┐
│    User      │         │ PlayerStats  │
├──────────────┤         ├──────────────┤
│ Id (PK)      │◄────────│ UserId (FK)  │
│ Username     │    1:1  │ TotalKills   │
│ Email        │         │ HighestLevel │
│ PasswordHash │         │ TotalMatches │
└──────────────┘         └──────────────┘


┌──────────────┐         ┌──────────────┐
│    User      │         │    Match     │
├──────────────┤         ├──────────────┤
│ Id (PK)      │◄────────│ UserId (FK)  │
│ Username     │    1:N  │ Id (PK)      │
│ Email        │         │ Time         │
│ ...          │         │ Level        │
└──────────────┘         └──────────────┘
                               │
                               │ 1:N
                               │
                         ┌──────────────┐
                         │  MatchItem   │
                         ├──────────────┤
                         │ MatchId (FK) │
                         │ ItemId (FK)  │
                         └──────────────┘
```

### LINQ & Entity Framework Queries

#### ❌ N+1 Query Problem
```csharp
// ROSSZ - Sok query!
var stats = _context.PlayerStats.ToList();

foreach (var stat in stats)
{
    var user = _context.Users
        .FirstOrDefault(u => u.Id == stat.UserId);  // ← Extra query!
    Console.WriteLine(user.Username);
}
// 1 PlayerStats query + N User query = N+1 query!
```

#### ✅ Include (Eager Loading)
```csharp
// JÓ - Egy query!
var stats = await _context.PlayerStats
    .Include(ps => ps.User)  // ← JOIN az adatbázisban
    .ToListAsync();

foreach (var stat in stats)
{
    Console.WriteLine(stat.User.Username);  // Már loaded!
}
// Csak 1 query!
```

---

## 🔐 Authentication & Authorization

### JWT Token Flow

```
┌─────────────────────────────────────────────────────────────┐
│ 1. REGISZTRÁCIÓ                                             │
│    POST /api/auth/register                                  │
│    { username, email, password }                            │
│                          ↓                                  │
│ 2. Jelszó Hash (BCrypt)                                     │
│    Password → PasswordHash (irreversible)                   │
│                          ↓                                  │
│ 3. User & PlayerStats Mentés                               │
│    Adatbázisba mentés                                       │
│                          ↓                                  │
│ 4. Visszatérés                                              │
│    { success: true, token: "...", user: {...} }            │
└─────────────────────────────────────────────────────────────┘


┌─────────────────────────────────────────────────────────────┐
│ 5. BEJELENTKEZÉS                                            │
│    POST /api/auth/login                                     │
│    { email, password }                                      │
│                          ↓                                  │
│ 6. Jelszó Ellenőrzés                                        │
│    Password vs PasswordHash (BCrypt)                        │
│                          ↓                                  │
│ 7. JWT Token Generálás                                      │
│    Claims: userId, username, email                         │
│    Aláírás: SecretKey                                       │
│    Lejárat: 24 órás                                         │
│                          ↓                                  │
│ 8. Token Visszaadása                                        │
│    eyJhbGc... (hosszú string)                               │
└─────────────────────────────────────────────────────────────┘


┌─────────────────────────────────────────────────────────────┐
│ 9. VÉDETT ENDPOINT HÍVÁSA                                   │
│    GET /api/player/stats                                    │
│    Authorization: Bearer eyJhbGc...                         │
│                          ↓                                  │
│ 10. Token Validáció                                         │
│     - Aláírás ellenőrzése                                   │
│     - Lejárati idő ellenőrzése                              │
│     - Claims kinyerése                                      │
│                          ↓                                  │
│ 11. UserId Kinyerése                                        │
│     Claims → NameIdentifier → userId                       │
│                          ↓                                  │
│ 12. Logika & Válasz                                         │
│     Service hívása userId-vel                              │
└─────────────────────────────────────────────────────────────┘
```

### JWT Token Szerkezete

```
eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzd WIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9l IiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c

└─────────────────────────────────────────────────────────┬─────────────────────────────────────────────────────────┘
                                                         │
                                          ┌──────────────┴──────────────┐
                                          │                             │
                                    ┌─────▼─────┐             ┌────────▼────────┐
                                    │  HEADER   │             │  PAYLOAD        │
                                    ├───────────┤             ├─────────────────┤
                                    │ alg: HS256│             │ sub: userId     │
                                    │ typ: JWT  │             │ name: username  │
                                    └───────────┘             │ email: email    │
                                                              │ iat: 1234567890 │
                                                              │ exp: 1234654290 │
                                                              └─────────────────┘
                                                                      │
                                                                      │
                                                              ┌───────▼─────────┐
                                                              │  SIGNATURE      │
                                                              ├─────────────────┤
                                                              │ HMAC-SHA256(    │
                                                              │  header.payload,│
                                                              │  secretKey      │
                                                              │ )               │
                                                              └─────────────────┘
```

### ApSettings.json Konfigurálása

```json
{
  "JwtSettings": {
    "SecretKey": "your-very-long-secret-key-min-32-chars",
    "ExpirationInMinutes": 1440,
    "Issuer": "BloodwaveAPI",
    "Audience": "BloodwaveClients"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=bloodwave;User Id=sa;Password=..."
  }
}
```

---

## ✅ Best Practices

### 1️⃣ **SOLID Principles**
```csharp
// ❌ ROSSZ - Tight coupling
public class PlayerController
{
    private PlayerService _service = new PlayerService();  // Direkt instantiáció
}

// ✅ JОТА - Dependency Injection
public class PlayerController
{
    private readonly IPlayerService _service;
    
    public PlayerController(IPlayerService service)  // Interface függőség
        => _service = service;
}
```

### 2️⃣ **Error Handling**
```csharp
// ✅ Megfelelő HTTP státuszkódok
try
{
    var result = await _playerService.GetPlayerStatsAsync(userId);
    
    if (result == null)
        return NotFound(new { message = "Player not found" });  // 404
    
    return Ok(result);  // 200
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error occurred");
    return StatusCode(500, new { message = "Internal server error" });  // 500
}
```

### 3️⃣ **Logging**
```csharp
private readonly ILogger<PlayerService> _logger;

public PlayerService(ILogger<PlayerService> logger)
    => _logger = logger;

public async Task<MatchDto> CreateMatchAsync(int userId, CreateMatchDto dto)
{
    _logger.LogInformation("Creating match for user {UserId}", userId);
    
    // Logika...
    
    _logger.LogInformation("Match created successfully. MatchId: {MatchId}", match.Id);
}
```

### 4️⃣ **Async/Await**
```csharp
// ✅ JOTA - Async az egész stack-ben
public async Task<ActionResult<PlayerStatsDto>> GetStats()
{
    var stats = await _playerService.GetPlayerStatsAsync(userId);
    return Ok(stats);
}

public async Task<PlayerStatsDto> GetPlayerStatsAsync(int userId)
{
    var stats = await _context.PlayerStats
        .FirstOrDefaultAsync(ps => ps.UserId == userId);
    return MapToDto(stats);
}
```

### 5️⃣ **Input Validation**
```csharp
// DTO-val
public class CreateMatchDto
{
    [Required(ErrorMessage = "Time is required")]
    [Range(1, int.MaxValue)]
    public int Time { get; set; }
    
    [Required]
    [Range(1, 100)]
    public int Level { get; set; }
}

// Controller-ben
if (!ModelState.IsValid)
    return BadRequest(ModelState);  // Automatikus validáció
```

### 6️⃣ **Security Best Practices**
```csharp
// ✅ Jelszó hashing (BCrypt)
public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
{
    var hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password);
    var user = new User 
    { 
        Username = dto.Username,
        Email = dto.Email,
        PasswordHash = hashedPassword  // Nem plain text!
    };
    
    await _context.Users.AddAsync(user);
    await _context.SaveChangesAsync();
}

// ✅ JWT aláírás
var securityKey = new SymmetricSecurityKey(
    Encoding.ASCII.GetBytes(configuration["JwtSettings:SecretKey"]));

var token = new JwtSecurityToken(
    issuer: configuration["JwtSettings:Issuer"],
    audience: configuration["JwtSettings:Audience"],
    claims: claims,
    expires: DateTime.UtcNow.AddHours(24),
    signingCredentials: new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256)
);
```

---

## 🎓 Összefoglalás

| Komponens | Felelőssége | Példa |
|-----------|------------|--------|
| **Controller** | HTTP request kezelése | PlayerController |
| **Service** | Üzleti logika | PlayerService |
| **Repository** | Adatbázis operációk | DbContext |
| **DTO** | Adat transzfer | PlayerStatsDto |
| **Extension** | Kódismétlődés csökkentése | ValidateAndGetUserId |
| **Entity** | Adatbázis tábla | User, Match |

---

## 📚 Hasznos Linkek
- [Microsoft - ASP.NET Core](https://docs.microsoft.com/aspnet/)
- [Entity Framework Core](https://docs.microsoft.com/ef/)
- [JWT](https://jwt.io/)
- [SOLID Principles](https://en.wikipedia.org/wiki/SOLID)

---

**Utolsó frissítés:** 2026. február 19.
**Verzió:** 1.0