# SecureSistem

## Contexto del Proyecto

Este es un proyecto plantilla (template) en ASP.NET Core 8 que sirve como base para construir sistemas más complejos.

### Propósito
- Sistema base reutilizable con control de usuarios, roles y permisos
- Árbol de rutas (navegación jerárquica)
- Pensado para ser extendido en proyectos futuros

### Stack Tecnológico
- .NET 8 (ASP.NET Core Web API)
- Entity Framework Core (Code First)
- SQL Server como base de datos principal
- Swagger/OpenAPI para documentación de endpoints
- JWT para autenticación

### Idioma
- El código (clases, métodos, variables, comentarios técnicos) se escribe en inglés
- La documentación y comunicación con el desarrollador es en español
- **Todo mensaje que la API regresa al frontend va en español.** Esto incluye el
  `message` de `BadRequest`/`NotFound`/`Unauthorized`/`StatusCode`/`Ok`, y el
  `ErrorMessage` de los atributos de validación (`[RegularExpression]`,
  `[MinLength]`, etc.) en los DTOs — cualquier texto que el usuario final
  pueda llegar a ver en la UI. No aplica a nombres de propiedades/campos JSON
  (esos son identificadores, se quedan en inglés) ni a logs (`ILogger`), que
  siguen la convención de código en inglés de la sección de arriba.
- Excepción conocida y pendiente: los mensajes de validación por defecto que
  genera ASP.NET Core para atributos de `DataAnnotations` sin `ErrorMessage`
  explícito (ej. `[Required]` sin mensaje propio) siguen en inglés porque
  vienen del framework, no de código propio — ver `TECH_DEBT.md`.

### Fechas y Horas

- **Todo timestamp de negocio (`CreatedAt`, `ModifiedAt`, `OpenedAt`,
  `ClosedAt`, `LastLoginAt`, etc.) se genera con `Common/DateTimeHelper.Now`**,
  nunca con `DateTime.UtcNow` ni `DateTime.Now` directo. `DateTimeHelper.Now`
  convierte explícitamente a la zona horaria de Ciudad de México
  (`America/Mexico_City`, con `Central Standard Time (Mexico)` como
  fallback si esa lista IANA no está disponible en el runtime) y lo entrega
  como un valor "naive" (`DateTimeKind.Unspecified`, sin sufijo `Z`/offset al
  serializar a JSON), para que el frontend lo muestre tal cual sin tener que
  convertir nada.
  - **Por qué**: al registrar una venta la hora salía mal en el frontend.
    La causa real eran dos bugs relacionados: (1) el código usaba
    `DateTime.UtcNow` para guardar los timestamps, y (2) por cómo EF Core lee
    `datetime2` de SQL Server, un valor recién creado en memoria serializaba
    con `Z` (UTC correcto) pero el mismo valor releído de la base (como pasa
    en `SalesController.Create`, que hace un re-fetch después de guardar)
    perdía el `Kind=Utc` y serializaba sin `Z` — el navegador entonces lo
    interpretaba como si ya fuera hora local, mostrando una hora ~6 horas
    adelantada. Guardar directamente en hora de México (siempre como valor
    "naive", consistente sin importar si el dato es recién creado o releído)
    elimina el bug de raíz y de paso ahorra al frontend tener que convertir
    zonas horarias.
  - **Qué NO cambia**: la expiración de JWT/refresh tokens/reset tokens
    (`ITokenService`, `RefreshToken.IsActive`, `PasswordResetToken.IsActive`,
    y las comparaciones contra `ExpiresAt` en `AuthController`/
    `CompaniesController`) se queda en `DateTime.UtcNow` a propósito — es un
    reloj interno que nunca se le muestra al usuario, y emitir con un reloj y
    comparar con otro rompería la expiración de sesiones.
  - Si se agrega un controller/entidad nueva con timestamps, usar
    `DateTimeHelper.Now` para lo que el usuario ve, y `DateTime.UtcNow` solo
    para lógica interna de expiración/comparación que nunca se muestra.

## Estándares de Código

### Convenciones de Naming
- Clases y métodos: PascalCase (`UserService`, `GetAllUsers`)
- Interfaces: prefijo "I" + PascalCase (`IUserRepository`, `ITokenService`)
- Variables locales y parámetros: camelCase (`userName`, `roleId`)
- Campos privados: _camelCase con underscore (`_userRepository`, `_logger`)
- Constantes: PascalCase (`MaxRetryCount`)
- DTOs: sufijo según propósito (`CreateUserRequest`, `UserResponse`)
- Enums: PascalCase singular (`UserStatus`, `PermissionType`)

### Estructura de Archivos
- Una clase por archivo
- El nombre del archivo debe coincidir con el nombre de la clase
- Organizar por feature/dominio dentro de cada capa, no por tipo técnico

