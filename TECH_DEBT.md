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
- [ ] **`User.LastSeenAt` es por usuario, no por sesión/dispositivo**: el timeout
  de inactividad real (`AuthController.Refresh`, ver `CLAUDE.md`-adjacent — no
  documentado ahí, es interno de `AuthController`) compara la última actividad
  del usuario completo, no de cada `RefreshToken` individual. Si el mismo
  usuario tiene sesión abierta en dos dispositivos, actividad real en uno
  mantiene "viva" la sesión del otro aunque ese segundo dispositivo esté
  genuinamente inactivo. Para precisión por sesión habría que mover
  `LastSeenAt` a `RefreshToken`, lo que requiere que el middleware sepa qué
  refresh token corresponde al access token en uso (hoy no viaja ninguna
  referencia al refresh token dentro del JWT).

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

- [ ] **Permisos granulares por acción: solo a nivel frontend, decisión
  consciente.** Existe `Permission`/`RolePermission` (46 acciones, ver
  `permissions.md`) y `GET /api/permissions/my-permissions`, pero **ningún
  endpoint del backend valida estos permisos** — es intencional, se decidió
  que alcanza con que el frontend oculte/deshabilite botones según el
  permiso (ver `POS_PLAN.md` → "Permisos aplicados al POS"). Implicación a
  tener presente: cualquiera con el JWT puede seguir llamando cualquier
  endpoint directo por API (Postman, consola del navegador, etc.) sin que el
  backend revise el permiso, sin importar lo que el frontend oculte — no es
  un control de seguridad real, solo de UI. Si en algún momento se necesita
  que sí lo sea, `Common/PermissionHelper.HasPermissionAsync` ya existe para
  ese propósito; solo falta conectarlo acción por acción en el controller
  que corresponda.
- [ ] Tabla de auditoría de acciones (más allá de los campos `CreatedBy`/`ModifiedBy`
  que ya existen en cada entidad).
- [ ] Paginación y filtrado en los listados (`Users`, `Roles`, `NavigationRoutes`,
  `Companies` devuelven todo sin paginar).
- [ ] Versionado de API (`/api/v1/...`).
- [ ] Health checks.
- [ ] **Mensajes de validación automática de ASP.NET Core en inglés**: todos los
  `message` que escribe el código propio (controllers) y los `ErrorMessage`
  explícitos de las DTOs ya están en español (ver `CLAUDE.md` → "Idioma"), pero
  un atributo de `DataAnnotations` sin `ErrorMessage` propio (ej. `[Required]`
  a secas) sigue generando el mensaje default del framework en inglés — 78
  atributos en 34 DTOs están en ese caso hoy. Arreglarlo de raíz requiere
  localización real (`AddDataAnnotationsLocalization` + archivos de recursos
  con `DisplayName` traducido por campo) en vez de escribir `ErrorMessage` a
  mano en cada atributo uno por uno.

## Limpieza técnica

- [ ] `Scripts/full_migration.sql` (124 líneas) parece un script viejo/duplicado de
  `Scripts/001_FullMigration.sql` (el que se regenera con cada migración nueva y sí
  está al día). Revisar si sigue siendo necesario o se puede eliminar.
