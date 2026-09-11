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
