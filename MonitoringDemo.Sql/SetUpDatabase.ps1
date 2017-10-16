Param(
    [string]$server = ".\SQLEXPRESS",
    [string]$databaseName = "ParticularMonitoringDemo",
    [string]$user,
    [string]$password,
    [bool]$defaultCredentials = $true
)

Import-Module ./support/SqlTransport.psm1
Import-Module ./support/Utils.psm1

Write-Host "Creating Database"
New-Database -server $server -databaseName $databaseName -user $user -password $password -defaultCredentials $defaultCredentials

Write-Host "Creating endpoint queues"
$connectionString = New-ConnectionString -server $server -databaseName $databaseName -user $user -password $password -defaultCredentials $defaultCredentials
$sqlConnection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$sqlConnection.Open()

try {
    CreateQueue -connection $sqlConnection -queueName "audit"
    CreateQueue -connection $sqlConnection -queueName "error"

    # NOTE: ServiceControl requires this queue to exist but does not use it for anything
    CreateQueue -connection $sqlConnection -queueName "Particular.ServiceControl.$env:computername"
} finally {
    $sqlConnection.Close()
    $sqlConnection.Dispose()
}

CreateQueuesForEndpoint -connection $connectionString -endpointName "Particular.ServiceControl"
CreateQueuesForEndpoint -connection $connectionString -endpointName "Particular.Monitoring" -includeRetries
CreateQueuesForEndpoint -connection $connectionString -endpointName "ClientUI"
CreateQueuesForEndpoint -connection $connectionString -endpointName "Sales"
CreateQueuesForEndpoint -connection $connectionString -endpointName "Billing"
CreateQueuesForEndpoint -connection $connectionString -endpointName "Shipping"

Write-Host "Updating connection strings"
Update-ConnectionStrings -ConnectionString $connectionString -ConfigFile "$($PSScriptRoot)\Platform\servicecontrol\monitoring-instance\ServiceControl.Monitoring.exe.config"
Update-ConnectionStrings -ConnectionString $connectionString -ConfigFile "$($PSScriptRoot)\Platform\servicecontrol\servicecontrol-instance\bin\ServiceControl.exe.config"
Update-ConnectionStrings -ConnectionString $connectionString -ConfigFile "$($PSScriptRoot)\Solution\ClientUI\App.config"
Update-ConnectionStrings -ConnectionString $connectionString -ConfigFile "$($PSScriptRoot)\Solution\Sales\App.config"
Update-ConnectionStrings -ConnectionString $connectionString -ConfigFile "$($PSScriptRoot)\Solution\Billing\App.config"
Update-ConnectionStrings -ConnectionString $connectionString -ConfigFile "$($PSScriptRoot)\Solution\Shipping\App.config"