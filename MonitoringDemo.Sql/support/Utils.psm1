Function New-ConnectionString {
    param(
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string] $server,

        [Parameter(Mandatory=$false)]
        [string] $databaseName,

        [Parameter(Mandatory=$false)]
        [bool] $integratedSecurity = $true,

        [Parameter(Mandatory=$false)]
        [ValidateNotNullOrEmpty()]
        [string] $uid,

        [Parameter(Mandatory=$false)]
        [ValidateNotNullOrEmpty()]
        [string] $pwd
    )

    if($databaseName)
    {
        $db = ";Database=$($databaseName)"
    }

    if($integratedSecurity -eq $true){
        return "server=$($server);Integrated Security=SSPI$db"
    }
    else
    {
        return "server=$($server);Uid=$($uid);Pwd=$($pwd)$db"
    }
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

function Write-Exception 
{
    param(
        [System.Management.Automation.ErrorRecord]$error
    )

    $formatstring = "{0} : {1}`n{2}`n" +
    "    + CategoryInfo          : {3}`n"
    "    + FullyQualifiedErrorId : {4}`n"

    $fields = $error.InvocationInfo.MyCommand.Name,
    $error.ErrorDetails.Message,
    $error.InvocationInfo.PositionMessage,
    $error.CategoryInfo.ToString(),
    $error.FullyQualifiedErrorId

    Write-Host -Foreground Red -Background Black ($formatstring -f $fields)
}

Function Test-SQLConnection
{    
    [OutputType([bool])]
    Param
    (
        [Parameter(Mandatory=$true,
                    ValueFromPipelineByPropertyName=$true,
                    Position=0)]
        $ConnectionString
    )
    try
    {
        $sqlConnection = New-Object System.Data.SqlClient.SqlConnection $ConnectionString;
        $sqlConnection.Open();
        $sqlConnection.Close();
    }
    catch
    {
        Write-Error -Message "Could not connect to Sql Server. $PSItem"
        Write-Exception $_
        Read-Host
        
        exit
    }
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