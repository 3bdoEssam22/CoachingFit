# CoachingFit — Backend Progress & Context

## Project Overview
A commission-based fitness marketplace built on **.NET 10 Microservices**.
Two Flutter mobile apps (Coach app + Trainee app — Uber/Uber Driver model) backed by a single backend.
Business model: coaches sell custom workout/nutrition plan bundles, platform takes a commission per sale.

---

## Build Order
1. ✅ Identity Service
2. ✅ API Gateway (YARP)
3. ✅ User Service
4. 🔜 Coach Flutter App (next)
5. 🔜 Admin Web Dashboard
6. 🔜 Trainee Flutter App
7. ⬜ Remaining backend services

---

## Services Overview

| # | Service | Port | Status |
|---|---|---|---|
| 1 | Identity Service | 7272 | ✅ Complete |
| 2 | API Gateway (YARP) | 5001 | ✅ Complete |
| 3 | User Service | 7024 | ✅ Complete |
| 4 | Catalog Service | TBD | ⬜ Pending |
| 5 | Order Service | TBD | ⬜ Pending |
| 6 | Plan Service | TBD | ⬜ Pending |
| 7 | Progress Service | TBD | ⬜ Pending |
| 8 | Chat Service | TBD | ⬜ Pending |
| 9 | Review Service | TBD | ⬜ Pending |
| 10 | Wallet Service | TBD | ⬜ Pending |
| 11 | Notification Service | TBD | ⬜ Pending |

---

## Solution Structure

```
CoachingFit.sln
└── src/
    ├── Gateway/
    │   └── CoachingFit.Gateway                  (port 5001)
    └── Services/
        ├── Identity/                             (port 7272)
        │   ├── CoachingFit.Identity.API
        │   ├── CoachingFit.Identity.Core
        │   ├── CoachingFit.Identity.Shared
        │   ├── CoachingFit.Identity.Services.Abstraction
        │   ├── CoachingFit.Identity.Services
        │   └── CoachingFit.Identity.Infrastructure
        └── User/                                 (port 7024)
            ├── CoachingFit.User.API
            ├── CoachingFit.User.Core
            ├── CoachingFit.User.Shared
            ├── CoachingFit.User.Services.Abstraction
            ├── CoachingFit.User.Services
            └── CoachingFit.User.Infrastructure
```

---

## Architecture Standards

### Onion Architecture — 6 Projects Per Service
```
API                   → Controllers, Program.cs, Extensions
Core                  → Entities, Enums, Contracts/Interfaces (IDbContext)
Shared                → DTOs (Requests, Responses), Wrappers
Services.Abstraction  → Service interfaces
Services              → Service implementations, Validators
Infrastructure        → DbContext, Migrations, External services, Extensions
```

### Package Placement Rules
| Layer | Allowed Packages |
|---|---|
| Core | `Microsoft.EntityFrameworkCore` (base abstractions only), `Microsoft.AspNetCore.Identity.EntityFrameworkCore` (Identity service only) |
| Shared | `FrameworkReference: Microsoft.AspNetCore.App` (when IFormFile needed) |
| Services.Abstraction | `FrameworkReference: Microsoft.AspNetCore.App` |
| Services | `FluentValidation`, `FluentValidation.DependencyInjectionExtensions`, `FrameworkReference: Microsoft.AspNetCore.App` |
| Infrastructure | `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.EntityFrameworkCore.Design`, `CloudinaryDotNet`, `FrameworkReference: Microsoft.AspNetCore.App`, external service SDKs |
| API | `Swashbuckle.AspNetCore`, `Microsoft.AspNetCore.Authentication.JwtBearer`, `Microsoft.EntityFrameworkCore.Tools`, `Microsoft.EntityFrameworkCore.Design` |

**Rule:** Only `Microsoft.EntityFrameworkCore` (base) in Core — never the SQL Server provider. Provider stays in Infrastructure.

---

## Global Patterns

### GenericResponse Wrapper
```csharp
public class GenericResponse<T>
{
    public int    StatusCode { get; set; }
    public string Message    { get; set; } = null!;
    public T?     Data       { get; set; }
}
```

### BaseApiController HandleResponse
```csharp
return response.StatusCode switch
{
    200 => Ok(response),
    201 => StatusCode(201, response),
    400 => BadRequest(response),
    401 => Unauthorized(response),
    403 => StatusCode(403, response),
    404 => NotFound(response),
    500 => StatusCode(500, response),
    _   => StatusCode(response.StatusCode)
};
```

