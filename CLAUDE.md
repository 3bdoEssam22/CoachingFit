# CoachingFit — Claude Code Context

## What This Is
You are reviewing and working on **CoachingFit**, a commission-based fitness marketplace built on .NET 10 Microservices. Two Flutter mobile apps (Coach + Trainee) consume this backend through a single YARP gateway.

**Do NOT assume code is missing or broken without reading the actual files first.** Always `cat` or open the file before claiming something is wrong.

---

## Current State — What's Built and Working

### Sprint 1 — Identity Service (port 7272) ✅ COMPLETE
### Sprint 2 — API Gateway / YARP (port 5001) ✅ COMPLETE
### Sprint 3 — User Service (port 7024) ✅ COMPLETE
### Sprint 4 — Coach Certificates (User Service) ✅ COMPLETE

All sprints are merged and tested. The backend is stable.

---

## Architecture — Non-Negotiable Rules

These are deliberate decisions, not bugs. Do NOT flag them as issues:

### Onion Architecture — 6 Projects Per Service
```
API → Controllers, Program.cs, Extensions
Core → Entities, Enums, Contracts/Interfaces
Shared → DTOs (Requests, Responses), Wrappers
Services.Abstraction → Service interfaces
Services → Service implementations, Validators
Infrastructure → DbContext, Migrations, External services
```

### Package Placement
- Core gets `Microsoft.EntityFrameworkCore` (base only) — NEVER the SqlServer provider
- Infrastructure gets `Microsoft.EntityFrameworkCore.SqlServer`
- This is correct. Do not suggest moving packages.

### DbContext Pattern
- No Unit of Work, no Generic Repository — this is intentional
- Direct `IDbContext` interface injection (same as Microsoft eShopOnContainers)
- `IUserDbContext` exposes `IQueryable<T>` not `DbSet<T>` — this is a deliberate abstraction

### GenericResponse<T>
Every endpoint returns `GenericResponse<T>`. This is the pattern. Do not suggest alternatives.

---

## Identity Service — Current Design Decisions (NOT BUGS)

### Roles — ASP.NET Identity Only
`ApplicationUser` has NO `UserRole` property. Roles come exclusively from ASP.NET Identity via `GetRolesAsync` / `IsInRoleAsync` / `GetUsersInRoleAsync`. The `UserRole` column was dropped in migration `20260510080000_DropUserRoleColumn`.

### IsActive Check in Login
`IsActive` is **NOT** checked during login. Inactive coaches receive a valid JWT. The `isActive` field is returned in `AuthResponse` and the Flutter app uses it to route to `/pending-approval`. This is intentional — coaches need a token to reach the pending screen and upload certificates during the approval flow.

### Login Gate Order
The actual order in `AuthService.LoginAsync` is:
```
1. User exists? → 401
2. IsLockedOut? → 403
3. Password correct? → AccessFailedAsync + 401 if wrong
4. ResetAccessFailedCount
5. EmailConfirmed? → 401
6. Generate token (isActive returned in payload, not enforced here)
```
This is the intended order. Do NOT suggest reordering or re-adding an IsActive gate here.

### JWT — 15-Minute Access Tokens + Single-Use Rotating Refresh Tokens
- Access token: 15 minutes (`Jwt:AccessTokenDurationInMinutes`).
- Refresh token: 7-day sliding, 90-day absolute (`Jwt:RefreshTokenDurationInDays`, `Jwt:RefreshTokenAbsoluteExpiryInDays`).
- Refresh tokens are 64 random bytes, base64url-encoded, returned in the JSON body. Stored in DB as SHA-256 hex hashes — never in plaintext.
- Single-use rotation: every successful `/api/Auth/refresh` invalidates the old refresh token and issues a new one. The 90-day absolute cap does NOT extend on rotation.
- Theft detection: presenting a *revoked* (already-rotated) refresh token revokes all of that user's active refresh tokens — they must re-login.
- `/api/Auth/revoke` logs the user out by revoking the supplied refresh token (idempotent, always returns 200).
- Admin-driven session revocation is not exposed yet (Wallet/Order sprint).

