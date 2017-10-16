[Reflection.Assembly]::LoadWithPartialName("Microsoft.SqlServer.Smo")

Function New-Database
{
    param(
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string] $server,

        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string] $databaseName,

        [Parameter()]
        [string] $user,

        [Parameter()]
        [string] $password,

        [Parameter(Mandatory=$true)]
        [bool] $defaultCredentials = $true
    )
    
    $srv = New-Object Microsoft.SqlServer.Management.Smo.Server($server)

    if($defaultCredentials -eq $false) {
        $srv.ConnectionContext.LoginSecure=$false;
        $srv.ConnectionContext.set_Login($user);
        $srv.ConnectionContext.set_Password($password);
    }


    # Check can connect
    $srv.ConnectionContext.Connect()

    if ($srv.Databases[$databaseName].Name -eq $Null)
    {
        $db = New-Object Microsoft.SqlServer.Management.Smo.Database( $srv, $databaseName )
        # TODO: Do we need to explicitly set credentials and create the schema? 
        $db.Create()
    }
}

Function New-ConnectionString {
    param(
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string] $server,

        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string] $databaseName,

        [string] $user,
        [string] $password,
        [bool] $defaultCredentials
    )

    $security = "Integrated Security=SSPI;"
    
    if($defaultCredentials -eq $false) {
        $security = "User=$($user);Password=$($password)"
    }

    $connectionString = "server=$($server);Database=$($databaseName);$($security)"

    return $connectionString

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