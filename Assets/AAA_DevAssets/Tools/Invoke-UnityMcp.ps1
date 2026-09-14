param(
    [Parameter(Mandatory = $true)][string]$Method,
    [string]$ParamsJson = '{}',
    [string]$SessionId
)
$ErrorActionPreference = 'Stop'
$request = @{ jsonrpc = '2.0'; method = $Method; params = ($ParamsJson | ConvertFrom-Json) }
if (-not $Method.StartsWith('notifications/')) { $request.id = 1 }
$headers = @{ Accept = 'application/json, text/event-stream' }
if ($SessionId) { $headers['mcp-session-id'] = $SessionId }
$payload = [Text.Encoding]::UTF8.GetBytes(($request | ConvertTo-Json -Depth 80 -Compress))
$response = Invoke-WebRequest -Uri 'http://127.0.0.1:8080/mcp' -Method Post -ContentType 'application/json' -Headers $headers -Body $payload -TimeoutSec 60
$content = [string]$response.Content
if ([string]::IsNullOrWhiteSpace($content)) { '{"accepted":true}'; return }
if ($content.TrimStart().StartsWith('{')) {
    $message = $content | ConvertFrom-Json
} else {
    $dataLine = @($content -split '\r?\n' | Where-Object { $_.StartsWith('data: ') })[-1]
    if (-not $dataLine) { throw 'The MCP response contained no JSON data.' }
    $message = $dataLine.Substring(6) | ConvertFrom-Json
}
if ($message.error) { throw ($message.error | ConvertTo-Json -Depth 20 -Compress) }
if ($Method -eq 'initialize') {
    @{ sessionId = ($response.Headers['mcp-session-id'] -join ''); result = $message.result } | ConvertTo-Json -Depth 80 -Compress
} else {
    $message.result | ConvertTo-Json -Depth 80 -Compress
}