### Reading UserId in Controllers
```csharp
var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
```

### DbContext Pattern (Microservices)
- No Unit of Work, no Generic Repository
- Interface defined in `Core/Contracts` (e.g. `IUserDbContext`)
- Implemented in Infrastructure (`UserDbContext`)
- Registered as scoped: `services.AddScoped<IUserDbContext>(sp => sp.GetRequiredService<UserDbContext>())`
- Industry standard — same as Microsoft eShopOnContainers

### FluentValidation
- Validators in `Services/Validators/`
- Registered via `services.AddValidatorsFromAssemblyContaining<SomeValidator>()`
- Async DB checks where needed
- Password complexity handled by ASP.NET Identity — not duplicated in FluentValidation
- Photo validation uses `When(x => x.Photo is not null, ...)` — only runs if file provided

### Email
- MailKit — Identity Service only
- Other services do not send emails directly (will use RabbitMQ events later)

### BaseUrl Pattern
- Passed as controller parameter via `GetBaseUrl()` helper
- Never part of request DTO body

---

## JWT Configuration
```json
{
  "Jwt": {
    "Issuer":        "CoachingFit.Identity",
    "Audience":      "CoachingFit.Client",
    "DurationInDays": 30
  }
}
```
- 30-day tokens, no refresh tokens
- Issued by Identity Service only
- Validated by every downstream service independently
- Gateway does NOT validate JWT — passes through as-is

### JWT Claims
```csharp
new(JwtRegisteredClaimNames.Sub,        user.Id),
new(ClaimTypes.NameIdentifier,          user.Id),
new(JwtRegisteredClaimNames.Email,      user.Email!),
new(JwtRegisteredClaimNames.GivenName,  user.FirstName),
new(JwtRegisteredClaimNames.FamilyName, user.LastName),
new(JwtRegisteredClaimNames.Jti,        Guid.NewGuid().ToString()),
new(ClaimTypes.Role,                    role)
```

---

## User Secrets Structure

### Identity Service
```json
{
  "ConnectionStrings": { "DefaultConnection": "..." },
  "Jwt": { "Key": "..." },
  "Seeding": { "AdminEmail": "...", "AdminPassword": "..." },
  "EmailSettings": { "Email": "...", "Password": "..." }
}
```

### User Service
```json
{
  "ConnectionStrings": { "DefaultConnection": "..." },
  "Jwt": { "Key": "..." },
  "Cloudinary": { "CloudName": "...", "ApiKey": "...", "ApiSecret": "..." }
}
```

**Rule:** Jwt:Key must be identical in all services. Never put secrets in appsettings.json.

---

## Sprint 1 — Identity Service ✅

### Database
- Name: `CoachingFit_Identity`
- Schema: `identity`

### Entities
- `ApplicationUser : IdentityUser` — FirstName, LastName, IsActive, CreatedAt, UpdatedAt, UserRole (enum)
- `UserRole` enum: Admin = 1, Coach = 2, Trainee = 3

### DbContext
- `IdentityDbContext : IdentityDbContext<ApplicationUser>`
- `UserRole` stored as int (`.HasConversion<int>()`)
- `CreatedAt` default: `GETUTCDATE()`
- `FullName` computed property — ignored by EF (`.Ignore(u => u.FullName)`)
- Custom table names: Users, Roles, UserRoles, UserClaims, UserLogins, UserTokens, RoleClaims

### Roles
- Seeded on startup: Admin, Coach, Trainee
- Role stored in both `UserRole` property and ASP.NET Identity roles
- JWT role claim read from `UserRole` property: `user.UserRole.ToString()`
- `GetCurrentUserAsync` also uses `UserRole` property
- Both set together at registration — must stay in sync
- Known trade-off: two sources of truth, simplifies token generation

### Endpoints
| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `/api/Auth/register/coach` | ❌ | Register coach, send confirmation email |
| POST | `/api/Auth/register/trainee` | ❌ | Register trainee, send confirmation email |
| POST | `/api/Auth/login` | ❌ | Login all roles |
| GET | `/api/Auth/me` | ✅ | Get current user |
| GET | `/api/Auth/confirm-email?userId=&token=` | ❌ | Confirm email |
| POST | `/api/Auth/resend-confirmation?email=` | ❌ | Resend confirmation email |
| PUT | `/api/Auth/coaches/{id}/activate` | ✅ Admin | Activate coach |
| GET | `/api/Auth/coaches/pending` | ✅ Admin | Get pending coach user IDs |

