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

try {
    Write-Host -ForegroundColor Yellow "Checking prerequisites"

    Write-Host "Checking LocalDB"
    if((Get-Command "sqllocaldb.exe" -ErrorAction SilentlyContinue) -eq $null){
      Write-Host "Could not find localdb. See demo prerequisites at https://github.com/Particular/MonitoringDemo#prerequisites."
      throw "No LocalDB installation detected"
    }

    Write-Host "Checking Sql Utilities"
    if((Get-Command "sqlcmd.exe" -ErrorAction SilentlyContinue) -eq $null){
      Write-Host "Could not find Sql Utilities. See demo prerequisites at https://github.com/Particular/MonitoringDemo#prerequisites."
      throw "No Sql Untilites installation detected"
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
    Write-Host "Sharing SQL Instance"
    sqllocaldb share particular-monitoring particular-monitoring
    Write-Host "Starting SQL Instance"
    sqllocaldb start particular-monitoring


    Write-Host "Dropping and creating database"
    sqlcmd -S "(LocalDB)\particular-monitoring" -i "$PSScriptRoot\support\RecreateDB.sql" -v RootPath="`"$PSScriptRoot`"" 

    Write-Host "Creating shared queues"
    sqlcmd -S "(LocalDB)\particular-monitoring" -d ParticularMonitoringDemo -i .\support\CreateQueue.sql -v queueName="audit" 
    sqlcmd -S "(LocalDB)\particular-monitoring" -d ParticularMonitoringDemo -i .\support\CreateQueue.sql -v queueName="error" 

    Write-Host "Creating ServiceControl instance queues"
    sqlcmd -S "(LocalDB)\particular-monitoring" -d ParticularMonitoringDemo -i .\support\CreateQueue.sql -v queueName="Particular.ServiceControl" 
    sqlcmd -S "(LocalDB)\particular-monitoring" -d ParticularMonitoringDemo -i .\support\CreateQueue.sql -v queueName="Particular.ServiceControl.$env:computername" 
    sqlcmd -S "(LocalDB)\particular-monitoring" -d ParticularMonitoringDemo -i .\support\CreateQueue.sql -v queueName="Particular.ServiceControl.staging" 
    sqlcmd -S "(LocalDB)\particular-monitoring" -d ParticularMonitoringDemo -i .\support\CreateQueue.sql -v queueName="Particular.ServiceControl.timeouts" 
    sqlcmd -S "(LocalDB)\particular-monitoring" -d ParticularMonitoringDemo -i .\support\CreateQueue.sql -v queueName="Particular.ServiceControl.timeoutsdispatcher" 
    sqlcmd -S "(LocalDB)\particular-monitoring" -d ParticularMonitoringDemo -i .\support\CreateQueue.sql -v queueName="Particular.ServiceControl.retries" 

    Write-Host "Starting ServiceControl instance"
    $sc = Start-Process ".\Platform\servicecontrol\servicecontrol-instance\bin\ServiceControl.exe" -WorkingDirectory ".\Platform\servicecontrol\servicecontrol-instance\bin" -Verb runAs -PassThru -WindowStyle Minimized

    Write-Host "Creating Monitoring instance queues"
    sqlcmd -S "(LocalDB)\particular-monitoring" -d ParticularMonitoringDemo -i .\support\CreateQueue.sql -v queueName="Particular.Monitoring" 
    sqlcmd -S "(LocalDB)\particular-monitoring" -d ParticularMonitoringDemo -i .\support\CreateQueue.sql -v queueName="Particular.Monitoring.staging" 
    sqlcmd -S "(LocalDB)\particular-monitoring" -d ParticularMonitoringDemo -i .\support\CreateQueue.sql -v queueName="Particular.Monitoring.timeouts" 
    sqlcmd -S "(LocalDB)\particular-monitoring" -d ParticularMonitoringDemo -i .\support\CreateQueue.sql -v queueName="Particular.Monitoring.timeoutsdispatcher" 
    sqlcmd -S "(LocalDB)\particular-monitoring" -d ParticularMonitoringDemo -i .\support\CreateQueue.sql -v queueName="Particular.Monitoring.retries" 

    Write-Host "Starting Monitoring instance"
    $mon = Start-Process ".\Platform\servicecontrol\monitoring-instance\ServiceControl.Monitoring.exe" -WorkingDirectory ".\Platform\servicecontrol\monitoring-instance" -Verb runAs -PassThru -WindowStyle Minimized

    Write-Host "Creating ClientUI queues"
    sqlcmd -S "(LocalDB)\particular-monitoring" -d ParticularMonitoringDemo -i .\support\CreateQueue.sql -v queueName="ClientUI" 
    sqlcmd -S "(LocalDB)\particular-monitoring" -d ParticularMonitoringDemo -i .\support\CreateQueue.sql -v queueName="ClientUI.staging" 
    sqlcmd -S "(LocalDB)\particular-monitoring" -d ParticularMonitoringDemo -i .\support\CreateQueue.sql -v queueName="ClientUI.timeouts" 
    sqlcmd -S "(LocalDB)\particular-monitoring" -d ParticularMonitoringDemo -i .\support\CreateQueue.sql -v queueName="ClientUI.timeoutsdispatcher" 
    sqlcmd -S "(LocalDB)\particular-monitoring" -d ParticularMonitoringDemo -i .\support\CreateQueue.sql -v queueName="ClientUI.retries" 

    Write-Host "Creating Sales queues"
    sqlcmd -S "(LocalDB)\particular-monitoring" -d ParticularMonitoringDemo -i .\support\CreateQueue.sql -v queueName="Sales" 
    sqlcmd -S "(LocalDB)\particular-monitoring" -d ParticularMonitoringDemo -i .\support\CreateQueue.sql -v queueName="Sales.staging" 
    sqlcmd -S "(LocalDB)\particular-monitoring" -d ParticularMonitoringDemo -i .\support\CreateQueue.sql -v queueName="Sales.timeouts" 
    sqlcmd -S "(LocalDB)\particular-monitoring" -d ParticularMonitoringDemo -i .\support\CreateQueue.sql -v queueName="Sales.timeoutsdispatcher" 
    sqlcmd -S "(LocalDB)\particular-monitoring" -d ParticularMonitoringDemo -i .\support\CreateQueue.sql -v queueName="Sales.retries" 

    Write-Host "Creating Billing queues"
    sqlcmd -S "(LocalDB)\particular-monitoring" -d ParticularMonitoringDemo -i .\support\CreateQueue.sql -v queueName="Billing" 
    sqlcmd -S "(LocalDB)\particular-monitoring" -d ParticularMonitoringDemo -i .\support\CreateQueue.sql -v queueName="Billing.staging" 
    sqlcmd -S "(LocalDB)\particular-monitoring" -d ParticularMonitoringDemo -i .\support\CreateQueue.sql -v queueName="Billing.timeouts" 
    sqlcmd -S "(LocalDB)\particular-monitoring" -d ParticularMonitoringDemo -i .\support\CreateQueue.sql -v queueName="Billing.timeoutsdispatcher" 
    sqlcmd -S "(LocalDB)\particular-monitoring" -d ParticularMonitoringDemo -i .\support\CreateQueue.sql -v queueName="Billing.retries" 

    Write-Host "Creating Shipping queues"
    sqlcmd -S "(LocalDB)\particular-monitoring" -d ParticularMonitoringDemo -i .\support\CreateQueue.sql -v queueName="Shipping" 
    sqlcmd -S "(LocalDB)\particular-monitoring" -d ParticularMonitoringDemo -i .\support\CreateQueue.sql -v queueName="Shipping.staging" 
    sqlcmd -S "(LocalDB)\particular-monitoring" -d ParticularMonitoringDemo -i .\support\CreateQueue.sql -v queueName="Shipping.timeouts" 
    sqlcmd -S "(LocalDB)\particular-monitoring" -d ParticularMonitoringDemo -i .\support\CreateQueue.sql -v queueName="Shipping.timeoutsdispatcher" 
    sqlcmd -S "(LocalDB)\particular-monitoring" -d ParticularMonitoringDemo -i .\support\CreateQueue.sql -v queueName="Shipping.retries"
        
    Write-Host "Starting Demo Solution"
    $billing = Start-Process ".\Solution\binaries\Billing\net461\Billing.exe" -WorkingDirectory ".\Solution\binaries\Billing\net461\" -PassThru -WindowStyle Minimized
    $sales = Start-Process ".\Solution\binaries\Sales\net461\Sales.exe" -WorkingDirectory ".\Solution\binaries\Sales\net461\" -PassThru -WindowStyle Minimized
    $shipping = Start-Process ".\Solution\binaries\Shipping\net461\Shipping.exe" -WorkingDirectory ".\Solution\binaries\Shipping\net461\" -PassThru -WindowStyle Minimized
    $clientUI = Start-Process ".\Solution\binaries\ClientUI\net461\ClientUI.exe" -WorkingDirectory ".\Solution\binaries\ClientUI\net461\" -PassThru -WindowStyle Minimized
        
    Write-Host -ForegroundColor Yellow "Once ServiceControl has finished starting a browser window will pop up showing the ServicePulse monitoring tab"
    Start-Sleep -s 25

    Write-Host "Starting ServicePulse"
    $pulse = (Start-Process ".\Platform\servicepulse\ServicePulse.Host.exe" -WorkingDirectory ".\Platform\servicepulse" -Verb runAs -PassThru -WindowStyle Minimized)

    Write-Host -ForegroundColor Yellow "Press enter to shut down demo"
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

Write-Host -ForegroundColor Yellow "Done"
Read-Host