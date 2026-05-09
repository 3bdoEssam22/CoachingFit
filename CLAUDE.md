# CoachingFit — Claude Code Context

## What This Is
You are reviewing and working on **CoachingFit**, a commission-based fitness marketplace built on .NET 10 Microservices. Two Flutter mobile apps (Coach + Trainee) consume this backend through a single YARP gateway.

**Do NOT assume code is missing or broken without reading the actual files first.** Always `cat` or open the file before claiming something is wrong.

---

## Current State — What's Built and Working

### Sprint 1 — Identity Service (port 7272) ✅ COMPLETE
### Sprint 2 — API Gateway / YARP (port 5001) ✅ COMPLETE
### Sprint 3 — User Service (port 7024) ✅ COMPLETE

All three sprints are merged and tested. The backend is stable.

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

### UserRole Property + Identity Roles (Dual Source)
`ApplicationUser` has BOTH:
- `public UserRole UserRole { get; set; }` — used for JWT generation and `GetCurrentUser`
- ASP.NET Identity roles — used for `GetUsersInRoleAsync`, `IsInRoleAsync`

Both are set together at registration. This is a **known trade-off** that simplifies token generation. The `UserRole` column EXISTS in the database via migration `20260427122805_AddingUserRoleColumnToUsersTable`. Do NOT suggest removing it or creating a migration to drop it.

### IsActive Check in Login
`IsActive` IS checked in `LoginAsync`. It returns 403 for inactive accounts. Read `AuthService.cs` before claiming otherwise.

### Login Gate Order
The actual order in `AuthService.LoginAsync` is:
```
1. User exists? → 401
2. IsLockedOut? → 403
3. Password correct? → AccessFailedAsync + 401 if wrong
4. ResetAccessFailedCount
5. EmailConfirmed? → 401
6. IsActive? → 403
7. Generate token
```
This is the intended order. Do NOT suggest reordering.

### JWT — 30-Day Tokens, No Refresh
This is a deliberate MVP decision. We are aware of the revocation limitation. It will be addressed when Wallet/Payment services are built. Do NOT flag this as a bug.

### Email — TrySendConfirmationEmailAsync
Returns `bool`, never throws. Email failure does not fail registration — this is intentional graceful degradation.

### Cloudinary — No public_id Persistence
We store only `SecureUrl`. Old photos are orphaned on replacement. This is a known deferral, not a bug. It will be addressed later.

---

## Known Issue to Fix

### ResendConfirmationEmailAsync — Information Leakage
**This IS a real bug.** The method returns 200 for unknown/confirmed users but returns 500 when email sending fails for a real unconfirmed user. An attacker can distinguish the two via status code. Fix: return 200 regardless, log the failure internally.

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

---

## Business Rules

### Coach Lifecycle
```
Register → Confirm Email → Login → Create Profile → Admin Reviews → Activates → Goes Live in Catalog
```

### Trainee Lifecycle
```
Register → Confirm Email → Login → Create Profile → Browse Coaches → Purchase Plan
```

### Key Rules
- Coach registers with `IsActive = false`, trainee with `IsActive = true`
- Both must confirm email before login
- `IsActive = false` returns 403 on login
- Lockout: 5 failed attempts → 15-minute lock
- Photos: optional, max 5MB, jpg/png/webp only, Cloudinary face-crop 400×400
- On profile update: no photo sent = existing photo URL preserved
- `ResendConfirmation` always returns 200 (anti-enumeration — except bug #4 above)
- Registration returns 201 Created

---

## What's Coming Next
- Coach Flutter App (in progress)
- Admin Web Dashboard
- Trainee Flutter App
- Catalog Service, Order Service, Plan Service, etc.
- RabbitMQ + MassTransit, gRPC, Hangfire, Docker, PayMob, SignalR

---

## GitHub
https://github.com/3bdoEssam22/CoachingFit
