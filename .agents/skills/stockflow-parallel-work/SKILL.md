---
name: stockflow-parallel-work
description: Divide trabajos amplios de StockFlow en tareas independientes para subagentes y coordina cambios seguros en el worktree compartido.
---

# Coordinación paralela en StockFlow

Usa esta skill cuando el usuario pida delegación/paralelismo o cuando una tarea amplia tenga revisiones o módulos genuinamente independientes.

1. Lee `AGENTS.md` y el alcance de la etapa actual en `docs/development-plan.md`.
2. Separa tareas por dependencia: paraleliza investigación, revisión de solo lectura o ediciones en módulos/archivos distintos. Mantén seriales las tareas que dependen de una decisión previa.
3. Da a cada subagente un objetivo acotado, contexto suficiente, entregable esperado, ruta/área permitida y límites (por ejemplo, “solo lectura; no editar ni ejecutar build”).
4. Considera que todos comparten el mismo worktree. Designa un único propietario para cualquier archivo compartido y evita que distintos agentes modifiquen simultáneamente solución, configuración central, documentación común o el mismo endpoint.
5. Acuerda contratos/interfaces entre tareas paralelas de implementación. Si la interfaz no está clara, delega primero análisis y decide antes de asignar ediciones concurrentes.
6. Usa solo tantos subagentes como generen ahorro real y conserva capacidad para integrar el resultado.
7. El agente principal integra todo: revisa los cambios en disco, resuelve solapamientos y discrepancias, ejecuta la validación adecuada al objetivo y comunica al usuario qué se delegó y qué se incorporó.
