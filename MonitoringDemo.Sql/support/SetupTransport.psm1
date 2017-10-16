#Requires -RunAsAdministrator

Import-Module ./support/SqlTransport.psm1
Import-Module ./support/Utils.psm1

Function Install-SqlTransport {
    param (
        $localDbMsiPath = ".\support\SqlLocalDB.msi",
        $instanceName = "particular-monitoring",
        $databaseName = "ParticularMonitoringDemo"
    )

    $serverName = "(localdb)\" + $instanceName

    Write-Host "Installing LocalDb"
    Install-Msi (Get-Item $localDbMsiPath)

    Write-Host "Adding LocalDb instance"
    Add-LocalDbInstance  $instanceName

    Write-Host "Creating Database"
    New-Database -server $serverName -databaseName $databaseName

    Write-Host "Creating endpoint queues"
    $connectionString = New-ConnectionString -server $serverName -databaseName $databaseName
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

    CreateQueuesForEndpoint -connection $connectionString -endpointName "Particular.ServiceControl" -includeRetries
    CreateQueuesForEndpoint -connection $connectionString -endpointName "Particular.Monitoring" -includeRetries
    CreateQueuesForEndpoint -connection $connectionString -endpointName "ClientUI" -includeRetries
    CreateQueuesForEndpoint -connection $connectionString -endpointName "Sales" -includeRetries
    CreateQueuesForEndpoint -connection $connectionString -endpointName "Billing" -includeRetries
    CreateQueuesForEndpoint -connection $connectionString -endpointName "Shipping" -includeRetries
}