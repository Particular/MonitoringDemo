# Run SQL on Docker
Write-Host "Starting SQL Server"
& docker-compose.exe up -d --force-recreate
# Wait for SQL Server to start
Write-Host "Waiting for SQL to Start"
Start-Sleep -s 45
# Create Database and update connection strings
Write-Host "Setting up schema"
$params = @{server=".";user="SA";password="Passw0rd";defaultCredentials=$false}
& "./SetUpDatabase.ps1" @params

$message = "SQL Running. Press to close"

if ($psISE)
{
    Add-Type -AssemblyName System.Windows.Forms
    [System.Windows.Forms.MessageBox]::Show("$message")
}
else
{
    Write-Host "$message" -ForegroundColor Yellow
    $x = $host.ui.RawUI.ReadKey("NoEcho,IncludeKeyDown")
}

Write-Host "Shutting SQL Down"
& docker-compose.exe stop