Ejemplo correcto:
```
Domain/Users/User.cs
Domain/Users/IUserRepository.cs
Domain/Roles/Role.cs
```

Ejemplo incorrecto:
```
Domain/Entities/User.cs
Domain/Entities/Role.cs
Domain/Interfaces/IUserRepository.cs
```

### HTTP Methods
- Solo usar GET y POST en los endpoints de la API
- GET para consultas y obtención de datos
- POST para creación, actualización y acciones (incluyendo soft delete)
- No usar PUT, PATCH ni DELETE

### Soft Delete
- Nunca eliminar registros físicamente de la base de datos
- Usar el campo `IsActive` (bit) para desactivar registros (soft delete)
- Los listados solo deben mostrar registros activos por defecto

### Paginación en listados
- Cada vez que se agregue un `GET` nuevo que devuelva una lista, evaluar si necesita
  paginación **antes** de dar el endpoint por terminado, no como algo a agregar después:
  - **Tablas transaccionales** (crecen sin límite con el tiempo — ventas, movimientos de
    inventario, turnos de caja, devoluciones, consignaciones): paginación **y** filtro de
    rango de fechas (`from`/`to`) son obligatorios desde el día uno.
  - **Catálogos que pueden crecer mucho** (productos, clientes, inventario, usuarios):
    paginación recomendada; no necesitan `from`/`to` (no son eventos en el tiempo).
  - **Catálogos acotados** (sucursales, cajas, categorías, roles, proveedores, impuestos,
    almacenes): paginación opcional, no urgente — **decisión tomada: se quedan sin
    paginar**, no hace falta revisarlos de nuevo por esto.
  - **`GET /api/navigationroutes/tree` y `/my-tree` nunca se paginan, bajo ninguna
    circunstancia.** Son endpoints del propio sistema base (árbol de navegación), no
    catálogos de negocio — el frontend arma el menú completo a partir de la respuesta, así
    que una página parcial rompería la navegación. Esta regla es permanente, no depende
    del tamaño que llegue a tener el árbol.
- Contrato: query params `page` (1-based, default `1`) y `pageSize` (default `25`, tope
  `100`) — usar `Common/PaginationHelper.cs` (`.Normalize()`, `.ApplyPage()`,
  `.ToPagedResponse()`) en vez de reimplementar el cálculo cada vez.
- Respuesta: siempre el sobre `PagedResponse<T>` (`DTOs/Common/PagedResponse.cs` — `{
  items, page, pageSize, totalCount, totalPages }`), nunca un array plano, en cualquier
  endpoint que pagine.
- **Migrar un endpoint que ya devolvía `T[]` a este sobre es un breaking change** — avisar
  al frontend en el mismo turno (su `.service.ts` correspondiente necesita actualizarse a
  la vez, si no truena iterando un objeto como si fuera arreglo). Ver `pagination.md` en
  la raíz para el inventario completo de endpoints existentes y su prioridad.

### Patrones Obligatorios
- Repositorios genéricos para operaciones CRUD base
- Repositorios específicos para queries complejas
- Result Pattern para retorno de operaciones (evitar excepciones para flujo de control)
- Guard Clauses para validación de parámetros en constructores/métodos

### Seguridad
- Nunca exponer entidades de dominio directamente en los endpoints
- Siempre usar DTOs para entrada y salida
- Validar todo input del usuario en la capa de Application
- Usar parameterized queries (EF Core lo hace por defecto)
- No almacenar secretos en el código fuente
- Passwords hasheados con BCrypt o similar

### Manejo de Errores
- Excepciones de dominio personalizadas (NotFoundException, BusinessRuleException, etc.)
- Middleware global para capturar excepciones y retornar respuestas consistentes
- Logging estructurado con ILogger
- No exponer stack traces ni detalles internos en producción

### Documentación
- XML comments en interfaces públicas y métodos de servicio
- Swagger annotations en los controllers para documentación de API
- README actualizado con instrucciones de setup

## Migraciones y Despliegue

- La aplicación **no** aplica migraciones automáticamente al arrancar (no hay
  `Database.Migrate()` en `Program.cs`, y es una decisión deliberada, no un
  olvido). Esto evita que instancias corriendo en paralelo se pisen migrando al
  mismo tiempo, y evita que una migración con errores se aplique sola en
  producción sin que alguien la revise primero.
- `Scripts/001_FullMigration.sql` es la fuente de verdad para llevar el esquema
  de una base de datos a producción — es un script SQL idempotente (se puede
  correr varias veces sin romper nada) que agrupa **todas** las migraciones de
  EF Core hasta el momento.
- Cada vez que se agregue una migración nueva (`dotnet ef migrations add ...`),
  hay que regenerar ese script con:
  ```
  dotnet ef migrations script --idempotent -o Scripts/001_FullMigration.sql
  ```
  y aplicarlo manualmente (o desde un paso explícito de CI/CD) contra la base de
  datos de destino antes o durante cada release — nunca depender de que la app
  lo haga sola al iniciar.

