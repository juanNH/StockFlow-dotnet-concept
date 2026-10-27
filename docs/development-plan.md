# Plan de desarrollo de StockFlow

**Cantidad total: 11 etapas.** Avanza en orden y mantén cada etapa lo bastante pequeña como para revisarla. El esqueleto inicial está implementado; la validación de la etapa 1 queda pendiente porque esta máquina solo tiene el SDK .NET 8 y el repositorio apunta a .NET 10. Completa una etapa cuando sus entregables y criterios estén satisfechos. Actualiza la colección Postman a medida que agregues endpoints.

## Etapa 1 — Esqueleto y entorno local ⏳

- Solución Clean Architecture con proyectos Domain, Application, Infrastructure y API.
- Target .NET 10, versiones centrales de paquetes, Swagger, ProblemDetails, servicios locales en Compose y colección inicial de Postman.
- Ejemplo de cotización con Frankfurter y Redis; PostgreSQL y Npgsql/EF Core registrados para próximas etapas.
- **Criterio:** el SDK configurado restaura y compila la solución, PostgreSQL y Redis arrancan, Swagger abre y las dos requests iniciales funcionan desde Postman. Instala .NET 10 para terminar esta verificación y marcar la etapa como completa.

## Etapa 2 — Persistencia y primera migración

- Define el primer modelo relacional y las configuraciones del `DbContext`.
- Incorpora la factoría de diseño/herramientas de EF Core y una migración inicial explícita.
- Documenta cómo aplicar migraciones en local; no migres al iniciar la API.
- **Criterio:** una base local vacía se crea desde migraciones y el esquema coincide con el modelo.

## Etapa 3 — Identidad y separación por organización

- Define organizaciones, usuarios, membresías y roles/permisos para el ejercicio SaaS.
- Agrega autenticación y contexto de tenant en el límite de la API.
- Aplica el tenant en Application/Infrastructure; no confíes solo en un tenant enviado en el cuerpo del request.
- **Criterio:** cada usuario autenticado solo accede a datos de su organización. Agrega variables de entorno Postman para tokens y organizaciones.

## Etapa 4 — Catálogo de productos

- Agrega reglas de dominio de productos/SKU, mappings y casos de uso CRUD/búsqueda.
- Incorpora paginación, ordenamiento, validación de requests y errores estables.
- **Criterio:** los endpoints están filtrados por tenant, documentados en Swagger y representados en Postman.

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
