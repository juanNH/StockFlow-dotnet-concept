# Reglas de desarrollo de StockFlow

Antes de cambiar las referencias entre proyectos o introducir un patrón arquitectónico, lee `docs/architecture.md`. Usa `docs/development-plan.md` para limitar cada cambio a la etapa activa. Este es un proyecto para aprender: explica las decisiones relevantes y evita sumar infraestructura solo para aparentar complejidad.

## Límites de arquitectura

- `StockFlow.Domain` contiene reglas y tipos de negocio. No referencia otros proyectos StockFlow, ASP.NET Core, EF Core, Npgsql, Redis ni clientes de servicios externos.
- `StockFlow.Application` contiene casos de uso, contratos y orquestación. Puede depender de Domain y abstracciones de framework, pero no de Infrastructure ni de tipos de API.
- `StockFlow.Infrastructure` implementa contratos de Application y administra PostgreSQL, EF Core, Npgsql, Redis y adaptadores HTTP externos.
- `StockFlow.Api` administra rutas HTTP, validación de requests, serialización, Swagger y composición de dependencias. Puede referenciar Application e Infrastructure únicamente para configurar la aplicación.
- Mantén las dependencias apuntando hacia adentro. No pongas reglas de negocio en controladores, configuraciones EF ni clientes externos.
- Prefiere casos de uso enfocados e interfaces explícitas. Evita repositorios genéricos, service locator o mediator si no aportan al caso.

## API y aplicación

- Mantén los controladores delgados y usa rutas versionadas bajo `/api/v1`.
- Propaga `CancellationToken` desde HTTP por todas las llamadas asíncronas de Application e Infrastructure.
- Valida datos en el límite de la API y devuelve errores consistentes `ProblemDetails`; nunca expongas stack traces o secretos en la respuesta.
- Documenta endpoints y errores esperados en Swagger. Mantén actualizadas las requests correspondientes en `postman/StockFlow.postman_collection.json`.
- Lee configuración desde `IConfiguration` u Options. Deja los valores locales en configuración Development; usa user secrets o variables de entorno para secretos reales.
- Usa logs estructurados con identificadores útiles. Nunca registres credenciales, tokens ni payloads sensibles completos.

## Datos y consistencia

- PostgreSQL es la fuente de verdad para inventario y pedidos. Nunca autorices una reserva usando datos de Redis.
- Evoluciona el esquema con migraciones de EF Core. No uses `EnsureCreated` ni apliques migraciones automáticamente al iniciar la API.
- Usa EF Core para persistencia habitual. Cuando una etapa practique SQL manual, parametriza todos los valores y mantén escrituras relacionadas de ledger/pedido/outbox dentro de una transacción explícita.
- Haz decrementos de stock condicionales en SQL y revisa las filas afectadas. Trata la concurrencia como un resultado esperado, no como una excepción de base sin manejar.
- Cachea datos derivados o de lectura con TTL e invalidación explícitos. Actualiza o invalida la caché después del commit de la base.
- Realiza llamadas HTTP externas mediante adaptadores `IHttpClientFactory` en Infrastructure. Configura timeout, propaga cancelación y convierte fallos upstream en errores API seguros.
- No combines una transacción SQL con una llamada de red externa. Persiste primero la intención/outbox cuando necesites una entrega asíncrona confiable.

## Calidad de cambios

- Sigue la etapa activa; no adelantes funcionalidades futuras del roadmap.
- Agrega cobertura unitaria o de integración enfocada cuando corresponda, sobre todo para invariantes de stock, rollback, idempotencia, aislamiento por tenant e invalidación de caché.
- Mantén las versiones de paquetes en `Directory.Packages.props`; no agregues versiones inline en los proyectos.
- Mantén sincronizados los ejemplos de API, comandos de inicio, notas de arquitectura y estado del roadmap.
- Antes de terminar un cambio, revisa el diff e informa cualquier requisito local de SDK o servicio que haya impedido una validación solicitada.

## Delegación y trabajo en paralelo

- Usa subagentes cuando el trabajo tenga revisiones independientes o módulos que puedan avanzarse en paralelo y la coordinación ahorre tiempo. Para cambios pequeños o dependientes, trabaja en serie.
- Antes de delegar, divide el objetivo en tareas con alcance, archivos/área, entregable y límites explícitos. Prefiere auditorías de solo lectura para revisiones.
- Todos los agentes comparten el mismo worktree. Asigna un único agente por archivo compartido; para edición paralela, delimita archivos/módulos distintos y acuerda contratos entre ellos antes de iniciar.
- No paralelices tareas que dependan del resultado de otra, ni permitas ediciones simultáneas de `AGENTS.md`, archivos de solución/configuración central o el mismo endpoint.
- Conserva al agente principal como responsable de integrar cambios, resolver contradicciones, revisar el diff y completar la validación final. Trata los reportes de subagentes como hallazgos que deben comprobarse.
- Mantén la delegación acotada: usa solo los agentes que aporten trabajo independiente y deja capacidad para integración y seguimiento.
