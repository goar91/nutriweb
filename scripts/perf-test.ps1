param(
    [string]$BaseUrl = "http://localhost:5000",
    [string]$Endpoint = "/api/nutrition/status",
    [int]$Requests = 100,
    [int]$Warmup = 5,
    [string]$Token
)

$ErrorActionPreference = "Stop"

function Get-Percentile {
    param(
        [double[]]$Values,
        [double]$Percent
    )

    if (-not $Values -or $Values.Count -eq 0) {
        return 0
    }

    $index = [int][Math]::Ceiling($Values.Count * $Percent) - 1
    if ($index -lt 0) { $index = 0 }
    if ($index -ge $Values.Count) { $index = $Values.Count - 1 }
    return $Values[$index]
}

$base = $BaseUrl.TrimEnd("/")
$uri = "$base$Endpoint"

$headers = @{}
if ($Token) {
    $headers["Authorization"] = "Bearer $Token"
}

Write-Host "=== NutriWeb Performance Test ==="
Write-Host "URL: $uri"
Write-Host "Requests: $Requests (warmup $Warmup)"

for ($i = 0; $i -lt $Warmup; $i++) {
    Invoke-WebRequest -UseBasicParsing -Uri $uri -Headers $headers -TimeoutSec 10 | Out-Null
}

$times = New-Object System.Collections.Generic.List[double]
$total = [System.Diagnostics.Stopwatch]::StartNew()

for ($i = 0; $i -lt $Requests; $i++) {
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    Invoke-WebRequest -UseBasicParsing -Uri $uri -Headers $headers -TimeoutSec 10 | Out-Null
    $sw.Stop()
    $times.Add($sw.Elapsed.TotalMilliseconds)
}

$total.Stop()

$sorted = $times.ToArray()
[Array]::Sort($sorted)

$avg = ($times | Measure-Object -Average).Average
$p50 = Get-Percentile -Values $sorted -Percent 0.50
$p95 = Get-Percentile -Values $sorted -Percent 0.95
$max = ($times | Measure-Object -Maximum).Maximum
$rps = if ($total.Elapsed.TotalSeconds -gt 0) { [Math]::Round($Requests / $total.Elapsed.TotalSeconds, 2) } else { 0 }

Write-Host ""
Write-Host "Resultados:"
Write-Host ("  Promedio (ms): {0:N2}" -f $avg)
Write-Host ("  P50 (ms):      {0:N2}" -f $p50)
Write-Host ("  P95 (ms):      {0:N2}" -f $p95)
Write-Host ("  Max (ms):      {0:N2}" -f $max)
Write-Host ("  Req/seg:       {0:N2}" -f $rps)
