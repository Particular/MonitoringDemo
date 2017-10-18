[Reflection.Assembly]::LoadWithPartialName("Microsoft.SqlServer.Smo")

Function New-Database
{
    param(
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string] $instanceName,

        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string] $databaseName
    )

    $server = "(localdb)\" + $instanceName
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

	Write-Host $connectionString

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
        "IACCEPTSQLLOCALDBLICENSETERMS=YES"
    )
    
    Start-Process "msiexec.exe" -ArgumentList $MSIArguments -Wait -NoNewWindow 
}

Function Add-LocalDbInstance {
    param (
        [string]$instanceName
    )
    
    sqllocaldb create $instanceName | Out-Null
    sqllocaldb share $instanceName $instanceName | Out-Null
    sqllocaldb start $instanceName | Out-Null
    $info = sqllocaldb info $instanceName

    return $info.Split(" ") | where{$_ -like "np:\\.\pipe*"}
}

Function Update-ConnectionStrings {
    param (
        [string]$ConfigFile,
        [string]$ConnectionString
    )

    $xml = [xml](Get-Content $ConfigFile)
    $xml.SelectNodes("//connectionStrings/add") | % {
        $_."connectionString" = $ConnectionString
    }

    $xml.Save($ConfigFile)
}