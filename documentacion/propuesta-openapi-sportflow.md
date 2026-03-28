# Propuesta de Especificación OpenAPI 3.1
## SportFlow SaaS — Plataforma Multi-Tenant de Gestión Deportiva

**Versión:** 1.0.0  
**Fecha:** Marzo 2026  
**Estado:** Borrador para revisión técnica  
**Audiencia:** Equipo de arquitectura, líderes técnicos, product management

---

## Tabla de Contenidos

1. [Visión General de la API](#1-visión-general-de-la-api)
2. [Principios de Diseño](#2-principios-de-diseño)
3. [Seguridad y Autenticación](#3-seguridad-y-autenticación)
4. [Convenciones Globales](#4-convenciones-globales)
5. [Estructura de Dominios](#5-estructura-de-dominios)
6. [Dominio: Identity](#6-dominio-identity)
7. [Dominio: Tenants](#7-dominio-tenants)
8. [Dominio: Persons](#8-dominio-persons)
9. [Dominio: Memberships](#9-dominio-memberships)
10. [Dominio: Billing](#10-dominio-billing)
11. [Dominio: Scheduling](#11-dominio-scheduling)
12. [Dominio: Attendance](#12-dominio-attendance)
13. [Dominio: Competitions](#13-dominio-competitions)
14. [Dominio: Scoring](#14-dominio-scoring)
15. [Dominio: Forms](#15-dominio-forms)
16. [Dominio: Notifications](#16-dominio-notifications)
17. [Dominio: Reports](#17-dominio-reports)
18. [Schemas Transversales](#18-schemas-transversales)
19. [Códigos de Error Propios](#19-códigos-de-error-propios)
20. [Parámetros y Componentes Globales](#20-parámetros-y-componentes-globales)
21. [Estructura Raíz del Spec](#21-estructura-raíz-del-spec)
22. [Guía de Implementación por Fases](#22-guía-de-implementación-por-fases)
23. [Decisiones de Diseño y Justificaciones](#23-decisiones-de-diseño-y-justificaciones)
24. [Checklist de Calidad](#24-checklist-de-calidad)

---

## 1. Visión General de la API

SportFlow expone una **API REST versionada** bajo el paradigma **API-first**: el contrato OpenAPI es la fuente de verdad antes que el código. Toda funcionalidad de la plataforma, sin excepción, es accesible a través de esta API, incluyendo las interfaces web propia, la app móvil y cualquier integración de terceros.

### 1.1 Características Principales

La API está diseñada para soportar:

- **Multi-tenancy real**: cada tenant opera en un espacio lógico completamente aislado, sin posibilidad de cruce de datos
- **Multi-sede**: una organización puede gestionar N sedes desde la misma cuenta con configuraciones independientes
- **Multi-deporte**: la plataforma no es rígida para ningún deporte; los dominios de scheduling, competencias y scoring son configurables por disciplina
- **Multi-rol**: el mismo JWT puede representar a un administrador, coach, atleta, juez o integrante de staff, con scopes diferenciados

### 1.2 Servidores

```yaml
servers:
  - url: https://api.sportflow.io/v1
    description: Producción
  - url: https://api.staging.sportflow.io/v1
    description: Staging
  - url: http://localhost:8080/v1
    description: Desarrollo local
```

### 1.3 Versionado

- La versión mayor va en el path: `/v1/`, `/v2/`
- Versiones menores se comunican vía headers `API-Version: 1.3`
- Deprecación de endpoints: headers `Deprecation: true` y `Sunset: <fecha ISO>`
- No se rompe un contrato sin bump de versión mayor
- Ambas versiones coexisten durante mínimo **6 meses** antes de retirar la anterior

---

## 2. Principios de Diseño

### 2.1 API-First

El spec OpenAPI se escribe y revisa **antes** de escribir código. Ningún endpoint puede existir sin documentación en el spec. Los contratos se versionan en el repositorio junto al código fuente.

### 2.2 RESTful con Pragmatismo

Se respetan los verbos HTTP según semántica:

| Verbo    | Uso                                              |
|----------|--------------------------------------------------|
| `GET`    | Lectura, nunca modifica estado                   |
| `POST`   | Creación de recurso o acción sin idempotencia    |
| `PUT`    | Reemplazo total de recurso (poco frecuente)      |
| `PATCH`  | Modificación parcial (principal verbo de update) |
| `DELETE` | Eliminación lógica (soft delete preferido)       |

Las **acciones que no encajan en CRUD** se modelan como sub-recursos de acción:

```
POST /v1/memberships/subscriptions/{id}/freeze
POST /v1/memberships/subscriptions/{id}/cancel
POST /v1/competitions/{id}/publish
POST /v1/scoring/events/{eventId}/scores/{scoreId}/validate
```

### 2.3 Consistencia sobre Originalidad

Toda respuesta, error, paginación y estructura de request sigue el mismo patrón a lo largo de los 12 dominios. Un desarrollador que aprende un dominio puede predecir cómo funciona cualquier otro.

### 2.4 Seguridad por Defecto

Todos los endpoints requieren autenticación excepto los marcados explícitamente como `public`. La ausencia de `security` en un endpoint es un error de documentación, no un endpoint público.

### 2.5 Nunca Exponer IDs Internos

Todos los identificadores en la API son `UUID v4`. Los IDs secuenciales de base de datos nunca se exponen. El `tenant_id` jamás viaja en el path: se extrae exclusivamente del JWT.

---

## 3. Seguridad y Autenticación

### 3.1 Security Schemes

```yaml
components:
  securitySchemes:
    BearerAuth:
      type: http
      scheme: bearer
      bearerFormat: JWT
      description: >
        JWT firmado por el servidor de identidad de SportFlow.
        El token contiene: sub (person_id), tenant_id, tenant_slug,
        role, scopes, location_ids (sedes autorizadas), exp, iat.
        El tenant_id se extrae del token; nunca debe ir en el path.

    ApiKeyAuth:
      type: apiKey
      in: header
      name: X-API-Key
      description: >
        Para integraciones máquina-a-máquina: torniquetes, kioscos de check-in,
        integraciones de pasarela de pago, webhooks entrantes.
        Cada API Key está asociada a un tenant y tiene scopes limitados.
```

### 3.2 Roles y Scopes

Los scopes se incluyen en el payload del JWT como array `scopes`:

| Rol        | Scope              | Descripción                                              |
|------------|--------------------|----------------------------------------------------------|
| `superadmin` | `platform:admin` | Solo Anthropic/SportFlow. Acceso transversal a tenants   |
| `admin`    | `tenant:admin`     | Administrador completo del tenant                        |
| `staff`    | `tenant:staff`     | Operaciones del día a día, sin configuración             |
| `coach`    | `tenant:coach`     | Gestión de clases y alumnos propios                      |
| `judge`    | `competition:judge`| Solo validación de scores en competencias asignadas      |
| `member`   | `member:self`      | Solo acceso a sus propios datos y reservas               |
| `api_key`  | Configurado por integración | Acceso programático limitado                  |

### 3.3 Aplicación de Seguridad Global

```yaml
security:
  - BearerAuth: []
```

Declarado a nivel raíz del spec, aplica a todos los endpoints. Los endpoints públicos lo sobreescriben con `security: []`.

### 3.4 Endpoints Públicos (sin autenticación)

```
GET  /v1/competitions/{id}          # Vista pública de competencia
GET  /v1/competitions/{id}/leaderboard  # Leaderboard público
GET  /v1/scheduling/availability    # Disponibilidad pública
POST /v1/auth/login
POST /v1/auth/refresh
POST /v1/auth/forgot-password
POST /v1/auth/reset-password
```

---

## 4. Convenciones Globales

### 4.1 Estructura de Paths

```
/v1/{dominio}/{recurso}                         # colección
/v1/{dominio}/{recurso}/{id}                    # ítem individual
/v1/{dominio}/{recurso}/{id}/{subrecurso}       # sub-colección
/v1/{dominio}/{recurso}/{id}/actions/{acción}   # acción explícita (opcional)
```

Ejemplos:
```
GET  /v1/persons
GET  /v1/persons/{personId}
GET  /v1/persons/{personId}/memberships
POST /v1/memberships/subscriptions/{id}/freeze
GET  /v1/competitions/{id}/categories/{catId}/registrations
```

### 4.2 Envelope de Respuesta

**Colecciones (listados):**

```yaml
# 200 OK
{
  "data": [ { ... }, { ... } ],
  "meta": {
    "page": 1,
    "limit": 20,
    "total": 154,
    "totalPages": 8
  }
}
```

**Ítem individual:**

```yaml
# 200 OK
{
  "data": { ... }
}
```

**Creación exitosa:**

```yaml
# 201 Created
{
  "data": { ... }
}
```

**Acción exitosa sin cuerpo:**

```yaml
# 204 No Content
(sin cuerpo)
```

### 4.3 Paginación

Todos los endpoints de colección aceptan:

| Parámetro | Tipo    | Default | Máx | Descripción                      |
|-----------|---------|---------|-----|----------------------------------|
| `page`    | integer | 1       | —   | Número de página (1-indexed)     |
| `limit`   | integer | 20      | 100 | Registros por página             |
| `sort`    | string  | varía   | —   | Campo(s) de orden. `-campo` = DESC |

Ejemplo: `GET /v1/persons?page=2&limit=50&sort=-createdAt,lastName1`

### 4.4 Filtros Comunes

Los filtros van siempre como query parameters:

```
?status=active
?locationId=uuid
?dateFrom=2026-01-01&dateTo=2026-03-31
?search=texto libre
?personId=uuid
```

### 4.5 Campos de Fecha y Hora

| Tipo       | Formato OpenAPI       | Ejemplo                    |
|------------|-----------------------|----------------------------|
| Fecha      | `format: date`        | `2026-03-25`               |
| Fecha-hora | `format: date-time`   | `2026-03-25T14:30:00-03:00` |
| Duración   | `format: duration`    | `PT1H30M`                  |

Todas las fechas-hora se almacenan en **UTC** y se retornan en UTC. El cliente es responsable de la conversión a la zona horaria local. La zona horaria del tenant se usa para reglas de negocio (ej: inicio de día para cupos diarios).

### 4.6 Soft Delete

Los recursos no se eliminan físicamente de la base de datos. El campo `status` o `deletedAt` marca la eliminación lógica. Los endpoints `DELETE` retornan `204` y marcan el recurso como eliminado/archivado.

---

## 5. Estructura de Dominios

La API está organizada en **12 dominios**, cada uno con prefijo de path propio:

| Dominio         | Prefijo                | Responsabilidad                                     |
|-----------------|------------------------|-----------------------------------------------------|
| Identity        | `/v1/auth`             | Autenticación, tokens, contraseñas                  |
| Tenants         | `/v1/tenants`          | Organizaciones, sedes, configuración                |
| Persons         | `/v1/persons`          | Registro de personas, identificadores               |
| Memberships     | `/v1/memberships`      | Planes y suscripciones                              |
| Billing         | `/v1/billing`          | Facturas, pagos, deuda, descuentos                  |
| Scheduling      | `/v1/scheduling`       | Clases, agenda, reservas                            |
| Attendance      | `/v1/attendance`       | Check-in, asistencia, control de acceso             |
| Competitions    | `/v1/competitions`     | Torneos, eventos, categorías, inscripciones         |
| Scoring         | `/v1/scoring`          | Puntuación, ranking, leaderboard                    |
| Forms           | `/v1/forms`            | Formularios dinámicos                               |
| Notifications   | `/v1/notifications`    | Comunicaciones, plantillas, preferencias            |
| Reports         | `/v1/reports`          | Analítica, KPIs, exportación                        |

---

## 6. Dominio: Identity

**Prefijo:** `/v1/auth`  
**Propósito:** Gestión del ciclo de vida de autenticación. Emite y renueva JWT. No gestiona datos de la persona (eso es responsabilidad del dominio Persons).

### 6.1 Endpoints

```
POST   /v1/auth/login
POST   /v1/auth/refresh
POST   /v1/auth/logout
POST   /v1/auth/forgot-password
POST   /v1/auth/reset-password
GET    /v1/auth/me
PATCH  /v1/auth/me/password
POST   /v1/auth/me/sessions          # listar sesiones activas
DELETE /v1/auth/me/sessions/{id}     # revocar sesión
```

### 6.2 Schemas

```yaml
# POST /v1/auth/login
LoginRequest:
  type: object
  required: [identifier, password, tenantSlug]
  properties:
    identifier:
      type: string
      description: Email o identificador de persona (RUT, DNI, etc.)
    password:
      type: string
      format: password
    tenantSlug:
      type: string
      description: Slug único del tenant. Permite login en múltiples organizaciones.
    locationId:
      type: string
      format: uuid
      description: Sede de acceso. Requerida si el tenant tiene múltiples sedes.

# Respuesta de login exitoso
TokenResponse:
  type: object
  required: [accessToken, refreshToken, expiresIn, tokenType]
  properties:
    accessToken:
      type: string
      description: JWT de corta duración (15-60 min)
    refreshToken:
      type: string
      description: Token opaco de larga duración (30 días)
    expiresIn:
      type: integer
      description: Segundos de validez del access token
    tokenType:
      type: string
      enum: [Bearer]
    me:
      $ref: '#/components/schemas/MeResponse'

# GET /v1/auth/me
MeResponse:
  type: object
  required: [personId, email, role, tenant]
  properties:
    personId:
      type: string
      format: uuid
    firstName:
      type: string
    lastName1:
      type: string
    email:
      type: string
      format: email
    role:
      type: string
      enum: [admin, staff, coach, judge, member]
    scopes:
      type: array
      items:
        type: string
    tenant:
      type: object
      properties:
        id:    { type: string, format: uuid }
        name:  { type: string }
        slug:  { type: string }
        plan:  { type: string }
    authorizedLocations:
      type: array
      items:
        type: string
        format: uuid
    permissions:
      type: array
      items:
        type: string
      description: Lista granular de permisos para uso en frontend
```

### 6.3 Respuestas de Error Específicas

| Status | Código interno     | Cuándo                                        |
|--------|--------------------|-----------------------------------------------|
| 401    | `INVALID_CREDENTIALS` | Credenciales incorrectas                   |
| 401    | `TOKEN_EXPIRED`    | Access token vencido                          |
| 401    | `REFRESH_EXPIRED`  | Refresh token vencido                         |
| 403    | `ACCOUNT_SUSPENDED`| Cuenta suspendida por deuda u otra razón      |
| 403    | `TENANT_INACTIVE`  | El tenant está desactivado                    |
| 404    | `TENANT_NOT_FOUND` | El slug no corresponde a ningún tenant activo |
| 429    | `TOO_MANY_ATTEMPTS`| Rate limit de login (5 intentos / 15 min)     |

---

## 7. Dominio: Tenants

**Prefijo:** `/v1/tenants`  
**Propósito:** Gestión de la organización (tenant) y sus sedes. Cada tenant es un cliente de SportFlow. Una organización puede tener múltiples sedes con configuraciones propias.

### 7.1 Endpoints

```
# Organización
GET    /v1/tenants/me
PATCH  /v1/tenants/me
GET    /v1/tenants/me/settings
PATCH  /v1/tenants/me/settings

# Sedes
GET    /v1/tenants/me/locations
POST   /v1/tenants/me/locations
GET    /v1/tenants/me/locations/{locationId}
PATCH  /v1/tenants/me/locations/{locationId}
DELETE /v1/tenants/me/locations/{locationId}
GET    /v1/tenants/me/locations/{locationId}/settings
PATCH  /v1/tenants/me/locations/{locationId}/settings

# Disciplinas configuradas
GET    /v1/tenants/me/disciplines
POST   /v1/tenants/me/disciplines
PATCH  /v1/tenants/me/disciplines/{disciplineId}
DELETE /v1/tenants/me/disciplines/{disciplineId}

# Staff y roles
GET    /v1/tenants/me/staff
POST   /v1/tenants/me/staff
PATCH  /v1/tenants/me/staff/{staffId}
DELETE /v1/tenants/me/staff/{staffId}
```

### 7.2 Schemas

```yaml
Tenant:
  type: object
  required: [id, name, slug, status]
  properties:
    id:
      type: string
      format: uuid
    name:
      type: string
      example: "Box CrossFit Providencia"
    slug:
      type: string
      description: Identificador único URL-safe. No modificable después de creación.
      example: "crossfit-providencia"
    legalName:
      type: string
    taxId:
      type: string
      description: RUT de empresa en Chile. Formato XX.XXX.XXX-X
    country:
      type: string
      default: CL
    currency:
      type: string
      default: CLP
    timezone:
      type: string
      default: "America/Santiago"
    logoUrl:
      type: string
      format: uri
    plan:
      type: string
      enum: [starter, professional, enterprise]
      description: Plan de SportFlow contratado por el tenant
    status:
      type: string
      enum: [active, trial, suspended, cancelled]
    trialEndsAt:
      type: string
      format: date-time
      nullable: true
    settings:
      $ref: '#/components/schemas/TenantSettings'
    createdAt:
      type: string
      format: date-time

TenantSettings:
  type: object
  properties:
    allowGuestCheckin:
      type: boolean
      default: false
    requireMedicalCertificate:
      type: boolean
      default: false
    defaultBookingCancellationHours:
      type: integer
      default: 2
      description: Horas mínimas de anticipación para cancelar reserva sin penalidad
    noShowPenaltyEnabled:
      type: boolean
      default: false
    noShowPenaltyType:
      type: string
      enum: [block_next, deduct_class, none]
    autoSuspendOnDebt:
      type: boolean
      default: true
    debtThresholdDays:
      type: integer
      default: 7
      description: Días de deuda antes de suspensión automática
    allowWaitlist:
      type: boolean
      default: true
    waitlistAutoPromote:
      type: boolean
      default: true
    memberPortalEnabled:
      type: boolean
      default: true
    publicScheduleEnabled:
      type: boolean
      default: false

Location:
  type: object
  required: [id, name, status]
  properties:
    id:
      type: string
      format: uuid
    name:
      type: string
      example: "Sede Providencia"
    code:
      type: string
      description: Código corto para reportes y etiquetas
      example: "PRV"
    address:
      $ref: '#/components/schemas/Address'
    phone:
      type: string
    email:
      type: string
      format: email
    timezone:
      type: string
      description: Hereda del tenant si no se especifica
    capacity:
      type: integer
      minimum: 1
      description: Aforo máximo del local
    status:
      type: string
      enum: [active, inactive, maintenance]
    openingHours:
      type: array
      description: Horarios de apertura por día de semana
      items:
        type: object
        properties:
          dayOfWeek: { type: integer, minimum: 0, maximum: 6 }
          opensAt:   { type: string, pattern: '^([01]\d|2[0-3]):[0-5]\d$' }
          closesAt:  { type: string, pattern: '^([01]\d|2[0-3]):[0-5]\d$' }
          isClosed:  { type: boolean }
    settings:
      $ref: '#/components/schemas/LocationSettings'

Discipline:
  type: object
  required: [id, name]
  properties:
    id:      { type: string, format: uuid }
    name:    { type: string, example: "CrossFit" }
    slug:    { type: string, example: "crossfit" }
    icon:    { type: string }
    color:   { type: string, pattern: '^#[0-9A-Fa-f]{6}$' }
    scoringUnit:
      type: string
      enum: [reps, kg, seconds, points, distance_m, custom]
    config:
      type: object
      additionalProperties: true
      description: Configuración específica de la disciplina (libre)
    isActive:
      type: boolean
```

---

## 8. Dominio: Persons

**Prefijo:** `/v1/persons`  
**Propósito:** Registro unificado de todas las personas que interactúan con la plataforma: atletas, alumnos, coaches, staff, jueces, apoderados. Una persona existe en el contexto de un tenant y puede tener múltiples identificadores.

### 8.1 Endpoints

```
GET    /v1/persons
POST   /v1/persons
GET    /v1/persons/{personId}
PATCH  /v1/persons/{personId}
DELETE /v1/persons/{personId}

# Búsqueda por identificador (RUT, DNI, pasaporte)
POST   /v1/persons/search

# Merge de registros duplicados
POST   /v1/persons/merge

# Identificadores
GET    /v1/persons/{personId}/identifiers
POST   /v1/persons/{personId}/identifiers
DELETE /v1/persons/{personId}/identifiers/{identifierId}
PATCH  /v1/persons/{personId}/identifiers/{identifierId}/set-primary

# Historia del atleta
GET    /v1/persons/{personId}/memberships
GET    /v1/persons/{personId}/attendance
GET    /v1/persons/{personId}/competition-history
GET    /v1/persons/{personId}/invoices
GET    /v1/persons/{personId}/timeline        # actividad reciente unificada

# QR de acceso
GET    /v1/persons/{personId}/qr-code
POST   /v1/persons/{personId}/qr-code/regenerate
```

### 8.2 Schemas

```yaml
Person:
  type: object
  required: [id, firstName, lastName1, status]
  properties:
    id:
      type: string
      format: uuid
    firstName:
      type: string
      example: "Valentina"
    lastName1:
      type: string
      description: Apellido paterno
      example: "Morales"
    lastName2:
      type: string
      nullable: true
      description: Apellido materno (opcional según país)
      example: "Soto"
    displayName:
      type: string
      readOnly: true
      description: Nombre de visualización calculado
    email:
      type: string
      format: email
      nullable: true
    phone:
      type: string
      nullable: true
    birthDate:
      type: string
      format: date
      nullable: true
    gender:
      type: string
      enum: [male, female, non_binary, prefer_not_to_say]
      nullable: true
    nationality:
      type: string
      description: Código ISO 3166-1 alpha-2
      nullable: true
    isMinor:
      type: boolean
      readOnly: true
      description: true si birthDate indica menor de 18 años
    guardianId:
      type: string
      format: uuid
      nullable: true
      description: Person ID del apoderado. Requerido si isMinor=true
    identifiers:
      type: array
      readOnly: true
      items:
        $ref: '#/components/schemas/PersonIdentifier'
    customFields:
      type: object
      additionalProperties: true
      description: Campos dinámicos configurados por el tenant
    photoUrl:
      type: string
      format: uri
      nullable: true
    roles:
      type: array
      description: Roles del tenant asignados a esta persona
      items:
        type: string
        enum: [member, coach, staff, judge, admin]
    status:
      type: string
      enum: [active, inactive, suspended, blocked]
    suspensionReason:
      type: string
      nullable: true
    locationIds:
      type: array
      description: Sedes a las que pertenece esta persona
      items:
        type: string
        format: uuid
    tags:
      type: array
      items:
        type: string
      description: Etiquetas libres para segmentación
    createdAt:
      type: string
      format: date-time
    updatedAt:
      type: string
      format: date-time

PersonIdentifier:
  type: object
  required: [id, type, value]
  properties:
    id:
      type: string
      format: uuid
    type:
      type: string
      enum: [rut, dni, passport, internal, other]
    value:
      type: string
      description: Valor del identificador (RUT en formato sin puntos con guión: 12345678-9)
    country:
      type: string
      nullable: true
      description: País emisor (para pasaporte o DNI)
    isPrimary:
      type: boolean
      description: Identificador principal de la persona en este tenant
    verifiedAt:
      type: string
      format: date-time
      nullable: true
    verifiedBy:
      type: string
      format: uuid
      nullable: true

# POST /v1/persons/search
PersonSearchRequest:
  type: object
  properties:
    identifierType:
      type: string
      enum: [rut, dni, passport, internal, other, any]
    identifierValue:
      type: string
    email:
      type: string
    phone:
      type: string
    name:
      type: string

# POST /v1/persons/merge
PersonMergeRequest:
  type: object
  required: [primaryPersonId, duplicatePersonIds]
  properties:
    primaryPersonId:
      type: string
      format: uuid
      description: Registro que se conserva como fuente de verdad
    duplicatePersonIds:
      type: array
      items:
        type: string
        format: uuid
      minItems: 1
      description: Registros que se fusionan y eliminan
    mergeStrategy:
      type: object
      description: Campos específicos a tomar de registros secundarios
      properties:
        preferEmail:  { type: string, format: uuid }
        preferPhone:  { type: string, format: uuid }
```

---

## 9. Dominio: Memberships

**Prefijo:** `/v1/memberships`  
**Propósito:** Gestión de planes (configuración) y suscripciones (instancias de plan por persona). Controla el acceso, los límites de uso y el ciclo de vida de la relación comercial con el miembro.

### 9.1 Endpoints

```
# Planes (configuración del tenant)
GET    /v1/memberships/plans
POST   /v1/memberships/plans
GET    /v1/memberships/plans/{planId}
PATCH  /v1/memberships/plans/{planId}
DELETE /v1/memberships/plans/{planId}
POST   /v1/memberships/plans/{planId}/duplicate

# Suscripciones (por persona)
GET    /v1/memberships/subscriptions
POST   /v1/memberships/subscriptions
GET    /v1/memberships/subscriptions/{subscriptionId}
PATCH  /v1/memberships/subscriptions/{subscriptionId}

# Acciones del ciclo de vida
POST   /v1/memberships/subscriptions/{subscriptionId}/freeze
POST   /v1/memberships/subscriptions/{subscriptionId}/unfreeze
POST   /v1/memberships/subscriptions/{subscriptionId}/cancel
POST   /v1/memberships/subscriptions/{subscriptionId}/upgrade
POST   /v1/memberships/subscriptions/{subscriptionId}/downgrade
POST   /v1/memberships/subscriptions/{subscriptionId}/renew
POST   /v1/memberships/subscriptions/{subscriptionId}/suspend
POST   /v1/memberships/subscriptions/{subscriptionId}/reactivate

# Convenios y tarifas especiales
GET    /v1/memberships/agreements
POST   /v1/memberships/agreements
PATCH  /v1/memberships/agreements/{agreementId}
```

### 9.2 Schemas

```yaml
MembershipPlan:
  type: object
  required: [id, name, type, billingCycle, price, currency]
  properties:
    id:
      type: string
      format: uuid
    name:
      type: string
      example: "Plan Ilimitado Mensual"
    description:
      type: string
    type:
      type: string
      enum: [unlimited, classes_pack, days_pack, trial, agreement, custom]
      description: >
        unlimited: sin límite de clases.
        classes_pack: N clases en el período.
        days_pack: acceso en días específicos de la semana.
        trial: período de prueba (una sola vez por persona).
        agreement: tarifa especial por convenio.
        custom: configuración libre.
    billingCycle:
      type: string
      enum: [monthly, quarterly, biannual, annual, one_time]
    price:
      type: number
      format: decimal
      example: 59990
    currency:
      type: string
      example: CLP
    enrollmentFee:
      type: number
      format: decimal
      default: 0
      description: Matrícula cobrada una sola vez al suscribirse
    classesLimit:
      type: integer
      nullable: true
      description: Máximo de clases por período. null = ilimitado
    sessionsPerDay:
      type: integer
      nullable: true
      description: Máximo de reservas por día calendario
    sessionsPerWeek:
      type: integer
      nullable: true
    allowedDaysOfWeek:
      type: array
      items:
        type: integer
        minimum: 0
        maximum: 6
      description: 0=Domingo. Vacío = todos los días
    freezeAllowed:
      type: boolean
      default: true
    freezeMaxDays:
      type: integer
      default: 30
    freezeMinDays:
      type: integer
      default: 7
    freezeMaxTimesPerCycle:
      type: integer
      default: 1
    autoRenew:
      type: boolean
      default: true
    trialDurationDays:
      type: integer
      nullable: true
      description: Solo para type=trial
    locations:
      type: array
      items:
        type: string
        format: uuid
      description: Sedes válidas. Vacío = todas las sedes del tenant
    disciplines:
      type: array
      items:
        type: string
      description: Disciplinas accesibles. Vacío = todas
    isPublic:
      type: boolean
      description: Visible en el portal de miembro para auto-suscripción
    isActive:
      type: boolean
    archivedAt:
      type: string
      format: date-time
      nullable: true

MembershipSubscription:
  type: object
  required: [id, personId, planId, status, startDate]
  properties:
    id:
      type: string
      format: uuid
    personId:
      type: string
      format: uuid
    planId:
      type: string
      format: uuid
    plan:
      $ref: '#/components/schemas/MembershipPlanRef'
    locationId:
      type: string
      format: uuid
      nullable: true
    status:
      type: string
      enum: [active, frozen, suspended, cancelled, expired, pending_payment]
    startDate:
      type: string
      format: date
    endDate:
      type: string
      format: date
      nullable: true
    nextBillingDate:
      type: string
      format: date
      nullable: true
    autoRenew:
      type: boolean
    frozenFrom:
      type: string
      format: date
      nullable: true
    frozenUntil:
      type: string
      format: date
      nullable: true
    frozenReason:
      type: string
      nullable: true
    classesUsed:
      type: integer
      readOnly: true
    classesRemaining:
      type: integer
      nullable: true
      readOnly: true
    enrollmentFeePaid:
      type: boolean
    priceOverride:
      type: number
      nullable: true
      description: Precio diferente al del plan (para convenios o excepciones)
    discountId:
      type: string
      format: uuid
      nullable: true
    notes:
      type: string
    createdAt:
      type: string
      format: date-time

# POST .../freeze
FreezeRequest:
  type: object
  required: [frozenFrom, frozenUntil]
  properties:
    frozenFrom:
      type: string
      format: date
    frozenUntil:
      type: string
      format: date
    reason:
      type: string
      enum: [medical, travel, personal, other]
    notes:
      type: string

# POST .../upgrade y .../downgrade
PlanChangeRequest:
  type: object
  required: [newPlanId]
  properties:
    newPlanId:
      type: string
      format: uuid
    effectiveDate:
      type: string
      format: date
      description: Cuándo aplica el cambio. Default = hoy
    prorateAmount:
      type: boolean
      default: true
      description: Aplicar crédito o cobro proporcional al período vigente
```

---

## 10. Dominio: Billing

**Prefijo:** `/v1/billing`  
**Propósito:** Gestión financiera: facturación, cobranza, control de deuda, descuentos y medios de pago. Se integra con pasarelas externas pero mantiene el estado canónico de las transacciones.

### 10.1 Endpoints

```
# Facturas
GET    /v1/billing/invoices
POST   /v1/billing/invoices
GET    /v1/billing/invoices/{invoiceId}
POST   /v1/billing/invoices/{invoiceId}/pay
POST   /v1/billing/invoices/{invoiceId}/cancel
POST   /v1/billing/invoices/{invoiceId}/refund
POST   /v1/billing/invoices/{invoiceId}/send    # reenviar por email

# Transacciones
GET    /v1/billing/transactions
GET    /v1/billing/transactions/{transactionId}

# Control de deuda
GET    /v1/billing/debts
GET    /v1/billing/debts/{personId}
POST   /v1/billing/debts/{personId}/collect     # cobro manual
POST   /v1/billing/debts/{personId}/write-off   # castigo de deuda

# Descuentos
GET    /v1/billing/discounts
POST   /v1/billing/discounts
GET    /v1/billing/discounts/{discountId}
PATCH  /v1/billing/discounts/{discountId}
DELETE /v1/billing/discounts/{discountId}

# Cupones
POST   /v1/billing/coupons/validate
GET    /v1/billing/coupons
POST   /v1/billing/coupons
PATCH  /v1/billing/coupons/{couponId}
DELETE /v1/billing/coupons/{couponId}
```

### 10.2 Schemas

```yaml
Invoice:
  type: object
  required: [id, personId, status, amount, currency]
  properties:
    id:
      type: string
      format: uuid
    personId:
      type: string
      format: uuid
    subscriptionId:
      type: string
      format: uuid
      nullable: true
    competitionRegistrationId:
      type: string
      format: uuid
      nullable: true
    items:
      type: array
      items:
        $ref: '#/components/schemas/InvoiceItem'
    subtotal:
      type: number
      format: decimal
    discountAmount:
      type: number
      format: decimal
      default: 0
    taxAmount:
      type: number
      format: decimal
      default: 0
    amount:
      type: number
      format: decimal
      description: Total a cobrar (subtotal - discount + tax)
    currency:
      type: string
    status:
      type: string
      enum: [draft, pending, paid, overdue, cancelled, refunded, partial_refund]
    dueDate:
      type: string
      format: date
    paidAt:
      type: string
      format: date-time
      nullable: true
    paymentMethod:
      type: string
      enum: [cash, card, transfer, webpay, flow, mercadopago, other]
      nullable: true
    externalPaymentRef:
      type: string
      nullable: true
      description: ID de transacción en pasarela externa
    discountId:
      type: string
      format: uuid
      nullable: true
    couponCode:
      type: string
      nullable: true
    notes:
      type: string
    createdAt:
      type: string
      format: date-time

InvoiceItem:
  type: object
  required: [description, quantity, unitPrice]
  properties:
    description:
      type: string
      example: "Plan Ilimitado Mensual - Marzo 2026"
    type:
      type: string
      enum: [subscription, enrollment_fee, competition, class, product, other]
    quantity:
      type: integer
      default: 1
    unitPrice:
      type: number
      format: decimal
    total:
      type: number
      format: decimal
      readOnly: true

Discount:
  type: object
  required: [id, name, type, value]
  properties:
    id:
      type: string
      format: uuid
    name:
      type: string
      example: "Descuento Estudiante"
    type:
      type: string
      enum: [percentage, fixed_amount]
    value:
      type: number
      description: Porcentaje (0-100) o monto fijo según type
    appliesToPlanIds:
      type: array
      items:
        type: string
        format: uuid
      description: Vacío = aplica a todos los planes
    maxUsages:
      type: integer
      nullable: true
    validFrom:
      type: string
      format: date
      nullable: true
    validUntil:
      type: string
      format: date
      nullable: true
    isActive:
      type: boolean
```

---

## 11. Dominio: Scheduling

**Prefijo:** `/v1/scheduling`  
**Propósito:** Gestión de la agenda de clases, bloques horarios, reservas, lista de espera y disponibilidad de espacios físicos. Soporta recurrencia mediante RRULE (RFC 5545).

### 11.1 Endpoints

```
# Clases
GET    /v1/scheduling/classes
POST   /v1/scheduling/classes
GET    /v1/scheduling/classes/{classId}
PATCH  /v1/scheduling/classes/{classId}
DELETE /v1/scheduling/classes/{classId}
POST   /v1/scheduling/classes/{classId}/cancel
POST   /v1/scheduling/classes/{classId}/block
POST   /v1/scheduling/classes/{classId}/unblock

# Edición de ocurrencias de clase recurrente
POST   /v1/scheduling/classes/{classId}/exceptions   # modifica una ocurrencia

# Reservas (bookings)
GET    /v1/scheduling/bookings
POST   /v1/scheduling/bookings
GET    /v1/scheduling/bookings/{bookingId}
DELETE /v1/scheduling/bookings/{bookingId}
POST   /v1/scheduling/bookings/{bookingId}/checkin
POST   /v1/scheduling/bookings/{bookingId}/noshow
POST   /v1/scheduling/bookings/{bookingId}/reschedule
POST   /v1/scheduling/bookings/{bookingId}/promote-waitlist  # uso interno

# Disponibilidad (para portales y apps)
GET    /v1/scheduling/availability
GET    /v1/scheduling/availability/{classId}

# Espacios físicos
GET    /v1/scheduling/rooms
POST   /v1/scheduling/rooms
PATCH  /v1/scheduling/rooms/{roomId}
```

### 11.2 Schemas

```yaml
ScheduledClass:
  type: object
  required: [id, locationId, discipline, startAt, endAt, capacity, status]
  properties:
    id:
      type: string
      format: uuid
    locationId:
      type: string
      format: uuid
    roomId:
      type: string
      format: uuid
      nullable: true
    discipline:
      type: string
      example: "crossfit"
    title:
      type: string
      example: "WOD Morning"
    description:
      type: string
    coachId:
      type: string
      format: uuid
      nullable: true
    coachName:
      type: string
      readOnly: true
    startAt:
      type: string
      format: date-time
    endAt:
      type: string
      format: date-time
    durationMinutes:
      type: integer
      readOnly: true
    capacity:
      type: integer
      minimum: 1
      description: Cupos base de la clase
    overCapacity:
      type: integer
      default: 0
      description: Cupos extra permitidos (sobrecupo)
    totalCapacity:
      type: integer
      readOnly: true
      description: capacity + overCapacity
    waitlistEnabled:
      type: boolean
      default: true
    waitlistLimit:
      type: integer
      nullable: true
      description: null = lista de espera ilimitada
    enrolledCount:
      type: integer
      readOnly: true
    waitlistCount:
      type: integer
      readOnly: true
    availableSpots:
      type: integer
      readOnly: true
    isRecurring:
      type: boolean
    recurrenceRuleId:
      type: string
      format: uuid
      nullable: true
    recurrenceRule:
      type: string
      nullable: true
      description: Regla RRULE formato RFC 5545. Ej: FREQ=WEEKLY;BYDAY=MO,WE,FR
    requiredPlanTypes:
      type: array
      items:
        type: string
      description: Tipos de plan que dan acceso. Vacío = cualquier plan activo
    status:
      type: string
      enum: [scheduled, cancelled, completed, blocked]
    cancelReason:
      type: string
      nullable: true
    cancelledAt:
      type: string
      format: date-time
      nullable: true
    isException:
      type: boolean
      description: true si esta ocurrencia fue modificada de la regla de recurrencia
    parentClassId:
      type: string
      format: uuid
      nullable: true

Booking:
  type: object
  required: [id, personId, classId, status]
  properties:
    id:
      type: string
      format: uuid
    personId:
      type: string
      format: uuid
    classId:
      type: string
      format: uuid
    subscriptionId:
      type: string
      format: uuid
      nullable: true
    locationId:
      type: string
      format: uuid
      readOnly: true
    position:
      type: integer
      nullable: true
      description: Posición en lista de espera. null si no está en espera
    isWaitlisted:
      type: boolean
      readOnly: true
    status:
      type: string
      enum: [confirmed, waitlisted, cancelled, checked_in, no_show]
    bookedAt:
      type: string
      format: date-time
    checkedInAt:
      type: string
      format: date-time
      nullable: true
    cancelledAt:
      type: string
      format: date-time
      nullable: true
    cancelReason:
      type: string
      nullable: true
    noShowAt:
      type: string
      format: date-time
      nullable: true
    penaltyApplied:
      type: boolean
      description: Si se aplicó penalidad por no-show

# POST /v1/scheduling/bookings
BookingCreateRequest:
  type: object
  required: [personId, classId]
  properties:
    personId:
      type: string
      format: uuid
    classId:
      type: string
      format: uuid
    forceWaitlist:
      type: boolean
      default: false
      description: Si true y hay cupos, igual pone en lista de espera
    subscriptionId:
      type: string
      format: uuid
      nullable: true
      description: Suscripción específica a descontar. Auto-selecciona si no se indica
```

---

## 12. Dominio: Attendance

**Prefijo:** `/v1/attendance`  
**Propósito:** Registro canónico de asistencia. Puede derivar de un booking confirmado (check-in de reserva) o ser un registro directo de acceso (torniquete, kiosco). Fuente de verdad para reportes de uso y control de morosidad.

### 12.1 Endpoints

```
GET    /v1/attendance
POST   /v1/attendance/checkin           # check-in manual por staff
POST   /v1/attendance/checkin/qr        # check-in por QR del miembro
POST   /v1/attendance/checkin/nfc       # check-in por tarjeta NFC
POST   /v1/attendance/checkin/access-control  # torniquete/kiosco con ApiKey
GET    /v1/attendance/{attendanceId}
PATCH  /v1/attendance/{attendanceId}    # corrección manual
GET    /v1/attendance/report
```

### 12.2 Schemas

```yaml
AttendanceRecord:
  type: object
  required: [id, personId, locationId, status, method]
  properties:
    id:
      type: string
      format: uuid
    personId:
      type: string
      format: uuid
    classId:
      type: string
      format: uuid
      nullable: true
    bookingId:
      type: string
      format: uuid
      nullable: true
    locationId:
      type: string
      format: uuid
    subscriptionId:
      type: string
      format: uuid
      nullable: true
    checkedInAt:
      type: string
      format: date-time
    checkedInBy:
      type: string
      format: uuid
      nullable: true
      description: Staff que realizó el check-in. null si fue automático
    method:
      type: string
      enum: [manual, qr, nfc, access_control, app, kiosk]
    status:
      type: string
      enum: [present, absent, no_show, late, excused]
    lateByMinutes:
      type: integer
      nullable: true
    notes:
      type: string
      nullable: true
    classDeducted:
      type: boolean
      description: Si se descontó una clase del plan en este registro

# POST /v1/attendance/checkin/qr
QrCheckinRequest:
  type: object
  required: [qrToken, locationId]
  properties:
    qrToken:
      type: string
      description: Token codificado en el QR del miembro
    locationId:
      type: string
      format: uuid
    classId:
      type: string
      format: uuid
      nullable: true

# Respuesta de checkin (incluye estado de membresía)
CheckinResponse:
  type: object
  properties:
    attendance:
      $ref: '#/components/schemas/AttendanceRecord'
    person:
      type: object
      properties:
        id:          { type: string, format: uuid }
        displayName: { type: string }
        photoUrl:    { type: string, format: uri }
    subscription:
      type: object
      nullable: true
      properties:
        status:           { type: string }
        classesRemaining: { type: integer, nullable: true }
        expiresAt:        { type: string, format: date }
    warnings:
      type: array
      items:
        type: object
        properties:
          code:    { type: string }
          message: { type: string }
      description: Avisos no bloqueantes (ej: plan próximo a vencer, clases por agotarse)
```

---

## 13. Dominio: Competitions

**Prefijo:** `/v1/competitions`  
**Propósito:** Gestión completa del ciclo de vida de competencias y eventos deportivos: creación, categorías, inscripciones individuales y por equipo, estructura de rondas y heats, y administración de cupos con lista de espera.

### 13.1 Endpoints

```
# Competencias
GET    /v1/competitions
POST   /v1/competitions
GET    /v1/competitions/{competitionId}
PATCH  /v1/competitions/{competitionId}
DELETE /v1/competitions/{competitionId}
POST   /v1/competitions/{competitionId}/publish
POST   /v1/competitions/{competitionId}/cancel
POST   /v1/competitions/{competitionId}/close-registration
POST   /v1/competitions/{competitionId}/clone

# Categorías
GET    /v1/competitions/{competitionId}/categories
POST   /v1/competitions/{competitionId}/categories
GET    /v1/competitions/{competitionId}/categories/{categoryId}
PATCH  /v1/competitions/{competitionId}/categories/{categoryId}
DELETE /v1/competitions/{competitionId}/categories/{categoryId}

# Inscripciones
GET    /v1/competitions/{competitionId}/registrations
POST   /v1/competitions/{competitionId}/registrations
GET    /v1/competitions/{competitionId}/registrations/{registrationId}
PATCH  /v1/competitions/{competitionId}/registrations/{registrationId}
POST   /v1/competitions/{competitionId}/registrations/{registrationId}/confirm
POST   /v1/competitions/{competitionId}/registrations/{registrationId}/withdraw
POST   /v1/competitions/{competitionId}/registrations/{registrationId}/waitlist-promote
POST   /v1/competitions/{competitionId}/registrations/{registrationId}/change-category

# Equipos
GET    /v1/competitions/{competitionId}/teams
POST   /v1/competitions/{competitionId}/teams
PATCH  /v1/competitions/{competitionId}/teams/{teamId}
POST   /v1/competitions/{competitionId}/teams/{teamId}/members
DELETE /v1/competitions/{competitionId}/teams/{teamId}/members/{personId}

# Rondas y Heats
GET    /v1/competitions/{competitionId}/rounds
POST   /v1/competitions/{competitionId}/rounds
GET    /v1/competitions/{competitionId}/rounds/{roundId}/heats
POST   /v1/competitions/{competitionId}/rounds/{roundId}/heats
POST   /v1/competitions/{competitionId}/rounds/{roundId}/heats/generate  # asignación automática

# Cronograma
GET    /v1/competitions/{competitionId}/schedule
POST   /v1/competitions/{competitionId}/schedule

# Resultados públicos
GET    /v1/competitions/{competitionId}/results      # público, no requiere auth
```

### 13.2 Schemas

```yaml
Competition:
  type: object
  required: [id, name, type, format, status]
  properties:
    id:
      type: string
      format: uuid
    name:
      type: string
      example: "Open CrossFit Santiago 2026"
    slug:
      type: string
      readOnly: true
    description:
      type: string
    sport:
      type: string
      example: "crossfit"
    discipline:
      type: string
      nullable: true
    type:
      type: string
      enum: [individual, pairs, team, mixed]
    format:
      type: string
      enum: [in_person, online, hybrid]
    status:
      type: string
      enum: [draft, published, registration_open, registration_closed, in_progress, finished, cancelled]
    startDate:
      type: string
      format: date
    endDate:
      type: string
      format: date
    registrationOpenAt:
      type: string
      format: date-time
    registrationCloseAt:
      type: string
      format: date-time
    locationId:
      type: string
      format: uuid
      nullable: true
    address:
      $ref: '#/components/schemas/Address'
    bannerUrl:
      type: string
      format: uri
    enrollmentFee:
      type: number
      format: decimal
    currency:
      type: string
    requiresPaymentToConfirm:
      type: boolean
    waiverRequired:
      type: boolean
    waiverText:
      type: string
      nullable: true
    rules:
      type: string
    kitDescription:
      type: string
      nullable: true
    isPublic:
      type: boolean
    categories:
      type: array
      readOnly: true
      items:
        $ref: '#/components/schemas/CompetitionCategorySummary'
    totalRegistrations:
      type: integer
      readOnly: true

CompetitionCategory:
  type: object
  required: [id, competitionId, name]
  properties:
    id:
      type: string
      format: uuid
    competitionId:
      type: string
      format: uuid
    name:
      type: string
      example: "Rx Femenino 18-34"
    description:
      type: string
    criteria:
      type: object
      description: Reglas de elegibilidad configurables por el organizador
      properties:
        gender:
          type: string
          enum: [male, female, any, mixed]
          nullable: true
        minAge:
          type: integer
          nullable: true
        maxAge:
          type: integer
          nullable: true
        minWeight:
          type: number
          nullable: true
        maxWeight:
          type: number
          nullable: true
        level:
          type: string
          nullable: true
        clubId:
          type: string
          format: uuid
          nullable: true
        customCriteria:
          type: object
          additionalProperties: true
    capacity:
      type: integer
      nullable: true
      description: null = sin límite
    waitlistEnabled:
      type: boolean
    registeredCount:
      type: integer
      readOnly: true
    waitlistCount:
      type: integer
      readOnly: true
    availableSpots:
      type: integer
      nullable: true
      readOnly: true
    status:
      type: string
      enum: [open, full, closed, cancelled]

CompetitionRegistration:
  type: object
  required: [id, competitionId, categoryId, status]
  properties:
    id:
      type: string
      format: uuid
    competitionId:
      type: string
      format: uuid
    categoryId:
      type: string
      format: uuid
    personId:
      type: string
      format: uuid
      nullable: true
      description: Para inscripción individual o como capitán de equipo
    teamId:
      type: string
      format: uuid
      nullable: true
    members:
      type: array
      description: Para inscripciones de parejas o equipos
      items:
        type: object
        properties:
          personId: { type: string, format: uuid }
          role:     { type: string, enum: [captain, member] }
    status:
      type: string
      enum: [pending, confirmed, waitlisted, withdrawn, disqualified]
    waitlistPosition:
      type: integer
      nullable: true
    invoiceId:
      type: string
      format: uuid
      nullable: true
    formResponseId:
      type: string
      format: uuid
      nullable: true
      description: Respuestas al formulario de inscripción de la competencia
    waiverSignedAt:
      type: string
      format: date-time
      nullable: true
    registeredAt:
      type: string
      format: date-time
    confirmedAt:
      type: string
      format: date-time
      nullable: true
    withdrawnAt:
      type: string
      format: date-time
      nullable: true
    notes:
      type: string

Team:
  type: object
  required: [id, name, competitionId, categoryId]
  properties:
    id:
      type: string
      format: uuid
    name:
      type: string
    competitionId:
      type: string
      format: uuid
    categoryId:
      type: string
      format: uuid
    captainId:
      type: string
      format: uuid
    members:
      type: array
      items:
        type: object
        properties:
          personId:    { type: string, format: uuid }
          role:        { type: string, enum: [captain, member, alternate] }
          joinedAt:    { type: string, format: date-time }
          isConfirmed: { type: boolean }
    registrationId:
      type: string
      format: uuid
    status:
      type: string
      enum: [forming, complete, confirmed, withdrawn]
```

---

## 14. Dominio: Scoring

**Prefijo:** `/v1/scoring`  
**Propósito:** Captura, validación y publicación de resultados en competencias. Soporta diferentes unidades de medida por disciplina, roles de juez con validación cruzada, resolución de empates, override administrativo y leaderboard en tiempo real.

### 14.1 Endpoints

```
# Definiciones de scoring (plantillas reutilizables)
GET    /v1/scoring/definitions
POST   /v1/scoring/definitions
GET    /v1/scoring/definitions/{definitionId}
PATCH  /v1/scoring/definitions/{definitionId}

# Eventos de scoring (workout o prueba dentro de una competencia)
GET    /v1/competitions/{competitionId}/events
POST   /v1/competitions/{competitionId}/events
PATCH  /v1/competitions/{competitionId}/events/{eventId}

# Scores
GET    /v1/scoring/events/{eventId}/scores
POST   /v1/scoring/events/{eventId}/scores
GET    /v1/scoring/events/{eventId}/scores/{scoreId}
PATCH  /v1/scoring/events/{eventId}/scores/{scoreId}

# Flujo de validación
POST   /v1/scoring/events/{eventId}/scores/{scoreId}/submit
POST   /v1/scoring/events/{eventId}/scores/{scoreId}/validate
POST   /v1/scoring/events/{eventId}/scores/{scoreId}/dispute
POST   /v1/scoring/events/{eventId}/scores/{scoreId}/resolve-dispute
POST   /v1/scoring/events/{eventId}/scores/{scoreId}/override

# Leaderboard
GET    /v1/scoring/events/{eventId}/leaderboard
GET    /v1/competitions/{competitionId}/leaderboard
GET    /v1/competitions/{competitionId}/categories/{categoryId}/leaderboard
```

### 14.2 Schemas

```yaml
ScoringDefinition:
  type: object
  required: [id, name, unit, rankingDirection]
  properties:
    id:
      type: string
      format: uuid
    name:
      type: string
      example: "For Time"
    sport:
      type: string
    unit:
      type: string
      enum: [reps, kg, seconds, points, distance_m, custom]
    rankingDirection:
      type: string
      enum: [asc, desc]
      description: >
        asc = menor es mejor (tiempo).
        desc = mayor es mejor (reps, kg, puntos).
    tiebreakerUnit:
      type: string
      nullable: true
    tiebreakerDirection:
      type: string
      enum: [asc, desc]
      nullable: true
    requiresJudgeValidation:
      type: boolean
      default: true
    allowVideoSubmission:
      type: boolean
    cappedValue:
      type: number
      nullable: true
      description: Valor máximo (ej: tiempo de cap en CrossFit)
    cappedRepsEnabled:
      type: boolean
      description: Si alcanzó el cap, se registran reps adicionales como tiebreaker

Score:
  type: object
  required: [id, registrationId, eventId, status]
  properties:
    id:
      type: string
      format: uuid
    registrationId:
      type: string
      format: uuid
    heatId:
      type: string
      format: uuid
      nullable: true
    eventId:
      type: string
      format: uuid
    judgeId:
      type: string
      format: uuid
      nullable: true
    value:
      type: number
      nullable: true
      description: Resultado principal
    unit:
      type: string
    tiebreaker:
      type: number
      nullable: true
    isCapped:
      type: boolean
      default: false
    cappedReps:
      type: integer
      nullable: true
    rank:
      type: integer
      nullable: true
      readOnly: true
    status:
      type: string
      enum: [pending, submitted, validated, disputed, overridden, disqualified]
    submittedAt:
      type: string
      format: date-time
      nullable: true
    validatedBy:
      type: string
      format: uuid
      nullable: true
    validatedAt:
      type: string
      format: date-time
      nullable: true
    disputedAt:
      type: string
      format: date-time
      nullable: true
    disputeReason:
      type: string
      nullable: true
    overrideReason:
      type: string
      nullable: true
    overriddenBy:
      type: string
      format: uuid
      nullable: true
    videoUrl:
      type: string
      format: uri
      nullable: true
    notes:
      type: string

LeaderboardEntry:
  type: object
  properties:
    rank:
      type: integer
    previousRank:
      type: integer
      nullable: true
    registrationId:
      type: string
      format: uuid
    personId:
      type: string
      format: uuid
      nullable: true
    teamId:
      type: string
      format: uuid
      nullable: true
    displayName:
      type: string
    categoryId:
      type: string
      format: uuid
    totalScore:
      type: number
      nullable: true
    scores:
      type: array
      items:
        type: object
        properties:
          eventId:    { type: string, format: uuid }
          eventName:  { type: string }
          value:      { type: number }
          rank:       { type: integer }
          points:     { type: number }
    isTied:
      type: boolean
    updatedAt:
      type: string
      format: date-time
```

---

## 15. Dominio: Forms

**Prefijo:** `/v1/forms`  
**Propósito:** Formularios dinámicos configurables por el tenant para diferentes contextos: registro de persona, inscripción a competencia, recopilación de datos adicionales. Soporta campos condicionales, validaciones y múltiples tipos de campo.

### 15.1 Endpoints

```
GET    /v1/forms
POST   /v1/forms
GET    /v1/forms/{formId}
PATCH  /v1/forms/{formId}
DELETE /v1/forms/{formId}
POST   /v1/forms/{formId}/duplicate

GET    /v1/forms/{formId}/responses
POST   /v1/forms/{formId}/responses
GET    /v1/forms/{formId}/responses/{responseId}
PATCH  /v1/forms/{formId}/responses/{responseId}
GET    /v1/forms/{formId}/responses/export       # CSV
```

### 15.2 Schemas

```yaml
FormDefinition:
  type: object
  required: [id, name, context, fields]
  properties:
    id:
      type: string
      format: uuid
    name:
      type: string
    context:
      type: string
      enum: [person_registration, competition_registration, event, custom]
    description:
      type: string
    fields:
      type: array
      items:
        $ref: '#/components/schemas/FormField'
    isActive:
      type: boolean

FormField:
  type: object
  required: [id, name, type, label]
  properties:
    id:
      type: string
      description: ID único dentro del formulario (slug-style)
    name:
      type: string
    label:
      type: string
    type:
      type: string
      enum:
        [text, email, phone, date, number, select, multiselect,
         checkbox, radio, file, textarea, heading, divider]
    placeholder:
      type: string
    helpText:
      type: string
    required:
      type: boolean
      default: false
    conditionalOn:
      type: object
      nullable: true
      description: Muestra el campo solo si se cumple la condición
      properties:
        fieldId:
          type: string
        operator:
          type: string
          enum: [equals, not_equals, contains, gt, lt]
        value: {}
    options:
      type: array
      nullable: true
      description: Para tipos select, multiselect, radio
      items:
        type: object
        properties:
          value: { type: string }
          label: { type: string }
    validations:
      type: object
      properties:
        minLength:   { type: integer }
        maxLength:   { type: integer }
        min:         { type: number }
        max:         { type: number }
        pattern:     { type: string }
        allowedExtensions:
          type: array
          items: { type: string }
    order:
      type: integer
```

---

## 16. Dominio: Notifications

**Prefijo:** `/v1/notifications`  
**Propósito:** Envío de comunicaciones por múltiples canales (email, WhatsApp, SMS, push). Gestión de plantillas configurables por tenant y preferencias de recepción por persona.

### 16.1 Endpoints

```
POST   /v1/notifications/send               # envío manual ad-hoc
POST   /v1/notifications/bulk               # envío masivo a segmento

GET    /v1/notifications/templates
POST   /v1/notifications/templates
GET    /v1/notifications/templates/{templateId}
PATCH  /v1/notifications/templates/{templateId}
DELETE /v1/notifications/templates/{templateId}
POST   /v1/notifications/templates/{templateId}/preview

GET    /v1/notifications/logs
GET    /v1/notifications/logs/{logId}

GET    /v1/notifications/preferences/{personId}
PATCH  /v1/notifications/preferences/{personId}
```

### 16.2 Schemas

```yaml
NotificationTemplate:
  type: object
  required: [id, name, event, channels]
  properties:
    id:
      type: string
      format: uuid
    name:
      type: string
    event:
      type: string
      enum:
        [subscription_created, subscription_expiring, subscription_expired,
         subscription_renewed, payment_received, payment_overdue, payment_failed,
         booking_confirmed, booking_cancelled, booking_reminder, waitlist_promoted,
         class_cancelled, competition_registration_confirmed, competition_results_published,
         custom]
    channels:
      type: array
      items:
        type: string
        enum: [email, whatsapp, sms, push]
    subject:
      type: string
      description: Para email. Soporta variables {{person.firstName}}
    body:
      type: string
      description: Cuerpo del mensaje. Soporta variables y Markdown básico
    isActive:
      type: boolean
    isEditable:
      type: boolean
      description: false = plantilla del sistema, no modificable
```

---

## 17. Dominio: Reports

**Prefijo:** `/v1/reports`  
**Propósito:** Consultas analíticas, KPIs del negocio y exportación de datos. Los reportes son síncronos para rangos pequeños y asíncronos (job) para exportaciones grandes.

### 17.1 Endpoints

```
GET    /v1/reports/attendance
GET    /v1/reports/revenue
GET    /v1/reports/memberships
GET    /v1/reports/occupancy
GET    /v1/reports/delinquency
GET    /v1/reports/competitions/{competitionId}
GET    /v1/reports/persons

# Exportación asíncrona
POST   /v1/reports/export
GET    /v1/reports/export/{jobId}
GET    /v1/reports/export/{jobId}/download
DELETE /v1/reports/export/{jobId}
```

### 17.2 Query Parameters Comunes de Reportes

```yaml
ReportQueryParams:
  - name: dateFrom
    in: query
    required: true
    schema: { type: string, format: date }
  - name: dateTo
    in: query
    required: true
    schema: { type: string, format: date }
  - name: locationId
    in: query
    schema: { type: string, format: uuid }
  - name: groupBy
    in: query
    schema:
      type: string
      enum: [day, week, month, location, discipline, plan, coach]
```

---

## 18. Schemas Transversales

Componentes reutilizables declarados en `components/schemas`:

```yaml
# ── Paginación ──────────────────────────────────────────────────────────────
PaginationMeta:
  type: object
  required: [page, limit, total, totalPages]
  properties:
    page:       { type: integer, example: 1 }
    limit:      { type: integer, example: 20 }
    total:      { type: integer, example: 154 }
    totalPages: { type: integer, example: 8 }

# ── Auditoría ────────────────────────────────────────────────────────────────
AuditFields:
  type: object
  properties:
    createdAt: { type: string, format: date-time }
    updatedAt: { type: string, format: date-time }
    createdBy: { type: string, format: uuid }
    updatedBy: { type: string, format: uuid }
    deletedAt: { type: string, format: date-time, nullable: true }

# ── Dirección ────────────────────────────────────────────────────────────────
Address:
  type: object
  properties:
    street:  { type: string }
    number:  { type: string }
    city:    { type: string, example: "Santiago" }
    region:  { type: string, example: "Metropolitana" }
    country: { type: string, example: "CL" }
    zip:     { type: string, nullable: true }
    lat:     { type: number, format: double, nullable: true }
    lng:     { type: number, format: double, nullable: true }

# ── Dinero ───────────────────────────────────────────────────────────────────
Money:
  type: object
  required: [amount, currency]
  properties:
    amount:   { type: number, format: decimal, example: 59990 }
    currency: { type: string, minLength: 3, maxLength: 3, example: CLP }

# ── Error RFC 7807 ───────────────────────────────────────────────────────────
ProblemDetail:
  type: object
  required: [type, title, status, detail]
  properties:
    type:
      type: string
      format: uri
      example: "https://api.sportflow.io/errors/debt-blocked"
    title:
      type: string
      example: "Acceso bloqueado por deuda"
    status:
      type: integer
      example: 402
    detail:
      type: string
      example: "La persona tiene una deuda pendiente desde hace 10 días."
    instance:
      type: string
      format: uri
      example: "/v1/scheduling/bookings"
    code:
      type: string
      example: "DEBT_BLOCKED"
    errors:
      type: array
      description: Errores de validación por campo
      items:
        type: object
        required: [field, message, code]
        properties:
          field:   { type: string, example: "email" }
          message: { type: string, example: "El email ya está registrado" }
          code:    { type: string, example: "ALREADY_EXISTS" }
    traceId:
      type: string
      description: ID de trazabilidad para soporte
      example: "abc123-xyz"
```

---

## 19. Códigos de Error Propios

Errores de negocio específicos de la plataforma. Se retornan dentro del schema `ProblemDetail` en el campo `code`:

| Código                    | HTTP | Cuándo se produce                                              |
|---------------------------|------|----------------------------------------------------------------|
| `DEBT_BLOCKED`            | 402  | Reserva/acceso bloqueado por deuda pendiente                   |
| `PLAN_FROZEN`             | 409  | La suscripción está congelada actualmente                      |
| `PLAN_EXPIRED`            | 409  | La suscripción venció y no se ha renovado                      |
| `PLAN_SUSPENDED`          | 409  | Suscripción suspendida manualmente o por deuda                 |
| `PLAN_LIMIT_REACHED`      | 429  | El plan no tiene más clases disponibles en el período          |
| `SESSION_DAILY_LIMIT`     | 429  | Se alcanzó el límite de sesiones por día del plan              |
| `CLASS_FULL`              | 409  | La clase no tiene cupos disponibles (devuelve info de waitlist)|
| `WAITLIST_FULL`           | 409  | La lista de espera también está llena                          |
| `ALREADY_BOOKED`          | 409  | La persona ya tiene una reserva en esta clase                  |
| `CLASS_CANCELLED`         | 409  | La clase fue cancelada y ya no acepta reservas                 |
| `CLASS_IN_PAST`           | 422  | No se puede reservar/modificar una clase que ya ocurrió        |
| `BOOKING_NOT_CANCELLABLE` | 409  | Fuera del plazo de cancelación sin penalidad                   |
| `DUPLICATE_IDENTIFIER`    | 409  | El RUT/DNI/pasaporte ya está asociado a otra persona           |
| `INVALID_RUT`             | 422  | Formato o dígito verificador de RUT inválido                   |
| `INVALID_IDENTIFIER`      | 422  | Formato de identificador no válido para el tipo indicado       |
| `MINOR_NO_GUARDIAN`       | 422  | Menor de edad sin tutor/apoderado registrado                   |
| `CATEGORY_CLOSED`         | 409  | La categoría de la competencia está llena o cerrada            |
| `CATEGORY_INELIGIBLE`     | 422  | La persona no cumple los criterios de la categoría             |
| `REGISTRATION_CONFIRMED`  | 409  | La inscripción ya está confirmada y no puede modificarse       |
| `SCORE_LOCKED`            | 409  | El score está validado; requiere override para modificar       |
| `SCORE_ALREADY_SUBMITTED` | 409  | Ya existe un score para esta inscripción y evento              |
| `INVALID_TENANT`          | 403  | El recurso pertenece a otro tenant                             |
| `TENANT_PLAN_EXCEEDED`    | 402  | El tenant superó el límite de su plan de SportFlow             |
| `COMPETITION_NOT_OPEN`    | 409  | Las inscripciones están cerradas o no abiertas aún             |
| `FREEZE_LIMIT_REACHED`    | 409  | Se alcanzó el máximo de congelamientos del período             |
| `FREEZE_MIN_DAYS`         | 422  | El período de congelamiento es menor al mínimo permitido       |

---

## 20. Parámetros y Componentes Globales

```yaml
components:
  parameters:
    PageParam:
      name: page
      in: query
      schema: { type: integer, minimum: 1, default: 1 }
      description: Número de página (1-indexed)

    LimitParam:
      name: limit
      in: query
      schema: { type: integer, minimum: 1, maximum: 100, default: 20 }

    SortParam:
      name: sort
      in: query
      schema: { type: string }
      description: Campos separados por coma. Prefijo - para DESC. Ej: -createdAt,name

    LocationIdParam:
      name: locationId
      in: query
      schema: { type: string, format: uuid }

    DateFromParam:
      name: dateFrom
      in: query
      schema: { type: string, format: date }

    DateToParam:
      name: dateTo
      in: query
      schema: { type: string, format: date }

    StatusParam:
      name: status
      in: query
      schema: { type: string }

    SearchParam:
      name: search
      in: query
      schema: { type: string, minLength: 2 }
      description: Búsqueda de texto libre sobre campos indexados

  responses:
    400BadRequest:
      description: Request inválido o parámetros incorrectos
      content:
        application/problem+json:
          schema: { $ref: '#/components/schemas/ProblemDetail' }

    401Unauthorized:
      description: No autenticado o token inválido
      content:
        application/problem+json:
          schema: { $ref: '#/components/schemas/ProblemDetail' }

    403Forbidden:
      description: Sin permisos para realizar esta acción
      content:
        application/problem+json:
          schema: { $ref: '#/components/schemas/ProblemDetail' }

    404NotFound:
      description: Recurso no encontrado
      content:
        application/problem+json:
          schema: { $ref: '#/components/schemas/ProblemDetail' }

    409Conflict:
      description: Conflicto con el estado actual del recurso
      content:
        application/problem+json:
          schema: { $ref: '#/components/schemas/ProblemDetail' }

    422UnprocessableEntity:
      description: Datos válidos sintácticamente pero no procesables
      content:
        application/problem+json:
          schema: { $ref: '#/components/schemas/ProblemDetail' }

    429TooManyRequests:
      description: Límite de rate o de negocio alcanzado
      headers:
        Retry-After:
          schema: { type: integer }
          description: Segundos hasta próximo intento
      content:
        application/problem+json:
          schema: { $ref: '#/components/schemas/ProblemDetail' }

    500InternalServerError:
      description: Error interno del servidor
      content:
        application/problem+json:
          schema: { $ref: '#/components/schemas/ProblemDetail' }
```

---

## 21. Estructura Raíz del Spec

El archivo de entrada del spec completo (`openapi.yaml`):

```yaml
openapi: '3.1.0'

info:
  title: SportFlow API
  version: '1.0.0'
  description: |
    API REST multi-tenant para gestión integral de centros deportivos y competencias.
    
    ## Autenticación
    Todos los endpoints requieren `Authorization: Bearer <token>` excepto los marcados como públicos.
    
    ## Multi-tenancy
    El `tenant_id` se extrae del JWT. Nunca se pasa en el path ni como query parameter.
    
    ## Paginación
    Las colecciones retornan `{ data: [...], meta: { page, limit, total, totalPages } }`.
    
    ## Errores
    Todos los errores siguen RFC 7807 Problem Details con campo `code` propio de la plataforma.
    
  contact:
    name: SportFlow Developer Support
    email: api@sportflow.io
    url: https://docs.sportflow.io
  license:
    name: Proprietary
    url: https://sportflow.io/terms

servers:
  - url: https://api.sportflow.io/v1
    description: Producción
  - url: https://api.staging.sportflow.io/v1
    description: Staging / QA
  - url: http://localhost:8080/v1
    description: Desarrollo local

security:
  - BearerAuth: []

tags:
  - name: auth
    description: Autenticación y sesiones
  - name: tenants
    description: Organización y sedes
  - name: persons
    description: Registro de personas e identificadores
  - name: memberships
    description: Planes y suscripciones
  - name: billing
    description: Facturación, pagos y deuda
  - name: scheduling
    description: Clases, agenda y reservas
  - name: attendance
    description: Asistencia y control de acceso
  - name: competitions
    description: Torneos, eventos e inscripciones
  - name: scoring
    description: Puntuación, ranking y leaderboard
  - name: forms
    description: Formularios dinámicos
  - name: notifications
    description: Comunicaciones y notificaciones
  - name: reports
    description: Analítica y reportes

paths:
  # Se recomienda split por archivo usando $ref:
  # $ref: './paths/auth.yaml'
  # $ref: './paths/tenants.yaml'
  # $ref: './paths/persons.yaml'
  # ... etc
```

### 21.1 Estructura de Archivos Recomendada

```
openapi/
├── openapi.yaml                  # Raíz: info, servers, tags, security
├── paths/
│   ├── auth.yaml
│   ├── tenants.yaml
│   ├── persons.yaml
│   ├── memberships.yaml
│   ├── billing.yaml
│   ├── scheduling.yaml
│   ├── attendance.yaml
│   ├── competitions.yaml
│   ├── scoring.yaml
│   ├── forms.yaml
│   ├── notifications.yaml
│   └── reports.yaml
├── schemas/
│   ├── common.yaml               # ProblemDetail, Pagination, Address, Money, Audit
│   ├── identity.yaml
│   ├── tenants.yaml
│   ├── persons.yaml
│   ├── memberships.yaml
│   ├── billing.yaml
│   ├── scheduling.yaml
│   ├── attendance.yaml
│   ├── competitions.yaml
│   ├── scoring.yaml
│   └── forms.yaml
├── parameters/
│   └── common.yaml               # PageParam, LimitParam, SortParam, etc.
└── responses/
    └── errors.yaml               # 400, 401, 403, 404, 409, 422, 429, 500
```

---

## 22. Guía de Implementación por Fases

### Fase 1 — MVP (meses 1-4)

Endpoints mínimos para operar un centro deportivo básico:

| Dominio      | Endpoints prioritarios                                        |
|--------------|---------------------------------------------------------------|
| Identity     | login, refresh, logout, me                                    |
| Tenants      | me, locations CRUD                                            |
| Persons      | CRUD básico, search por identificador                         |
| Memberships  | plans CRUD, subscriptions CRUD, freeze, cancel                |
| Billing      | invoices CRUD, pay, basic debt                                |
| Scheduling   | classes CRUD, bookings CRUD, checkin, noshow                  |
| Attendance   | checkin manual y QR                                           |

### Fase 2 — Competencias (meses 5-8)

| Dominio      | Endpoints prioritarios                                        |
|--------------|---------------------------------------------------------------|
| Competitions | CRUD, categories, registrations, teams                        |
| Scoring      | scores, validate, leaderboard                                 |
| Forms        | definition CRUD, responses                                    |
| Notifications| templates, send, preferences                                  |

### Fase 3 — Analítica e Integraciones (meses 9-12)

| Dominio      | Endpoints prioritarios                                        |
|--------------|---------------------------------------------------------------|
| Reports      | todos los reportes, export async                              |
| Attendance   | NFC, access-control con ApiKey                                |
| Billing      | recurrencia automática, integración Webpay/Flow               |
| Persons      | merge, competition-history, timeline                          |

---

## 23. Decisiones de Diseño y Justificaciones

### 23.1 ¿Por qué `tenant_id` en el JWT y no en el path?

El modelo `/{tenantId}/persons` crea URLs que exponen la estructura de datos interna, complican el routing y requieren que el cliente gestione el tenant en cada request. Con el JWT, el middleware extrae el tenant automáticamente, simplifica el cliente y hace imposible el acceso cross-tenant por error de URL.

### 23.2 ¿Por qué acciones como sub-recursos y no como PATCH?

`PATCH /subscriptions/{id}` con `{ "status": "frozen" }` mezcla operaciones de negocio con updates de campo, pierde la semántica, no permite params específicos de la acción (fechas de congelamiento) y dificulta el logging de auditoría. `POST .../freeze` con `{ frozenFrom, frozenUntil, reason }` es autodescriptivo, loggeable y validable de forma independiente.

### 23.3 ¿Por qué RFC 7807 para errores?

Es el estándar de facto para APIs REST. El campo `code` propio de la plataforma permite que el frontend muestre mensajes específicos en el idioma del usuario sin parsear strings del `detail`. El campo `errors[]` permite mapear errores de validación a campos del formulario.

### 23.4 ¿Por qué RRULE para recurrencia de clases?

RFC 5545 (RRULE) es el estándar de calendario más adoptado. Permite expresar cualquier patrón de recurrencia (`FREQ=WEEKLY;BYDAY=MO,WE,FR`, `FREQ=DAILY;COUNT=10`, `FREQ=MONTHLY;BYDAY=1MO`) sin inventar un esquema propio. Es compatible con Google Calendar, iCal y la mayoría de librerías de calendario.

### 23.5 ¿Por qué `application/problem+json` como Content-Type de error?

Diferencia semánticamente los errores de las respuestas exitosas al nivel del Content-Type, permitiendo a los clientes detectar errores antes de parsear el body. Es la convención de RFC 7807.

---

## 24. Checklist de Calidad

Antes de hacer merge de cualquier PR que modifique el spec:

### Estructura
- [ ] Versión OpenAPI declarada como `3.1.0`
- [ ] Todos los paths siguen la convención `/v1/{dominio}/{recurso}`
- [ ] Tags asignados a todos los endpoints
- [ ] Summary y description en todos los endpoints
- [ ] OperationId único por endpoint (formato: `{verbo}{Recurso}`, ej: `createPerson`)

### Schemas
- [ ] Todos los IDs tipados como `format: uuid`
- [ ] Todas las fechas tipadas como `format: date` o `format: date-time`
- [ ] Todos los montos tipados como `format: decimal`
- [ ] Campos `readOnly: true` en campos calculados
- [ ] Campos `nullable: true` en campos opcionales que pueden ser null
- [ ] Enums completos con todos los valores posibles del dominio
- [ ] `required` declarado en todos los schemas (mínimo campos obligatorios)

### Seguridad
- [ ] Todos los endpoints protegidos tienen `security: [{ BearerAuth: [] }]`
- [ ] Endpoints públicos tienen `security: []` explícito
- [ ] Endpoints con ApiKey tienen `security: [{ ApiKeyAuth: [] }]`

### Respuestas
- [ ] Colecciones retornan envelope `{ data: [], meta: {} }`
- [ ] Ítems individuales retornan envelope `{ data: {} }`
- [ ] Creaciones retornan `201 Created`
- [ ] Acciones sin body retornan `204 No Content`
- [ ] Todos los endpoints tienen respuesta `400`, `401`, `403`, `404` como mínimo
- [ ] Endpoints con posibles conflictos tienen respuesta `409`
- [ ] Errores siguen schema `ProblemDetail`

### Paginación
- [ ] Todos los endpoints de colección aceptan `page`, `limit`, `sort`
- [ ] La respuesta incluye `meta.total` y `meta.totalPages`

### Ejemplos
- [ ] Al menos un ejemplo por schema principal con datos deportivos reales
- [ ] Los valores de ejemplo son coherentes con los enums y tipos declarados

---

*Propuesta elaborada para SportFlow SaaS — Revisión técnica requerida antes de implementación*  
*Versión del documento: 1.0.0 — Marzo 2026*