### Key Business Rules
- Coach registers with `IsActive = false`
- Both coach and trainee must confirm email before login
- Login order: check lockout → verify password → reset failed count → check email confirmed → check IsActive → generate token
- `IsActive = false` returns 403 "Your account is not yet activated. Please wait for admin approval."
- Lockout: 5 failed attempts → 15 minutes
- Trainee registers with `IsActive = true`
- Admin seeded on startup with email auto-confirmed, `UserRole = Admin`, `IsActive = true`
- `AddToRoleAsync` failure → delete user (manual rollback)
- Registration returns 201 Created

### Email (MailKit)
- Host: smtp.gmail.com, Port: 587, StartTls
- Sends: registration confirmation (both coach and trainee), activation notification
- `TrySendConfirmationEmailAsync` — returns bool, never throws, logs errors via ILogger
- Email failure doesn't fail registration — graceful fallback with adjusted success message
- Activation email failure doesn't fail the activation — caught, logged, swallowed

### Registration Flow
- **Coach:** creates user with `IsActive = false`, `UserRole = Coach` → adds to "Coach" Identity role → sends confirmation email via `TrySendConfirmationEmailAsync`
- **Trainee:** creates user with `IsActive = true`, `UserRole = Trainee` → adds to "Trainee" Identity role → sends confirmation email via `TrySendConfirmationEmailAsync`

### Information Security
- `ResendConfirmationEmailAsync` returns same 200 message whether user exists or not — prevents email enumeration
- `ConfirmEmailAsync` returns generic "Invalid or expired confirmation link" on user-not-found (no info leakage)
- Email send failures are logged, not exposed to user

### `GetPendingCoachUserIdsAsync`
```csharp
var coaches = await _userManager.GetUsersInRoleAsync(nameof(UserRole.Coach));
var pendingIds = coaches.Where(c => !c.IsActive).Select(c => c.Id);
```

### Services Registered
- `IAuthService` → `AuthService` (scoped, in API Program.cs)
- `IJwtService` → `JwtService` (scoped, in InfrastructureExtensions)
- `IEmailService` → `EmailService` (transient, in InfrastructureExtensions)
- `IDataInitializer` → `DataInitializer` (scoped, in InfrastructureExtensions)
- FluentValidation: `RegisterCoachValidator`, `RegisterTraineeValidator`, `LoginValidator`

---

## Sprint 2 — API Gateway ✅

### Setup
- Package: `Yarp.ReverseProxy`
- Port: 5001 (HTTPS), 5000 (HTTP)
- Health check: `/health`
- Pure reverse proxy — no business logic, no JWT validation

### Routes (Order Matters)
```json
"identity-route":       { "Order": 0, "Path": "/api/Auth/{**catch-all}" }          → https://localhost:7272
"coach-profile-route":  { "Order": 1, "Path": "/api/CoachProfile/{**catch-all}" }   → https://localhost:7024
"trainee-profile-route":{ "Order": 2, "Path": "/api/TraineeProfile/{**catch-all}" } → https://localhost:7024
```

---

## Sprint 3 — User Service ✅

### Database
- Name: `CoachingFit_User`
- Schema: `user`

### Entities
```
BaseEntity        → Id (Guid), CreatedAt, UpdatedAt
CoachProfile      → UserId, Bio, ExperienceYears, ProfilePhotoUrl, Gender
TraineeProfile    → UserId, Gender, DateOfBirth, WeightKg, HeightCm,
                    FitnessLevel, Goals, MedicalNotes, ProfilePhotoUrl
```

### Enums
```csharp
Gender:       Male = 1, Female = 2
FitnessLevel: Beginner = 1, Intermediate = 2, Advanced = 3
```

### IUserDbContext
```csharp
public interface IUserDbContext
{
    IQueryable<CoachProfile>   CoachProfiles   { get; }
    IQueryable<TraineeProfile> TraineeProfiles { get; }

    Task<CoachProfile?>   FindCoachProfileAsync(Guid id, CancellationToken ct = default);
    Task<TraineeProfile?> FindTraineeProfileAsync(Guid id, CancellationToken ct = default);
    Task AddCoachProfileAsync(CoachProfile profile, CancellationToken ct = default);
    Task AddTraineeProfileAsync(TraineeProfile profile, CancellationToken ct = default);
    void UpdateCoachProfile(CoachProfile profile);
    void UpdateTraineeProfile(TraineeProfile profile);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
```
Note: exposes `IQueryable<T>` not `DbSet<T>` — cleaner abstraction boundary.

