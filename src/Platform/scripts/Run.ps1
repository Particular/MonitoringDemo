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
  Get-ChildItem -Recurse $PSScriptRoot | Unblock-File

  Write-Host -ForegroundColor Yellow "Starting demo"

  Write-Host "Starting the Particular Platform"
  $platform = Start-Process ".\Platform\net461\Platform.exe" -WorkingDirectory ".\Platform\net461\" -PassThru -WindowStyle Minimized

  Write-Host -ForegroundColor Yellow "Once ServiceControl has finished starting a browser window will pop up showing the ServicePulse monitoring tab"

  Write-Host "Starting Demo Solution"
  $billing = Start-Process ".\Billing\net461\Billing.exe" -WorkingDirectory ".\Billing\net461\" -PassThru -WindowStyle Minimized
  $sales = Start-Process ".\Sales\net461\Sales.exe" -WorkingDirectory ".\Sales\net461\" -PassThru -WindowStyle Minimized
  $shipping = Start-Process ".\Shipping\net461\Shipping.exe" -WorkingDirectory ".\Shipping\net461\" -PassThru -WindowStyle Minimized
  $clientUI = Start-Process ".\ClientUI\net461\ClientUI.exe" -WorkingDirectory ".\ClientUI\net461\" -PassThru -WindowStyle Minimized

  Write-Host -ForegroundColor Yellow "Press ENTER to shutdown demo"
  Read-Host
  Write-Host -ForegroundColor Yellow "Shutting down"

} catch {
  Write-Error -Message "Error starting setting up demo."
  Write-Exception $_
} finally { 

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

  if( $platform ) { 
    Write-Host "Shutting down the Particular Platform"
    Stop-Process -InputObject $platform 
  }

  Write-Host "Removing Transport Files"
  Remove-Item ".learningtransport\" -Force -Recurse

  Write-Host "Deleting log folders"
  Remove-Item ".logs" -Force -Recurse

  Start-Sleep -Seconds 5

  Write-Host "Deleting db folders"
  Remove-Item ".db" -Force -Recurse
}

Write-Host -ForegroundColor Yellow "Done, press ENTER"
Read-Host