# Bloodwave Game Backend API

## 📋 Projekt Áttekintés

A **Bloodwave Game Backend** egy ASP.NET Core 8.0 Web API, amely biztosítja:
- **User Authentication**: JWT token-alapú autentikáció
- **Jelszóbiztonság**: BCrypt hash algoritmus
- **Adatbázis**: MySQL 8.0+
- **CORS**: Cross-Origin Request Support

---

## 🏗️ Architektúra

### Mappastruktúra
```
Controllers/
  └── AuthController.cs          # Login/Register endpointok
Services/
  ├── AuthService.cs             # Üzleti logika (register, login, JWT)
  └── JwtService.cs              # JWT token generálás (lehetséges refresh tokenekhez)
Models/
  ├── User.cs                    # User entitás
  └── RefreshToken.cs            # Refresh token tárolás
Data/
  └── BloodwaveDbContext.cs       # Entity Framework DbContext
DTOs/
  ├── AuthResponseDto.cs         # Válasz objektum (Success, Message, Token, User)
  ├── LoginDto.cs                # Login request (Username, Password)
  ├── RegisterDto.cs             # Register request (Username, Email, Password)
  ├── RefreshRequestDto.cs        # Refresh token request
  └── UserDto.cs                 # User adat transfer object
```

---

## 🚀 API Endpointok

### Authentication

#### 1. **Register** - Új felhasználó regisztrálása
```
POST /api/auth/register
Content-Type: application/json

{
  "username": "jani",
  "email": "jani@example.com",
  "password": "SecurePass123!"
}

Response:
{
  "success": true,
  "message": "User registered successfully",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": 1,
    "username": "jani",
    "email": "jani@example.com"
  }
}
```

#### 2. **Login** - Felhasználó bejelentkezése
```
POST /api/auth/login
Content-Type: application/json

{
  "username": "jani",
  "password": "SecurePass123!"
}

Response:
{
  "success": true,
  "message": "Login successful",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": 1,
    "username": "jani",
    "email": "jani@example.com"
  }
}
```

---

## 🔐 JWT Token

### Token Tartalom
```
Header:
{
  "alg": "HS256",
  "typ": "JWT"
}

Payload:
{
  "sub": "1",
  "name": "jani",
  "email": "jani@example.com",
  "exp": 1676892345,
  "iss": "BloodwaveApi",
  "aud": "BloodwaveClient"
}

Signature: HMACSHA256(header.payload, secret)
```

### Token Érvényessége
- **Kiállítás után**: 24 óra
- **Secret Key**: `appsettings.json` → `Jwt:Key`

### Autentikáció Használata
```
GET /api/player/stats
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

---

## 🗄️ Adatbázis Schema

### Users Tábla
```sql
CREATE TABLE Users (
  Id INT PRIMARY KEY AUTO_INCREMENT,
  Username NVARCHAR(255) UNIQUE NOT NULL,
  Email NVARCHAR(255) UNIQUE NOT NULL,
  PasswordHash NVARCHAR(MAX) NOT NULL,
  IsActive BIT DEFAULT 1,
  CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
  UpdatedAt DATETIME NULL
);
```

### RefreshTokens Tábla
```sql
CREATE TABLE RefreshTokens (
  Id INT PRIMARY KEY AUTO_INCREMENT,
  UserId INT NOT NULL,
  Token NVARCHAR(255) NOT NULL,
  CreatedAt DATETIME NOT NULL,
  ExpiresAt DATETIME NOT NULL,
  RevokedAt DATETIME NULL,
  ReplacesToken NVARCHAR(255) NULL,
  CreatedByIp NVARCHAR(45) NULL,
  UserAgent NVARCHAR(255) NULL,
  FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);