### UserDbContext
- Inherits `DbContext`, implements `IUserDbContext`
- `DbSet<CoachProfile> CoachProfilesSet` / `DbSet<TraineeProfile> TraineeProfilesSet` — internal EF sets
- `IQueryable<T>` properties delegate to DbSets
- Explicit CRUD methods delegate to DbSet internally
- `CreatedAt` default: `GETUTCDATE()`
- `UserId` unique index on both tables
- Gender and FitnessLevel stored as `int` (`.HasConversion<int>()`)

### Endpoints
| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `/api/CoachProfile` | ✅ Coach | Create coach profile |
| GET | `/api/CoachProfile/me` | ✅ Coach | Get my profile |
| GET | `/api/CoachProfile/{id}` | ✅ Any | Get profile by ID |
| PUT | `/api/CoachProfile` | ✅ Coach | Update profile |
| GET | `/api/CoachProfile/pending?userIds=` | ✅ Admin | Get pending profiles |
| POST | `/api/TraineeProfile` | ✅ Trainee | Create trainee profile |
| GET | `/api/TraineeProfile/me` | ✅ Trainee | Get my profile |
| GET | `/api/TraineeProfile/{id}` | ✅ Coach | Get trainee profile by ID |
| PUT | `/api/TraineeProfile` | ✅ Trainee | Update profile |

### Request DTOs — multipart/form-data
All create/update endpoints use `[FromForm]` not `[FromBody]`.
```
CreateCoachProfileRequest:   Gender (string), Bio, ExperienceYears, Photo (IFormFile?)
UpdateCoachProfileRequest:   Bio, ExperienceYears, Photo (IFormFile?)
CreateTraineeProfileRequest: Gender (string), DateOfBirth, WeightKg, HeightCm,
                             FitnessLevel, Goals, MedicalNotes?, Photo (IFormFile?)
UpdateTraineeProfileRequest: WeightKg, HeightCm, FitnessLevel, Goals,
                             MedicalNotes?, Photo (IFormFile?)
```

### Response DTOs
```
CoachProfileResponse:   Id, UserId, Gender (string), Bio, ExperienceYears,
                        ProfilePhotoUrl?, CreatedAt
TraineeProfileResponse: Id, UserId, Gender (string), DateOfBirth, WeightKg,
                        HeightCm, FitnessLevel, Goals, MedicalNotes?,
                        ProfilePhotoUrl?, CreatedAt
```

### Gender Handling
- Request DTO: `string Gender` — sent as "Male" or "Female"
- Parsed in service: `Enum.Parse<Gender>(request.Gender, true)`
- Validated in FluentValidation: `Must(g => Enum.TryParse<Gender>(g, true, out _))`
- Response DTO: `string Gender` — returned as "Male" or "Female" via `.ToString()`
- `JsonStringEnumConverter` registered in Program.cs for FitnessLevel in responses

### Photo Validation (FluentValidation)
```csharp
When(x => x.Photo is not null, () =>
{
    RuleFor(x => x.Photo!.Length)
        .LessThanOrEqualTo(5 * 1024 * 1024)  // 5MB
        .WithMessage("Photo must not exceed 5MB.");

    RuleFor(x => x.Photo!.ContentType)
        .Must(type => _allowedTypes.Contains(type))
        .WithMessage("Photo must be a JPG, PNG, or WebP image.");
});
// Allowed: image/jpeg, image/jpg, image/png, image/webp
```

### Cloudinary Integration
- Package: `CloudinaryDotNet`
- Folder: `coachingfit/profiles`
- Transformation: 400×400, crop fill, gravity face
- Returns `SecureUrl` — stored in `ProfilePhotoUrl`
- Error throws `InvalidOperationException` with Cloudinary error message
- Caught in service layer — returns 500 with user-friendly message, logs error via ILogger
- Settings bound from `Cloudinary` config section (User Secrets)

### Update Photo Logic
```csharp
if (request.Photo is not null)
    profile.ProfilePhotoUrl = await _cloudinaryService.UploadImageAsync(request.Photo);
// else: existing URL untouched
```

