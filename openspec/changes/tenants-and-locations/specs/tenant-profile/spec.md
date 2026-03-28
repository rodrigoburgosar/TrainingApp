## ADDED Requirements

### Requirement: Consulta del perfil del tenant activo
El sistema SHALL exponer un endpoint `GET /v1/tenants/me` que retorne los datos del tenant asociado al JWT activo. Cualquier usuario autenticado (independientemente de su rol) SHALL poder acceder a este endpoint.

#### Scenario: Usuario autenticado consulta su tenant
- **WHEN** un usuario autenticado envía `GET /v1/tenants/me` con un JWT válido que contiene `tenant_id`
- **THEN** el sistema retorna `200 OK` con `TenantMeResponse` conteniendo `id`, `name`, `slug`, `status`, `plan` y `createdAt` del tenant correspondiente

#### Scenario: Tenant no encontrado
- **WHEN** un usuario autenticado envía `GET /v1/tenants/me` pero el `tenant_id` del JWT no corresponde a ningún tenant activo en la base de datos
- **THEN** el sistema retorna `404 Not Found`

#### Scenario: SuperAdmin sin tenant no puede acceder
- **WHEN** un SuperAdmin (sin `tenant_id` en el JWT) envía `GET /v1/tenants/me`
- **THEN** el sistema retorna `400 Bad Request` con código `NO_TENANT_CONTEXT`

#### Scenario: Request sin autenticación es rechazado
- **WHEN** se envía `GET /v1/tenants/me` sin header `Authorization`
- **THEN** el sistema retorna `401 Unauthorized`

---

### Requirement: Datos del tenant persistidos en base de datos
El sistema SHALL almacenar los tenants en la tabla `Tenants` de PostgreSQL. La fuente de verdad para los datos del tenant SHALL ser la base de datos, no archivos de configuración.

#### Scenario: Tenant activo es retornado
- **WHEN** el tenant tiene `Status = "active"` en la base de datos
- **THEN** `GET /v1/tenants/me` retorna el tenant con sus datos actuales

#### Scenario: Tenant suspendido sigue siendo visible para sus usuarios
- **WHEN** el tenant tiene `Status = "suspended"`
- **THEN** `GET /v1/tenants/me` retorna el tenant con `status = "suspended"` (el usuario puede ver el estado pero no operar)

#### Scenario: Tenant cancelado es invisible
- **WHEN** el tenant tiene `Status = "cancelled"`
- **THEN** el Global Query Filter excluye al tenant de todos los queries y `GET /v1/tenants/me` retorna `404 Not Found`
