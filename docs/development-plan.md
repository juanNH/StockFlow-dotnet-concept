# Plan de desarrollo de StockFlow

**Cantidad total: 11 etapas.** Avanza en orden y mantén cada etapa lo bastante pequeña como para revisarla. Se requiere el SDK .NET 10 según `global.json`. Completa cada etapa cuando sus entregables y criterios estén satisfechos. Actualiza la colección Postman a medida que agregues endpoints.

## Etapa 1 — Esqueleto y entorno local ✅

- Solución Clean Architecture con proyectos Domain, Application, Infrastructure y API.
- Target .NET 10, versiones centrales de paquetes, Swagger, ProblemDetails, servicios locales en Compose y colección inicial de Postman.
- Ejemplo de cotización con Frankfurter y Redis; PostgreSQL y Npgsql/EF Core registrados para próximas etapas.
- **Validado:** restore/build sin advertencias; PostgreSQL y Redis quedan healthy en Compose; Swagger y `/health/live` responden; cotización USD/EUR devuelve 200 y el par no soportado 400.
- **Caché validada:** la primera request encontró la clave ausente, guardó la cotización en Redis y la segunda respondió desde caché (20 ms).
- **Criterio cumplido:** los endpoints de la colección Postman responden y los servicios locales arrancan.

## Etapa 2 — Persistencia y primera migración ✅

- **Implementado:** entidad de dominio `Organization`, mapping PostgreSQL explícito, `DbSet`, factory de diseño EF Core y migración `InitialCreate` con índice único por `slug`.
- **Implementado:** guía de migraciones locales y registro central de la herramienta `dotnet-ef`.
- **Validado:** restore/build sin advertencias; `dotnet-ef` generó la migración y su script SQL contiene la tabla `organizations` y el índice único.
- **Validado:** `dotnet ef database update` aplicó la migración a una base PostgreSQL vacía; `information_schema`, `pg_indexes` e `__EFMigrationsHistory` confirman columnas, índice único y versión aplicada.
- **Criterio cumplido:** una base local vacía se crea desde migraciones y el esquema coincide con el modelo.

## Etapa 3 — Identidad y separación por organización

- **Implementado:** usuario ASP.NET Core Identity, membresía por organización con roles `Owner`, `Admin` y `Member`, restricción única `(user_id, organization_id)` y FK a usuario/organización.
- **Implementado:** registro transaccional `POST /api/v1/auth/register`, login por email/contraseña/slug `POST /api/v1/auth/login` y consulta autenticada `GET /api/v1/auth/me`.
- **Implementado:** JWT firmado y de vida corta con `sub`, `org_id`, `org_role`, issuer, audience, expiración y `jti`; validación de issuer/firma/audience/lifetime y formato de claims de tenant en API, y bearer en Swagger.
- **Implementado:** política de contraseña alineada entre API e Identity; verificación ficticia para emails inexistentes, límite de cinco logins y tres registros por minuto por IP; no se bloquea la cuenta a partir de intentos fallidos.
- **Implementado:** `ICurrentTenant` en Application y adaptador HTTP en API; nunca se toma el tenant de body/header para autorizar. Clave local configurada con User Secrets.
- **Implementado:** requests de Postman y entorno con `accessToken`, `organizationId` y `organizationSlug`.
- **Validado:** build sin advertencias; migración generada y aplicada; registro, login, acceso anónimo/autenticado, tenant del `/me`, errores de credenciales y Swagger verificados con la API local.
- **Criterio cumplido:** el token solo se emite si la membresía para la organización existe; `/me` devuelve exclusivamente el contexto incluido por el servidor en el JWT validado.

## Etapa 4 — Catálogo de productos ✅

