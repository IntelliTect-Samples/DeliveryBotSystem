param(
    [string]$ResourceGroup = "ewu-deliverybotsystem-rg",
    [string]$AccountName = "deliverybot-rbnr-dev-rbnr-mtgpw6",
    [string]$DatabaseName = "bot-network",
    [string]$ContainerName = "bots",
    [string]$BotId = "readbots-function-startup-probe"
)

$key = az cosmosdb keys list `
    --resource-group $ResourceGroup `
    --name $AccountName `
    --query primaryMasterKey `
    -o tsv

if (-not $key) {
    Write-Output '{"found":false,"error":"KEY_LOOKUP_FAILED"}'
    exit 1
}

$resourceLink = "dbs/$DatabaseName/colls/$ContainerName/docs/$BotId"
$date = [DateTime]::UtcNow.ToString('r').ToLowerInvariant()
$payload = "get`ndocs`n$resourceLink`n$date`n`n"

$hmac = New-Object System.Security.Cryptography.HMACSHA256
$hmac.Key = [Convert]::FromBase64String($key)
$signature = [Convert]::ToBase64String($hmac.ComputeHash([Text.Encoding]::UTF8.GetBytes($payload)))
$auth = [System.Net.WebUtility]::UrlEncode("type=master&ver=1.0&sig=$signature")

$headers = @{
    "authorization"                  = $auth
    "x-ms-date"                      = $date
    "x-ms-version"                   = "2018-12-31"
    "x-ms-documentdb-partitionkey"   = "[`"$BotId`"]"
}

$uri = "https://$AccountName.documents.azure.com/$resourceLink"

try {
    $doc = Invoke-RestMethod -Method Get -Uri $uri -Headers $headers
    [pscustomobject]@{
        found        = $true
        id           = $doc.id
        botId        = $doc.botId
        status       = $doc.status
        isRemoved    = $doc.isRemoved
        updatedAtUtc = $doc.updatedAtUtc
    } | ConvertTo-Json -Compress
}
catch {
    [pscustomobject]@{
        found      = $false
        statusCode = $_.Exception.Response.StatusCode.value__
    } | ConvertTo-Json -Compress
}
