# Directrices para agentes

- Sigue una arquitectura en capas basada en Clean Architecture con los proyectos `Domain`, `Application`, `Infrastructure` y `Presentation`.
- Usa .NET 8 y C# 12 para cualquier código backend.
- Habilita `nullable` y aplica convenciones de nombres en PascalCase para clases y métodos, camelCase para parámetros y propiedades.
- Implementa entidades y configuraciones de EF Core alineadas con el esquema descrito en `docs/documentacion.md`.
- Registra los servicios en clases de extensión `DependencyInjection` por capa.
- El código nuevo debe acompañarse de pruebas o, en su defecto, justificar claramente la ausencia en la descripción del PR.
- Ejecuta `dotnet build` antes de finalizar los cambios para garantizar que la solución compila.
