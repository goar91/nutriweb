param(
    [string]$BaseUrl = "http://localhost:5000",
    [string]$Username = "admin",
    [string]$Password = "admin",
    [switch]$Shutdown
)

$ErrorActionPreference = "Stop"

function Invoke-Api {
    param(
        [string]$Method,
        [string]$Url,
        [object]$Body,
        [string]$Token
    )

    $headers = @{}
    if ($Token) {
        $headers["Authorization"] = "Bearer $Token"
    }

    if ($null -ne $Body) {
        $json = $Body | ConvertTo-Json -Depth 10
        return Invoke-RestMethod -Method $Method -Uri $Url -Headers $headers -Body $json -ContentType "application/json" -TimeoutSec 20
    }

    return Invoke-RestMethod -Method $Method -Uri $Url -Headers $headers -TimeoutSec 20
}

Write-Host "=== NutriWeb Smoke Test ==="
$base = $BaseUrl.TrimEnd("/")

Write-Host "[1/7] Login..."
$login = Invoke-Api -Method "POST" -Url "$base/api/auth/login" -Body @{ username = $Username; password = $Password } -Token $null
if (-not $login.success) {
    throw "Login failed"
}
$token = $login.token
Write-Host "  OK - token received"

Write-Host "[2/7] Status..."
$status = Invoke-Api -Method "GET" -Url "$base/api/nutrition/status" -Body $null -Token $token
if ($status.status -ne "running") {
    throw "Status endpoint failed"
}
Write-Host "  OK - status running"

Write-Host "[3/7] Create historia clinica..."
$cedula = "ST" + (Get-Date -Format "yyyyMMddHHmmss")
$historyPayload = @{
    personalData = @{
        numeroCedula = $cedula
        nombre = "Smoke Test"
        edadCronologica = "30"
        sexo = "M"
        lugarResidencia = "Test City"
        estadoCivil = "Soltero"
        telefono = "0990000000"
        ocupacion = "QA"
        email = "smoke-$cedula@example.com"
        fechaConsulta = (Get-Date).ToString("yyyy-MM-dd")
    }
    motivoConsulta = "Smoke test"
    diagnostico = "N/A"
    notasExtras = "Smoke test"
    antecedentes = @{
        apf = ""
        app = ""
        apq = ""
        ago = ""
        menarquia = ""
        p = ""
        g = ""
        c = ""
        a = ""
        alergias = ""
    }
    habitos = @{
        fuma = ""
        alcohol = ""
        cafe = ""
        hidratacion = ""
        gaseosas = ""
        actividadFisica = ""
        te = ""
        edulcorantes = ""
        alimentacion = ""
    }
    signosVitales = @{
        presionArterial = "120/80"
        frecuenciaCardiaca = "70"
        frecuenciaRespiratoria = "16"
        temperatura = "36.5"
    }
    datosAntropometricos = @{
        peso = "70"
        talla = "1.70"
        circunferenciaCintura = "80"
        circunferenciaCadera = "90"
        circunferenciaBrazo = "30"
        pantorrilla = "35"
    }
    valoresBioquimicos = @{
        glicemia = "90"
        colesterolTotal = "180"
        trigliceridos = "120"
    }
    recordatorio24h = @{
        desayuno = "Avena"
        snack1 = "Fruta"
        almuerzo = "Arroz"
        snack2 = "Yogurt"
        cena = "Ensalada"
        extras = "Agua"
    }
    frequency = @{}
}

$history = Invoke-Api -Method "POST" -Url "$base/api/nutrition/history" -Body $historyPayload -Token $token
$historiaId = $history.id
if (-not $historiaId) {
    throw "Historia clinica no creada"
}
Write-Host "  OK - historia $historiaId"

Write-Host "[4/7] List pacientes..."
$pacientes = Invoke-Api -Method "GET" -Url "$base/api/nutrition/pacientes" -Body $null -Token $token
$found = $pacientes | Where-Object { $_.numero_cedula -eq $cedula }
if (-not $found) {
    throw "Paciente no encontrado en listado"
}
Write-Host "  OK - paciente encontrado"

Write-Host "[5/7] Crear plan nutricional..."
$planPayload = @{
    historiaId = $historiaId
    fechaInicio = (Get-Date).ToString("yyyy-MM-dd")
    objetivo = "Smoke test"
    caloriasDiarias = 2000
    observaciones = "Smoke test"
    activo = $true
    alimentacionSemanal = @(
        @{
            semana = 1
            diaSemana = 1
            desayuno = "Avena"
            snackManana = "Fruta"
            almuerzo = "Arroz"
            snackTarde = "Yogurt"
            cena = "Ensalada"
            snackNoche = "Nueces"
            observaciones = "OK"
        }
    )
}

$plan = Invoke-Api -Method "POST" -Url "$base/api/nutrition/planes" -Body $planPayload -Token $token
$planId = $plan.planId
if (-not $planId) {
    throw "Plan no creado"
}
Write-Host "  OK - plan $planId"

Write-Host "[6/7] Obtener planes..."
$planes = Invoke-Api -Method "GET" -Url "$base/api/nutrition/planes/$historiaId" -Body $null -Token $token
if (-not ($planes | Where-Object { $_.id -eq $planId })) {
    throw "Plan no aparece en listado"
}
Write-Host "  OK - plan listado"

Write-Host "[7/7] Logout..."
$logoutBuilder = [System.UriBuilder]::new($base)
$logoutBuilder.Path = "/api/auth/logout"
if ($Shutdown) {
    $logoutBuilder.Query = "shutdown=1"
}
$logoutUrl = $logoutBuilder.Uri.AbsoluteUri
Invoke-Api -Method "POST" -Url $logoutUrl -Body @{} -Token $token | Out-Null
Write-Host "  OK - logout"

Write-Host "Smoke test completo."
