param(
    [Parameter(Mandatory = $true)]
    [string]$OutputPath,
    [string]$PrometheusEndpoint = $env:PROMETHEUS_ENDPOINT,
    [string]$AzureSubscriptionId = $env:AZURE_SUBSCRIPTION_ID,
    [decimal]$BudgetLimit = [decimal]::Parse(($env:FINOPS_BUDGET_LIMIT ?? '0'), [System.Globalization.CultureInfo]::InvariantCulture)
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not (Test-Path -Path $OutputPath)) {
    New-Item -Path $OutputPath -ItemType Directory -Force | Out-Null
}

$now = [DateTime]::UtcNow
$metrics = [ordered]@{
    generatedAt = $now.ToString('o')
    azureCost = $null
    prometheusUsage = @()
    notes = @()
}

if ($AzureSubscriptionId) {
    try {
        $costQuery = az costmanagement query --type Usage --timeframe MonthToDate --dataset-aggregation usageCost=sum --output json
        if ($costQuery) {
            $costPayload = $costQuery | ConvertFrom-Json
            $costValue = 0
            if ($costPayload.properties.rows.Length -gt 0) {
                $costValue = [decimal]$costPayload.properties.rows[0][0]
            }

            $metrics.azureCost = [ordered]@{
                subscriptionId = $AzureSubscriptionId
                monthToDate = [Math]::Round($costValue, 2)
                currency = $costPayload.properties.columns[0].name ?? 'USD'
            }
        }
    }
    catch {
        $metrics.notes += "Azure cost query failed: $($_.Exception.Message)"
    }
}
else {
    $metrics.notes += 'Azure subscription not configured; falling back to simulated consumption.'
    $metrics.azureCost = [ordered]@{
        subscriptionId = 'simulated'
        monthToDate = 1250.40
        currency = 'USD'
    }
}

if ($PrometheusEndpoint) {
    try {
        $query = "$PrometheusEndpoint/api/v1/query?query=sum(rate(container_cpu_usage_seconds_total[1h]))"
        $response = Invoke-WebRequest -UseBasicParsing -Uri $query -TimeoutSec 30
        $payload = $response.Content | ConvertFrom-Json
        $series = @()
        foreach ($result in $payload.data.result) {
            $series += [ordered]@{
                pod = $result.metric.pod
                value = [double]$result.value[1]
            }
        }
        $metrics.prometheusUsage = $series
    }
    catch {
        $metrics.notes += "Prometheus query failed: $($_.Exception.Message)"
    }
}
else {
    $metrics.notes += 'Prometheus endpoint not configured; using mocked utilisation.'
    $metrics.prometheusUsage = @(
        @{ pod = 'api'; value = 0.42 },
        @{ pod = 'workers'; value = 0.31 },
        @{ pod = 'frontend'; value = 0.24 }
    )
}

$alerts = @()
if ($metrics.azureCost -and $BudgetLimit -gt 0 -and $metrics.azureCost.monthToDate -ge $BudgetLimit) {
    $alerts += [ordered]@{
        type = 'budget'
        severity = 'warning'
        message = "Monthly spend $($metrics.azureCost.monthToDate) exceeds limit $BudgetLimit"
        triggeredAt = $now.ToString('o')
    }
}

$metricsPath = Join-Path $OutputPath 'metrics.json'
$alertsPath = Join-Path $OutputPath 'alerts.json'

$metrics | ConvertTo-Json -Depth 5 | Out-File -FilePath $metricsPath -Encoding UTF8
$alerts | ConvertTo-Json -Depth 5 | Out-File -FilePath $alertsPath -Encoding UTF8

Write-Host "FinOps metrics saved to $OutputPath"
