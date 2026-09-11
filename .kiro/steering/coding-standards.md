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
