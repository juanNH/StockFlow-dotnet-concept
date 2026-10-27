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

2. Restaurar y ejecutar la API:

   ```powershell
   dotnet restore
   dotnet run --project src/StockFlow.Api
   ```

3. Abrir Swagger en `http://localhost:5080/swagger` o importar `postman/StockFlow.postman_collection.json` en Postman. La base incluye un endpoint de estado y otro de cotización que consulta Frankfurter y guarda el resultado en Redis.

Los valores de `appsettings.Development.json` y `compose.yaml` son credenciales locales de ejemplo. Para otros entornos, usa secretos de usuario o variables de entorno y no subas credenciales reales al repositorio.

## Migraciones de base de datos

Con PostgreSQL activo, restaura la herramienta local y genera/aplica migraciones desde la raíz:

```powershell
dotnet tool restore
dotnet ef migrations add NombreDeLaMigracion --project src/StockFlow.Infrastructure --startup-project src/StockFlow.Api --context StockFlowDbContext --output-dir Persistence/Migrations
dotnet ef database update --project src/StockFlow.Infrastructure --startup-project src/StockFlow.Api --context StockFlowDbContext
```

La factoría de diseño usa `ConnectionStrings__Postgres` si está definida; en local tiene el mismo fallback de desarrollo que Compose. La API no aplica migraciones automáticamente al arrancar.

## Arquitectura y plan

- [Arquitectura](docs/architecture.md)
- [Plan de desarrollo: 11 etapas](docs/development-plan.md)
- [Reglas de contribución](AGENTS.md)

Las skills de Codex específicas del repositorio están en `.agents/skills/`.
