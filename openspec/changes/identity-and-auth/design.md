## Context

SportFlow es una plataforma multi-tenant SaaS para gestión de gimnasios. El módulo Identity es la base de toda la plataforma: todos los demás dominios dependen de saber quién es el usuario, a qué tenant pertenece y qué puede hacer. Este es el primer change del proyecto — no existe código previo.

Stack definido: .NET 10, ASP.NET Core Web API, Clean Architecture, CQRS manual (sin MediatR), EF Core 10, SQL Server, JWT Bearer.

## Goals / Non-Goals

**Goals:**
- Login con email + password + tenantSlug, retornando access token (JWT) + refresh token
- JWT con claims: `sub` (userId), `tenant_id`, `role`, `scopes`, `location_ids`
- Refresh token rotation: cada uso genera uno nuevo e invalida el anterior
- Middleware que extrae `tenant_id` del JWT y lo inyecta en `ITenantContext` (DI scoped)
- 6 roles del sistema: `SuperAdmin`, `TenantOwner`, `TenantManager`, `Staff`, `Coach`, `Member`
- Un usuario puede tener roles en múltiples tenants (via `UserTenantRoles`)
- Endpoints: login, refresh, logout, me, change-password, forgot-password, reset-password
- Configuración inicial del `SportFlowDbContext` con las 3 tablas de identity
- Primera migración EF Core

**Non-Goals:**
- OAuth2 / login con Google, Microsoft (v2)
- MFA / 2FA (v2)
- SSO entre tenants (no aplica en v1)
- Rate limiting de login (se agrega como middleware cross-cutting en otro change)
- Gestión de permisos granulares (los roles son suficientes para v1)
- Invitación de usuarios por email (se incluye en tenant-setup change)

## Decisions

### D1: User vs Person son entidades separadas

**Decisión:** `User` maneja credenciales de autenticación. `Person` es la entidad de dominio del gym (el atleta/socio). Un `Person` puede existir sin `User` (creado por el admin antes de que el socio active su cuenta). La vinculación es `Person.UserId FK → Users.Id`.

**Alternativa considerada:** Unificar en una sola entidad. Rechazado porque el admin necesita crear socios sin que tengan acceso al sistema todavía.

---

### D2: tenant_id en JWT, no en el path

**Decisión:** El `tenant_id` se embebe en el JWT al momento del login (el usuario provee el `tenantSlug`). El middleware lo extrae y lo inyecta en `ITenantContext`. Ningún endpoint lleva `tenant_id` en la URL.

**Alternativa considerada:** `/{tenantId}/v1/...` en el path. Rechazado: expone estructura interna, complica routing y permite errores de cross-tenant por URL incorrecta.

**Implementación:**
```
ITenantContext (scoped):
  Guid TenantId { get; }
  string TenantSlug { get; }
  string Role { get; }
  bool IsSuperAdmin { get; }

TenantResolutionMiddleware:
  → Extrae claim "tenant_id" del JWT
  → Si es SuperAdmin y no hay tenant_id → TenantContext.IsSuperAdmin = true
  → Registra ITenantContext en HttpContext.RequestServices
```

---

### D3: Refresh Token Rotation con almacenamiento en DB

**Decisión:** Los refresh tokens se almacenan hasheados en la tabla `RefreshTokens`. Cada uso genera un nuevo token y revoca el anterior (rotation). Token comprometido es detectado si se usa un token ya rotado (invalidar toda la familia).

**Alternativa considerada:** Refresh tokens stateless (JWT de larga duración). Rechazado: no permite revocación, riesgo de seguridad para plataforma SaaS.

```
Flujo de refresh:
  1. Cliente envía refresh_token
  2. Se busca por hash en DB
  3. Si revoked_at IS NOT NULL → token comprometido → invalidar todos los tokens del user
  4. Si válido → crear nuevo refresh_token, revocar el anterior
  5. Retornar nuevo access_token + nuevo refresh_token
```

---

### D4: ASP.NET Core Identity solo para password hashing

**Decisión:** Se usa `PasswordHasher<User>` de ASP.NET Core Identity únicamente para hashear/verificar contraseñas. No se usa `UserManager`, `SignInManager` ni el store completo de Identity — son demasiado opinionados y dificultan Clean Architecture.

**Alternativa considerada:** Identity completo. Rechazado: acopla la capa de dominio a ASP.NET Core, genera tablas propias que colisionan con nuestro schema.

---

### D5: Roles como enum + tabla UserTenantRoles

