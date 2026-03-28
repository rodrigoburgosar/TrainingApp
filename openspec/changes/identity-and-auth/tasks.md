## 1. Estructura del proyecto y dependencias

- [x] 1.1 Crear la solución .NET con los proyectos: `SportFlow.Domain`, `SportFlow.Application`, `SportFlow.Infrastructure`, `SportFlow.API`, `SportFlow.Shared`
- [x] 1.2 Agregar referencias entre proyectos (Domain ← Application ← Infrastructure ← API; Shared ← todos)
- [x] 1.3 Agregar NuGet packages: `Microsoft.AspNetCore.Authentication.JwtBearer`, `Microsoft.AspNetCore.Identity.Core`, `Microsoft.EntityFrameworkCore.SqlServer`, `Serilog.AspNetCore`, `Swashbuckle.AspNetCore`, `FluentValidation.AspNetCore`, `Mapperly`

## 2. Domain layer — Identity

- [x] 2.1 Crear value objects `UserId` y `TenantId` (strongly-typed wrappers de `Guid`) en `SportFlow.Domain/Shared/ValueObjects/`
- [x] 2.2 Crear entidad `User` con propiedades: `Id`, `Email`, `PasswordHash`, `SystemRole`, `IsActive`, `IsEmailVerified`, tokens de verificación/reset y auditoría (`CreatedAt`, `UpdatedAt`, `DeletedAt`)
- [x] 2.3 Crear entidad `RefreshToken` con propiedades: `Id`, `UserId`, `TenantId?`, `TokenHash`, `ExpiresAt`, `RevokedAt?`, `IpAddress?`, `UserAgent?`, `CreatedAt`
- [x] 2.4 Crear entidad `UserTenantRole` con propiedades: `Id`, `UserId`, `TenantId`, `Role`, `IsActive`, `CreatedAt`
- [x] 2.5 Crear clase estática `SystemRoles` con constantes: `SuperAdmin`, `TenantOwner`, `TenantManager`, `Staff`, `Coach`, `Member`
- [x] 2.6 Crear interfaz `IUserRepository` en `SportFlow.Domain/Identity/`

## 3. Application layer — Contratos e interfaces

- [x] 3.1 Crear interfaces genéricas `ICommandHandler<TCommand>`, `ICommandHandler<TCommand, TResult>` y `IQueryHandler<TQuery, TResult>` en `SportFlow.Application/Abstractions/`
- [x] 3.2 Crear interfaz `ITenantContext` con propiedades: `TenantId`, `TenantSlug`, `UserId`, `Role`, `IsSuperAdmin`
- [x] 3.3 Crear interfaz `IJwtService` con métodos `GenerateAccessToken` y `ValidatePrincipal`
- [x] 3.4 Crear clase `Result<T>` y `Result` (Result Pattern) en `SportFlow.Shared/Results/`
- [x] 3.5 Crear excepciones base: `SportFlowException`, `NotFoundException`, `ForbiddenException`, `ValidationException` en `SportFlow.Shared/Exceptions/`

## 4. Application layer — Records (DTOs)

- [x] 4.1 Crear records de request: `LoginRequest`, `RefreshTokenRequest`, `LogoutRequest`, `ChangePasswordRequest`, `ForgotPasswordRequest`, `ResetPasswordRequest`
- [x] 4.2 Crear records de response: `TokenResponse`, `MeResponse`, `TenantRef`, `SessionResponse`

## 5. Application layer — Command handlers

- [x] 5.1 Implementar `LoginCommandHandler`: valida credenciales, verifica tenant activo, genera JWT + refresh token, retorna `TokenResponse`
- [x] 5.2 Implementar `RefreshTokenCommandHandler`: valida refresh token, implementa rotation (revoca anterior, emite nuevo), detecta token comprometido
- [x] 5.3 Implementar `LogoutCommandHandler`: revoca el refresh token recibido
- [x] 5.4 Implementar `ChangePasswordCommandHandler`: verifica contraseña actual, actualiza hash, revoca todas las sesiones excepto la activa
- [x] 5.5 Implementar `ForgotPasswordCommandHandler`: genera token de reset con expiración 2h, encola email (stub en v1)
- [x] 5.6 Implementar `ResetPasswordCommandHandler`: valida token de reset, actualiza hash, revoca todas las sesiones

## 6. Application layer — Query handlers

- [x] 6.1 Implementar `GetMeQueryHandler`: retorna datos del usuario autenticado con su tenant y permisos
- [x] 6.2 Implementar `GetSessionsQueryHandler`: lista refresh tokens activos del usuario
- [x] 6.3 Implementar `RevokeSessionCommandHandler`: revoca una sesión específica por ID

