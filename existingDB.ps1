Set-Location $PSScriptRoot

function Invoke-SQL {
    param(
        [string] $connectionString,
        [string] $file,
        [string] $v
      )
  
    $connection = new-object system.data.SqlClient.SQLConnection($connectionString)
    $command = new-object system.data.sqlclient.sqlcommand($sqlCommand, $connection)
    
    $connection.Open()
    
    $queryTemplate = [IO.File]::ReadAllText($file);
    $command.CommandText = $queryTemplate.Replace("{arg}", $v)
    $command.ExecuteNonQuery();
  
    $connection.Close()
}

function Add-EndpointQueues {
    param(
        [string] $connectionString,
        [string] $endpointName
    )

    Invoke-SQL -connectionString $connectionString -file "$($PSScriptRoot)\support\CreateQueue.sql" -v "$endpointName" | Out-Null
    Invoke-SQL -connectionString $connectionString -file "$($PSScriptRoot)\support\CreateQueue.sql" -v "$endpointName.staging" | Out-Null
    Invoke-SQL -connectionString $connectionString -file "$($PSScriptRoot)\support\CreateQueue.sql" -v "$endpointName.timeouts" | Out-Null
    Invoke-SQL -connectionString $connectionString -file "$($PSScriptRoot)\support\CreateQueue.sql" -v "$endpointName.timeoutsdispatcher" | Out-Null
    Invoke-SQL -connectionString $connectionString -file "$($PSScriptRoot)\support\CreateQueue.sql" -v "$endpointName.retries" | Out-Null
}

