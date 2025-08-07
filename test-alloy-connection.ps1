# Test Alloy Loki HTTP endpoint connection directly using config.json
$config = Get-Content -Raw -Path "config.json" | ConvertFrom-Json
$url = $config.lokiAlloyUrl + "/loki/api/v1/push"

# Create a simple test log entry with correct timestamp (compatible with PowerShell 5.1)
$epoch = Get-Date "1970-01-01 00:00:00Z"
$timestamp = [long]((Get-Date).ToUniversalTime() - $epoch).TotalMilliseconds * 1000000
$testLog = @{
    streams = @(
        @{
            stream = @{
                app = "test-to-alloy"
                env = "debug"
                source = "powershell-test"
            }
            values = ,@($timestamp.ToString(), "Test log entry to Alloy - $(Get-Date)")
        }
    )
} | ConvertTo-Json -Depth 10

Write-Host "Testing connection to Alloy..."
Write-Host "URL: $url"
Write-Host "Timestamp: $timestamp"
Write-Host "Test log: $testLog"

try {
    $headers = @{
        "Content-Type" = "application/json"
    }
    
    $response = Invoke-RestMethod -Uri $url -Method Post -Body $testLog -Headers $headers
    
    Write-Host "SUCCESS: Log sent to Alloy!" -ForegroundColor Green
    Write-Host "Response: $response"
} catch {
    Write-Host "ERROR: Failed to send log to Alloy" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)"
    if ($_.Exception.Response) {
        Write-Host "Status Code: $($_.Exception.Response.StatusCode)"
        Write-Host "Status Description: $($_.Exception.Response.StatusDescription)"
        
        # Try to get response body for more details
        try {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $responseBody = $reader.ReadToEnd()
            Write-Host "Response Body: $responseBody"
        } catch {
            Write-Host "Could not read response body"
        }
    }
} 