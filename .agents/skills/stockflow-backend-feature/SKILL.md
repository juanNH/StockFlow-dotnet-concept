---
name: stockflow-backend-feature
description: Agrega o extiende un caso de uso de StockFlow a través de las capas de Clean Architecture, incluyendo su contrato HTTP y request de Postman.
---

# Flujo para implementar funcionalidades backend en StockFlow

Usa esta skill al implementar un endpoint nuevo o extender un caso de uso existente.

1. Lee `AGENTS.md`, `docs/architecture.md` y la etapa activa de `docs/development-plan.md`.
2. Mantén el cambio dentro de la etapa actual. Define la regla de negocio y el contrato API antes de editar.
3. Ubica invariantes en Domain, orquestación y puertos en Application, adaptadores en Infrastructure, y validación/mapeo HTTP en API. Conserva dependencias hacia adentro.
4. Propaga `CancellationToken` en llamadas asíncronas. Valida entradas y devuelve errores `ProblemDetails` seguros. Mantén los controladores delgados.
5. Actualiza metadata/respuestas de Swagger y agrega o corrige la request correspondiente en `postman/StockFlow.postman_collection.json`.
6. Para cambios de base, SQL transaccional o Redis, lee también `.agents/skills/stockflow-persistence-caching/SKILL.md`.
7. Actualiza la documentación afectada e informa endpoint, comportamiento, configuración y validaciones realizadas.