function Add-Queue {
    param(
        [string] $connectionString,
        [string] $queueName
    )

    Invoke-SQL -connectionString $connectionString -file "$($PSScriptRoot)\support\CreateQueue.sql" -v "$queueName" | Out-Null
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

try {
    Write-Host -ForegroundColor Yellow "Checking prerequisites"

    Write-Host "Checking if port for ServiceControl - 33533 is available"
    $scPortListeners = Get-NetTCPConnection -State Listen | Where-Object {$_.LocalPort -eq "33533"}
    if($scPortListeners){
        Write-Host "Default port for SC - 33533 is being used at the moment. It might be another SC instance running on this machine."
        throw "Cannot install ServiceControl. Port 33533 is taken."
    }

    Write-Host "Checking if port for SC Monitoring - 33833 is available"
    $scMonitoringPortListeners = Get-NetTCPConnection -State Listen | Where-Object {$_.LocalPort -eq "33833"}
    if($scMonitoringPortListeners){
        Write-Host "Default port for SC Monitoring - 33833 is being used at the moment. It might be another SC Monitoring instance running on this machine."
        throw "Cannot install SC Monitoring. Port 33833 is taken."
    }

    Write-Host "Checking if port for ServicePulse - 8051 is available"
    $spPortListeners = Get-NetTCPConnection -State Listen | Where-Object {$_.LocalPort -eq "8051"}
    if($spPortListeners){
        Write-Host "Default port for ServicePulse - 8051 is being used at the moment. It might be another Service Pulse running on this machine."
        throw "Cannot install Service Pulse. Port 8051 is taken."
    }

    Write-Host -ForegroundColor Yellow "Please provide configuration details"

    $default = "localhost"
    $serverName = if(($result = Read-Host "Enter SQL Server instance name [$default]") -eq ''){"$default"}else{$result}

    $default = "ParticularMonitoringDemo"
    $databaseName = if(($result = Read-Host "Enter Database catalog name [$default]") -eq ''){"$default"}else{$result}
    

    $yes = New-Object System.Management.Automation.Host.ChoiceDescription "&Yes",""
    $no = New-Object System.Management.Automation.Host.ChoiceDescription "&No",""
    $choices = [System.Management.Automation.Host.ChoiceDescription[]]($yes,$no)
    $message = "Use Integrated Security?"
    
    $useIntegratedSecuirty = $Host.UI.PromptForChoice("",$message,$choices,0)
    
    $connectionString = ""

    if($useIntegratedSecuirty -eq 0) 
    { 
            $defaultCatalogConnectionString = New-ConnectionString -server $serverName
            $connectionString = New-ConnectionString -server $serverName -databaseName $databaseName
    }
    else
    {
            $uid = Read-Host "Enter user id"
            $pwd = Read-host "Enter password"
            
            $defaultCatalogConnectionString = New-ConnectionString -server $serverName -integratedSecurity $false -uid $uid -pwd $pwd
            $connectionString = New-ConnectionString -server $serverName -databaseName $databaseName -integratedSecurity $false -uid $uid -pwd $pwd
    }
    
    Write-Host "Testing connectivity. Using connectionString: $defaultCatalogConnectionString"
    Test-SQLConnection -connectionString $defaultCatalogConnectionString

    Write-Host "Try create database if it doesn't exist yet..."
    Invoke-SQL -connectionString $defaultCatalogConnectionString -file "$($PSScriptRoot)\support\CreateCatalog.sql" -v $databaseName | Out-Null

    Write-Host -ForegroundColor Yellow "Starting demo"

    Write-Host "Creating shared queues"
    Add-Queue -connectionString $connectionString -queueName "audit" 
    Add-Queue -connectionString $connectionString -queueName "error"

    Write-Host "Creating ServiceControl instance queues"
    Add-EndpointQueues -connectionString $connectionString -endpointName "Particular.ServiceControl"
    Add-Queue -connectionString $connectionString -queueName "Particular.ServiceControl.$env:computername"

    Write-Host "Updating connection strings"
    
    Update-ConnectionStrings -ConnectionString $connectionString -ConfigFile "$($PSScriptRoot)\Platform\servicecontrol\monitoring-instance\ServiceControl.Monitoring.exe.config"
    Update-ConnectionStrings -ConnectionString $connectionString -ConfigFile "$($PSScriptRoot)\Platform\servicecontrol\servicecontrol-instance\bin\ServiceControl.exe.config"
    
    Update-ConnectionStrings -ConnectionString $connectionString -ConfigFile "$($PSScriptRoot)\Solution\binaries\ClientUI\net461\ClientUI.exe.config"
    Update-ConnectionStrings -ConnectionString $connectionString -ConfigFile "$($PSScriptRoot)\Solution\binaries\Sales\net461\Sales.exe.config"
    Update-ConnectionStrings -ConnectionString $connectionString -ConfigFile "$($PSScriptRoot)\Solution\binaries\Billing\net461\Billing.exe.config"
    Update-ConnectionStrings -ConnectionString $connectionString -ConfigFile "$($PSScriptRoot)\Solution\binaries\Shipping\net461\Shipping.exe.config"

    Write-Host "Starting ServiceControl instance"
    $sc = Start-Process ".\Platform\servicecontrol\servicecontrol-instance\bin\ServiceControl.exe" -WorkingDirectory ".\Platform\servicecontrol\servicecontrol-instance\bin" -Verb runAs -PassThru -WindowStyle Minimized

    Write-Host "Creating Monitoring instance queues"
    Add-EndpointQueues -connectionString $connectionString -endpointName "Particular.Monitoring"

    Write-Host "Starting Monitoring instance"
    $mon = Start-Process ".\Platform\servicecontrol\monitoring-instance\ServiceControl.Monitoring.exe" -WorkingDirectory ".\Platform\servicecontrol\monitoring-instance" -Verb runAs -PassThru -WindowStyle Minimized

    Write-Host "Creating ClientUI queues"
    Add-EndpointQueues -connectionString $connectionString -endpointName "ClientUI"

    Write-Host "Creating Sales queues"
    Add-EndpointQueues -connectionString $connectionString -endpointName "Sales"

    Write-Host "Creating Billing queues"
    Add-EndpointQueues -connectionString $connectionString -endpointName "Billing"
    
    Write-Host "Creating Shipping queues"
    Add-EndpointQueues -connectionString $connectionString -endpointName "Shipping"
        
    Write-Host "Starting Demo Solution"
    $billing = Start-Process ".\Solution\binaries\Billing\net461\Billing.exe" -WorkingDirectory ".\Solution\binaries\Billing\net461\" -PassThru -WindowStyle Minimized
    $sales = Start-Process ".\Solution\binaries\Sales\net461\Sales.exe" -WorkingDirectory ".\Solution\binaries\Sales\net461\" -PassThru -WindowStyle Minimized
    $shipping = Start-Process ".\Solution\binaries\Shipping\net461\Shipping.exe" -WorkingDirectory ".\Solution\binaries\Shipping\net461\" -PassThru -WindowStyle Minimized
    $clientUI = Start-Process ".\Solution\binaries\ClientUI\net461\ClientUI.exe" -WorkingDirectory ".\Solution\binaries\ClientUI\net461\" -PassThru -WindowStyle Minimized
        
    Write-Host -ForegroundColor Yellow "Once ServiceControl has finished starting a browser window will pop up showing the ServicePulse monitoring tab"

    $status = -1
    do {
        Write-Host -NoNewline '.'
        Start-Sleep -s 1
        try {
          $status = (Invoke-WebRequest http://localhost:33533/api ).StatusCode
        } catch {
          $status = $_.Exception.Response.StatusCode
        }
    } while ( $status -ne 200 )

    Write-Host "ServiceControl has started"


    Write-Host "Starting ServicePulse"
    $pulse = (Start-Process ".\Platform\servicepulse\ServicePulse.Host.exe" -ArgumentList "--url=`"http://localhost:8051`"" -WorkingDirectory ".\Platform\servicepulse" -Verb runAs -PassThru -WindowStyle Minimized)

    Write-Host -ForegroundColor Yellow "Press ENTER to shut down demo"
    Read-Host
    Write-Host -ForegroundColor Yellow "Shutting down"

} catch {
  Write-Error -Message "Error starting setting up demo."
  Write-Exception $_
} finally { 

  if( $pulse ) { 
    Write-Host "Shutting down ServicePulse"
    Stop-Process -InputObject $pulse 
  }

  if( $billing ) { 
    Write-Host "Shutting down Billing endpoint"
    Stop-Process -InputObject $billing 
  }
  
  if( $shipping ) { 
    Write-Host "Shutting down Shipping endpoint"
    Stop-Process -InputObject $shipping 
  }

  if( $sales ) {
    Write-Host "Shutting down Sales endpoint"
    Stop-Process -InputObject $sales 
  }
  
  if( $clientUI ) {
    Write-Host "Shutting down ClientUI endpoint"
    Stop-Process -InputObject $clientUI
  }


  if( $mon ) { 
    Write-Host "Shutting down Monitoring instance"
    Stop-Process -InputObject $mon 
  }

  if( $sc ) { 
    Write-Host "Shutting down ServiceControl instance"
    Stop-Process -InputObject $sc
  }
}

Write-Host -ForegroundColor Yellow "Done, press ENTER"
Read-Host
