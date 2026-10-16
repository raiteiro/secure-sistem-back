# Deuda técnica y pendientes

Trabajo diferido detectado durante el desarrollo: cosas que "hay que hacer después",
configuración pendiente antes de producción, o inconsistencias detectadas pero no
resueltas en el momento. Se mantiene actualizado automáticamente como parte del
trabajo normal (ver `CLAUDE.md`).

## Bloqueante antes de producción

- [ ] **El SQL Server de producción de este despliegue (`50.21.190.109`) es SQL Server
  2008 SP3 (10.0.5500.0)** — confirmado con `SELECT @@VERSION`, descontinuado por
  Microsoft desde 2019. Su compatibility level máximo es 100 (no hay nada que subir;
  a diferencia del incidente anterior con `AT TIME ZONE`, aquí no es un tema de
  compatibility level de la base sino del motor mismo). Hasta ahora se encontraron
  dos sintaxis modernas que no soporta:
  - `AT TIME ZONE` (requiere SQL Server 2016+) — resuelto reemplazando por
    `DATEADD(HOUR, -6, SYSUTCDATETIME())` en las migraciones de seed.
  - `OFFSET...FETCH NEXT` para paginación (requiere SQL Server 2012+) — **ojo**:
    `SqlServerDbContextOptionsBuilder.UseCompatibilityLevel(100)` fue el primer
    intento y no funciona (EF Core 3.0+ quitó el soporte para traducir
    `Skip()/Take()` a `ROW_NUMBER()` en SQL Server pre-2012; esa opción no controla
    eso en EF Core moderno). El arreglo real quedó en
    `Common/PaginationHelper.ApplyPage`: materializa la query (`.ToList()`) y pagina
    en memoria (`.Skip().Take()` sobre el `List<T>`, no sobre el `IQueryable`), así
    nunca se genera `OFFSET/FETCH` en el SQL. Costo: trae a memoria todo el
    resultado filtrado/ordenado antes de paginar — aceptable para el volumen de
    datos esperado aquí, pero no escala indefinidamente (ver si se vuelve un
    problema en catálogos grandes sin filtro de fecha, como Productos/Clientes).
  - `list.Contains(x.Propiedad)` dentro de un `.Where()` traducido a SQL (requiere
    `OPENJSON`, SQL Server 2016+) — EF Core 8 cambió el default: una colección
    capturada (`List<int>`, `HashSet<int>`, etc.) usada en `.Contains()` se manda
    como un solo parámetro JSON y se decodifica con `OPENJSON(...) WITH (...)` en
    vez de generar `IN (1, 2, 3, ...)` literal. No existe un interruptor global en
    esta versión de EF Core para desactivarlo (se buscó `UseCompatibilityLevel`,
    `TranslateParameterizedCollectionsToConstants` — ninguno existe/aplica); el
    arreglo es `EF.Constant(lista)` en cada `.Contains()` afectado, que fuerza el
    `IN (...)` literal. Se aplicó en los ~21 sitios detectados (`Controllers/*`,
    `Services/SaleService.cs`) al momento de este despliegue — pero es un patrón
    fácil de reintroducir: **cualquier `.Where(x => algunaLista.Contains(x.Id))`
    nuevo contra `_context` necesita `EF.Constant()` alrededor de la lista**, o
    tronará con `Incorrect syntax near the keyword 'WITH'` en este servidor.
  - **Riesgo abierto**: no hay garantía de que no aparezcan más (ej. `STRING_AGG`,
    `FORMAT()`, `TRY_PARSE`, funciones JSON, todas post-2012/2016) en queries LINQ no
    probadas aún contra este motor. Si aparece un 500 nuevo en producción, revisar
    primero si es una sintaxis de SQL Server moderna que EF Core generó. La solución
    real de fondo es migrar la base a un SQL Server soportado, no seguir parchando
    sintaxis una por una.
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
- [x] ~~**Primer administrador de sistema** solo se podía crear con SQL directo~~ —
  **resuelto**: la migración `SeedSystemAdmin` crea la empresa "Administración del
  Sistema" (con sucursal, almacén, árbol de rutas y rol con todos los permisos por
  defecto, igual que `CompaniesController.Create`) y el usuario `sysadmin` con
  `IsSystemAdmin = 1`. Solo corre si la base no tiene ya un administrador de sistema.
- [ ] **Contraseña inicial de `sysadmin` conocida (`Admin123!`)**: está en el repo
  (migración `SeedSystemAdmin` y `Scripts/001_FullMigration.sql`). Se mitiga con
  `MustChangePassword = 1` (obliga a cambiarla en el primer login), pero en un
  despliegue real hay que entrar y cambiarla **inmediatamente** después de aplicar
  el script, antes de exponer la API.
