Set-Location $PSScriptRoot

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

    $credentials = "-U sa -P particular"
    sqlcmd $credentials -S $serverName -Q "IF NOT exists(select * from sys.databases where name='$databaseName') CREATE DATABASE [$databaseName];"
    
    #sqlcmd $credentials -S $serverName -Q "IF NOT exists(select * from sys.databases where name='test') CREATE DATABASE [test];"

    #Start-Process -FilePath 'sqlcmd' -ArgumentList '$credentials -S $serverName -Q "IF NOT exists(select * from sys.databases where name=`"test`") CREATE DATABASE [test];"' -Wait

    # $args = "$credentials -S $serverName -Q 'IF NOT exists(select * from sys.databases where name='test') CREATE DATABASE [test];'"
    $args = "$credentials -S . -Q 'select * from sys.databases'"
    Write-Host $args
    & sqlcmd $args
    exit

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
            $testconnectionString = New-ConnectionString -server $serverName
            $connectionString = New-ConnectionString -server $serverName -databaseName $databaseName
    }
    else
    {
            $uid = Read-Host "Enter user id"
            $pwd = Read-host "Enter password"
            
            $testconnectionString = New-ConnectionString -server $serverName -integratedSecurity $false -uid $uid -pwd $pwd
            $connectionString = New-ConnectionString -server $serverName -databaseName $databaseName -integratedSecurity $false -uid $uid -pwd $pwd
    }
    
    Write-Host "Testing connectivity. Using connectionString: $testconnectionString"
    Test-SQLConnection -connectionString $testconnectionString


    Write-Host "Try create database if it doesn't exist yet..."
    sqlcmd -S $serverName -Q "IF NOT exists(select * from sys.databases where name='$databaseName') CREATE DATABASE [$databaseName];"

    Write-Host -ForegroundColor Yellow "Starting demo"

    Write-Host "Creating shared queues"
    sqlcmd -S $serverName -d $databaseName -i .\support\CreateQueue.sql -v queueName="audit" 
    sqlcmd -S $serverName -d $databaseName -i .\support\CreateQueue.sql -v queueName="error" 

    Write-Host "Creating ServiceControl instance queues"
    sqlcmd -S $serverName -d $databaseName -i .\support\CreateQueue.sql -v queueName="Particular.ServiceControl" 
    sqlcmd -S $serverName -d $databaseName -i .\support\CreateQueue.sql -v queueName="Particular.ServiceControl.$env:computername" 
    sqlcmd -S $serverName -d $databaseName -i .\support\CreateQueue.sql -v queueName="Particular.ServiceControl.staging" 
    sqlcmd -S $serverName -d $databaseName -i .\support\CreateQueue.sql -v queueName="Particular.ServiceControl.timeouts" 
    sqlcmd -S $serverName -d $databaseName -i .\support\CreateQueue.sql -v queueName="Particular.ServiceControl.timeoutsdispatcher" 
    sqlcmd -S $serverName -d $databaseName -i .\support\CreateQueue.sql -v queueName="Particular.ServiceControl.retries" 

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
    sqlcmd -S $serverName -d $databaseName -i .\support\CreateQueue.sql -v queueName="Particular.Monitoring" 
    sqlcmd -S $serverName -d $databaseName -i .\support\CreateQueue.sql -v queueName="Particular.Monitoring.staging" 
    sqlcmd -S $serverName -d $databaseName -i .\support\CreateQueue.sql -v queueName="Particular.Monitoring.timeouts" 
    sqlcmd -S $serverName -d $databaseName -i .\support\CreateQueue.sql -v queueName="Particular.Monitoring.timeoutsdispatcher" 
    sqlcmd -S $serverName -d $databaseName -i .\support\CreateQueue.sql -v queueName="Particular.Monitoring.retries" 

    Write-Host "Starting Monitoring instance"
    $mon = Start-Process ".\Platform\servicecontrol\monitoring-instance\ServiceControl.Monitoring.exe" -WorkingDirectory ".\Platform\servicecontrol\monitoring-instance" -Verb runAs -PassThru -WindowStyle Minimized

    Write-Host "Creating ClientUI queues"
    sqlcmd -S $serverName -d $databaseName -i .\support\CreateQueue.sql -v queueName="ClientUI" 
    sqlcmd -S $serverName -d $databaseName -i .\support\CreateQueue.sql -v queueName="ClientUI.staging" 
    sqlcmd -S $serverName -d $databaseName -i .\support\CreateQueue.sql -v queueName="ClientUI.timeouts" 
    sqlcmd -S $serverName -d $databaseName -i .\support\CreateQueue.sql -v queueName="ClientUI.timeoutsdispatcher" 
    sqlcmd -S $serverName -d $databaseName -i .\support\CreateQueue.sql -v queueName="ClientUI.retries" 

    Write-Host "Creating Sales queues"
    sqlcmd -S $serverName -d $databaseName -i .\support\CreateQueue.sql -v queueName="Sales" 
    sqlcmd -S $serverName -d $databaseName -i .\support\CreateQueue.sql -v queueName="Sales.staging" 
    sqlcmd -S $serverName -d $databaseName -i .\support\CreateQueue.sql -v queueName="Sales.timeouts" 
    sqlcmd -S $serverName -d $databaseName -i .\support\CreateQueue.sql -v queueName="Sales.timeoutsdispatcher" 
    sqlcmd -S $serverName -d $databaseName -i .\support\CreateQueue.sql -v queueName="Sales.retries" 

    Write-Host "Creating Billing queues"
    sqlcmd -S $serverName -d $databaseName -i .\support\CreateQueue.sql -v queueName="Billing" 
    sqlcmd -S $serverName -d $databaseName -i .\support\CreateQueue.sql -v queueName="Billing.staging" 
    sqlcmd -S $serverName -d $databaseName -i .\support\CreateQueue.sql -v queueName="Billing.timeouts" 
    sqlcmd -S $serverName -d $databaseName -i .\support\CreateQueue.sql -v queueName="Billing.timeoutsdispatcher" 
    sqlcmd -S $serverName -d $databaseName -i .\support\CreateQueue.sql -v queueName="Billing.retries" 

    Write-Host "Creating Shipping queues"
    sqlcmd -S $serverName -d $databaseName -i .\support\CreateQueue.sql -v queueName="Shipping" 
    sqlcmd -S $serverName -d $databaseName -i .\support\CreateQueue.sql -v queueName="Shipping.staging" 
    sqlcmd -S $serverName -d $databaseName -i .\support\CreateQueue.sql -v queueName="Shipping.timeouts" 
    sqlcmd -S $serverName -d $databaseName -i .\support\CreateQueue.sql -v queueName="Shipping.timeoutsdispatcher" 
    sqlcmd -S $serverName -d $databaseName -i .\support\CreateQueue.sql -v queueName="Shipping.retries"
        
    Write-Host "Starting Demo Solution"
    $billing = Start-Process ".\Solution\binaries\Billing\net461\Billing.exe" -WorkingDirectory ".\Solution\binaries\Billing\net461\" -PassThru -WindowStyle Minimized
    $sales = Start-Process ".\Solution\binaries\Sales\net461\Sales.exe" -WorkingDirectory ".\Solution\binaries\Sales\net461\" -PassThru -WindowStyle Minimized
    $shipping = Start-Process ".\Solution\binaries\Shipping\net461\Shipping.exe" -WorkingDirectory ".\Solution\binaries\Shipping\net461\" -PassThru -WindowStyle Minimized
    $clientUI = Start-Process ".\Solution\binaries\ClientUI\net461\ClientUI.exe" -WorkingDirectory ".\Solution\binaries\ClientUI\net461\" -PassThru -WindowStyle Minimized
        
    Write-Host -ForegroundColor Yellow "Once ServiceControl has finished starting a browser window will pop up showing the ServicePulse monitoring tab"
    Write-Host "Sleeping for 25 seconds..."
    Start-Sleep -s 25

    Write-Host "Starting ServicePulse"
    $pulse = (Start-Process ".\Platform\servicepulse\ServicePulse.Host.exe" -WorkingDirectory ".\Platform\servicepulse" -Verb runAs -PassThru -WindowStyle Minimized)

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
