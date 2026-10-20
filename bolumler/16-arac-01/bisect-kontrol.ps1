# git bisect run için yardımcı (Windows PowerShell).
# Bisect sırasında bu dosya da değişebileceği için önce depo dışına kopyalayın:
#   Copy-Item bolumler\16-arac-01\bisect-kontrol.ps1 $env:TEMP\
#   git bisect start bolum/16-arac-01 main
#   git bisect run powershell -NoProfile -ExecutionPolicy Bypass -File $env:TEMP\bisect-kontrol.ps1
#
# Çıkış kodu: 0 = iyi, 1 = kötü, 125 = bu commit test edilemiyor (bisect atlar).
$ErrorActionPreference = 'Continue'
$root = (git rev-parse --show-toplevel).Trim()
$log = Join-Path $env:TEMP 'tezgah-bisect-api.log'

function Stop-Api { Get-Process Tezgah.Api -ErrorAction SilentlyContinue | Stop-Process -Force }

Stop-Api
Push-Location (Join-Path $root 'backend')
dotnet build src/Tezgah.Api -v q *> $null; if ($LASTEXITCODE -ne 0) { Pop-Location; exit 125 }
dotnet build tools/Tezgah.Bolum -v q *> $null; if ($LASTEXITCODE -ne 0) { Pop-Location; exit 125 }
$api = Start-Process dotnet -ArgumentList 'run','--no-build','--project','src/Tezgah.Api','--launch-profile','http' -RedirectStandardOutput $log -NoNewWindow -PassThru
Pop-Location

$up = $false
for ($i = 0; $i -lt 60 -and -not $up; $i++) {
  try { Invoke-WebRequest -UseBasicParsing http://localhost:5080/health -TimeoutSec 2 | Out-Null; $up = $true } catch { Start-Sleep -Seconds 1 }
}
if (-not $up) { Stop-Api; exit 125 }

Push-Location $root
dotnet run --no-build --project backend/tools/Tezgah.Bolum -- kontrol 16
$result = $LASTEXITCODE
Pop-Location
Stop-Api
if ($result -eq 0) { exit 0 }
if ($result -eq 1) { exit 1 }
exit 125
