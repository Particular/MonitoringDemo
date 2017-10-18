#Requires -RunAsAdministrator

Import-Module ./support/SetupTransport.psm1

Write-Host "Setting up SqlTransport on LocalDb"
Install-SqlTransport

Write-Host "Starting ServiceControl instance"
Start-Process ".\Platform\servicecontrol\servicecontrol-instance\bin\ServiceControl.exe" -WorkingDirectory ".\Platform\servicecontrol\servicecontrol-instance\bin"

Write-Host "Starting Monitoring instance"
Start-Process ".\Platform\servicecontrol\monitoring-instance\ServiceControl.Monitoring.exe" -WorkingDirectory ".\Platform\servicecontrol\monitoring-instance"

Write-Host "Wait for SC instances to have fully started"
Start-Sleep -s 5

Write-Host "Starting Demo Solution"
Start-Process ".\Solution\binaries\Billing\net461\Billing.exe" -WorkingDirectory ".\Solution\binaries\Billing\net461\"
Start-Process ".\Solution\binaries\Sales\net461\Sales.exe" -WorkingDirectory ".\Solution\binaries\Sales\net461\"
Start-Process ".\Solution\binaries\Shipping\net461\Shipping.exe" -WorkingDirectory ".\Solution\binaries\Shipping\net461\"
Start-Process ".\Solution\binaries\ClientUI\net461\ClientUI.exe" -WorkingDirectory ".\Solution\binaries\ClientUI\net461\"

Write-Host "Starting ServicePulse"
Start-Process ".\Platform\servicepulse\ServicePulse.Host.exe" -WorkingDirectory ".\Platform\servicepulse"