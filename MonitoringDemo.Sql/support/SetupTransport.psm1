#Requires -RunAsAdministrator

Import-Module ./support/SqlTransport.psm1
Import-Module ./support/Utils.psm1

Function Install-SqlTransport {
    param (
        $instanceName = "particular-monitoring",
        $databaseName = "ParticularMonitoringDemo"
    )


    Write-Host "Adding LocalDb instance"
    $serverName = Add-LocalDbInstance -instanceName $instanceName

    Write-Host "Creating Database"
    New-Database -instanceName $instanceName -databaseName $databaseName

    Write-Host "$serverName"

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

    Write-Host "Updating connection strings"
    Update-ConnectionStrings -ConnectionString $connectionString -ConfigFile "$($PSScriptRoot)\..\Platform\servicecontrol\monitoring-instance\ServiceControl.Monitoring.exe.config"
    Update-ConnectionStrings -ConnectionString $connectionString -ConfigFile "$($PSScriptRoot)\..\Platform\servicecontrol\servicecontrol-instance\bin\ServiceControl.exe.config"
    
    Update-ConnectionStrings -ConnectionString $connectionString -ConfigFile "$($PSScriptRoot)\..\Solution\binaries\ClientUI\net461\ClientUI.exe.config"
    Update-ConnectionStrings -ConnectionString $connectionString -ConfigFile "$($PSScriptRoot)\..\Solution\binaries\Sales\net461\Sales.exe.config"
    Update-ConnectionStrings -ConnectionString $connectionString -ConfigFile "$($PSScriptRoot)\..\Solution\binaries\Billing\net461\Billing.exe.config"
    Update-ConnectionStrings -ConnectionString $connectionString -ConfigFile "$($PSScriptRoot)\..\Solution\binaries\Shipping\net461\Shipping.exe.config"
}