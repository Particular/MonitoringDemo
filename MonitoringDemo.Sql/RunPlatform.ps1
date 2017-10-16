Write-Host "Starting ServiceControl instance"
Start-Process ".\Platform\servicecontrol\servicecontrol-instance\bin\ServiceControl.exe" -WorkingDirectory ".\Platform\servicecontrol\servicecontrol-instance\bin"

Write-Host "Starting Monitoring instance"
Start-Process ".\Platform\servicecontrol\monitoring-instance\ServiceControl.Monitoring.exe" -WorkingDirectory ".\Platform\servicecontrol\monitoring-instance"

Write-Host "Wait for SC instances to have fully started"
Start-Sleep -s 5

Write-Host "Starting ServicePulse"
Start-Process ".\Platform\servicepulse\ServicePulse.Host.exe" -WorkingDirectory ".\Platform\servicepulse"