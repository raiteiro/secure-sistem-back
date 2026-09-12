# Deuda técnica y pendientes

Trabajo diferido detectado durante el desarrollo: cosas que "hay que hacer después",
configuración pendiente antes de producción, o inconsistencias detectadas pero no
resueltas en el momento. Se mantiene actualizado automáticamente como parte del
trabajo normal (ver `CLAUDE.md`).

## Bloqueante antes de producción

- [x] ~~Secretos en texto plano en `appsettings.json`~~ — **resuelto**:
  `ConnectionStrings:DefaultConnection` y `Jwt:Key` quedaron vacíos en el
  `appsettings.json` que se commitea; se resuelven por variables de entorno
  (`ConnectionStrings__DefaultConnection`, `Jwt__Key`) en producción. Verificado
  en vivo que la app falla claro sin ellas y funciona bien con ellas. Ver
  `CLAUDE.md` → "Secretos y Variables de Entorno".
- [ ] **SMTP sin configurar**: `Smtp:Host`/`Username`/`Password` están vacíos en
  ambos `appsettings*.json`. `POST /api/auth/forgot-password` ya no revienta si
  falla el envío (queda logueado como error), pero nadie recibe el correo hasta
  que se configure un servidor SMTP real (SendGrid, SES, etc.) por entorno.
- [ ] **Primer administrador de sistema**: hoy la única forma de poner
  `IsSystemAdmin = 1` en un usuario es con SQL directo contra la base. Antes de un
  despliegue real, decidir un mecanismo de bootstrap (seed controlado, flag de
  arranque, endpoint protegido por secreto de infraestructura, etc.).
- [ ] **Imágenes guardadas en disco local (`wwwroot/uploads/companies/`,
  `wwwroot/uploads/products/`)**: funciona para un solo servidor, pero no
  sobrevive a un despliegue con múltiples instancias, contenedores efímeros o
  redeploys (el disco no es compartido ni persistente en esos escenarios).
  Antes de escalar horizontalmente, mover a un storage compartido (Azure Blob
  Storage, S3, un volumen persistente, etc.) y actualizar
  `CompaniesController.UploadLogo` / `ProductsController.UploadImage` para
  subir ahí en vez de al disco local.

## Seguridad / consistencia

- [x] ~~`UsersController.Update`/`.Deactivate` sin bypass de `IsSystemAdmin()`~~ —
  **resuelto**: se agregó el mismo bypass a `Update`/`Deactivate` en `Users`,
  `Roles` (incluyendo `GetRoutes`/`AssignRoutes`) y `NavigationRoutes`. Causaba un
  404 real al editar/desactivar recursos de otra empresa desde las pantallas de
  administración como admin de sistema.
- [ ] **Sin bloqueo de cuenta por intentos fallidos de login** (protección básica
  contra fuerza bruta).

## Funcionalidad pendiente (del roadmap "qué le falta a un sistema base")

- [x] ~~`CashSession.Close` no suma ventas en efectivo al `ExpectedAmount`~~ —
  **resuelto**: ahora suma `OpeningAmount` más los `Payment.Amount` con
  `Method == "Cash"` de las ventas cuyo `Sale.CashSessionId` es el de la
  sesión que se cierra. Pagos con tarjeta/otro método no afectan el efectivo
  esperado. Verificado en vivo con una venta de pago mixto (efectivo + tarjeta).

- [ ] **`SalesController.Create` genera `FolioNumber` con `MAX(FolioNumber) + 1`
  por empresa**: bajo alta concurrencia (dos cajeros cobrando al mismo tiempo
  en la misma empresa), dos ventas podrían calcular el mismo folio antes de
  que la primera haga commit, violando el índice único `(CompanyId,
  FolioNumber)` y tirando un error de base de datos en vez de reintentar. Para
  volumen bajo/medio no es urgente; si se vuelve un problema real, usar una
  secuencia de SQL Server por empresa o un `UPDLOCK`/`SERIALIZABLE` explícito
  al leer el máximo.

- [ ] Permisos granulares por acción — hoy el control de acceso es solo por ruta de
  navegación (ver/no ver un módulo), no por operación dentro de un módulo (ej.
  `users.create` vs `users.delete`).
- [ ] Tabla de auditoría de acciones (más allá de los campos `CreatedBy`/`ModifiedBy`
  que ya existen en cada entidad).
- [ ] Paginación y filtrado en los listados (`Users`, `Roles`, `NavigationRoutes`,
  `Companies` devuelven todo sin paginar).
- [ ] Versionado de API (`/api/v1/...`).
- [ ] Health checks.

## Limpieza técnica

- [ ] `Scripts/full_migration.sql` (124 líneas) parece un script viejo/duplicado de
  `Scripts/001_FullMigration.sql` (el que se regenera con cada migración nueva y sí
  está al día). Revisar si sigue siendo necesario o se puede eliminar.
