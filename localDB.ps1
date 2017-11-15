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

try {
    Write-Host -ForegroundColor Yellow "Checking prerequisites"

    Write-Host "Checking LocalDB"
    if((Get-Command "sqllocaldb.exe" -ErrorAction SilentlyContinue) -eq $null){
      Write-Host "Could not find localdb. See demo prerequisites at https://github.com/Particular/MonitoringDemo#prerequisites."
      throw "No LocalDB installation detected"
    }


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


    Write-Host -ForegroundColor Yellow "Starting demo"

    Write-Host "Creating SQL Instance"
    sqllocaldb create particular-monitoring
    Write-Host "Starting SQL Instance"
    sqllocaldb start particular-monitoring

    Write-Host "Dropping and creating database"
    Invoke-SQL -connectionString "Server=(localDB)\particular-monitoring;Integrated Security=SSPI;" -file "$($PSScriptRoot)\support\CreateCatalogInLocalDB.sql" -v $PSScriptRoot | Out-Null

    Write-Host "Creating shared queues"
    
    $connectionString = "Server=(localDB)\particular-monitoring;Database=ParticularMonitoringDemo;Integrated Security=SSPI;"

    Invoke-SQL -connectionString $connectionString -file "$($PSScriptRoot)\support\CreateQueue.sql" -v "audit" | Out-Null
    Invoke-SQL -connectionString $connectionString -file "$($PSScriptRoot)\support\CreateQueue.sql" -v "error" | Out-Null

    Write-Host "Creating ServiceControl instance queues"

    Invoke-SQL -connectionString $connectionString -file "$($PSScriptRoot)\support\CreateQueue.sql" -v "Particular.ServiceControl" | Out-Null
    Invoke-SQL -connectionString $connectionString -file "$($PSScriptRoot)\support\CreateQueue.sql" -v "Particular.ServiceControl.$env:computername" | Out-Null
    Invoke-SQL -connectionString $connectionString -file "$($PSScriptRoot)\support\CreateQueue.sql" -v "Particular.ServiceControl.staging"  | Out-Null
    Invoke-SQL -connectionString $connectionString -file "$($PSScriptRoot)\support\CreateQueue.sql" -v "Particular.ServiceControl.timeouts" | Out-Null
    Invoke-SQL -connectionString $connectionString -file "$($PSScriptRoot)\support\CreateQueue.sql" -v "Particular.ServiceControl.timeoutsdispatcher" | Out-Null
    Invoke-SQL -connectionString $connectionString -file "$($PSScriptRoot)\support\CreateQueue.sql" -v "Particular.ServiceControl.retries" | Out-Null
    
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
    Invoke-SQL -connectionString $connectionString -file "$($PSScriptRoot)\support\CreateQueue.sql" -v "Particular.Monitoring" | Out-Null
    Invoke-SQL -connectionString $connectionString -file "$($PSScriptRoot)\support\CreateQueue.sql" -v "Particular.Monitoring.staging" | Out-Null
    Invoke-SQL -connectionString $connectionString -file "$($PSScriptRoot)\support\CreateQueue.sql" -v "Particular.Monitoring.timeouts" | Out-Null
    Invoke-SQL -connectionString $connectionString -file "$($PSScriptRoot)\support\CreateQueue.sql" -v "Particular.Monitoring.timeoutsdispatcher" | Out-Null
    Invoke-SQL -connectionString $connectionString -file "$($PSScriptRoot)\support\CreateQueue.sql" -v "Particular.Monitoring.retries" | Out-Null

    Write-Host "Starting Monitoring instance"
    $mon = Start-Process ".\Platform\servicecontrol\monitoring-instance\ServiceControl.Monitoring.exe" -WorkingDirectory ".\Platform\servicecontrol\monitoring-instance" -Verb runAs -PassThru -WindowStyle Minimized

    Write-Host "Creating ClientUI queues"
    Invoke-SQL -connectionString $connectionString -file "$($PSScriptRoot)\support\CreateQueue.sql" -v "ClientUI" | Out-Null
    Invoke-SQL -connectionString $connectionString -file "$($PSScriptRoot)\support\CreateQueue.sql" -v "ClientUI.staging" | Out-Null
    Invoke-SQL -connectionString $connectionString -file "$($PSScriptRoot)\support\CreateQueue.sql" -v "ClientUI.timeouts" | Out-Null
    Invoke-SQL -connectionString $connectionString -file "$($PSScriptRoot)\support\CreateQueue.sql" -v "ClientUI.timeoutsdispatcher" | Out-Null
    Invoke-SQL -connectionString $connectionString -file "$($PSScriptRoot)\support\CreateQueue.sql" -v "ClientUI.retries" | Out-Null

    Write-Host "Creating Sales queues"
    Invoke-SQL -connectionString $connectionString -file "$($PSScriptRoot)\support\CreateQueue.sql" -v "Sales" | Out-Null
    Invoke-SQL -connectionString $connectionString -file "$($PSScriptRoot)\support\CreateQueue.sql" -v "Sales.staging" | Out-Null
    Invoke-SQL -connectionString $connectionString -file "$($PSScriptRoot)\support\CreateQueue.sql" -v "Sales.timeouts" | Out-Null
    Invoke-SQL -connectionString $connectionString -file "$($PSScriptRoot)\support\CreateQueue.sql" -v "Sales.timeoutsdispatcher" | Out-Null
    Invoke-SQL -connectionString $connectionString -file "$($PSScriptRoot)\support\CreateQueue.sql" -v "Sales.retries" | Out-Null

    Write-Host "Creating Billing queues"
    Invoke-SQL -connectionString $connectionString -file "$($PSScriptRoot)\support\CreateQueue.sql" -v "Billing" | Out-Null
    Invoke-SQL -connectionString $connectionString -file "$($PSScriptRoot)\support\CreateQueue.sql" -v "Billing.staging" | Out-Null
    Invoke-SQL -connectionString $connectionString -file "$($PSScriptRoot)\support\CreateQueue.sql" -v "Billing.timeouts" | Out-Null
    Invoke-SQL -connectionString $connectionString -file "$($PSScriptRoot)\support\CreateQueue.sql" -v "Billing.timeoutsdispatcher" | Out-Null
    Invoke-SQL -connectionString $connectionString -file "$($PSScriptRoot)\support\CreateQueue.sql" -v "Billing.retries" | Out-Null

    Write-Host "Creating Shipping queues"
    Invoke-SQL -connectionString $connectionString -file "$($PSScriptRoot)\support\CreateQueue.sql" -v "Shipping" | Out-Null
    Invoke-SQL -connectionString $connectionString -file "$($PSScriptRoot)\support\CreateQueue.sql" -v "Shipping.staging" | Out-Null
    Invoke-SQL -connectionString $connectionString -file "$($PSScriptRoot)\support\CreateQueue.sql" -v "Shipping.timeouts" | Out-Null
    Invoke-SQL -connectionString $connectionString -file "$($PSScriptRoot)\support\CreateQueue.sql" -v "Shipping.timeoutsdispatcher" | Out-Null
    Invoke-SQL -connectionString $connectionString -file "$($PSScriptRoot)\support\CreateQueue.sql" -v "Shipping.retries"| Out-Null
        
    Write-Host "Starting Demo Solution"
    $billing = Start-Process ".\Solution\binaries\Billing\net461\Billing.exe" -WorkingDirectory ".\Solution\binaries\Billing\net461\" -PassThru -WindowStyle Minimized
    $sales = Start-Process ".\Solution\binaries\Sales\net461\Sales.exe" -WorkingDirectory ".\Solution\binaries\Sales\net461\" -PassThru -WindowStyle Minimized
    $shipping = Start-Process ".\Solution\binaries\Shipping\net461\Shipping.exe" -WorkingDirectory ".\Solution\binaries\Shipping\net461\" -PassThru -WindowStyle Minimized
    $clientUI = Start-Process ".\Solution\binaries\ClientUI\net461\ClientUI.exe" -WorkingDirectory ".\Solution\binaries\ClientUI\net461\" -PassThru -WindowStyle Minimized
        
    Write-Host -ForegroundColor Yellow "Once ServiceControl has finished starting a browser window will pop up showing the ServicePulse monitoring tab"
    Write-Host "Sleeping for 25 seconds..."
    Start-Sleep -s 25

    Write-Host "Starting ServicePulse"
    $pulse = (Start-Process ".\Platform\servicepulse\ServicePulse.Host.exe" -ArgumentList "--url=`"http://localhost:8051`"" -WorkingDirectory ".\Platform\servicepulse" -Verb runAs -PassThru -WindowStyle Minimized)

    Write-Host -ForegroundColor Yellow "Press ENTER to shutdown demo"
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

  Write-Host "Stopping SQL Instance"
  sqllocaldb stop particular-monitoring

  Write-Host "Deleting SQL Instance"
  sqllocaldb delete particular-monitoring

  Write-Host "Removing Database Files"
  Remove-Item .\transport\ParticularMonitoringDemo.mdf
  Remove-Item .\transport\ParticularMonitoringDemo_log.ldf
}

Write-Host -ForegroundColor Yellow "Done, press ENTER"
Read-Host
