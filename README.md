<div align="center">

# 💥 Project: Bloodwave Backend

[![Tech Stack](https://skillicons.dev/icons?i=cs,dotnet,mysql,vscode,github)](https://skillicons.dev)

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)](#)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-Web%20API-5C2D91?logo=dotnet&logoColor=white)](#)
[![MySQL](https://img.shields.io/badge/MySQL-8+-4479A1?logo=mysql&logoColor=white)](#)
[![EF Core](https://img.shields.io/badge/Entity%20Framework-Core-68217A?logo=.net&logoColor=white)](#)
[![JWT](https://img.shields.io/badge/Auth-JWT-000000?logo=jsonwebtokens&logoColor=white)](#)

This repository contains the ASP.NET Core backend for Project Bloodwave. It provides authentication, user management, match tracking, item and weapon catalogs, achievement handling, refresh token workflows, and a Swagger-documented REST API for web and game clients.

</div>

---

## Overview

Project Bloodwave Backend is a .NET 8 Web API built around a MySQL database using Entity Framework Core. Its main goals are:

- Provide secure JWT based authentication for clients
- Store and expose gameplay data (matches, items, weapons, achievements)
- Support account security workflows (refresh token rotation, forgot/reset password)
- Expose a clear, testable REST API with Swagger UI
- Act as the data backbone for the Bloodwave frontend portal and game integrations

---

## Highlights

- ASP.NET Core Web API on .NET 8
- Entity Framework Core + Pomelo MySQL provider
- JWT access tokens with role-based authorization
- Refresh token storage and rotation logic
- Password reset flow with SMTP email support
- Swagger/OpenAPI available under `/api/docs`
- Modular service/controller structure for easier maintenance

---

## Features

- Authentication and account lifecycle
- Register, login, logout
- Forgot password and reset password via tokenized flow
- Refresh token issue/refresh/revoke patterns

- User management
- Current user profile (`me`) endpoints
- Admin-only user read/update/delete endpoints
- Password verification before self-delete

- Game data management
- Matches with stats (time, level, damage dealt/taken, enemies killed, coins, max health)
- Item and weapon CRUD endpoints
- Achievement CRUD and user unlock tracking

- Operational endpoints
- Health-like ping endpoint (`/api/test/ping`)
- SMTP email test endpoint (`/api/test/send-mail`)

---

## Architecture and Project Layout

This backend follows a classic ASP.NET Core layered structure:

- `Controllers/` - HTTP endpoints and authorization boundaries
- `Services/` - business logic (auth, game CRUD, mail)
- `Data/` - EF Core DbContext and model relationships
- `Models/` - persistence entities
- `DTOs/` - API contracts for requests/responses
- `Extensions/` - service registration and controller helper extensions
- `Migrations/` - EF Core migration history

Core entrypoint:

- `Program.cs` configures CORS, DB context, JWT auth, Swagger, middleware, and routing

---

## API Surface (Summary)

Base route prefix: `/api`

### User (`/api/user`)

- `POST /api/user` - register
- `POST /api/user/login` - login
- `POST /api/user/forgot-password` - start password reset
- `POST /api/user/reset-password` - complete password reset
- `POST /api/user/logout` - logout current user
- `GET /api/user/me` - get current user
- `PUT /api/user/me` - update current user
- `DELETE /api/user/me` - delete current user (password required)
- `GET /api/user/name?id=...` - public username lookup
- `GET /api/user/{userId}` - admin only
- `PUT /api/user/{userId}` - admin only
- `DELETE /api/user/{userId}` - admin only

### Refresh Tokens (`/api/refreshtoken`)

- `POST /api/refreshtoken/refresh` - exchange refresh token for new tokens
- `GET /api/refreshtoken` - list own refresh tokens
- `GET /api/refreshtoken/{id}` - get own refresh token by id
- `POST /api/refreshtoken` - create refresh token
- `PUT /api/refreshtoken/{id}` - rotate refresh token
- `DELETE /api/refreshtoken/{id}` - admin only (for own user scope)

### Matches (`/api/match`)

- `GET /api/match` - list all matches (public)
- `GET /api/match/player?playerId=...` - list matches for user (public)
- `GET /api/match/{matchId}` - get own match by id
- `POST /api/match` - create match for authenticated user
- `PUT /api/match/{matchId}` - admin only
- `DELETE /api/match/{matchId}` - owner or admin

### Items (`/api/item`)

- `GET /api/item` - list items
- `GET /api/item/{itemId}` - get item
- `POST /api/item` - admin only
- `PUT /api/item/{itemId}` - admin only
- `DELETE /api/item/{itemId}` - admin only

### Weapons (`/api/weapon`)

- `GET /api/weapon` - list weapons
- `GET /api/weapon/{weaponId}` - get weapon
- `POST /api/weapon` - admin only
- `PUT /api/weapon/{weaponId}` - admin only
- `DELETE /api/weapon/{weaponId}` - admin only

### Achievements (`/api/achievment`)

Note: route/entity naming currently uses `Achievment` in code.

- `GET /api/achievment` - list achievements (public)
- `GET /api/achievment/{achievmentId}` - get achievement (public)
- `GET /api/achievment/me` - list own unlocked achievements
- `GET /api/achievment/user/{userId}` - admin only
- `POST /api/achievment` - admin only
- `PUT /api/achievment/{achievmentId}` - admin only
- `DELETE /api/achievment/{achievmentId}` - admin only
- `POST /api/achievment/{achievmentId}/unlock` - unlock for current user

### Test (`/api/test`)

- `GET /api/test/ping` - service alive check
- `POST /api/test/send-mail` - SMTP mail test

---

## Development - Getting Started

Requirements:

- .NET 8 SDK
- MySQL Server
- Optional SMTP server for mail features

Clone and restore:

```bash
git clone <your-backend-repo-url>
cd Project-Bloodwave-Backend
dotnet restore
```

Configure application settings:

- `ConnectionStrings:DefaultConnection`
- `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience`
- `Authorization:AdminUsernames` / `Authorization:AdminEmails`
- `App:PasswordResetUrl`
- `Smtp:*` settings

Apply migrations:

```bash
dotnet ef database update
```

Run the API:

```bash
dotnet run
```

Swagger docs:

- UI: `/api/docs`
- OpenAPI JSON: `/api/docs/v1/openapi.json`
- Convenience redirect: `/api`

---

## Configuration Example

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=bloodwave_game;User=bloodwave;Password=your_password;"
  },
  "Jwt": {
    "Key": "replace-with-a-long-random-secret",
    "Issuer": "BloodwaveApi",
    "Audience": "BloodwaveClient"
  },
  "Authorization": {
    "AdminUsernames": ["admin"],
    "AdminEmails": []
  },
  "App": {
    "PasswordResetUrl": "https://your-frontend/reset-password"
  },
  "Smtp": {
    "Host": "127.0.0.1",
    "Port": 25,
    "UseSsl": false,
    "UseAuthentication": false,
    "Username": "",
    "Password": "",
    "FromEmail": "project.bloodwave.web@gmail.com",
    "FromName": "Bloodwave"
  }
}
```

---

## Security Notes

- Never commit production secrets to `appsettings.json`
- Use environment variables or secret stores for JWT keys and SMTP credentials
- Use a strong random JWT key in non-dev environments
- Restrict CORS policy in production instead of `AllowAnyOrigin`
- Enable HTTPS and reverse-proxy hardening in deployment

---

## Verification Checklist

```bash
dotnet build
dotnet run
```

Then verify:

- Open `/api/docs` and confirm endpoint discovery
- Call `/api/test/ping` for basic liveliness
- Test register -> login -> refresh flow
- Create and query a match with related item/weapon data

---

## Troubleshooting

- `dotnet ef` command missing:

```bash
dotnet tool install --global dotnet-ef
```

- Database connection failures:
  - Check MySQL host/port/user/password and DB existence
  - Verify the connection string in active environment config

- `401 Unauthorized` on protected endpoints:
  - Ensure `Authorization: Bearer <token>` header is sent
  - Confirm token issuer/audience/key match backend settings

- Mail sending fails:
  - Validate `Smtp:*` config and port reachability
  - If auth is enabled, ensure username/password are set

---

<div align="center">

## Made with ❤️ — contributors

</div>