### Email — TrySendConfirmationEmailAsync
Returns `bool`. Catches known SMTP / network / mime-parse failures (MailKit `ServiceNotConnectedException`, `ServiceNotAuthenticatedException`, `AuthenticationException`, `SmtpCommandException`, `SmtpProtocolException`, plus `SocketException`, `IOException`, `TimeoutException`, `MimeKit.ParseException`) and returns `false` so registration still succeeds (graceful degradation). Unexpected exceptions (programming bugs) bubble up — this is intentional so real defects aren't silently swallowed. The activation email send in `ActivateCoachAsync` follows the same narrowed-catch pattern.

### Cloudinary — No public_id Persistence
We store only `SecureUrl`. Old photos are orphaned on replacement. This is a known deferral, not a bug. It will be addressed later.

### Idempotency-Key System
Protects 5 high-risk POST endpoints from duplicate execution (network retries, race conditions):

| Service | Endpoint | Risk mitigated |
|---|---|---|
| Identity | `POST /api/Auth/register/coach` | Duplicate accounts on retry |
| Identity | `POST /api/Auth/register/trainee` | Duplicate accounts on retry |
| Identity | `POST /api/Auth/refresh` | Refresh-token reuse-detection race (double-tap wipes chain) |
| User | `POST /api/CoachProfile` | Duplicate Cloudinary upload + duplicate profile row |
| User | `POST /api/CoachCertificate` | Duplicate Cloudinary upload + duplicate certificate row |

**Header contract:**
- `Idempotency-Key: <ascii, 1–128 chars>` — **REQUIRED** on the 5 scoped endpoints.
- Missing / invalid → **400** `"Idempotency-Key header is required for this endpoint."`
- Same key + same body hash → replay cached response (2xx or 4xx; never 5xx).
- Same key + different body hash → **422** `"Idempotency-Key was reused with a different request body."`
- Recommended client format: UUID v4 per logical operation.

**Implementation:** `[Idempotent]` attribute + `IdempotencyKeyFilter : IAsyncActionFilter` in each API project's `Infrastructure/Idempotency/` folder. Cache via `HybridCache` (in-memory, 24h TTL). Single-flight via `GetOrCreateAsync` — two concurrent requests with the same key result in one factory execution (fixes the refresh-token race). Body hash = SHA-256 of all bound DTO fields except `IFormFile` (files are not hashed). 5xx responses are never cached; client may retry.

**Known limitation:** In-memory HybridCache is single-instance only. Cache does not survive restarts and is not shared across multiple pods. When Redis is added, register `IDistributedCache` — the filter transparently goes distributed with no code changes.

---

---

## File Locations

```
CoachingFit.sln
└── src/
    ├── Gateway/CoachingFit.Gateway/
    └── Services/
        ├── Identity/
        │   ├── CoachingFit.Identity.API/
        │   │   ├── Controllers/AuthController.cs
        │   │   ├── Controllers/BaseApiController.cs
        │   │   ├── Extensions/WebApplicationExtensions.cs
        │   │   └── Program.cs
        │   ├── CoachingFit.Identity.Core/
        │   │   ├── Entities/ApplicationUser.cs
        │   │   ├── Enums/UserRole.cs
        │   │   └── Contracts/IDataInitializer.cs
        │   ├── CoachingFit.Identity.Shared/
        │   │   ├── DTOs/Requests/ (LoginRequest, RegisterCoachRequest, RegisterTraineeRequest)
        │   │   ├── DTOs/Responses/AuthResponse.cs
        │   │   ├── Messages/EmailMessage.cs
        │   │   └── Wrappers/GenericResponse.cs
        │   ├── CoachingFit.Identity.Services.Abstraction/
        │   │   ├── IAuthService.cs
        │   │   ├── IEmailService.cs
        │   │   └── IJwtService.cs
        │   ├── CoachingFit.Identity.Services/
        │   │   ├── AuthService.cs
        │   │   └── Validators/ (LoginValidator, RegisterCoachValidator, RegisterTraineeValidator)
        │   └── CoachingFit.Identity.Infrastructure/
        │       ├── Data/Context/IdentityDbContext.cs
        │       ├── Data/DataSeed/DataInitializer.cs
        │       ├── ExternalServices/ (EmailService.cs, EmailSettings.cs)
        │       ├── Services/JwtService.cs
        │       └── Extensions/InfrastructureExtensions.cs
        └── User/
            ├── CoachingFit.User.API/
            │   ├── Controllers/ (BaseApiController, CoachProfileController, TraineeProfileController)
            │   ├── Extensions/WebApplicationExtensions.cs
            │   └── Program.cs
            ├── CoachingFit.User.Core/
            │   ├── Entities/ (BaseEntity, CoachProfile, TraineeProfile)
            │   ├── Enums/ (Gender, FitnessLevel)
            │   └── Contracts/IUserDbContext.cs
            ├── CoachingFit.User.Shared/
            │   ├── DTOs/Requests/ (Create/Update Coach/Trainee ProfileRequest)
            │   ├── DTOs/Responses/ (CoachProfileResponse, TraineeProfileResponse)
            │   └── Wrappers/GenericResponse.cs
            ├── CoachingFit.User.Services.Abstraction/
            │   ├── ICoachProfileService.cs
            │   ├── ITraineeProfileService.cs
            │   └── ICloudinaryService.cs
            ├── CoachingFit.User.Services/
            │   ├── CoachProfileService.cs
            │   ├── TraineeProfileService.cs
            │   └── Validators/ (4 validators)
            └── CoachingFit.User.Infrastructure/
                ├── Data/Context/UserDbContext.cs
                ├── ExternalServices/ (CloudinaryService.cs, CloudinarySettings.cs)
                └── Extensions/InfrastructureExtensions.cs
```