## 7. Application layer — Validaciones

- [x] 7.1 Crear `LoginRequestValidator` (FluentValidation): email válido, password no vacío, tenantSlug no vacío
- [x] 7.2 Crear `ChangePasswordRequestValidator`: newPassword mínimo 8 caracteres
- [x] 7.3 Crear `ResetPasswordRequestValidator`: token no vacío, newPassword mínimo 8 caracteres

## 8. Infrastructure layer — DbContext y configuraciones EF Core

- [x] 8.1 Crear `SportFlowDbContext` con `DbSet<User>`, `DbSet<RefreshToken>`, `DbSet<UserTenantRole>` y constructor que recibe `ITenantContext`
- [x] 8.2 Crear `UsersConfiguration : IEntityTypeConfiguration<User>`: tabla `Users`, PK, índice único en `Email`, conversión `UserId ↔ Guid`, Global Query Filter `WHERE deleted_at IS NULL`
- [x] 8.3 Crear `RefreshTokensConfiguration`: tabla `RefreshTokens`, índice en `token_hash`, FK a `Users`
- [x] 8.4 Crear `UserTenantRolesConfiguration`: tabla `UserTenantRoles`, índice único `(user_id, tenant_id, role)`, FKs
- [x] 8.5 Generar la primera migración EF Core: `InitialIdentitySchema`

## 9. Infrastructure layer — Repositorios y servicios

- [x] 9.1 Implementar `UserRepository : IUserRepository` con métodos: `GetByEmailAndTenantAsync`, `GetByIdAsync`, `AddAsync`, `UpdateAsync`
- [x] 9.2 Implementar `RefreshTokenRepository` con métodos: `GetByHashAsync`, `AddAsync`, `RevokeAsync`, `RevokeAllForUserAsync`
- [x] 9.3 Implementar `JwtService : IJwtService`: genera JWT con claims `sub`, `tenant_id`, `role`, `scopes`; dura 15 min configurable
- [x] 9.4 Implementar `TenantContext : ITenantContext` (clase scoped que se rellena desde el middleware)

## 10. API layer — Middleware

- [x] 10.1 Implementar `TenantResolutionMiddleware`: extrae `tenant_id` del JWT, instancia `ITenantContext`, maneja caso SuperAdmin sin tenant
- [x] 10.2 Implementar `ExceptionHandlingMiddleware`: captura excepciones del dominio y las traduce a respuestas HTTP con formato `ProblemDetail` (RFC 7807)

## 11. API layer — Controlador y configuración

- [x] 11.1 Crear `AuthController` con endpoints: `POST /v1/auth/login`, `POST /v1/auth/refresh`, `POST /v1/auth/logout`, `GET /v1/auth/me`, `PATCH /v1/auth/me/password`, `POST /v1/auth/forgot-password`, `POST /v1/auth/reset-password`
- [x] 11.2 Crear `SessionsController` con endpoints: `GET /v1/auth/me/sessions`, `DELETE /v1/auth/me/sessions/{id}`
- [x] 11.3 Configurar autenticación JWT Bearer en `Program.cs` con validación de `Issuer`, `Audience`, `Lifetime` y `SigningKey`
- [x] 11.4 Registrar políticas de autorización en `Program.cs`: `RequireSuperAdmin`, `RequireTenantOwner`, `RequireStaffOrAbove`, `RequireCoachOrAbove`
- [x] 11.5 Registrar todos los handlers, repositorios y servicios en el DI container en `Program.cs`
- [x] 11.6 Configurar Swagger con soporte para `BearerAuth` (botón "Authorize" en Swagger UI)
- [x] 11.7 Configurar Serilog con output a consola y archivo, con enriquecimiento de `TenantId` y `UserId` desde el contexto

## 12. Tests

- [x] 12.1 Tests unitarios para `LoginCommandHandler`: escenarios exitoso, credenciales inválidas, tenant inactivo, cuenta suspendida
- [x] 12.2 Tests unitarios para `RefreshTokenCommandHandler`: escenarios exitoso, token vencido, token comprometido (rotation attack)
- [x] 12.3 Tests unitarios para `JwtService`: verificar que los claims del token son correctos por rol
- [ ] 12.4 Tests unitarios para `TenantResolutionMiddleware`: tenant válido, SuperAdmin sin tenant, request sin JWT
- [ ] 12.5 Test de integración: flujo completo login → uso de API → refresh → logout usando `WebApplicationFactory`
