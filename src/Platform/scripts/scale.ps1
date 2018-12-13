
Write-Host "Scaling out Sales endpoint"
Start-Process ".\Sales\net461\Sales.exe"  -ArgumentList "instance-1" -WorkingDirectory ".\Sales\net461\"
Start-Sleep -Seconds 20
Start-Process ".\Sales\net461\Sales.exe"  -ArgumentList "instance-2" -WorkingDirectory ".\Sales\net461\"
Start-Sleep -Seconds 20
Start-Process ".\Sales\net461\Sales.exe"  -ArgumentList "instance-3" -WorkingDirectory ".\Sales\net461\"
