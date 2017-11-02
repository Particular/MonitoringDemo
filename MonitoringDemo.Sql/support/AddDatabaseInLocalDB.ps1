try {

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

        $db = New-Object Microsoft.SqlServer.Management.Smo.Database($srv, $databaseName)
        
        # TODO: Do we need to explicitly set credentials and create the schema? 
        $db.Create()
    }


    Write-Host "Creating $databaseName database"
    New-Database -server ("(localdb)\" + $args[0]) -databaseName $args[1]    
}
catch {
    Read-Host
}
