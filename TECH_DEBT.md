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

- [ ] **Cotizaciones no se pueden editar, solo cancelar.** `QuotesController`
  tiene `Create`/`Cancel`/`ConvertToSale` pero no `Update` — si el cliente
  pide cambiar cantidades/productos de una cotización ya creada, hoy hay
  que cancelarla y crear una nueva (pierde el folio original). Agregar un
  `POST /api/quotes/{id}/update` si se vuelve un flujo frecuente.
- [ ] **Órdenes de compra no se pueden editar ni agregar/quitar líneas**
  después de creadas — mismo caso que Cotizaciones, solo `Cancel` (y solo
  si nada se ha recibido todavía).
- [ ] **Combos: la composición no se snapshotea al vender.** `SaleItem`/
  `ReturnItem` no guardan qué componentes tenía el combo al momento de la
  venta — `SalesController.Cancel`/`ReturnsController.Create` consultan la
  composición **actual** (`ProductComboItem` activos) del combo para
  reponer stock. Si alguien edita los componentes de un combo (quita/agrega
  productos) entre la venta y una cancelación/devolución posterior, se
  repone la composición nueva, no la que realmente se vendió. Para
  productos normales no pasa (el precio/impuesto sí se snapshotea en
  `SaleItem`). Bajo riesgo mientras los combos no cambien de composición
  seguido, pero si se vuelve un problema real, la solución es snapshotear
  los componentes del combo en una tabla nueva al momento de la venta.

- [ ] **Consignación: devolución de una venta ya liquidada no se reconcilia
  sola.** Si se devuelve (total o parcialmente) un artículo consignado cuya
  `ConsignmentSale` ya fue pagada al consignador (`SettlementId` no nulo),
  `ReturnsController.Create` **no** ajusta el monto — solo deja un
  `LogWarning` (`ILogger`, no visible para el usuario) para que alguien lo
  reconcilie a mano. Revertir un pago que ya salió requiere una decisión de
  negocio (¿se descuenta del próximo pago? ¿se le pide de vuelta al
  consignador?) que no se puede automatizar sin más contexto. Caso poco
  común (requiere que la devolución llegue después de una liquidación), pero
  si se vuelve frecuente, conviene al menos exponer estas devoluciones en
  algún reporte visible en vez de solo loguearlas.
- [ ] **Consignación no cubre combos.** Si el componente de un combo
  (`ProductComboItem.ComponentProductId`) es a su vez un producto atribuido
  a un consignador, vender el combo descuenta su stock igual que cualquier
  componente, pero **no genera ningún `ConsignmentSale`** — no hay
  `SaleItem` propio para ese componente al que atribuírselo (el combo es
  una sola línea de venta). Si se necesita cobrar consignación dentro de
  combos, hay que diseñar cómo prorratear el precio del combo entre sus
  componentes primero.

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
- [x] ~~Paginación y filtrado en tablas transaccionales y catálogos grandes~~ —
  **resuelto** para las 11 de prioridad alta/media según `pagination.md` (auditoría del
  frontend): `GET /api/sales`, `/api/returns`, `/api/cashsessions`,
  `/api/inventorymovements`, `/api/consignment/sales`, `/api/consignment/settlements`
  (alta, con `from`/`to`) y `GET /api/products`, `/api/customers`, `/api/inventory`,
  `/api/users`, `/api/reports/cashier-closeouts` (media, sin `from`/`to` — son catálogos,
  no eventos en el tiempo) ya devuelven `PagedResponse<T>`
  (`items`/`page`/`pageSize`/`totalCount`/`totalPages`). Ver `Common/PaginationHelper.cs`
  y `CLAUDE.md` → "Paginación en listados" para el contrato a seguir en endpoints nuevos.
  **Decisión tomada — no se paginan:** el resto de prioridad baja según `pagination.md`
  (`Branches`, `CashRegisters`, `Categories`, `Companies`, `Roles`, `Roles/{id}/routes`,
  `Roles/{id}/permissions`, `Suppliers`, `TaxRates`, `Warehouses`) se queda sin paginar
  — catálogos acotados, no hace falta revisarlos de nuevo por esto.
  `GET /api/navigationroutes/tree` y `/my-tree` **nunca** se paginan (regla permanente,
  no una decisión de tamaño): el frontend arma el menú completo a partir de la
  respuesta completa. Ver `CLAUDE.md` → "Paginación en listados".
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
