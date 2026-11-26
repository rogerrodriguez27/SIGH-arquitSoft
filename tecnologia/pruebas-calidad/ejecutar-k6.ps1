<#
.SYNOPSIS
  Ejecuta una prueba k6 de atributos de calidad contra el sistema levantado con docker compose (T118, T120).

.DESCRIPTION
  Usa la imagen oficial grafana/k6 (no hace falta instalar k6). Guarda la salida de texto y el resumen
  JSON en docs/05-validacion-y-evidencias/resultados/. Del JSON se elimina "setup_data", que contiene
  los tokens y las contraseñas de los usuarios ficticios de la prueba: ninguna credencial debe quedar
  en el repositorio (principio VI, EQ-11).

  Las credenciales del administrador se piden en tiempo de ejecución o se leen de variables de entorno;
  nunca se escriben en el código. El administrador no debe tener el cambio de contraseña pendiente.

.EXAMPLE
  $env:ADMIN_USUARIO = 'admin'; $env:ADMIN_CONTRASENA = '<contraseña>'
  ./ejecutar-k6.ps1 -Prueba eq01-rendimiento
#>
param(
    [Parameter(Mandatory)]
    [ValidateSet('eq01-rendimiento', 'eq02-escalabilidad', 'eq04-seguridad')]
    [string]$Prueba,
    [string]$Api = 'http://host.docker.internal:8080'
)

$ErrorActionPreference = 'Stop'
$scripts = Join-Path $PSScriptRoot 'k6'
$resultados = (Resolve-Path (Join-Path $PSScriptRoot '..\..\docs\05-validacion-y-evidencias\resultados')).Path

$usuario = if ($env:ADMIN_USUARIO) { $env:ADMIN_USUARIO } else { Read-Host 'Usuario administrador' }
$contrasena = if ($env:ADMIN_CONTRASENA) { $env:ADMIN_CONTRASENA } else {
    [Runtime.InteropServices.Marshal]::PtrToStringAuto(
        [Runtime.InteropServices.Marshal]::SecureStringToBSTR((Read-Host 'Contraseña' -AsSecureString)))
}

$env:ADMIN_USUARIO = $usuario
$env:ADMIN_CONTRASENA = $contrasena
try {
    # k6 escribe sus mensajes en stderr: en PowerShell 5.1 eso no debe detener el script.
    $ErrorActionPreference = 'Continue'
    $salida = docker run --rm `
        -v "${scripts}:/scripts" -v "${resultados}:/resultados" `
        -e "API=$Api" -e ADMIN_USUARIO -e ADMIN_CONTRASENA `
        grafana/k6:latest run --quiet --no-color --summary-export "/resultados/$Prueba.json" "/scripts/$Prueba.js" 2>&1 |
        ForEach-Object { "$_" } | Out-String
    $codigo = $LASTEXITCODE
    $ErrorActionPreference = 'Stop'
    $salida
    [IO.File]::WriteAllText((Join-Path $resultados "$Prueba.txt"), $salida, (New-Object Text.UTF8Encoding($false)))
} finally {
    Remove-Item Env:ADMIN_CONTRASENA -ErrorAction SilentlyContinue
}

# Quita setup_data (tokens y contraseñas de los usuarios ficticios) del resumen JSON.
$rutaJson = Join-Path $resultados "$Prueba.json"
if (Test-Path $rutaJson) {
    $resumen = Get-Content $rutaJson -Raw -Encoding UTF8 | ConvertFrom-Json
    $resumen.PSObject.Properties.Remove('setup_data')
    $resumen | ConvertTo-Json -Depth 100 | Set-Content $rutaJson -Encoding UTF8
}

Write-Output "k6 terminó con código $codigo (0 = todos los umbrales cumplidos)."
exit $codigo
