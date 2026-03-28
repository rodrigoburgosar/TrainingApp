## Why

El módulo Identity ya emite JWTs con `tenant_id` y `tenant_slug`, pero no existe ningún endpoint que exponga la información del tenant al cliente ni permita gestionar sus sedes (locations). Sin estos endpoints, una app web o móvil no puede mostrar el nombre del gym, su plan activo ni las sedes disponibles para agendar clases o registrar asistencia.

## What Changes

- Nuevo endpoint `GET /v1/tenants/me` — devuelve la información del tenant asociado al JWT activo (nombre, slug, plan, estado, configuración básica)
- Nuevo CRUD de sedes bajo `GET|POST /v1/tenants/me/locations` y `GET|PATCH|DELETE /v1/tenants/me/locations/{id}`
- Nueva entidad de dominio `Tenant` (hasta ahora era solo configuración estática en `appsettings`)
- Nueva entidad de dominio `Location` con campos: nombre, dirección, timezone, capacidad máxima, estado activo
- Migración del `StubTenantRepository` a un repositorio real con tabla `Tenants` en PostgreSQL
- Políticas de acceso: `TenantOwner` y `TenantManager` pueden crear/editar/eliminar sedes; `Staff`, `Coach` y `Member` solo lectura

## Capabilities

### New Capabilities

- `tenant-profile`: Consulta del perfil del tenant activo — `GET /v1/tenants/me` con datos de nombre, slug, plan, estado y timestamps
- `location-management`: CRUD completo de sedes de un tenant — listado, creación, edición y baja lógica de locations

### Modified Capabilities

- `tenant-context`: El `ITenantContext` pasa a cargarse desde la base de datos (tabla `Tenants`) en lugar de `appsettings.json`. El contrato de la interfaz no cambia, pero la fuente de verdad sí.

## Impact

- **Domain**: nuevas entidades `Tenant`, `Location`, value objects `LocationId`
- **Application**: nuevos handlers `GetTenantMeQueryHandler`, `GetLocationsQueryHandler`, `GetLocationByIdQueryHandler`, `CreateLocationCommandHandler`, `UpdateLocationCommandHandler`, `DeactivateLocationCommandHandler`
- **Infrastructure**: `TenantRepository` real reemplaza `StubTenantRepository`; nuevo `LocationRepository`; configuraciones EF Core para `Tenants` y `Locations`; migración `AddTenantsAndLocations`
- **API**: nuevo `TenantsController` con los 6 endpoints; política `RequireTenantManagerOrAbove` para mutaciones
- **Breaking**: `StubTenantRepository` desaparece — requiere que los tenants existan en DB. El seed de tests de integración debe incluir una fila en `Tenants`.
