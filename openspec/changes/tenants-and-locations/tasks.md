## 1. Domain layer — Tenant y Location

- [x] 1.1 Crear value object `TenantId` si no existe aún (ya existe en `SportFlow.Domain/Shared/ValueObjects/`) — verificar y reusar
- [x] 1.2 Crear value object `LocationId` (strongly-typed wrapper de `Guid`) en `SportFlow.Domain/Shared/ValueObjects/`
- [x] 1.3 Crear entidad `Tenant` con propiedades: `Id`, `Name`, `Slug`, `Status`, `Plan`, `CreatedAt`, `UpdatedAt` y factory method `Create(name, slug, plan)`
- [x] 1.4 Crear entidad `Location` con propiedades: `Id`, `TenantId`, `Name`, `Address?`, `Timezone`, `MaxCapacity?`, `IsActive`, `CreatedAt`, `UpdatedAt` y factory methods `Create(...)` y `Deactivate()`
- [x] 1.5 Crear interfaz `ITenantRepository` con métodos: `GetByIdAsync`, `GetBySlugAsync` en `SportFlow.Domain/Tenants/`
- [x] 1.6 Crear interfaz `ILocationRepository` con métodos: `GetByTenantAsync`, `GetByIdAsync`, `AddAsync`, `UpdateAsync` en `SportFlow.Domain/Tenants/`

## 2. Application layer — DTOs

- [x] 2.1 Crear records de response: `TenantMeResponse`, `LocationResponse` en `SportFlow.Application/Tenants/DTOs/`
- [x] 2.2 Crear records de request: `CreateLocationRequest`, `UpdateLocationRequest` en `SportFlow.Application/Tenants/DTOs/`

## 3. Application layer — Query handlers

- [x] 3.1 Implementar `GetTenantMeQueryHandler`: lee `ITenantContext.TenantId`, busca en `ITenantRepository`, retorna `TenantMeResponse`; retorna error si `TenantId` es null (SuperAdmin) o tenant no encontrado
- [x] 3.2 Implementar `GetLocationsQueryHandler`: lista todas las locations activas del tenant desde `ILocationRepository`
- [x] 3.3 Implementar `GetLocationByIdQueryHandler`: obtiene una location por ID verificando que pertenece al tenant del contexto

## 4. Application layer — Command handlers

- [x] 4.1 Implementar `CreateLocationCommandHandler`: valida request, crea `Location`, persiste via `ILocationRepository` + `IUnitOfWork`, retorna `LocationResponse`
- [x] 4.2 Implementar `UpdateLocationCommandHandler`: encuentra location por ID, aplica cambios parciales (solo campos no-null), persiste, retorna `LocationResponse` actualizado
- [x] 4.3 Implementar `DeactivateLocationCommandHandler`: encuentra location, llama `location.Deactivate()`, persiste, retorna sin body

## 5. Application layer — Validadores

- [x] 5.1 Crear `CreateLocationRequestValidator`: `Name` requerido (max 100 chars), `Timezone` requerido y válido IANA (`TimeZoneInfo.TryFindSystemTimeZoneById`), `MaxCapacity` > 0 si se provee
- [x] 5.2 Crear `UpdateLocationRequestValidator`: campos opcionales, pero si `Timezone` está presente MUST ser IANA válido; si `MaxCapacity` está presente MUST ser > 0

## 6. Infrastructure layer — DbContext y configuraciones EF Core

- [x] 6.1 Agregar `DbSet<Tenant>` y `DbSet<Location>` a `SportFlowDbContext`
- [x] 6.2 Crear `TenantsConfiguration : IEntityTypeConfiguration<Tenant>`: tabla `Tenants`, PK con conversión `TenantId ↔ Guid`, índice único en `Slug`, Global Query Filter `WHERE status != 'cancelled'`
- [x] 6.3 Crear `LocationsConfiguration : IEntityTypeConfiguration<Location>`: tabla `Locations`, PK con conversión `LocationId ↔ Guid`, FK a `Tenants`, índice en `(tenant_id, name)`, Global Query Filter `WHERE is_active = true`
- [x] 6.4 Generar migración EF Core: `AddTenantsAndLocations`
- [x] 6.5 Crear script de seed SQL (o migración de datos) que inserte el tenant "demo" con ID `00000000-0000-0000-0000-000000000001` para desarrollo local

## 7. Infrastructure layer — Repositorios

- [x] 7.1 Implementar `TenantRepository : ITenantRepository` (reemplaza `StubTenantRepository`) con `GetByIdAsync` y `GetBySlugAsync` usando EF Core
- [x] 7.2 Implementar `LocationRepository : ILocationRepository` con `GetByTenantAsync`, `GetByIdAsync`, `AddAsync`, `UpdateAsync`

## 8. API layer — Controlador y configuración

- [x] 8.1 Crear `TenantsController` con endpoints: `GET /v1/tenants/me`, `GET /v1/tenants/me/locations`, `GET /v1/tenants/me/locations/{id}`, `POST /v1/tenants/me/locations`, `PATCH /v1/tenants/me/locations/{id}`, `DELETE /v1/tenants/me/locations/{id}`
- [x] 8.2 Registrar política `RequireTenantManagerOrAbove` en `Program.cs` (roles: SuperAdmin, TenantOwner, TenantManager)
- [x] 8.3 Registrar en DI: `TenantRepository`, `LocationRepository`, handlers de tenants y locations; reemplazar `StubTenantRepository` por `TenantRepository`

## 9. Tests

- [x] 9.1 Tests unitarios para `GetTenantMeQueryHandler`: escenarios exitoso, tenant no encontrado, SuperAdmin sin tenant
- [x] 9.2 Tests unitarios para `CreateLocationCommandHandler`: escenarios exitoso, timezone inválido, nombre vacío
- [x] 9.3 Tests unitarios para `UpdateLocationCommandHandler`: actualización parcial, location no encontrada
- [x] 9.4 Tests unitarios para `DeactivateLocationCommandHandler`: baja exitosa, location no encontrada
- [x] 9.5 Actualizar `SportFlowWebAppFactory` en tests de integración: agregar seed de `Tenant` en InMemory DB
- [x] 9.6 Agregar tests de integración: `GET /v1/tenants/me` retorna 200; `POST /v1/tenants/me/locations` con rol insuficiente retorna 403; flujo completo crear → listar → desactivar sede
