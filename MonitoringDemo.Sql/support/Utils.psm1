[Reflection.Assembly]::LoadWithPartialName("Microsoft.SqlServer.Smo")

Function New-Database
{
    param(
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string] $server,

        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string] $databaseName
    )
    
    $srv = New-Object Microsoft.SqlServer.Management.Smo.Server($server)

    # Check can connect
    $srv.ConnectionContext.Connect()

    if ($srv.Databases[$databaseName] -ne $Null)
    {
        Write-Host "Dropping" + $databaseName
        $srv.KillAllProcesses($databaseName)
        $srv.KillDatabase($databaseName)
    }

    $db = New-Object Microsoft.SqlServer.Management.Smo.Database( $srv, $databaseName )
    
    # TODO: Do we need to explicitly set credentials and create the schema? 
    $db.Create()
}

Function New-ConnectionString {
    param(
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string] $server,

        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string] $databaseName
    )

    $connectionString = "server=$($server);Database=$($databaseName);Integrated Security=SSPI;"

    return $connectionString
}

Function Install-Msi {
    param (
        [System.IO.FileInfo]$file
    )
    $DataStamp = get-date -Format yyyyMMddTHHmmss
    $logFile = '{0}-{1}.log' -f $file.fullname,$DataStamp
    $MSIArguments = @(
        "/i"
        ('"{0}"' -f $file.fullname)
        "/qn"
        "/norestart"
        "/L*v"
        $logFile
    )
    
    Start-Process "msiexec.exe" -ArgumentList $MSIArguments -Wait -NoNewWindow 
}

Function Add-LocalDbInstance {
    param (
        [string]$instanceName
    )
    
    sqllocaldb create $instanceName
    sqllocaldb share $instanceName $instanceName
    sqllocaldb start $instanceName
    sqllocaldb info $instanceName
}