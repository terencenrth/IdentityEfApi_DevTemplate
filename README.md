# IdentityEfApi – README

A minimal **.NET 8 Web API** that uses **ASP.NET Core Identity** for authentication, **JWT Bearer** for auth on endpoints, **EF Core (code-first)** with **SQL Server**, and a simple **Repository pattern**. Includes Swagger with an **Authorize** button.

---

## Prerequisites

- .NET SDK 8.x  
- SQL Server (local/remote)  
- (Optional) SQL Server Management Studio (SSMS)

---

## 1) Configure the connection string & JWT

Edit `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=db\\SQLDEV;Database=IdentityEfApi;User Id=dev;Password=your_password_here;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Issuer": "IdentityEfApi",
    "Audience": "IdentityEfApiClient",
    "Key": "REPLACE_WITH_A_LONG_RANDOM_SECRET_KEY"
  },
  "Logging": { "LogLevel": { "Default": "Information", "Microsoft.AspNetCore": "Warning" } },
  "AllowedHosts": "*"
}
```

> JSON requires escaping the backslash in `db\\SQLDEV`.

---

## 2) Create the database schema (EF Core)

If you haven’t already:

```bash
dotnet tool install --global dotnet-ef
dotnet ef database update
```

> If the login/database doesn’t exist, create them once in SSMS or with a script (create login `dev`, create DB `IdentityEfApi`, map user and grant `db_owner`).

---

## 3) Run the API

```bash
dotnet run
```

The app shows a HTTPS URL (e.g., `https://localhost:5023`).  
Open **Swagger** at: `https://localhost:5023/swagger`

---

## 4) End-to-end test flow (Register → Login → Authorize → Call API)

### A) Register a user

Swagger: `POST /auth/register`  
Body:

```json
{
  "email": "admin@example.com",
  "password": "P@ssw0rd!"
}
```

cURL:

```bash
curl -k -X POST https://localhost:5023/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@example.com","password":"P@ssw0rd!"}'
```

### B) Login to get JWT

Swagger: `POST /auth/login` → copy the `token` from the response.

cURL:

```bash
curl -k -X POST https://localhost:5023/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@example.com","password":"P@ssw0rd!"}'
```

Response (example):

```json
{ "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." }
```

### C) Authorize Swagger

- In Swagger UI, click **Authorize** (padlock)  
- Paste: `Bearer <token>`  
- Click **Authorize** → **Close**

### D) Call a secured endpoint (Products)

- `GET /api/products` → should return seeded rows (Keyboard, Mouse)
- `POST /api/products` (JWT required)  

Example body:

```json
{
  "name": "Laptop Stand",
  "price": 899.00,
  "description": "Aluminum"
}
```

cURL with token:

```bash
curl -k https://localhost:5023/api/products \
  -H "Authorization: Bearer <token>"
```

---

## 5) Project structure

```
IdentityEfApi/
 ├─ Database/
 │   ├─ AppDbContext.cs            // Inherits IdentityDbContext<ApplicationUser>
 │   └─ Entities/
 │       └─ Product.cs             // [Precision(18,2)] for decimal
 ├─ Auth/
 │   ├─ ApplicationUser.cs         // Identity user
 │   ├─ JwtOptions.cs              // Issuer/Audience/Key
 │   └─ AuthEndpoints.cs           // /auth/register & /auth/login
 ├─ Data/
 │   ├─ IRepository.cs
 │   └─ EfRepository.cs            // Generic repo using AppDbContext
 ├─ Features/
 │   └─ Products/
 │       └─ ProductsController.cs  // [Authorize] CRUD using IRepository<Product>
 ├─ Program.cs                     // DI, JWT, Swagger config, seed
 └─ appsettings.json
```

---

## 6) Architecture overview

- **ASP.NET Core Identity + EF Core**  
  - `ApplicationUser : IdentityUser`  
  - `AppDbContext : IdentityDbContext<ApplicationUser>`  
  - Identity tables live in the same database as app entities.

- **JWT Authentication**  
  - `/auth/login` signs a JWT with `Issuer`, `Audience`, and symmetric `Key`.  
  - API endpoints use `[Authorize]`; Swagger is configured with a **Bearer** scheme.

- **Repository Pattern (simple)**  
  - `IRepository<T>` abstracts data access for aggregate roots (e.g., `Product`).  
  - `EfRepository<T>` implements basic CRUD and query patterns using EF Core.

- **EF Core Code-First**  
  - Migrations create schema.  
  - `Product.Price` uses `[Precision(18, 2)]` (or `[Column("decimal(18,2)")]`).  
  - SQL Server provider with `EnableRetryOnFailure()` for transient resiliency.

- **Swagger/OpenAPI**  
  - `AddSwaggerGen` registers a JWT **security definition** and **requirement** so the **Authorize** padlock appears in UI.

- **Seeding (demo data)**  
  - On startup, seeds two `Product` rows if none exist.  
  - (Optional) can add role/user seeding for an initial admin.

---

## 7) Common troubleshooting

- **No “Authorize” button in Swagger**  
  Ensure `AddSwaggerGen` registers a bearer scheme, and `UseAuthentication()` + `UseAuthorization()` are in the pipeline.

- **“Cannot open database … Login failed”**  
  The DB or login doesn’t exist, or user lacks rights. Create DB and map login → user (`db_owner`) once, then run migrations.

- **Decimal truncation warning**  
  Add `[Precision(18,2)]` or `[Column(TypeName="decimal(18,2)")]` to `Product.Price`.

- **401 Unauthorized on Products**  
  Make sure you logged in, clicked **Authorize** in Swagger, and used `Bearer <token>`.

---

## 8) Useful commands

```bash
# Add or update migrations
dotnet ef migrations add <Name>
dotnet ef database update

# Run the app
dotnet run
```

---

## 9) Optional: seed an initial admin & role

Add this near startup (after building `app`):

```csharp
using (var scope = app.Services.CreateScope())
{
    var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    const string role = "Admin";
    if (!await roleMgr.RoleExistsAsync(role))
        await roleMgr.CreateAsync(new IdentityRole(role));

    var email = "admin@example.com";
    var user = await userMgr.FindByEmailAsync(email);
    if (user is null)
    {
        user = new ApplicationUser { UserName = email, Email = email };
        await userMgr.CreateAsync(user, "P@ssw0rd!");
        await userMgr.AddToRoleAsync(user, role);
    }
}
```

Then you can protect endpoints with `[Authorize(Roles = "Admin")]`.

---

## 10) Security notes (production)

- Store `Jwt:Key` and connection strings in **secrets/KeyVault**, not in `appsettings.json`.  
- Use HTTPS and valid TLS certificates.  
- Consider refresh tokens + token revocation for longer sessions.  
- Lock password policy and enable account lockout as needed.  
- Add structured logging and health checks.