### GetAllPendingAsync
```csharp
// Admin calls GET /api/Auth/coaches/pending first, then passes IDs here
GET /api/CoachProfile/pending?userIds=id1&userIds=id2
// Sanitizes input: filters blank, trims, deduplicates into HashSet
// Returns empty list with 200 if no IDs provided
```

### Services Registered (in InfrastructureExtensions)
- `IUserDbContext` → `UserDbContext` (scoped)
- `ICloudinaryService` → `CloudinaryService` (scoped)
- `ICoachProfileService` → `CoachProfileService` (scoped)
- `ITraineeProfileService` → `TraineeProfileService` (scoped)
- FluentValidation: all 4 validators auto-registered via assembly scan

### Infrastructure Project References
- `CoachingFit.User.Infrastructure` → references `CoachingFit.User.Services` (for registering service implementations and validators)
- Gets Core and Shared transitively through Services chain

---

## Key Decisions & Learnings

### No Unit of Work in Microservices
Direct `IDbContext` interface injection is the standard. Validated by Microsoft eShopOnContainers.

### EF Core Package Split
- Core: `Microsoft.EntityFrameworkCore` (abstractions only)
- Infrastructure: `Microsoft.EntityFrameworkCore.SqlServer` (provider)
- Never mix — Core must not know about SQL Server

### UserRole Property + Identity Roles (Dual Source)
- `UserRole` property on `ApplicationUser` used for JWT generation and `GetCurrentUser`
- ASP.NET Identity roles used for `GetUsersInRoleAsync`, `IsInRoleAsync`
- Both set together at registration — must stay in sync
- Known trade-off: two sources of truth, simplifies token generation

### IsActive Meaning
- `false` = coach account pending admin approval, returns 403 on login
- `true` = account active, can log in and use the platform
- Trainees always registered with `IsActive = true`
- Will also control catalog visibility (enforced in Catalog Service later)

### Login Gate Order
```
1. User exists?
2. IsLockedOut?
3. Password correct? → increment failed count if wrong
4. Reset failed count (password was correct)
5. EmailConfirmed?
6. IsActive?
7. Generate token
```

### BaseUrl in DTOs is a Design Smell
Pass as controller parameter, never in request body.

### Validation Responsibility Split
- Password complexity → ASP.NET Identity (not duplicated in FluentValidation)
- Business rules (uniqueness, domain constraints) → FluentValidation
- File validation → FluentValidation with `When()` guard
- Gender validation → `Must(g => Enum.TryParse<Gender>(g, true, out _))` since it's a string in DTO

### Information Security
- `ResendConfirmationEmailAsync` always returns 200 — prevents email enumeration
- `ConfirmEmailAsync` returns generic message on user-not-found (no info leakage)
- Email send failures are logged, not exposed to user

---

## Planned Technologies (Not Yet Implemented)
- **RabbitMQ + MassTransit** — async events (CoachActivated, OrderPlaced, RefundApproved, PlanCompleted)
- **gRPC** — high-frequency internal service communication
- **Hangfire** — background jobs (payout timers, plan expiration)
- **Docker + Docker Compose** — containerization
- **PayMob** — payment gateway
- **SignalR** — real-time chat

---

## Business Rules Summary

### Commission
- Admin sets commission % per coach
- Charged on original purchase amount

### Refund
- 14-day refund window
- 80% refund to trainee
- Platform keeps 20% of original, coach keeps 0%
- Requires admin approval

### Coach Lifecycle
```
Register → Confirm Email → Login → Create Profile → Admin Reviews → Activates → Goes Live in Catalog
```

### Trainee Lifecycle
```
Register → Confirm Email → Login → Create Profile → Browse Coaches → Purchase Plan
```

### Plans
- One active plan per trainee at a time
- One-time purchase
- Coach accepts or rejects plan request
- 4 weeks duration

### Payout
- 30-day hold after plan completion
- Coach receives 80% of sale price minus commission

### Data Retention
- Plans: 4 weeks after completion
- Messages: 90 days after plan end
- Photos: deleted when plan ends
- Payments: 7+ years

---

## Project Management
- **GitHub Projects** — Kanban board (Backlog, Sprint, In Progress, Done)
- **GitHub Milestones** — one per sprint
- **GitHub Issues** — user story format
- **Branches** — feature branches → PR → Development branch → main
- **Progress file** — `CoachingFit_Progress.md` in repo root for cross-conversation continuity

---

## GitHub
**Repository:** https://github.com/3bdoEssam22/CoachingFit
