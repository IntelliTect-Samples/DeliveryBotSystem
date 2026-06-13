$agentPort = 7071
$agentProject = Resolve-Path (Join-Path $PSScriptRoot "..\..\..\AgentService\AgentService\AgentService.csproj")
$agentWorkingDir = Split-Path $agentProject -Parent

$agentRunning = Get-NetTCPConnection -LocalPort $agentPort -State Listen -ErrorAction SilentlyContinue

if (-not $agentRunning) {
  Start-Process powershell -ArgumentList @(
    "-NoProfile",
    "-Command",
    "Set-Location '$agentWorkingDir'; dotnet run"
  ) -WindowStyle Hidden
  Start-Sleep -Seconds 3
}

& npm run dev:web