## Secretos y Variables de Entorno

- `appsettings.json` (el que se commitea al repo) **no** contiene secretos reales
  — `ConnectionStrings:DefaultConnection` y `Jwt:Key` quedan vacíos ahí a propósito.
  Si faltan al arrancar en producción, la app falla de forma clara en el primer
  request que los necesita (no corre en silencio con credenciales vacías).
- `appsettings.Development.json` sí puede tener valores reales pero solo de
  **desarrollo local** (SQL Server local con autenticación de Windows, sin
  contraseña; una JWT key de desarrollo claramente etiquetada como tal) — nunca
  credenciales de un servidor real.
- En producción, esos valores se proveen por variables de entorno, usando `__`
  (doble guion bajo) como separador de jerarquía (soporte nativo de
  configuración de .NET, sin código adicional):
  - `ConnectionStrings__DefaultConnection`
  - `Jwt__Key`
  - `Smtp__Host`, `Smtp__Username`, `Smtp__Password` (cuando se configure SMTP real)
- Verificado en vivo: sin estas variables, el login falla con una excepción clara
  (`SymmetricSecurityKey` con key vacía / conexión a base de datos vacía); con
  las variables seteadas, funciona igual que con `appsettings.Development.json`.

## Rutas de Navegación para Componentes Nuevos

- Cada vez que se agregue un componente/módulo nuevo con su propia pantalla de
  administración (un controller CRUD nuevo tipo Sucursales, Almacenes,
  Categorías, Productos, Impuestos, etc.), se debe crear también su
  `NavigationRoute` correspondiente y dejarla funcionando en automático para
  todos, sin que el usuario tenga que pedirlo cada vez:
  1. Crear el `NavigationRoute` de la pantalla (bajo el grupo que le
     corresponda — ej. "Catálogo" para módulos de POS/inventario), marcado con
     `IsDefaultForNewRoles = true`.
  2. Agregarlo al bootstrap de `CompaniesController.Create` para que las
     empresas nuevas lo tengan desde el día uno, asignado al rol admin inicial.
  3. Gracias al flag `IsDefaultForNewRoles`, cualquier rol que se cree después
     (`RolesController.Create`) lo recibe automáticamente activo — no hace
     falta tocar ese controller de nuevo por cada componente nuevo.
  4. Hacer **backfill** para las empresas y roles que ya existen: crear la
     ruta ahí también y asignarla a todos sus roles activos (no solo al rol
     admin), igual que se hizo con el grupo "Catálogo".
- Si una pantalla es sensible (solo debe verla un admin, no todos los roles),
  no se marca `IsDefaultForNewRoles = true` — se sigue el patrón viejo
  (asignación explícita solo al rol que corresponda), como ya pasa con
  Usuarios/Roles/Rutas/Empresas dentro de "Administración".

## Handoff al Frontend por Controller Nuevo

- Cada vez que se cree un controller nuevo para un módulo (CRUD de una entidad
  nueva: Sucursales, Almacenes, Categorías, Productos, Impuestos, Inventario,
  etc.), se debe entregar en el mismo turno un resumen de los cambios que el
  frontend necesita aplicar, sin que el usuario tenga que pedirlo cada vez:
  - Rutas del controller (métodos + paths).
  - Shape del body para crear/editar (con ejemplo de JSON).
  - Shape de la respuesta (con ejemplo de JSON), señalando campos calculados o
    resueltos que el front no necesita cruzar a mano (ej. `categoryName`,
    `isLowStock`).
  - Reglas de negocio que afectan la UI: validaciones especiales, códigos de
    error y su significado, qué botones deshabilitar según el estado.
  - Cualquier convención no obvia (ej. `rate` como fracción no como porcentaje,
    el signo de `quantity` según el `type` del movimiento).
  - Si el componente agrega una ruta de navegación nueva, mencionar bajo qué
    grupo quedó y si ya viene asignada a todos los roles.

## Gestión de Deuda Técnica

- Cuando durante el desarrollo surja trabajo diferido — algo que "hay que hacer
  después", una limitación conocida, configuración pendiente antes de producción
  (secretos, SMTP, credenciales, etc.), o una inconsistencia detectada pero no
  resuelta en el momento — se debe agregar como pendiente en `TECH_DEBT.md` (raíz
  del proyecto), en la sección que corresponda (bloqueante antes de producción,
  seguridad/consistencia, funcionalidad pendiente, limpieza técnica).
- Esto se hace de forma automática, sin que el usuario tenga que pedirlo cada vez.
- Al resolver un pendiente, marcarlo como hecho o eliminarlo de `TECH_DEBT.md` en el
  mismo cambio que lo resuelve.