---

## Endpoints Reference

### Identity Service — `/api/Auth`
| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `/api/Auth/register/coach` | ❌ | Register coach, send confirmation email |
| POST | `/api/Auth/register/trainee` | ❌ | Register trainee, send confirmation email |
| POST | `/api/Auth/login` | ❌ | Login all roles |
| POST | `/api/Auth/refresh` | ❌ | Exchange refresh token for new access+refresh pair |
| POST | `/api/Auth/revoke` | ❌ | Revoke a refresh token (logout) — idempotent |
| GET | `/api/Auth/me` | ✅ | Get current user |
| GET | `/api/Auth/confirm-email?userId=&token=` | ❌ | Confirm email |
| POST | `/api/Auth/resend-confirmation?email=` | ❌ | Resend confirmation |
| PUT | `/api/Auth/coaches/{id}/activate` | ✅ Admin | Activate coach |
| GET | `/api/Auth/coaches/pending` | ✅ Admin | Get pending coach IDs |

### User Service — `/api/CoachProfile`
| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `/api/CoachProfile` | ✅ Coach | Create profile (multipart/form-data) |
| GET | `/api/CoachProfile/me` | ✅ Coach | Get my profile |
| GET | `/api/CoachProfile/{id}` | ✅ Any | Get by ID |
| PUT | `/api/CoachProfile` | ✅ Coach | Update profile (multipart/form-data) |
| GET | `/api/CoachProfile/pending?userIds=` | ✅ Admin | Get pending profiles |

### User Service — `/api/TraineeProfile`
| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `/api/TraineeProfile` | ✅ Trainee | Create profile (multipart/form-data) |
| GET | `/api/TraineeProfile/me` | ✅ Trainee | Get my profile |
| GET | `/api/TraineeProfile/{id}` | ✅ Coach | Get trainee by ID |
| PUT | `/api/TraineeProfile` | ✅ Trainee | Update profile (multipart/form-data) |

### User Service — `/api/CoachCertificate`
| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `/api/CoachCertificate` | ✅ Coach | Upload certificate (multipart/form-data) |
| GET | `/api/CoachCertificate/me` | ✅ Coach | Get my certificates |
| GET | `/api/CoachCertificate/{id}` | ✅ Coach | Get certificate by ID (own only) |
| DELETE | `/api/CoachCertificate/{id}` | ✅ Coach | Delete pending/rejected certificate |
| GET | `/api/CoachCertificate/pending` | ✅ Admin | Get all pending certificates |
| GET | `/api/CoachCertificate/coach/{userId}` | ✅ Admin | Get all certs for a coach |
| PUT | `/api/CoachCertificate/{id}/approve` | ✅ Admin | Approve certificate |
| PUT | `/api/CoachCertificate/{id}/reject` | ✅ Admin | Reject with reason (JSON body) |

---

## Business Rules

