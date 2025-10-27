# Observabilidad del Gestor de Inventario

El servicio API expone métricas y trazas usando OpenTelemetry, con exportadores para OTLP, consola y Prometheus. Esta carpeta incluye ejemplos de dashboards para Application Insights (Azure Monitor) y Grafana.

## Azure Monitor / Application Insights

Importa el archivo `application-insights-workbook.json` como workbook en tu recurso de Application Insights. El workbook contiene:

- Visor de trazas con duración media de peticiones y consultas de base de datos.
- Métricas de errores por endpoint y códigos de respuesta.
- Uso de recursos (CPU, memoria) agregados por instancia.

## Grafana con Prometheus

El archivo `grafana-dashboard.json` define un dashboard con:

- Tasa de peticiones HTTP por ruta y código de estado.
- Latencia p95/p99 de la API.
- Tiempo de consulta de Entity Framework Core.
- Métricas de uso de CPU/memoria del proceso .NET y contenedor.

## Endpoints expuestos

- `/metrics`: endpoint de Prometheus (habilitado cuando `Observability:Prometheus:Enabled` es `true`).
- Exportador OTLP configurable mediante `Observability:Otlp`.

Revisa `appsettings.json` para ajustar los exportadores según el entorno.
