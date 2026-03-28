## ADDED Requirements

### Requirement: Jerarquía de roles del sistema
El sistema SHALL implementar 6 roles con permisos diferenciados. Los roles se asignan en el JWT al momento del login según la tabla `UserTenantRoles`. Un usuario puede tener distintos roles en distintos tenants.

#### Scenario: SuperAdmin accede a cualquier endpoint
- **WHEN** un request llega con JWT de rol `SuperAdmin`
- **THEN** el sistema permite el acceso a cualquier endpoint, incluidos los endpoints de gestión de plataforma

#### Scenario: TenantOwner accede a su tenant completo
- **WHEN** un request llega con JWT de rol `TenantOwner` para el tenant X
- **THEN** el sistema permite acceso a todos los endpoints del tenant X y rechaza acceso a datos de otros tenants

#### Scenario: Member solo accede a sus propios datos
- **WHEN** un usuario con rol `Member` intenta acceder a `GET /v1/persons` (listado de todos los socios)
- **THEN** el sistema retorna `403 Forbidden`

#### Scenario: Usuario sin rol para el endpoint requerido
- **WHEN** un usuario con rol `Coach` intenta acceder a un endpoint que requiere `TenantManager` o superior
- **THEN** el sistema retorna `403 Forbidden`

---

### Requirement: Protección de endpoints con políticas de autorización
El sistema SHALL proteger todos los endpoints con el atributo `[Authorize]` como mínimo. Los endpoints que requieren roles específicos SHALL usar políticas nombradas (`[Authorize(Policy = "RequireAdmin")]`). Los endpoints públicos SHALL usar `[AllowAnonymous]` explícitamente.

#### Scenario: Endpoint protegido sin token
- **WHEN** un request llega a cualquier endpoint protegido sin `Authorization: Bearer <token>`
- **THEN** el sistema retorna `401 Unauthorized`

#### Scenario: Endpoint protegido con token de rol insuficiente
- **WHEN** un request llega a un endpoint que requiere `TenantOwner` con un JWT de rol `Staff`
- **THEN** el sistema retorna `403 Forbidden`

#### Scenario: Endpoint público accesible sin autenticación
- **WHEN** un request llega a `POST /v1/auth/login` sin `Authorization` header
- **THEN** el sistema procesa el request normalmente (el endpoint tiene `[AllowAnonymous]`)

---

### Requirement: Scopes en el JWT para autorización granular en frontend
El sistema SHALL incluir en el JWT un array de `scopes` derivados del rol del usuario. Los scopes permiten al frontend mostrar/ocultar elementos de UI sin consultar el backend.

#### Scenario: JWT de TenantOwner contiene scopes de admin
- **WHEN** un TenantOwner hace login exitoso
- **THEN** el JWT incluye scopes como `["tenant:admin", "billing:read", "billing:write", "scheduling:write", "persons:write"]`

#### Scenario: JWT de Member contiene solo scopes propios
- **WHEN** un Member hace login exitoso
- **THEN** el JWT incluye solo `["member:self"]`

---

### Requirement: Gestión de roles de usuario por tenant
El sistema SHALL almacenar la asignación de roles en la tabla `UserTenantRoles`. Un usuario puede tener un solo rol por tenant (no múltiples roles simultáneos en el mismo tenant). El rol se puede cambiar por un admin del tenant.

#### Scenario: Usuario con roles en múltiples tenants
- **WHEN** el usuario hace login con el tenantSlug del tenant A
- **THEN** el JWT contiene el rol que ese usuario tiene en el tenant A, sin información de sus roles en otros tenants

#### Scenario: Cambio de tenant requiere nuevo login
- **WHEN** un TenantOwner que tiene roles en el tenant A y tenant B quiere operar en el tenant B
- **THEN** debe hacer login nuevamente con el tenantSlug del tenant B para obtener un JWT con el contexto del tenant B