```

---

## 🔧 Telepítés és Beállítás

### 1. Előfeltételek
- .NET 8.0 SDK
- MySQL 8.0+
- Visual Studio 2022 / VS Code

### 2. Projekt klónozása
```bash
cd /home/dani/Projects/Project-Bloodwave-Backend
```

### 3. NuGet Csomagok Telepítése
```bash
dotnet restore
```

### 4. Adatbázis Beállítása
```bash
# appsettings.json módosítása
# ConnectionStrings:DefaultConnection = "Server=localhost;Port=3306;Database=bloodwave_game;User=root;Password=root;"
```

### 5. Migrations (Entity Framework)
```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 6. Szerver Indítása
```bash
dotnet run
```

API elérhető: `https://localhost:5001` vagy `http://localhost:5000`

---

## 📦 Függőségek (NuGet Csomagok)

| Csomag | Verzió | Célja |
|--------|--------|-------|
| Microsoft.EntityFrameworkCore | 8.0.0 | ORM (Adatbázis) |
| Pomelo.EntityFrameworkCore.MySql | 8.0.0 | MySQL támogatás |
| System.IdentityModel.Tokens.Jwt | 7.1.0 | JWT token generálás |
| Microsoft.IdentityModel.Tokens | 7.1.0 | Token validáció |
| BCrypt.Net-Next | 4.0.3 | Jelszóhashálás |
| Swashbuckle.AspNetCore | 6.6.2 | Swagger/OpenAPI dokumentáció |

---

## 🔍 Kód Működésének Leírása

### 1. **Regisztráció (Registration)**
```
RegisterDto → AuthController → AuthService.RegisterAsync()
    ↓
- Username/Email duplikáció ellenőrzése
- Jelszó BCrypt hash-elésa
- User rekord mentése az adatbázisba
- JWT token generálása
- AuthResponseDto visszaadása (Success, Token, User)
```

### 2. **Bejelentkezés (Login)**
```
LoginDto → AuthController → AuthService.LoginAsync()
    ↓
- User megkeresése username alapján
- Jelszó verifikálása BCrypt-tel
- IsActive státusz ellenőrzése
- JWT token generálása
- AuthResponseDto visszaadása
```

### 3. **JWT Token Generálás**
```
User → AuthService.GenerateJwtToken()
    ↓
- Claims készítése (UserId, Username, Email)
- HS256 szignálás
- Token expiration: +24 óra
- Base64 encoded string visszaadása
```

### 4. **Autentikáció (Authorization)**
```
HTTP Request + Bearer Token → Program.cs JWT Middleware
    ↓
- Token szintaxis ellenőrzése
- Szignátúra validáció
- Expiration ellenőrzése
- Claims kinyerése
- Principal objektum létrehozása
- Request továbbítása az autentikált endpoint-hoz
```

---

## 🛡️ Biztonsági Jellemzők

| Feature | Implementáció |
|---------|-----------------|
| **Jelszóbiztonság** | BCrypt hash (10 rounds) |
| **Token Aláírás** | HMACSHA256 |
| **CORS** | `AllowAnyOrigin()` (dev), később restricting |
| **HTTPS** | Redirects HTTP → HTTPS |
| **JWT Expiration** | 24 óra |
| **Database Validation** | Unique constraints (Username, Email) |

---

## 📝 Swagger/OpenAPI Dokumentáció

Indítás után nyissa meg:
```
https://localhost:5001/swagger
```

Interaktívan tesztelhetők az összes endpoint.

---

## ❌ Hibahibaadatok

### Regisztrációs hibák
```json
{
  "success": false,
  "message": "Username already exists",
  "token": null,
  "user": null
}
```

### Bejelentkezési hibák
```json
{
  "success": false,
  "message": "Invalid username or password",
  "token": null,
  "user": null
}
```

---

## 📚 Lehetséges Bővítések

- [ ] Refresh Token implementáció (Token frissítés)
- [ ] Email verifikáció (Confirmation link)
- [ ] 2FA (Two-Factor Authentication)
- [ ] Rate Limiting
- [ ] Audit Logging
- [ ] Role-Based Access Control (RBAC)
- [ ] Social Login (Google, GitHub)

---

## 📄 Licenc

Egyedi projekt - Bloodwave Game Backend

---

## 👨‍💻 Szerző

Dani - 2026. február 17.
