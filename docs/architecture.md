# Arquitectura de StockFlow

StockFlow es un proyecto de aprendizaje sin interfaz. Se explora con Swagger UI y se prueba desde Postman. La base usa un monolito modular con estilo Clean Architecture: una API desplegable, una base PostgreSQL, Redis y adaptadores de Infrastructure para servicios externos.

## Proyectos y dependencias

```mermaid
flowchart TD
    Api[StockFlow.Api<br/>HTTP, Swagger, composición]
    App[StockFlow.Application<br/>casos de uso y puertos]
    Domain[StockFlow.Domain<br/>modelo y reglas]
    Infra[StockFlow.Infrastructure<br/>PostgreSQL, Redis, adaptadores HTTP]

    Api --> App
    Api --> Infra
    Infra --> App
    Infra --> Domain
    App --> Domain
```

`StockFlow.Api` referencia `StockFlow.Infrastructure` para registrar los adaptadores al iniciar la app. Application depende de interfaces propias. Domain no tiene dependencias de proyectos ni de infraestructura.

## Responsabilidades

| Proyecto | Contiene | No contiene |
| --- | --- | --- |
| `StockFlow.Domain` | Entidades, value objects, reglas y errores de dominio | HTTP, EF Core, SQL, Redis, SDKs de proveedores |
| `StockFlow.Application` | Casos de uso, puertos, orquestación y DTOs de aplicación | Controladores, detalles del proveedor de base, implementaciones HTTP |
| `StockFlow.Infrastructure` | DbContext/configuraciones/migraciones EF Core, adaptadores SQL, Redis y clientes externos | Comportamiento de endpoints o políticas de negocio |
| `StockFlow.Api` | Controladores, validación, ProblemDetails, Swagger, registro de dependencias | Reglas de negocio de pedidos/stock o consultas SQL |

El código inicial incluye un flujo vertical pequeño de cotización: Controller → caso de uso de Application → abstracciones de caché/proveedor → Redis o Frankfurter. PostgreSQL está conectado a través de EF Core y `IDbConnectionFactory`; `StockFlowDbContextFactory` prepara el contexto para las herramientas de diseño y migración.

La primera entidad persistida es `Organization`, como raíz de tenant para las siguientes etapas. Su tabla `organizations` tiene `id` UUID, `name` (máximo 120), `slug` (máximo 80, único) y `created_at_utc` en `timestamp with time zone`. Domain normaliza/valida el nombre, el slug y el instante UTC; Infrastructure mapea nombres explícitos de tabla/columnas e índice único.

## Identidad y tenant actual

`StockFlowUser` vive en Infrastructure y extiende ASP.NET Core Identity. Las credenciales y el hash se guardan en `AspNetUsers`; Domain no conoce Identity ni almacena contraseñas. Domain contiene `OrganizationMembership` y `OrganizationRole`, con una fila única por usuario y organización. La membresía enlaza el usuario de Identity con `Organization` y persiste su rol (`Owner`, `Admin` o `Member`).

El registro crea usuario, organización y membresía inicial dentro de una transacción. Identity valida y genera el hash de contraseña. El login valida email y contraseña, y después busca la membresía para el slug solicitado; el cliente no puede elegir un rol ni obtener un token para una organización de la que no es miembro. Una verificación ficticia de contraseña mantiene trabajo criptográfico en credenciales inexistentes. Login permite cinco requests por minuto por IP; registro permite tres. No se bloquea una cuenta por contraseña incorrecta, porque una tercera persona podría usarlo para impedirle el acceso.

La API valida firma HMAC-SHA256, issuer, audience, expiración y claims de identidad/tenant del JWT. El token de acceso local dura 20 minutos. `ICurrentTenant` pertenece a Application y su implementación en API lee `sub`, `org_id` y `org_role` de los claims ya validados. Los controladores futuros deben tomar la organización desde este contexto. No aceptar un tenant del cuerpo o de un header como autoridad.

En esta etapa `/api/v1/auth/me` demuestra el tenant autenticado. Las entidades tenant-owned y sus filtros/consultas se agregan en Etapa 4; al añadir SQL manual, cada consulta debe incluir explícitamente `organization_id`. Los roles se validan al emitir el token; una membresía revocada deja de reflejarse cuando vence el token, por lo que los tokens locales son cortos.

## Datos y consistencia

- PostgreSQL es la fuente de verdad para productos, pedidos e inventario.
- Usa EF Core para lecturas y escrituras comunes. Usa la factoría Npgsql para practicar SQL explícito cuando una escritura condicional o una transacción aporte valor.
- Una reserva de stock debe actualizar el saldo, insertar el movimiento y pedido, y guardar el mensaje outbox en una sola transacción. Verifica las filas afectadas en los decrementos condicionales.
- Cachea lecturas del catálogo y cotizaciones con TTL explícito. Nunca uses stock cacheado para aprobar pedidos. Invalida la caché del catálogo después del commit.
- Mantén llamadas HTTP fuera de transacciones SQL. Si el flujo necesita una entrega externa confiable, guarda el evento outbox y procésalo en segundo plano.
- Cambia el esquema con migraciones. Aplícalas explícitamente durante la configuración o el despliegue; no al iniciar la API.

## Flujo inicial de una request

1. `CurrencyRatesController` valida la ruta HTTP y llama a `GetCurrencyRateHandler`.
2. El handler normaliza los códigos y consulta `ICacheStore`.
3. Ante un cache miss, llama a `IExchangeRateProvider`, implementado por el adaptador HTTP de Frankfurter.
4. El handler guarda el resultado en Redis durante 12 horas y devuelve un DTO de Application.
5. `GlobalExceptionHandler` devuelve `400` para pares no soportados, `502` ante fallos upstream y `504` ante timeout, sin exponer detalles internos.

La caché de la cotización puede fallar sin bloquear la respuesta: operaciones Redis tienen un límite de un segundo, se registran como warnings y el caso de uso continúa con Frankfurter. La cancelación del cliente sigue propagándose.

## Decisiones transversales

- Framework: `net10.0`; `global.json` fija la línea del SDK.
- Las versiones de paquetes se administran centralmente en `Directory.Packages.props`.
- Swagger UI se habilita en Development. Postman es la colección portátil de requests.
- La clave JWT local se guarda con User Secrets (`Authentication:Jwt:SigningKey`) y no se incluye en `appsettings.Development.json`. No uses este emisor local como proveedor de identidad de producción.
- La API local sirve HTTP para el ejercicio en Postman. Al desplegar, termina TLS en la API o en un proxy confiable y configura encabezados reenviados solo desde esos proxies antes de usar la IP del cliente para limitar login.
- PostgreSQL y Redis locales se ejecutan con `compose.yaml`.
- Las credenciales de contenedor son solo para desarrollo local. Usa variables de entorno o user secrets fuera de ese entorno.

## Referencias

- [Política de soporte de .NET](https://dotnet.microsoft.com/en-us/platform/support/policy)
- [Versiones y soporte de EF Core](https://learn.microsoft.com/en-us/ef/core/what-is-new/)
- [API Frankfurter](https://frankfurter.dev/)
