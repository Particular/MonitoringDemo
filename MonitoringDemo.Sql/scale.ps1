
Write-Host "Scaling out Sales endpoint"
Start-Process ".\Solution\binaries\Sales\net461\Sales.exe"  -ArgumentList "instance-1" -WorkingDirectory ".\Solution\binaries\Sales\net461\"
Start-Sleep -Seconds 20
Start-Process ".\Solution\binaries\Sales\net461\Sales.exe"  -ArgumentList "instance-2" -WorkingDirectory ".\Solution\binaries\Sales\net461\"
Start-Sleep -Seconds 20
Start-Process ".\Solution\binaries\Sales\net461\Sales.exe"  -ArgumentList "instance-3" -WorkingDirectory ".\Solution\binaries\Sales\net461\"
