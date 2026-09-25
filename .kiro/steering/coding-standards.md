# Estándares de Código

## Convenciones de Naming
- Clases y métodos: PascalCase (`UserService`, `GetAllUsers`)
- Interfaces: prefijo "I" + PascalCase (`IUserRepository`, `ITokenService`)
- Variables locales y parámetros: camelCase (`userName`, `roleId`)
- Campos privados: _camelCase con underscore (`_userRepository`, `_logger`)
- Constantes: PascalCase (`MaxRetryCount`)
- DTOs: sufijo según propósito (`CreateUserRequest`, `UserResponse`)
- Enums: PascalCase singular (`UserStatus`, `PermissionType`)

## Estructura de Archivos
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

## HTTP Methods
- Solo usar GET y POST en los endpoints de la API
- GET para consultas y obtención de datos
- POST para creación, actualización y acciones (incluyendo soft delete)
- No usar PUT, PATCH ni DELETE

## Soft Delete
- Nunca eliminar registros físicamente de la base de datos
- Usar el campo `IsActive` (bit) para desactivar registros (soft delete)
- Los listados solo deben mostrar registros activos por defecto

## Paginación en listados
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

## Patrones Obligatorios
- Repositorios genéricos para operaciones CRUD base
- Repositorios específicos para queries complejas
- Result Pattern para retorno de operaciones (evitar excepciones para flujo de control)
- Guard Clauses para validación de parámetros en constructores/métodos

## Seguridad
- Nunca exponer entidades de dominio directamente en los endpoints
- Siempre usar DTOs para entrada y salida
- Validar todo input del usuario en la capa de Application
- Usar parameterized queries (EF Core lo hace por defecto)
- No almacenar secretos en el código fuente
- Passwords hasheados con BCrypt o similar

## Manejo de Errores
- Excepciones de dominio personalizadas (NotFoundException, BusinessRuleException, etc.)
- Middleware global para capturar excepciones y retornar respuestas consistentes
- Logging estructurado con ILogger
- No exponer stack traces ni detalles internos en producción

## Documentación
- XML comments en interfaces públicas y métodos de servicio
- Swagger annotations en los controllers para documentación de API
- README actualizado con instrucciones de setup