**Decisión:** Los roles se definen como strings constantes en el dominio. La tabla `UserTenantRoles` permite que un mismo `User` sea `TenantOwner` del gym A y `Coach` del gym B.

```
Jerarquía de roles:
  SuperAdmin     → acceso a toda la plataforma (sin tenant_id en JWT)
  TenantOwner    → acceso completo a su(s) tenant(s)
  TenantManager  → acceso operativo completo a un tenant
  Staff          → check-in, consulta de socios
  Coach          → sus clases y atletas asignados
  Member         → solo sus propios datos
```

---

### D6: Entidades y EF Core

**Entidades del dominio:**

```
User
  Id: UserId (strongly-typed, wraps Guid)
  Email: string
  PasswordHash: string
  SystemRole: string
  IsActive: bool
  IsEmailVerified: bool
  EmailVerificationToken: string?
  PasswordResetToken: string?
  PasswordResetExpiresAt: DateTime?
  LastLoginAt: DateTime?
  CreatedAt: DateTime
  UpdatedAt: DateTime
  DeletedAt: DateTime?

RefreshToken
  Id: Guid
  UserId: UserId
  TenantId: TenantId?
  TokenHash: string
  ExpiresAt: DateTime
  RevokedAt: DateTime?
  IpAddress: string?
  UserAgent: string?
  CreatedAt: DateTime

UserTenantRole
  Id: Guid
  UserId: UserId
  TenantId: TenantId
  Role: string
  IsActive: bool
  CreatedAt: DateTime
```

**EF Core Configurations:**

```
UsersConfiguration : IEntityTypeConfiguration<User>
  → ToTable("Users")
  → HasKey(u => u.Id)
  → HasIndex(u => u.Email).IsUnique()
  → HasQueryFilter(u => u.DeletedAt == null)
  → Property(u => u.Id).HasConversion(UserId → Guid)

RefreshTokensConfiguration
  → ToTable("RefreshTokens")
  → HasIndex(rt => rt.TokenHash)
  → HasOne<User>().WithMany().HasForeignKey(rt => rt.UserId)

UserTenantRolesConfiguration
  → ToTable("UserTenantRoles")
  → HasIndex(utr => new { utr.UserId, utr.TenantId, utr.Role }).IsUnique()
```

---

### D7: DTOs con record types

**Request/Response shapes:**

```csharp
// POST /v1/auth/login
record LoginRequest(string Identifier, string Password, string TenantSlug, Guid? LocationId);

// Respuesta exitosa
record TokenResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    string TokenType,
    MeResponse Me
);

// GET /v1/auth/me
record MeResponse(
    Guid PersonId,
    string FirstName,
    string LastName1,
    string Email,
    string Role,
    string[] Scopes,
    TenantRef Tenant,
    Guid[] AuthorizedLocations
);

// POST /v1/auth/refresh
record RefreshTokenRequest(string RefreshToken);

// PATCH /v1/auth/me/password
record ChangePasswordRequest(string CurrentPassword, string NewPassword);

// POST /v1/auth/forgot-password
record ForgotPasswordRequest(string Email, string TenantSlug);

// POST /v1/auth/reset-password
record ResetPasswordRequest(string Token, string NewPassword);
```

## Risks / Trade-offs

- **Refresh token en DB agrega latency** → Mitigación: índice en `token_hash`, la tabla es pequeña y el hit es una sola query por refresh.

- **JWT de corta duración (15 min) molesta en dev** → Mitigación: configurable por environment en `appsettings`. En desarrollo se puede poner 24h.

- **SuperAdmin sin tenant_id en JWT rompe el middleware** → Mitigación: `TenantResolutionMiddleware` verifica si el rol es `SuperAdmin` y omite la validación del tenant. Los endpoints de SuperAdmin tienen su propia política de autorización.

- **Multi-tenant con UserTenantRoles: ¿qué tenant va en el JWT si el user es owner de 2 gyms?** → El usuario elige el tenant en el `tenantSlug` del login request. Si quiere cambiar de gym, hace login de nuevo o usa un endpoint de "switch tenant" (v1.1).

## Open Questions

- ¿El email de verificación es obligatorio en v1 o se puede crear user y activar manualmente? → **Decisión tomada:** En v1 el admin crea users directamente (sin verificación por email). La verificación por email se agrega en v1.1.
- ¿Cuánto dura el access token? → **15 minutos** en producción, configurable en `appsettings.json`.
- ¿Cuánto dura el refresh token? → **30 días**, renovable con cada uso.
