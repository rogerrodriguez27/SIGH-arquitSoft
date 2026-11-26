<#
.SYNOPSIS
  EQ-03 (disponibilidad, AC03): el contenedor de la API se recupera solo en 60 segundos o menos
  después de una caída inesperada (T119).

.DESCRIPTION
  Con el sistema levantado por docker compose, termina el proceso de la API dentro de su contenedor
  (simula una caída) y mide cuánto tarda /salud en volver a responder 200 gracias a "restart: always"
  y al healthcheck de docker-compose.yml. Repite la medición varias veces.

.EXAMPLE
  ./eq03-reinicio-api.ps1 -Proyecto tecnologia -Repeticiones 3
#>
param(
    [string]$Proyecto = 'tecnologia',                 # nombre del proyecto de docker compose (carpeta por defecto)
    [string]$UrlSalud = 'http://localhost:8080/salud',
    [int]$Repeticiones = 3,
    [int]$LimiteSegundos = 60
)

$ErrorActionPreference = 'Stop'

function Test-Salud {
    try {
        $respuesta = Invoke-WebRequest -Uri $UrlSalud -UseBasicParsing -TimeoutSec 2
        return $respuesta.StatusCode -eq 200
    } catch {
        return $false
    }
}

$contenedor = (docker compose -p $Proyecto ps -q api).Trim()
if (-not $contenedor) { throw "No hay un contenedor 'api' en el proyecto '$Proyecto'." }
if (-not (Test-Salud)) { throw "La API no responde en $UrlSalud antes de la prueba." }

$resultados = @()
for ($i = 1; $i -le $Repeticiones; $i++) {
    $reiniciosAntes = [int](docker inspect -f '{{.RestartCount}}' $contenedor)

    # Caída inesperada: se termina el proceso principal (PID 1) de la API.
    docker exec $contenedor kill 1 | Out-Null
    $inicio = Get-Date

    # Espera a que la API deje de responder (máximo 10 s) y luego a que vuelva.
    while ((Test-Salud) -and ((Get-Date) - $inicio).TotalSeconds -lt 10) { Start-Sleep -Milliseconds 200 }
    $cayo = -not (Test-Salud)
    while (-not (Test-Salud) -and ((Get-Date) - $inicio).TotalSeconds -lt 180) { Start-Sleep -Milliseconds 500 }

    $segundos = [math]::Round(((Get-Date) - $inicio).TotalSeconds, 1)
    $reiniciosDespues = [int](docker inspect -f '{{.RestartCount}}' $contenedor)
    $resultados += [pscustomobject]@{
        Intento          = $i
        DejoDeResponder  = $cayo
        ReinicioDocker   = ($reiniciosDespues -gt $reiniciosAntes)
        SegundosHastaOk  = $segundos
        Cumple           = ($segundos -le $LimiteSegundos)
    }
    Start-Sleep -Seconds 5
}

$resultados | Format-Table -AutoSize
$maximo = ($resultados | Measure-Object -Property SegundosHastaOk -Maximum).Maximum
Write-Output "EQ-03: recuperación máxima = $maximo s (límite $LimiteSegundos s)"
if ($resultados | Where-Object { -not $_.Cumple -or -not $_.ReinicioDocker }) {
    Write-Output 'EQ-03: NO CUMPLE'
    exit 1
}
Write-Output 'EQ-03: CUMPLE'
