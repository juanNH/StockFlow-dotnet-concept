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

La búsqueda acepta `page` entre 1 y 1.000.000, `pageSize` entre 1 y 100, `sortBy` entre `name`, `sku`, `unitPrice` y `createdAtUtc`, y `sortDirection` `asc` o `desc`. Por defecto excluye productos inactivos; usa `includeInactive=true` para incluirlos. El SKU es único por organización. Importa la colección y el entorno Postman para tener ejemplos de creación, búsqueda, actualización y desactivación; Swagger describe los contratos y errores esperados. La lectura del catálogo usa PostgreSQL en esta etapa; Redis se integrará en la Etapa 6.

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

El ajuste todavía no es idempotente: repetir la misma request puede volver a cambiar el saldo y crear otro movimiento. Las claves de idempotencia se incorporan en la Etapa 7. El inventario no usa Redis ni toma decisiones a partir de datos cacheados.

## Arquitectura y plan

- [Arquitectura](docs/architecture.md)
- [Plan de desarrollo: 11 etapas](docs/development-plan.md)
- [Reglas de contribución](AGENTS.md)

Las skills de Codex específicas del repositorio están en `.agents/skills/`.
