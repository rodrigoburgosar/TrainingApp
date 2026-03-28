## ADDED Requirements

### Requirement: Listado de sedes del tenant
El sistema SHALL exponer `GET /v1/tenants/me/locations` que retorne todas las sedes activas del tenant del usuario autenticado. Cualquier rol autenticado SHALL poder listar las sedes.

#### Scenario: Listado exitoso de sedes activas
- **WHEN** un usuario autenticado envía `GET /v1/tenants/me/locations`
- **THEN** el sistema retorna `200 OK` con un array de `LocationResponse` con las sedes donde `IsActive = true`

#### Scenario: Tenant sin sedes retorna lista vacía
- **WHEN** el tenant no tiene sedes registradas
- **THEN** el sistema retorna `200 OK` con un array vacío `[]`

#### Scenario: Sedes de otro tenant no aparecen
- **WHEN** existe una sede activa de otro tenant con el mismo nombre
- **THEN** el sistema retorna solo las sedes del tenant del JWT, sin cruce de datos

---

### Requirement: Consulta de sede por ID
El sistema SHALL exponer `GET /v1/tenants/me/locations/{id}` que retorne el detalle de una sede específica. Cualquier rol autenticado SHALL poder consultarla.

#### Scenario: Sede encontrada
- **WHEN** un usuario autenticado envía `GET /v1/tenants/me/locations/{id}` con un ID válido de su tenant
- **THEN** el sistema retorna `200 OK` con el `LocationResponse` completo

#### Scenario: Sede no encontrada o inactiva
- **WHEN** el ID no corresponde a ninguna sede activa del tenant
- **THEN** el sistema retorna `404 Not Found`

---

### Requirement: Creación de sede
El sistema SHALL exponer `POST /v1/tenants/me/locations`. Solo los roles `TenantOwner` y `TenantManager` SHALL poder crear sedes. El campo `Timezone` MUST ser un timezone IANA válido.

#### Scenario: Creación exitosa de sede
- **WHEN** un `TenantOwner` o `TenantManager` envía `POST /v1/tenants/me/locations` con `name` y `timezone` válidos
- **THEN** el sistema persiste la sede con `IsActive = true` y retorna `201 Created` con el `LocationResponse` de la nueva sede

#### Scenario: Timezone inválido es rechazado
- **WHEN** se envía `POST /v1/tenants/me/locations` con un `timezone` que no es un identificador IANA válido (ej. "hora_chilena")
- **THEN** el sistema retorna `400 Bad Request` con detalle del campo inválido

#### Scenario: Nombre vacío es rechazado
- **WHEN** se envía `POST /v1/tenants/me/locations` sin el campo `name` o con `name` vacío
- **THEN** el sistema retorna `400 Bad Request`

#### Scenario: Rol insuficiente es rechazado
- **WHEN** un usuario con rol `Staff`, `Coach` o `Member` intenta crear una sede
- **THEN** el sistema retorna `403 Forbidden`

---

### Requirement: Actualización de sede
El sistema SHALL exponer `PATCH /v1/tenants/me/locations/{id}` para actualizar parcialmente una sede. Solo `TenantOwner` y `TenantManager` SHALL poder actualizar sedes. Solo los campos provistos en el body MUST ser modificados.

#### Scenario: Actualización parcial exitosa
- **WHEN** un `TenantManager` envía `PATCH /v1/tenants/me/locations/{id}` con solo `maxCapacity`
- **THEN** solo el campo `maxCapacity` es actualizado y el resto permanece sin cambios; retorna `200 OK` con `LocationResponse` actualizado

#### Scenario: Sede no encontrada al actualizar
- **WHEN** se envía `PATCH /v1/tenants/me/locations/{id}` con un ID inexistente
- **THEN** el sistema retorna `404 Not Found`

#### Scenario: Timezone inválido en update es rechazado
- **WHEN** se envía `PATCH /v1/tenants/me/locations/{id}` con un `timezone` inválido
- **THEN** el sistema retorna `400 Bad Request`

---

### Requirement: Baja lógica de sede
El sistema SHALL exponer `DELETE /v1/tenants/me/locations/{id}` que realiza una baja lógica (setea `IsActive = false`). Solo `TenantOwner` y `TenantManager` SHALL poder dar de baja sedes. Los registros históricos que referencien la sede MUST permanecer intactos.

#### Scenario: Baja lógica exitosa
- **WHEN** un `TenantOwner` envía `DELETE /v1/tenants/me/locations/{id}` para una sede activa
- **THEN** el sistema setea `IsActive = false` y retorna `204 No Content`; la sede ya no aparece en listados

#### Scenario: Sede ya inactiva retorna 404
- **WHEN** se envía `DELETE /v1/tenants/me/locations/{id}` para una sede con `IsActive = false`
- **THEN** el sistema retorna `404 Not Found` (el Global Query Filter la oculta)

#### Scenario: Rol insuficiente es rechazado
- **WHEN** un usuario con rol `Staff` intenta dar de baja una sede
- **THEN** el sistema retorna `403 Forbidden`
