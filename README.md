# SportFlow

API backend para una plataforma de gestión de entrenamiento deportivo. Permite a gyms y centros deportivos gestionar usuarios, planes de entrenamiento, sesiones, ejercicios y competencias, con soporte multi-tenant.

## Tech Stack

| Capa | Tecnología |
|------|-----------|
| Runtime | .NET 10 / C# |
| Web Framework | ASP.NET Core Web API |
| ORM | Entity Framework Core 10 |
| Base de datos | PostgreSQL (Npgsql) |
| Autenticación | JWT Bearer + ASP.NET Core Identity |
| Validación | FluentValidation |
| Mapping | Mapperly (compile-time) |
| Logging | Serilog |
| Documentación API | Swagger / OpenAPI (Swashbuckle) |
| Testing | xUnit · Moq · Shouldly |

## Arquitectura

Clean Architecture con cuatro capas:

```
src/
├── SportFlow.Domain/          # Entidades, value objects, reglas de negocio
├── SportFlow.Application/     # CQRS handlers, DTOs, interfaces, validadores
├── SportFlow.Infrastructure/  # EF Core, repositorios, servicios externos
├── SportFlow.API/             # Controllers, middleware, configuración
└── SportFlow.Shared/          # Excepciones y tipos resultado compartidos

tests/
├── SportFlow.Domain.Tests/
├── SportFlow.Application.Tests/
└── SportFlow.Integration.Tests/
```

**Patrones aplicados:**
- CQRS manual con `ICommandHandler<TCommand>` / `IQueryHandler<TQuery, TResult>` (sin MediatR)
- Repository pattern en Infrastructure
- Multi-tenancy mediante `ITenantContext` inyectado por middleware
- Strongly-typed IDs y `record` types para DTOs y value objects

## Roles del sistema

```
SuperAdmin → TenantOwner → TenantManager → Staff → Coach → Member
```

Un usuario puede tener roles distintos en múltiples tenants (`UserTenantRoles`).

## Requisitos previos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- PostgreSQL 15+

## Configuración

1. Copia y edita el archivo de configuración local:

```bash
cp src/SportFlow.API/appsettings.json src/SportFlow.API/appsettings.Development.json
```

2. Ajusta la cadena de conexión y el JWT secret en `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=sportflow_dev;Username=postgres;Password=tu_password"
  },
  "Jwt": {
    "SecretKey": "clave-secreta-de-256-bits-minimo"
  }
}
```

3. Aplica las migraciones:

```bash
dotnet ef database update --project src/SportFlow.Infrastructure --startup-project src/SportFlow.API
```

## Ejecutar

```bash
dotnet run --project src/SportFlow.API
```

La API queda disponible en `https://localhost:5001`. Swagger UI en `/swagger`.

## Tests

```bash
# Todos los tests
dotnet test

# Solo unitarios
dotnet test tests/SportFlow.Domain.Tests
dotnet test tests/SportFlow.Application.Tests

# Integración (requiere base de datos)
dotnet test tests/SportFlow.Integration.Tests
```

## Convenciones

- Commits en formato [Conventional Commits](https://www.conventionalcommits.org/) (`feat:`, `fix:`, `chore:`, `refactor:`, `test:`)
- Los controllers son thin — toda la lógica va en la capa Application
- `async/await` en todas las operaciones I/O
- Endpoints siguen convenciones RESTful