### Coach Lifecycle
```
Register → Confirm Email → Login → Create Profile → Upload Certificates → Admin Reviews → Activates → Goes Live in Catalog
```

### Trainee Lifecycle
```
Register → Confirm Email → Login → Create Profile → Browse Coaches → Purchase Plan
```

### Key Rules
- Coach registers with `IsActive = false`, trainee with `IsActive = true`
- Both must confirm email before login
- `IsActive = false` does NOT block login — a token is issued; the app routes to the pending screen based on `isActive` in the response
- Lockout: 5 failed attempts → 15-minute lock
- Photos: optional, max 5MB, jpg/png/webp only, Cloudinary face-crop 400×400
- On profile update: no photo sent = existing photo URL preserved
- Certificates: max 10MB, pdf/jpg/png/webp, stored in `coachingfit/certificates/` on Cloudinary (no face-crop)
- Certificate statuses: `Pending (0)`, `Approved (1)`, `Rejected (2)` stored as int
- Coach can delete only Pending or Rejected certificates (not Approved)
- Activation gate: admin decides independently — no code enforcement requiring certs before activation
- `ResendConfirmation` always returns 200 (anti-enumeration)
- Registration returns 201 Created

---

## What's Coming Next
- Coach Flutter App (in progress)
- Admin Web Dashboard
- Trainee Flutter App
- Catalog Service, Order Service, Plan Service, etc.
- RabbitMQ + MassTransit, gRPC, Hangfire, Docker, PayMob, SignalR

---

## Business Rules — For Future Services (Catalog, Order, Plan, Wallet)

These are product decisions baked into the domain. Encode them as you build the relevant service; do not redesign them.

### Commission
- Admin sets commission % per coach (not platform-wide flat rate)
- Charged on the original purchase amount, not net

### Refund
- 14-day refund window from purchase
- 80% refund to trainee, 20% kept by platform, **0% to coach**
- Requires admin approval

### Plans
- One active plan per trainee at a time
- One-time purchase (not subscription)
- Coach explicitly accepts or rejects each plan request
- 4 weeks duration

### Payout
- 30-day hold after plan completion before coach can withdraw
- Coach receives 80% of sale price minus the configured commission

### Data Retention
- Plans: 4 weeks after completion
- Messages: 90 days after plan end
- Photos: deleted when plan ends
- Payments: 7+ years (legal/tax)

---

## Production Deployment Checklist

These values are empty/wildcard in `appsettings.json` (dev-safe defaults). Before any production or staging deploy, set these:

- `AllowedHosts` — set to the real public domain(s) in `appsettings.Production.json` or the `AllowedHosts` environment variable (e.g. `"api.coachingfit.com"`). Never leave `"*"` in production.
- `App:BaseUrl` — the publicly reachable URL of the Identity API (e.g. `"https://api.coachingfit.com"`). Used to build the email-confirmation link; must **not** be derived from the `Host` request header.
- `App:AllowedOrigins` — array of browser origins permitted to call the APIs (e.g. `["https://admin.coachingfit.com"]`). Populated when the Admin Dashboard domain is known. In Dev this is ignored (all origins allowed).
- `Jwt:Key` — must be ≥ 32 bytes (256 bits). Both services validate at startup and refuse to start with a missing or too-short key.

---

## Gateway Ports
- `5000` — HTTP (used by Flutter apps via `10.0.2.2:5000` on Android emulator)
- `5001` — HTTPS
- The gateway does **not** validate JWT — every downstream service validates independently

---

## User Secrets Layout
**Identity Service**
```json
{
  "ConnectionStrings": { "DefaultConnection": "..." },
  "Jwt": { "Key": "..." },
  "Seeding": { "AdminEmail": "...", "AdminPassword": "..." },
  "EmailSettings": { "Email": "...", "Password": "..." }
}
```
**User Service**
```json
{
  "ConnectionStrings": { "DefaultConnection": "..." },
  "Jwt": { "Key": "..." },
  "Cloudinary": { "CloudName": "...", "ApiKey": "...", "ApiSecret": "..." }
}
```
`Jwt:Key` must be **identical** across all services. Never put secrets in `appsettings.json`.

---

## GitHub
https://github.com/3bdoEssam22/CoachingFit
