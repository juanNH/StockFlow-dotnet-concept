---
name: stockflow-persistence-caching
description: Cambia esquema PostgreSQL, mappings de EF Core, transacciones SQL explícitas o comportamiento Redis sin romper la consistencia.
---

# Flujo para persistencia y caché en StockFlow

Usa esta skill para cambios de esquema, persistencia, SQL manual, transacciones o caché.

1. Lee `AGENTS.md` y las reglas de consistencia de datos en `docs/architecture.md`.
2. Coloca contratos de persistencia en Application e implementaciones específicas del proveedor en Infrastructure. Mantén tipos EF/Npgsql/Redis fuera de Domain y de los contratos HTTP.
3. Evoluciona PostgreSQL con migraciones EF Core. No uses `EnsureCreated` ni ejecutes migraciones como efecto secundario del inicio de la API.
4. Parametriza cada valor SQL. Para una operación con varias escrituras, usa una conexión y transacción explícitas; confirma al terminar todas las escrituras relacionadas y revierte ante errores. Usa decrementos condicionales de stock y revisa filas afectadas.
5. No mantengas una transacción SQL abierta durante una llamada HTTP. Persiste una intención outbox si un efecto externo debe ejecutarse después de forma confiable.
6. Usa Redis para datos derivados/de lectura con TTL e invalidación claros. PostgreSQL sigue siendo autoridad para el stock disponible y la aceptación de pedidos. Invalida solo después de un commit exitoso.
7. Define qué ocurre si Redis falla; cuando sea razonable, las lecturas de catálogo/cotización deben poder usar su fuente. Nunca ocultes errores de base como cache miss.
8. Actualiza instrucciones de migración, documentación de flujo de datos y ejemplos Swagger/Postman si cambia el contrato público. Registra el comportamiento de consistencia relevante en el plan.
