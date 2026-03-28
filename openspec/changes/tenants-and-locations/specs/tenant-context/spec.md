## MODIFIED Requirements

### Requirement: Resolución automática del tenant desde el JWT
El sistema SHALL extraer el `tenant_id` del JWT en cada request autenticado y exponerlo como `ITenantContext` en el contenedor de DI, con scope `Scoped` (por request). Todos los repositorios y handlers SHALL obtener el `tenant_id` desde `ITenantContext`, nunca desde parámetros de la URL. La validación de existencia del tenant SHALL realizarse contra la tabla `Tenants` en la base de datos; el archivo `appsettings.json` ya no es la fuente de verdad para datos de tenants.

#### Scenario: Request autenticado con tenant válido
- **WHEN** un request llega con un JWT válido que contiene el claim `tenant_id`
- **THEN** el middleware inyecta un `ITenantContext` con `TenantId` y `TenantSlug` correctos, disponible para toda la cadena de procesamiento del request

#### Scenario: Request de SuperAdmin sin tenant
- **WHEN** un request llega con un JWT válido cuyo `system_role` es `SuperAdmin` y no contiene `tenant_id`
- **THEN** el middleware inyecta un `ITenantContext` con `IsSuperAdmin = true` y `TenantId = null`, y el request continúa sin filtro de tenant

#### Scenario: Request sin JWT
- **WHEN** un request llega a un endpoint protegido sin header `Authorization`
- **THEN** el middleware de autenticación de ASP.NET Core retorna `401 Unauthorized` antes de que el `TenantResolutionMiddleware` procese el tenant

---

### Requirement: Aislamiento de datos por tenant via Global Query Filters
El sistema SHALL configurar EF Core Global Query Filters en `SportFlowDbContext` para que toda query a entidades multi-tenant filtre automáticamente por `tenant_id = ITenantContext.TenantId`. Ningún query de datos de un tenant SHALL retornar datos de otro tenant.

#### Scenario: Query estándar filtrada por tenant
- **WHEN** un handler ejecuta `dbContext.Persons.ToListAsync()`
- **THEN** EF Core genera SQL con `WHERE tenant_id = @currentTenantId` automáticamente

#### Scenario: SuperAdmin omite el filtro de tenant
- **WHEN** un handler ejecuta una query con `IsSuperAdmin = true` en el contexto
- **THEN** el Global Query Filter está deshabilitado y la query retorna datos de todos los tenants

#### Scenario: Acceso cruzado imposible por URL incorrecta
- **WHEN** un usuario del tenant A envía un request con el ID de un recurso del tenant B
- **THEN** el Global Query Filter hace que EF Core no encuentre el recurso y el handler retorna `404 Not Found`
