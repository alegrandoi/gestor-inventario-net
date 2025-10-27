# Cumplimiento normativo y recuperación ante desastres

Este documento resume las políticas de cumplimiento implantadas en Gestor Inventario y enlaza con los controles técnicos incorporados en la base de código.

## Marcos de referencia

### ISO/IEC 27001
- **Controles A.5 y A.6**: se definen roles y responsabilidades dentro del módulo de administración; los informes legales generan firmas criptográficas (`X-Audit-Report-Signature`).
- **Control A.8 Gestión de activos**: el registro de políticas en `DataGovernancePolicyRegistry` clasifica datasets críticos (PII, financieros, operativos) y documenta controles exigidos (cifrado, acceso mínimo, almacenamiento inmutable).
- **Control A.12 Operación de seguridad**: los workers aplican sanitización y trazabilidad antes de registrar eventos, reduciendo el riesgo de fuga en canales operativos.

### GDPR y LOPD
- **Derecho al olvido (Art. 17 GDPR)**: el `DataGovernancePolicyEnforcer` anonimiza clientes inactivos una vez superados los periodos de retención establecidos.
- **Limitación de conservación (Art. 5(1)(e) GDPR)**: se purgan logs y transacciones de inventario de acuerdo con los periodos definidos.
- **Responsabilidad proactiva (LOPDGDD Art. 28)**: los exports legales (`SAF-T`, `SIN`) se generan bajo demanda, con hash criptográfico y trazabilidad del solicitante.

## Procesos operativos

1. **Catálogo de datos**: actualizado en `src/Infrastructure/GestorInventario.Infrastructure/DataGovernance/DataGovernancePolicyRegistry.cs`. Cualquier nuevo repositorio debe declararse aquí con clasificación y retención.
2. **Aplicación de políticas**: `DataGovernancePolicyEnforcer` se ejecuta en cada `SaveChangesAsync`, garantizando eliminación/sanitización automática.
3. **Sincronización segura**: los workers utilizan la clasificación para enmascarar datos sensibles antes de generar logs operativos o eventos.
4. **Exportaciones certificables**: el endpoint `/api/auditlogs/exports/{format}` genera ficheros firmados y registra la jurisdicción del informe.

## FinOps y gobierno de costes

- El workflow `FinOps Governance` (GitHub Actions) ejecuta `eng/finops/collect-metrics.ps1` para consolidar costes de Azure y consumo Prometheus.
- Los artefactos `metrics.json` y `alerts.json` funcionan como **data mart** de FinOps y alimentan cuadros de mando externos.
- El script admite simulaciones cuando no hay credenciales, facilitando pruebas en entornos de staging.

## Recuperación ante desastres (DR)

1. **RPO**: 24 horas. Las copias de seguridad deben incluir bases de datos operacionales y el artefacto FinOps más reciente.
2. **RTO**: 4 horas. Las restauraciones siguen el runbook: desplegar infraestructura IaC, restaurar base de datos, rehidratar colas de mensajería.
3. **Pruebas**: los escenarios de DR deben ejercitarse al menos dos veces al año y documentarse en el repositorio (subcarpeta `docs/dr-tests/`).
4. **Comunicación**: el módulo de administración expone los informes certificados para autoridades fiscales/legales; las firmas se verifican comparando la cabecera `X-Audit-Report-Signature` con el hash del fichero descargado.

## Automatización de cumplimiento

- **Secret scanning**: `ci.yml` ejecuta Gitleaks para detectar credenciales comprometidas antes de fusionar cambios.
- **Políticas IaC**: Checkov analiza plantillas de infraestructura (Terraform, Bicep, YAML) y bloquea configuraciones inseguras.
- **Alertas FinOps**: cuando el gasto mensual supera el presupuesto definido (`FINOPS_BUDGET_LIMIT`) se registra una alerta en `alerts.json` y se expone en el resumen del workflow.

> Para dudas o excepciones, contactar con el equipo de seguridad y cumplimiento (`security@gestorinventario.local`).