- **Implementado:** agregado el catálogo tenant-owned de productos con SKU normalizado, reglas de nombre, descripción, moneda y precio; la base protege SKU, formato de moneda y escala de precio y aplica unicidad por `(organization_id, sku)`.
- **Implementado:** agregado `POST /api/v1/products`, `GET /api/v1/products/{productId}`, `GET /api/v1/products`, `PUT /api/v1/products/{productId}` y `DELETE /api/v1/products/{productId}`. DELETE es una baja lógica (`is_active = false`).
- **Implementado:** la búsqueda pagina (page 1–1.000.000, pageSize 1–100), busca por nombre/SKU, excluye inactivos por defecto y admite orden por nombre, SKU, precio o fecha de creación con dirección ascendente/descendente.
- **Implementado:** todos los endpoints requieren autenticación y obtienen la organización de `ICurrentTenant`, derivado de los claims validados del JWT. EF Core aplica un filtro global de organización y el repositorio conserva predicados explícitos por organización.
- **Implementado:** requests, respuestas, errores esperados, Swagger y ejemplos de Postman actualizados. Las migraciones de EF Core se aplican explícitamente; la API no las ejecuta al iniciar.
- **Validado:** build sin advertencias; migraciones de catálogo y precisión de precio generadas y aplicadas. Smoke HTTP verificó registro/login, alta, actualización, búsqueda, paginación, SKU único por tenant, errores 400/404/409, baja lógica, bearer y Swagger. Inserción SQL manual con tres decimales rechazada por `ck_products_unit_price_scale`.
- **Criterio cumplido:** los endpoints están aislados por tenant, documentados en Swagger y representados en Postman; las reglas de precio también se aplican a escrituras SQL manuales.

## Etapa 5 — Inventario, ledger y SQL manual

- Agrega depósitos, saldos y un ledger append-only de movimientos de stock.
- Practica SQL parametrizado con Npgsql para ajustes y decrementos condicionales.
- Confirma el cambio de saldo y el movimiento en una transacción SQL; verifica filas afectadas y rollback.
- **Criterio:** el stock no puede quedar negativo y cada ajuste confirmado tiene su movimiento.

## Etapa 6 — Redis para lecturas del catálogo

- Cachea consultas y detalles de producto con claves versionadas y TTL documentado.
- Invalida las claves después de cambios confirmados; permite continuar desde PostgreSQL si Redis falla.
- Mantén la disponibilidad de stock fuera de las decisiones de pedidos basadas en caché.
- **Criterio:** Postman o los logs demuestran cache hit, invalidación después de actualizar y comportamiento correcto con Redis desconectado.

## Etapa 7 — Pedidos, reservas e idempotencia

- Agrega estados de pedido y snapshots de líneas (precio/nombre al momento de compra).
- Reserva inventario de forma transaccional y expira reservas con transiciones de estado explícitas.
- Implementa claves de idempotencia para que los reintentos no dupliquen pedidos ni reservas.
- **Criterio:** requests repetidas devuelven el resultado original y solicitudes concurrentes no venden stock de más.

## Etapa 8 — Cotizaciones externas y resiliencia

- Amplía la integración inicial con Frankfurter: valida respuestas, limita reintentos de GET transitorios y maneja timeouts/fallos upstream.
- Cachea cotizaciones según su frecuencia de actualización; define cuándo se acepta un valor vencido y expón la fecha de la cotización.
- Persiste en el pedido la tasa y moneda aplicadas cuando uses conversión.
- **Criterio:** Postman demuestra éxito, error de validación, caída/timeout del proveedor y comportamiento con caché.

## Etapa 9 — Outbox y procesamiento en segundo plano

- Agrega un registro outbox dentro de los flujos transaccionales de pedidos.
- Agrega un worker hospedado que tome mensajes pendientes, reintente fallos y registre finalización o dead-letter.
- Mantén los llamados externos fuera de la transacción del pedido y haz idempotentes los consumidores.
- **Criterio:** pedido y evento outbox se guardan juntos; el worker se recupera ante fallos transitorios sin repetir efectos.

## Etapa 10 — Operación y seguridad de API

- Agrega readiness checks para PostgreSQL y Redis, logs estructurados, tracing/métricas, rate limiting y validación de configuración.
- Revisa autenticación, aislamiento de tenants, errores, secretos, límites de entrada y paginación.
- **Criterio:** se distingue liveness de readiness y se puede diagnosticar una request sin filtrar información sensible.

## Etapa 11 — Cobertura, CI y entrega

- Completa cobertura unitaria/de integración para reglas de dominio, transacciones/concurrencia SQL, aislamiento de tenants, invalidación e idempotencia.
- Agrega GitHub Actions para restaurar, compilar y ejecutar la suite, además de una imagen de contenedor reproducible.
- Termina README, diagrama, guía de migraciones, colección/entorno Postman y escenarios de ejemplo.
- **Criterio:** un checkout limpio se inicia siguiendo los pasos documentados y el pipeline pasa con dependencias descartables.
