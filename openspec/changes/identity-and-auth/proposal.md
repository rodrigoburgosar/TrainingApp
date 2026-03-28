## Why

SportFlow necesita un sistema de autenticación y autorización como fundamento de toda la plataforma. Sin él, ningún otro dominio puede construirse — no hay forma de saber quién hace qué, en nombre de qué tenant, con qué permisos. Es el change 1 de 10 y desbloquea todos los demás.

## What Changes

- Nuevo módulo `Identity` con login, refresh token y logout
- Emisión y validación de JWT con claims de `tenant_id`, `role` y `scopes`
- Middleware de resolución de tenant (`TenantResolutionMiddleware`) que extrae el `tenant_id` del JWT y lo inyecta en `ITenantContext` para todos los módulos
- Sistema de roles jerárquico: `SuperAdmin`, `TenantOwner`, `TenantManager`, `Staff`, `Coach`, `Member`
- Tabla `UserTenantRoles` para que un usuario pueda ser owner/manager de múltiples tenants
- Endpoint `GET /v1/auth/me` que retorna el perfil del usuario autenticado
- Gestión de contraseñas: cambio y reset por email
- Gestión de sesiones: listar y revocar refresh tokens activos
- Atributos de autorización por rol para proteger endpoints en todos los dominios futuros

## Capabilities

### New Capabilities

- `user-authentication`: Login con email+password+tenantSlug, emisión de JWT (access + refresh), renovación de tokens, logout y revocación de sesiones
- `tenant-context`: Middleware que resuelve el tenant activo desde el JWT y lo expone como `ITenantContext` en el DI container, habilitando los Global Query Filters de EF Core para multi-tenancy
- `role-authorization`: Definición de roles del sistema (`SuperAdmin` → `Member`), tabla `UserTenantRoles` para asignación de roles por tenant, políticas de autorización de ASP.NET Core por rol y scope

### Modified Capabilities

_(ninguna — proyecto nuevo, no hay specs existentes)_

## Impact

- **Domain layer**: Entidades `User`, `RefreshToken`, `UserTenantRole`; value objects `UserId`, `TenantId`
- **Application layer**: Handlers `LoginCommand`, `RefreshTokenCommand`, `LogoutCommand`, `GetMeQuery`, `ChangePasswordCommand`, `ForgotPasswordCommand`, `ResetPasswordCommand`; interfaz `ITenantContext`
- **Infrastructure layer**: `SportFlowDbContext` inicial con tablas `Users`, `RefreshTokens`, `UserTenantRoles`; `JwtService`; primera migración EF Core
- **API layer**: `AuthController` con 8 endpoints; `TenantResolutionMiddleware`; configuración de JWT Bearer en `Program.cs`; políticas de autorización por rol
- **Dependencias externas**: `Microsoft.AspNetCore.Authentication.JwtBearer`, `Microsoft.AspNetCore.Identity.Core`, `System.IdentityModel.Tokens.Jwt`
- **No breaking changes** (proyecto nuevo)
