function CreateQueue {
    param (
        [Parameter(Mandatory=$true)]
        [ValidateNotNull()]
        [System.Data.SqlClient.SqlConnection] $connection,

        [ValidateNotNullOrEmpty()]
        [string] $schema = "dbo",

        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string] $queueName
    )

    $sql = @"
    if not  exists (select * from sys.objects where object_id = object_id(N'[{0}].[{1}]') and type in (N'U'))
        begin
        create table [{0}].[{1}](
            [Id] [uniqueidentifier] not null,
            [CorrelationId] [varchar](255),
            [ReplyToAddress] [varchar](255),
            [Recoverable] [bit] not null,
            [Expires] [datetime],
            [Headers] [nvarchar](max) not null,
            [Body] [varbinary](max),
            [RowVersion] [bigint] identity(1,1) not null
        );
        create clustered index [Index_RowVersion] on [{0}].[{1}]
        (
            [RowVersion]
        )
        create nonclustered index [Index_Expires] on [{0}].[{1}]
        (
            [Expires]
        )
        include
        (
            [Id],
            [RowVersion]
        )
        where
            [Expires] is not null
    end
"@ -f $schema, $queueName

    $command = New-Object System.Data.SqlClient.SqlCommand($sql, $connection)
    $command.ExecuteNonQuery()
    $command.Dispose()

    Write-Host "$schema.$queueName queue created."
}

function CreateDelayedQueue {
    param (
        [Parameter(Mandatory=$true)]
        [ValidateNotNull()]
        [System.Data.SqlClient.SqlConnection] $connection,

        [ValidateNotNullOrEmpty()]
        [string] $schema = "dbo",

        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string] $queueName
    )

    $sql = @"
    if not  exists (select * from sys.objects where object_id = object_id(N'[{0}].[{1}]') and type in (N'U'))
        begin
        create table [{0}].[{1}](
            [Headers] nvarchar(max) not null,
            [Body] varbinary(max),
            [Due] datetime not null,
            [RowVersion] bigint identity(1,1) not null
        );

        create nonclustered index [Index_Due] on [{0}].[{1}]
        (
            [Due]
        )
    end
"@ -f $schema, $queueName

    $command = New-Object System.Data.SqlClient.SqlCommand($sql, $connection)
    $command.ExecuteNonQuery()
    $command.Dispose()
}

Function CreateQueuesForEndpoint
{
    param(
        [Parameter(Mandatory=$true)]
        [ValidateNotNull()]
        [string] $connection,

        [ValidateNotNullOrEmpty()]
        [string] $schema = "dbo",

        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string] $endpointName,

        [Parameter(HelpMessage="Only required for NSB Versions 5 and below")]
        [Switch] $includeRetries,

        [Parameter(HelpMessage="Only required for SQL Server Version 3.1 and above if native delayed delivery is enabled")]
        [Switch] $includeDelayed
    )

    $sqlConnection = New-Object System.Data.SqlClient.SqlConnection($connection)
    $sqlConnection.Open()

    try {
        # main queue
        CreateQueue -connection $sqlConnection -schema $schema -queuename $endpointName

        # timeout queue
        CreateQueue -connection $sqlConnection -schema $schema -queuename "$endpointName.timeouts"

        # timeout dispatcher queue
        CreateQueue -connection $sqlConnection -schema $schema -queuename "$endpointName.timeoutsdispatcher"

        # retries queue
        if ($includeRetries) {
            CreateQueue -connection $sqlConnection -schema $schema -queuename "$endpointName.retries"
        }

        # retries queue
        if ($includeDelayed) {
            CreateDelayedQueue -connection $sqlConnection -schema $schema -queuename "$endpointName.delayed"
        }
    }
    finally {
        $sqlConnection.Close()
        $sqlConnection.Dispose()
    }
}

Function Set-SqlTransport {
    param (
        [string]$connectionString
    )

    Write-Host "Creating endpoint queues"
    $sqlConnection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $sqlConnection.Open()

    try {
        CreateQueue -connection $sqlConnection -queueName "audit"
        CreateQueue -connection $sqlConnection -queueName "error"

        # NOTE: ServiceControl requires this queue to exist but does not use it for anything
        CreateQueue -connection $sqlConnection -queueName "Particular.ServiceControl.$env:computername"

        # NOTE: Required for ServiceControl Retries
        CreateQueue -connection $sqlConnection -queueName "Particular.ServiceControl.staging"
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