- [x] ~~`001_FullMigration.sql` fallaba en servidores con SQL Server 2014 o
  anterior~~ — **resuelto**: las migraciones de seed (`SeedSystemAdmin`,
  `ZeroComboInventory`, `AddSuppliesAndProduction`, `AddUnitsOfMeasure`,
  `RegroupNavigationRoutes`) calculaban `@now` con
  `AT TIME ZONE 'Central Standard Time (Mexico)'` (sintaxis de SQL Server
  2016+, sin importar el compatibility level de la base). Al desplegar a un
  servidor IIS con un motor más viejo, esos batches fallaban en el parseo
  (`Incorrect syntax near 'AT'`) y, como el error es de parseo, el batch
  completo no corría — ni siquiera el `INSERT INTO __EFMigrationsHistory` —
  así que el script terminaba "sin errores visibles de estructura" pero sin
  datos semilla. Se reemplazó por `DATEADD(HOUR, -6, SYSUTCDATETIME())` en
  las 5 migraciones (México no observa horario de verano desde 2022, así que
  el offset de Ciudad de México es fijo todo el año), portable a cualquier
  versión de SQL Server, y se regeneró `001_FullMigration.sql`.
- [ ] **Índices `IX_RefreshTokens_Token` / `IX_PasswordResetTokens_Token`
  exceden el máximo de 900 bytes para una clave de índice** (warning de SQL
  Server al correr `001_FullMigration.sql`, no bloquea la migración): con
  combinaciones de valores grandes el insert/update sobre esos índices
  podría fallar. Revisar si conviene acortar la columna `Token` o usar un
  hash indexado en vez del token completo.
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

- [ ] **El costo de los productos no se actualiza solo.** Ni al recibir una orden de
  compra (`PurchaseOrdersController.Receive` no toca `Product.Cost` con el
  `UnitCost` recibido) ni al fabricar (`ProductionController.Produce` no calcula el
  costo del fabricado a partir del costo de sus insumos). `Cost` queda como lo
  capturó el usuario, así que los reportes de margen pueden desfasarse. Decidir la
  regla (último costo vs. costo promedio ponderado) antes de implementarlo.
- [ ] **Producción sin merma ni rendimiento.** La receta es fija: fabricar N
  descuenta exactamente cantidad × N de cada insumo. No se maneja merma ("de 1 kg de
  harina salen 0.95 kg útiles") ni ajustes por lote.
- [ ] **Folio de órdenes de producción con `MAX + 1`**: mismo riesgo de colisión bajo
  concurrencia que el folio de ventas (ver el pendiente de `SalesController.Create`).

- [ ] **Un combo sin componentes activos se puede vender y no descuenta nada.**
  `SaleService` recorre los `ProductComboItem` activos del combo; si no tiene
  ninguno, la venta pasa sin tocar inventario. `GET /api/inventory/combo-availability`
  ya lo reporta con disponible 0 (para no ofrecerlo en el front), pero el backend no
  lo impide. Si se quiere cerrar del todo, rechazar en `SaleService` la venta de un
  combo sin componentes activos (y revisar el mismo caso en
  `QuotesController.ConvertToSale`, que usa el mismo servicio).

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
  que corresponda. Aplica también a los permisos de producción
  (`WAREHOUSES.PRODUCE`, `CREATE_MANUFACTURED`, `EDIT_RECIPE`, `DISASSEMBLE`): existen
  en el catálogo para el frontend, pero ningún endpoint los valida.
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
- [ ] **Tabla `UnitMigrationReport`**: la creó la migración `AddUnitsOfMeasure` como
  reporte de revisión manual (productos que cayeron en "Pieza" por no reconocer su
  unidad de texto, y cantidades existentes con más decimales de los que permite su
  unidad nueva). No es parte del modelo de EF. En cada base donde se aplique la
  migración, revisarla (`SELECT * FROM UnitMigrationReport`), corregir a mano lo que
  haga falta y después borrarla (`DROP TABLE UnitMigrationReport`).
- [ ] `Scripts/002_SeedData.sql` y `Scripts/003_SeedNavigationRoutes.sql` quedaron
  obsoletos: los reemplaza la migración `SeedSystemAdmin`. Además están desfasados
  del esquema actual (no marcan `IsSystemAdmin`, no asignan permisos, rutas en
  inglés y con `WindowId` que chocan con las reales como `win-inventory`/
  `win-reports`). No correrlos; eliminarlos.
