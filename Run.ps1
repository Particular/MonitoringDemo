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

  Write-Host "Checking if port for ServiceControl - 33533 is available"
  $scPortListeners = Get-NetTCPConnection -State Listen | Where-Object {$_.LocalPort -eq "33533"}
  if($scPortListeners){
    Write-Host "Default port for SC - 33533 is being used at the moment. It might be another SC instance running on this machine."
    throw "Cannot install ServiceControl. Port 33533 is taken."
  }

  Write-Host "Checking if maintenance port for ServiceControl - 33534 is available"
  $scPortListeners = Get-NetTCPConnection -State Listen | Where-Object {$_.LocalPort -eq "33534"}
  if($scPortListeners){
    Write-Host "Maintenance port for SC - 33534 is being used at the moment. It might be another SC instance running on this machine."
    throw "Cannot install ServiceControl. Port 33534 is taken."
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

  Write-Host "Creating log folders"
  New-Item -ItemType Directory -Force -Path ".\logs\monitoring-instance"
  New-Item -ItemType Directory -Force -Path ".\logs\sc-instance"

  Write-Host "Creating transport folder"
  New-Item -ItemType Directory -Force -Path ".\.learningtransport"

  Write-Host "Starting ServiceControl instance"
  $sc = Start-Process ".\Platform\servicecontrol\servicecontrol-instance\bin\ServiceControl.exe" -WorkingDirectory ".\Platform\servicecontrol\servicecontrol-instance\bin" -Verb runAs -PassThru -WindowStyle Minimized

  Write-Host "Starting Monitoring instance"
  $mon = Start-Process ".\Platform\servicecontrol\monitoring-instance\ServiceControl.Monitoring.exe" -WorkingDirectory ".\Platform\servicecontrol\monitoring-instance" -Verb runAs -PassThru -WindowStyle Minimized

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
      $status = (Invoke-WebRequest http://localhost:33533/api -UseBasicParsing).StatusCode
    } catch {
      $status = $_.Exception.Response.StatusCode
    }
  } while ( $status -ne 200 )

  Write-Host
  Write-Host "ServiceControl has started"

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
    Get-Process | Where-Object {$_.Name -eq "Sales" } | Stop-Process
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

  Write-Host "Removing Transport Files"
  Remove-Item .\.learningtransport\ -Force -Recurse

  Write-Host "Deleting db folders"
  Remove-Item ".\Platform\servicecontrol\servicecontrol-instance\db" -Force -Recurse

  Write-Host "Deleting log folders"
  Remove-Item ".\logs" -Force -Recurse
}

Write-Host -ForegroundColor Yellow "Done, press ENTER"
Read-Host
