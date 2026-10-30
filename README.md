# StockFlow API

API sin interfaz para practicar inventario y pedidos con .NET 10, PostgreSQL, Redis y un servicio externo de cotizaciones. Se explora con Swagger UI y la colección de Postman de `postman/`.

## Requisitos

- [SDK de .NET 10](https://dotnet.microsoft.com/en-us/download/dotnet/10.0). El repositorio fija la línea del SDK en `global.json`.
- Docker Desktop con Docker Compose.

## Ejecutar en local

1. Iniciar PostgreSQL y Redis:

   ```powershell
   docker compose up -d
   ```

2. Guardar una clave JWT local de al menos 32 bytes en User Secrets. El comando genera una clave aleatoria y no la agrega al repositorio:

   ```powershell
   $keyBytes = New-Object byte[] 32
   $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
   $rng.GetBytes($keyBytes)
   $jwtKey = [Convert]::ToBase64String($keyBytes)
   dotnet user-secrets set "Authentication:Jwt:SigningKey" $jwtKey --project src/StockFlow.Api
   $rng.Dispose()
   Remove-Variable jwtKey,keyBytes,rng
   ```

3. Restaurar paquetes y aplicar las migraciones antes de iniciar la API. Instala el SDK 10 indicado por `global.json`; en el entorno de trabajo de Codex hay además una copia local ignorada por Git en `.dotnet/`.

   ```powershell
   dotnet restore
   dotnet tool restore
   dotnet ef database update --project src/StockFlow.Infrastructure --startup-project src/StockFlow.Api --context StockFlowDbContext
   ```

4. Iniciar la API:

   ```powershell
   dotnet run --project src/StockFlow.Api
   ```

5. Abrir Swagger en `http://localhost:5080/swagger` o importar `postman/StockFlow.postman_collection.json` y el entorno `postman/StockFlow Local.postman_environment.json` en Postman. Ejecuta Register o Login para obtener un bearer token; úsalo para consultar la identidad, operar el catálogo y probar depósitos e inventario. Los endpoints de estado y cotización siguen disponibles de forma anónima.

Los valores de `appsettings.Development.json` y `compose.yaml` son credenciales locales de ejemplo. Para otros entornos, usa secretos de usuario o variables de entorno y no subas credenciales reales al repositorio.

## Migraciones de base de datos

Con PostgreSQL activo, restaura la herramienta local y genera/aplica migraciones desde la raíz:

```powershell
dotnet tool restore
dotnet ef migrations add NombreDeLaMigracion --project src/StockFlow.Infrastructure --startup-project src/StockFlow.Api --context StockFlowDbContext --output-dir Persistence/Migrations
dotnet ef database update --project src/StockFlow.Infrastructure --startup-project src/StockFlow.Api --context StockFlowDbContext
```

La factoría de diseño usa `ConnectionStrings__Postgres` si está definida; en local tiene el mismo fallback de desarrollo que Compose. La API no aplica migraciones automáticamente al arrancar.

## Identidad y organizaciones

- `POST /api/v1/auth/register` crea un usuario, una organización y la membresía inicial `Owner` dentro de una transacción PostgreSQL. ASP.NET Core Identity guarda el hash de la contraseña; no se persiste la contraseña en texto plano.
- `POST /api/v1/auth/login` requiere email, contraseña y slug de organización. Solo emite un JWT cuando existe una membresía para esa organización. Login inválido devuelve un `401` genérico.
- `GET /api/v1/auth/me` devuelve los claims validados del usuario y su organización activa. El tenant se deriva del claim firmado `org_id`; endpoints futuros deben autorizar y filtrar datos usando `ICurrentTenant`, nunca un tenant elegido en el body.
- Los tokens expiran a los 20 minutos. El rol está guardado en cada membresía (`Owner`, `Admin`, `Member`), por lo que pertenece a una organización y no a la cuenta en general.
- Las contraseñas requieren 12–128 caracteres, mayúscula, minúscula y número. Registro limita a tres requests por minuto por IP y login a cinco; no se bloquean cuentas por contraseña incorrecta para evitar que terceros provoquen ese bloqueo.
- El emisor JWT local existe para aprender. Un sistema real debe delegar autenticación/emisión de access tokens en un proveedor OAuth 2.0 / OpenID Connect, servir HTTPS y configurar el API para validar sus tokens. Detrás de un proxy, limita login por una dirección reenviada solo si el proxy está explícitamente confiado.

## Catálogo de productos

Todos los endpoints del catálogo requieren `Authorization: Bearer <accessToken>`. La organización se toma del JWT validado; no se envía un `organizationId` en el body.

| Método | Endpoint | Comportamiento |
| --- | --- | --- |
| `POST` | `/api/v1/products` | Crea un producto; responde `201` y normaliza el SKU a mayúsculas. |
| `GET` | `/api/v1/products/{productId}` | Obtiene un producto de la organización actual; un ID de otra organización responde `404`. |
| `GET` | `/api/v1/products` | Busca por `search`, pagina con `page` y `pageSize`, y permite `sortBy`, `sortDirection` e `includeInactive`. |
| `PUT` | `/api/v1/products/{productId}` | Actualiza SKU, nombre, descripción, precio, moneda y estado activo. |
| `DELETE` | `/api/v1/products/{productId}` | Desactiva el producto (`is_active = false`); no lo elimina físicamente. |

La búsqueda acepta `page` entre 1 y 1.000.000, `pageSize` entre 1 y 100, `sortBy` entre `name`, `sku`, `unitPrice` y `createdAtUtc`, y `sortDirection` `asc` o `desc`. Por defecto excluye productos inactivos; usa `includeInactive=true` para incluirlos. El SKU es único por organización. Importa la colección y el entorno Postman para tener ejemplos de creación, búsqueda, actualización y desactivación; Swagger describe los contratos y errores esperados.

El detalle del producto se cachea en Redis durante 5 minutos; las búsquedas paginadas, durante 2 minutos. Las claves de catálogo usan la versión `stockflow:catalog:v1` e incluyen tenant y generación. La generación de cada tenant vence a los 30 días; create, update y deactivate la rotan solo después del commit PostgreSQL. Las claves de búsqueda usan SHA-256 de las opciones canónicas de búsqueda, paginación y orden.

PostgreSQL sigue siendo la fuente de verdad. Si Redis falla o expira, el API registra warnings/timeouts y responde desde PostgreSQL; si el GET de generación indica que Redis no está disponible, se evitan las llamadas de caché restantes para esa lectura. Los logs estructurados permiten seguir cache hit/miss y rotaciones de generación. Si falla la invalidación posterior al commit o el proceso se detiene antes de rotar, las entradas pueden seguir vigentes hasta 5 minutos después de su última escritura. Una request que empezó antes del commit puede escribir datos viejos después de ese momento y reiniciar el TTL; por eso, el período total puede exceder 5 minutos desde el commit si esa request sigue en curso. El inventario no se cachea.

## Depósitos e inventario

Todos los endpoints de depósitos e inventario requieren bearer token. La organización se deriva del JWT; no se envía un `organizationId` en la request.

| Método | Endpoint | Comportamiento |
| --- | --- | --- |
| `POST` | `/api/v1/warehouses` | Crea un depósito para la organización actual. |
| `GET` | `/api/v1/warehouses` | Lista los depósitos de la organización actual. |
| `GET` | `/api/v1/warehouses/{warehouseId}` | Obtiene un depósito de la organización actual. |
| `POST` | `/api/v1/warehouses/{warehouseId}/inventory/{productId}/adjustments` | Aplica un delta firmado y un motivo; un ajuste confirmado responde `201`. |
| `GET` | `/api/v1/warehouses/{warehouseId}/inventory` | Consulta el saldo del depósito. |
| `GET` | `/api/v1/warehouses/{warehouseId}/inventory/movements` | Consulta el ledger; admite `productId`, `page` y `pageSize`. |

Cada ajuste usa SQL parametrizado dentro de una transacción PostgreSQL: actualiza el saldo de forma condicional para que no sea negativo e inserta el movimiento en la misma transacción. Si falla la inserción, el saldo se revierte. Un saldo insuficiente responde `409` sin crear un movimiento; los IDs de otra organización responden `404`. Las FK compuestas validan que depósito y producto pertenezcan a la misma organización. El ledger es append-only y un trigger rechaza operaciones manuales `UPDATE`, `DELETE` y `TRUNCATE`.

El ajuste todavía no es idempotente: repetir la misma request puede volver a cambiar el saldo y crear otro movimiento. La Etapa 7 agrega idempotencia a la creación de pedidos; este endpoint de ajustes sigue sin una clave idempotente. El inventario no usa Redis ni toma decisiones a partir de datos cacheados.

## Pedidos y reservas — Etapa 7 en progreso

El flujo está implementado y queda pendiente completar su validación de aceptación:

- `POST /api/v1/orders` crea un pedido y sus reservas. Requiere `Idempotency-Key` y utiliza la organización del JWT autenticado.
- `GET /api/v1/orders/{orderId}` consulta un pedido del tenant actual con sus snapshots y estado de reserva.
- `POST /api/v1/orders/expire-due` libera únicamente reservas vencidas y cambia el estado `Reserved` a `Expired`. El vencimiento está configurado para 2 minutos en la demo.

La clave de idempotencia es obligatoria y única por organización. Se guarda un hash estable del depósito y las líneas normalizadas para reconocer reintentos equivalentes y rechazar el uso de la misma clave con contenido diferente. Las líneas conservan un snapshot de SKU, nombre, precio y moneda. Hasta la Etapa 8, todas las líneas del pedido deben compartir moneda; la conversión se agregará luego.

Un reintento equivalente devuelve la representación original de creación y el header `Idempotency-Replayed: true`; `GET /api/v1/orders/{orderId}` muestra el estado actual del pedido. El esquema también liga el creador a una membresía del tenant y obliga a que depósito y vencimiento de cada reserva coincidan con los del pedido.

La creación reserva stock de forma atómica incrementando `inventory_balances.quantity_reserved`, sin cambiar `quantity_on_hand` ni insertar en `stock_movements`. La expiración libera esas reservas vencidas explícitamente. El outbox y el worker quedan fuera de esta etapa y se incorporarán en la Etapa 9.

## Arquitectura y plan

- [Arquitectura](docs/architecture.md)
- [Plan de desarrollo: 11 etapas](docs/development-plan.md)
- [Reglas de contribución](AGENTS.md)

Las skills de Codex específicas del repositorio están en `.agents/skills/`.
