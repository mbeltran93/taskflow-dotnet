# TaskFlow API (C# + .NET)

![CI](https://github.com/mbeltran93/taskflow-dotnet/actions/workflows/ci.yml/badge.svg)

API REST de gestion de tareas/proyectos tipo Trello/Jira reducido. Es parte de un portafolio
comparativo: el mismo dominio y el mismo contrato de API implementados con distintas tecnologias
backend; esta es la version en **C# 13 + ASP.NET Core 10 (.NET 10 LTS)**.

## Por que este repo existe

El resto del portafolio cubre Java/Spring Boot, Node/TypeScript, Python, PHP y otros stacks, pero
el CV que acompaña a este portafolio lista **.NET** como tecnologia de soporte (trabajo real con
Java y .NET en paralelo en varios empleos) y no tenia, hasta ahora, ninguna pieza que lo
demostrara. Este repo cubre ese hueco: mismo dominio, mismo contrato de API que los demas, para
que sea directamente comparable, pero con el stack de Microsoft (ASP.NET Core, EF Core, xUnit)
en su version LTS mas reciente y multiplataforma (nada de .NET Framework).

## Stack

- **.NET 10** (LTS) + **ASP.NET Core** (Web API, controllers)
- **Entity Framework Core** + **Npgsql** (PostgreSQL) + migraciones de EF Core
- **JWT Bearer** (`Microsoft.AspNetCore.Authentication.JwtBearer`) + **BCrypt.Net-Next** para hashear passwords
- **FluentValidation** para los DTOs de entrada
- **Swashbuckle** (Swagger/OpenAPI)
- **xUnit** + **Moq** para unit tests de los services
- **xUnit** + `WebApplicationFactory` + **Testcontainers.PostgreSql** para tests de integracion contra Postgres real
- **Docker** (multi-stage) + **docker-compose** (app + Postgres)
- GitHub Actions: build + test, build de la imagen Docker, CodeQL (`csharp`)

## Arquitectura

Solucion en capas, como proyectos separados dentro de la misma `.sln`:

```
src/
  TaskFlow.Domain/         -> entidades (User, Project, TaskItem) y enums, sin dependencias
  TaskFlow.Application/    -> DTOs, interfaces de repositorios/servicios/seguridad, services
                              (logica de negocio), validadores de FluentValidation, excepciones
                              de dominio (NotFoundException, ConflictException, ...)
  TaskFlow.Infrastructure/ -> TaskFlowDbContext (EF Core), repositorios (implementan las
                              interfaces de Application), BCryptPasswordHasher, JwtTokenGenerator,
                              migraciones, DependencyInjection.AddInfrastructure(...)
  TaskFlow.Api/             -> Controllers, ExceptionMiddleware, Program.cs (wiring de DI, JWT,
                              Swagger, FluentValidation)
tests/
  TaskFlow.UnitTests/        -> servicios con Moq (sin base de datos)
  TaskFlow.IntegrationTests/ -> WebApplicationFactory<Program> + Testcontainers.PostgreSql
```

Puntos de diseño:

- **Inversion de dependencias real**: `Application` define `IUserRepository`, `IProjectRepository`,
  `ITaskRepository`, `IPasswordHasher`, `IJwtTokenGenerator`; `Infrastructure` los implementa.
  Los services de `Application` no conocen EF Core ni Npgsql.
- **JWT stateless**: sin sesiones ni cookies. `JwtTokenGenerator` firma el token (HMAC-SHA256) con
  el claim `sub`/`NameIdentifier` = id de usuario; los controllers leen ese claim para saber quien
  esta autenticado (por ejemplo, el `ownerId` al crear un proyecto).
- **Passwords con BCrypt** (`BCryptPasswordHasher`, work factor 12), nunca en texto plano ni en
  las respuestas.
- **Manejo de errores centralizado**: `ExceptionMiddleware` traduce cada subtipo de `AppException`
  (`NotFoundException` -> 404, `ConflictException` -> 409, `InvalidCredentialsException` -> 401,
  `ValidationAppException` / `FluentValidation.ValidationException` -> 400) a una respuesta JSON
  consistente, y cualquier excepcion no prevista -> 500 sin filtrar detalles internos.
- **Validacion de DTOs con FluentValidation**, enganchada via `AddFluentValidationAutoValidation()`
  para que corra automaticamente antes de que el controller reciba el request.
- **Migraciones de EF Core** versionadas en `src/TaskFlow.Infrastructure/Data/Migrations`; la app
  las aplica sola al iniciar (`db.Database.Migrate()` en `Program.cs`), salvo bajo el entorno
  `Testing`, donde los tests de integracion migran ellos mismos el Postgres de Testcontainers
  antes de levantar el host (ver nota mas abajo).
- **Configuracion leida de forma perezosa** (`DependencyInjection.AddInfrastructure` y el
  `AddJwtBearer` de `Program.cs` leen la cadena de conexion / el secreto JWT *dentro* del delegate
  de configuracion, no en una variable capturada antes): asi, cuando `WebApplicationFactory`
  inyecta overrides de configuracion para los tests de integracion, se resuelven de verdad en vez
  de quedarse con una copia tomada antes de que el override se aplicara.

## Contrato de API

Las lecturas (`GET`) son publicas; las escrituras (`POST`/`PUT`/`PATCH`/`DELETE`) requieren un JWT
valido en el header `Authorization: Bearer <token>`.

| Metodo | Ruta                        | Auth | Descripcion                                      |
|--------|-----------------------------|------|---------------------------------------------------|
| POST   | `/api/auth/login`           | No   | Autentica (email + password) y devuelve `{ token }` |
| GET    | `/api/users`                | No   | Lista usuarios                                     |
| GET    | `/api/users/{id}`           | No   | Busca un usuario por id                            |
| POST   | `/api/users`                | No   | Registra un usuario nuevo (necesario para poder loguearse despues) |
| GET    | `/api/projects`             | No   | Lista proyectos                                    |
| GET    | `/api/projects/{id}`        | No   | Busca un proyecto por id                           |
| POST   | `/api/projects`             | Si   | Crea un proyecto (el owner es el usuario del JWT)  |
| PUT    | `/api/projects/{id}`        | Si   | Actualiza nombre/descripcion                       |
| DELETE | `/api/projects/{id}`        | Si   | Elimina un proyecto                                |
| GET    | `/api/tasks?projectId=&status=` | No | Lista tareas, filtrables por proyecto y/o estado |
| GET    | `/api/tasks/{id}`           | No   | Busca una tarea por id                             |
| POST   | `/api/tasks`                | Si   | Crea una tarea dentro de un proyecto (estado inicial `TODO`) |
| PUT    | `/api/tasks/{id}`           | Si   | Actualiza titulo/descripcion/asignado/fecha limite |
| PATCH  | `/api/tasks/{id}/status`    | Si   | Mueve la tarea entre `TODO` / `IN_PROGRESS` / `DONE` |
| DELETE | `/api/tasks/{id}`           | Si   | Elimina una tarea                                  |

Swagger/OpenAPI interactivo en `/swagger` una vez levantada la app.

## Como correrlo

### Con Docker (recomendado)

```bash
docker compose up --build
```

Levanta Postgres (puerto host `5435`) y la API (puerto host `8084`). La app aplica las
migraciones de EF Core sola al iniciar. Probado de punta a punta:

```bash
# Registrar usuario
curl -s -X POST http://localhost:8084/api/users \
  -H "Content-Type: application/json" \
  -d '{"name":"Ada Lovelace","email":"ada@taskflow.com","password":"SuperSecret123"}'

# Login
TOKEN=$(curl -s -X POST http://localhost:8084/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"ada@taskflow.com","password":"SuperSecret123"}' | python3 -c "import sys,json;print(json.load(sys.stdin)['token'])")

# Crear proyecto (requiere JWT)
curl -s -X POST http://localhost:8084/api/projects \
  -H "Content-Type: application/json" -H "Authorization: Bearer $TOKEN" \
  -d '{"name":"Apollo","description":"Moon mission"}'

# Crear tarea, moverla a IN_PROGRESS, filtrar
curl -s -X POST http://localhost:8084/api/tasks \
  -H "Content-Type: application/json" -H "Authorization: Bearer $TOKEN" \
  -d '{"title":"Design trajectory","projectId":1}'
curl -s -X PATCH http://localhost:8084/api/tasks/1/status \
  -H "Content-Type: application/json" -H "Authorization: Bearer $TOKEN" \
  -d '{"status":"IN_PROGRESS"}'
curl -s "http://localhost:8084/api/tasks?projectId=1&status=IN_PROGRESS"
```

Este flujo completo (registro -> login -> crear proyecto -> crear tarea -> cambiar estado ->
filtrar -> 401 sin token) esta tambien automatizado en `postman_collection.json` y se valido con
Newman (`npx newman run postman_collection.json`), con 24 assertions en verde.

### Local (sin Docker)

Requiere el [SDK de .NET 10](https://dotnet.microsoft.com/download) y una instancia de Postgres
accesible (podes usar solo el servicio `db` de `docker-compose.yml`: `docker compose up db`).

```bash
dotnet restore
dotnet run --project src/TaskFlow.Api
```

### Tests

```bash
dotnet test
```

Corre ambos proyectos de test:

- **`TaskFlow.UnitTests`**: servicios (`AuthService`, `UserService`, `ProjectService`,
  `TaskService`) con sus dependencias mockeadas via Moq, mas `BCryptPasswordHasher` y
  `JwtTokenGenerator` sin mocks (son deterministas). 21 tests.
- **`TaskFlow.IntegrationTests`**: levanta un Postgres real en Docker via Testcontainers, aplica
  las migraciones reales de EF Core, y ejercita la app completa (`WebApplicationFactory<Program>`)
  con HTTP real: login, registro, CRUD de proyectos/tareas, filtros, y los 401/404/400/409
  esperados. 16 tests. Requiere Docker corriendo.

Los 37 tests (21 + 16) se corrieron localmente de punta a punta antes de este commit.

## Decisiones / limitaciones conocidas

- No hay endpoint de "refresh token" ni logout (el JWT simplemente expira; `ExpirationMinutes`
  es configurable). Para un MVP de portafolio alcanza con login + expiracion.
- `POST /api/users` es publico a proposito: el contrato original del portafolio no lo pide, pero
  sin el no habria forma de crear las cuentas que luego usan `/api/auth/login` (los backends
  hermanos del portafolio resuelven esto igual).
- Las migraciones se aplican automaticamente al iniciar el contenedor (`db.Database.Migrate()`).
  Es comodo para un demo de portafolio, pero en un entorno real conviene correrlas como un paso
  de release separado, no en el arranque de cada instancia.
- `ownerId`/`assigneeId` no se valida contra "el usuario solo puede borrar sus propios proyectos":
  cualquier usuario autenticado puede editar/borrar cualquier proyecto o tarea. Añadir esa regla de
  autorizacion es la mejora mas obvia si este repo creciera mas alla de portafolio.
