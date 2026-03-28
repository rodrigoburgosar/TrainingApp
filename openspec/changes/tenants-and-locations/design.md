## Context

SportFlow es multi-tenant SaaS. El módulo Identity (ya implementado) emite JWTs con `tenant_id` y `tenant_slug`, pero esos valores provenían de `appsettings.json` via `StubTenantRepository`. No existe tabla `Tenants` en la base de datos ni entidad de dominio `Tenant`.

Este change introduce la persistencia real de tenants y sedes, reemplazando el stub y habilitando los endpoints de consulta y gestión que necesita la app cliente.

## Goals / Non-Goals

**Goals:**
- Persistir tenants en base de datos (tabla `Tenants`)
- CRUD completo de sedes (`Locations`) con baja lógica
- `GET /v1/tenants/me` para que la app conozca el gym activo
- Reemplazar `StubTenantRepository` por implementación real
- Control de acceso por rol para mutaciones de locations

**Non-Goals:**
- Creación de tenants desde la API (se crean por seed/back-office en v1)
- Configuración avanzada del tenant (facturación, branding, integraciones)
- Multi-sede con horarios o cupos diferenciados por sede (eso es Scheduling)
- Gestión de usuarios por sede (asignación es responsabilidad de otro módulo)

## Decisions

### D1: Tenant como entidad de dominio, no como configuración

**Decisión:** Crear tabla `Tenants` y entidad `Tenant` con repositorio real. `StubTenantRepository` desaparece.

**Alternativa considerada:** Mantener tenants en config y agregar solo locations. Rechazado: el tenant necesita timestamps de auditoría, estado dinámico (`active/suspended`) y eventualmente plan de facturación. Config estática no escala.

---

### D2: Tenant seed manual en v1

**Decisión:** Los tenants se crean por migración de seed o script SQL, no por endpoint de API en v1. El endpoint `POST /v1/tenants` es un TODO para v2 (panel de superadmin).

**Alternativa:** Endpoint de creación de tenant en este change. Rechazado: requiere lógica de billing/plan, stripe webhook, etc. Demasiado scope para Fase 1.

---

### D3: Locations con baja lógica (IsActive)

**Decisión:** Las sedes no se eliminan físicamente. `DELETE /v1/tenants/me/locations/{id}` setea `IsActive = false`.

**Razón:** Las reservas y asistencias pasadas referencian locations. Borrar físicamente rompería la integridad histórica.

---

### D4: Entidades y EF Core

**Entidades del dominio:**

```
Tenant
  Id: TenantId (strongly-typed, wraps Guid)
  Name: string
  Slug: string (único, inmutable)
  Status: string  // "active" | "suspended" | "cancelled"
  Plan: string    // "basic" | "pro" | "enterprise"
  CreatedAt: DateTime
  UpdatedAt: DateTime

Location
  Id: LocationId (strongly-typed, wraps Guid)
  TenantId: TenantId
  Name: string
  Address: string?
  Timezone: string  // IANA timezone (e.g. "America/Santiago")
  MaxCapacity: int?
  IsActive: bool
  CreatedAt: DateTime
  UpdatedAt: DateTime
```

**EF Core Configurations:**

```
TenantsConfiguration : IEntityTypeConfiguration<Tenant>
  → ToTable("Tenants")
  → HasKey(t => t.Id) con conversión TenantId ↔ Guid
  → HasIndex(t => t.Slug).IsUnique()
  → HasQueryFilter(t => t.Status != "cancelled")  // tenants cancelados son invisibles

LocationsConfiguration : IEntityTypeConfiguration<Location>
  → ToTable("Locations")
  → HasKey(l => l.Id) con conversión LocationId ↔ Guid
  → HasIndex(l => new { l.TenantId, l.Name })
  → HasOne<Tenant>().WithMany().HasForeignKey(l => l.TenantId)
  → HasQueryFilter(l => l.IsActive)  // solo locations activas
```

---

### D5: DTOs (records)

```csharp
// GET /v1/tenants/me
record TenantMeResponse(
    Guid Id,
    string Name,
    string Slug,
    string Status,
    string Plan,
    DateTime CreatedAt
);

// GET /v1/tenants/me/locations
record LocationResponse(
    Guid Id,
    string Name,
    string? Address,
    string Timezone,
    int? MaxCapacity,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

// POST /v1/tenants/me/locations
record CreateLocationRequest(
    string Name,
    string? Address,
    string Timezone,
    int? MaxCapacity
);

// PATCH /v1/tenants/me/locations/{id}
record UpdateLocationRequest(
    string? Name,
    string? Address,
    string? Timezone,
    int? MaxCapacity
);
```

---

### D6: Control de acceso

| Endpoint | Roles permitidos |
|---|---|
| `GET /v1/tenants/me` | Todos los roles autenticados |
| `GET /v1/tenants/me/locations` | Todos los roles autenticados |
| `GET /v1/tenants/me/locations/{id}` | Todos los roles autenticados |
| `POST /v1/tenants/me/locations` | TenantOwner, TenantManager |
| `PATCH /v1/tenants/me/locations/{id}` | TenantOwner, TenantManager |
| `DELETE /v1/tenants/me/locations/{id}` | TenantOwner, TenantManager |

Nueva política: `RequireTenantManagerOrAbove` → claims role in [SuperAdmin, TenantOwner, TenantManager].

---

### D7: Seed de tenant en tests de integración

El `SportFlowWebAppFactory` debe insertar una fila en `Tenants` además del `User` y `UserTenantRole`. El `TenantId` del seed sigue siendo `00000000-0000-0000-0000-000000000001`.

## Risks / Trade-offs

- **Rompe `StubTenantRepository`** → Migración de datos necesaria: el seed de `appsettings.json` (tenant "demo") debe convertirse en una fila en `Tenants` en el ambiente de desarrollo. Los tests de integración ya usan InMemory y se actualizan en este mismo change.

- **Query filter `IsActive` en Locations** → Las locations inactivas son invisibles para todos los queries. Si se necesitan para reportes históricos, habrá que usar `.IgnoreQueryFilters()` explícitamente en esos queries.

- **Slug inmutable** → El slug del tenant aparece en el JWT al hacer login. Si se cambiara el slug, todos los tokens activos quedarían inconsistentes. Decisión: slug no tiene endpoint de modificación en v1.

## Open Questions

- ¿El `Timezone` de la location debe validarse contra la lista IANA? → Sí, con FluentValidation + `TimeZoneInfo.TryFindSystemTimeZoneById` en .NET.
- ¿El `MaxCapacity` aplica a la location completa o a cada clase? → Solo a la location en este change. La capacidad por clase se gestiona en Scheduling.
