Import-Module ./support/Utils.psm1
Import-Module ./support/SqlTransport.psm1

$databaseName = "ParticularMonitoringDemo"

function Start-Demo {

        Write-Host -ForegroundColor White "`n`nIn order to be able to listen on some network ports some process need to be run in elevated user mode."
        Read-Host "Press Enter to start the demo."

        Write-Host "Starting ServiceControl instance"
        Start-Process ".\Platform\servicecontrol\servicecontrol-instance\bin\ServiceControl.exe" -WorkingDirectory ".\Platform\servicecontrol\servicecontrol-instance\bin" -Verb runAs
        
        Write-Host "Starting Monitoring instance"
        Start-Process ".\Platform\servicecontrol\monitoring-instance\ServiceControl.Monitoring.exe" -WorkingDirectory ".\Platform\servicecontrol\monitoring-instance" -Verb runAs
        
        Write-Host "Wait for SC instances to have fully started"
        Start-Sleep -s 5
        
        Write-Host "Starting Demo Solution"
        Start-Process ".\Solution\binaries\Billing\net461\Billing.exe" -WorkingDirectory ".\Solution\binaries\Billing\net461\"
        Start-Process ".\Solution\binaries\Sales\net461\Sales.exe" -WorkingDirectory ".\Solution\binaries\Sales\net461\"
        Start-Process ".\Solution\binaries\Shipping\net461\Shipping.exe" -WorkingDirectory ".\Solution\binaries\Shipping\net461\"
        Start-Process ".\Solution\binaries\ClientUI\net461\ClientUI.exe" -WorkingDirectory ".\Solution\binaries\ClientUI\net461\"
        
        Write-Host "Starting ServicePulse"
        Start-Process ".\Platform\servicepulse\ServicePulse.Host.exe" -WorkingDirectory ".\Platform\servicepulse" -Verb runAs
}

function Check-SC-SP-Ports{
        Write-Host "Checking if port for ServiceControl - 33333 is available"
        $scPortListeners = Get-NetTCPConnection -State Listen | Where-Object {$_.LocalPort -eq "33333"}
        if($scPortListeners){
          Write-Host "Default port for SC - 33333 is being used at the moment. It might be another SC instance running on this machine."
          throw "Cannot install ServiceControl. Port 33333 is taken."
        }

        Write-Host "Checking if port for SC Monitoring - 33633 is available"
        $scMonitoringPortListeners = Get-NetTCPConnection -State Listen | Where-Object {$_.LocalPort -eq "33633"}
        if($scMonitoringPortListeners){
          Write-Host "Default port for SC Monitoring - 33633 is being used at the moment. It might be another SC Monitoring instance running on this machine."
          throw "Cannot install SC Monitoring. Port 33633 is taken."
        }

        Write-Host "Checking if port for ServicePulse - 8081 is available"
        $spPortListeners = Get-NetTCPConnection -State Listen | Where-Object {$_.LocalPort -eq "8081"}
        if($spPortListeners){
          Write-Host "Default port for ServicePulse - 8081 is being used at the moment. It might be another Service Pulse running on this machine."
          throw "Cannot install Service Pulse. Port 8081 is taken."
        }

}

function Show-Menu
{
     param (
           [string]$Title = 'NSB Montoring Setup'
     )

     Clear-Host
     Write-Host "================ $Title ================"
     
     Write-Host "1: Use existing SQL Server instance."
     Write-Host "2: Use LocalDB (may require LocalDB installation)."
     Write-Host "Q: Quit."
     Write-Host
}

try
{
        Check-SC-SP-Ports
        Show-Menu

        $input = Read-Host "Please make a selection and press <ENTER>"

        switch ($input)
        {
                '1' {
                        Clear-Host

                        $serverName = Read-Host "Enter SQL Server instance name"
                        $databaseName = Read-Host "Enter Database name"

                        #Make user choose integrated secuirty vs custom credentials
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

                        Write-Host "Creating database..."
                        New-Database -server $serverName -databaseName $databaseName

                        Write-Host "Configuring transport"
                        Set-SqlTransport -connectionString $connectionString
                
                        Start-Demo

                        Read-Host
                } 
                '2' {
                        Clear-Host

                        $args = '-ExecutionPolicy ByPass -Command "& {0}\support\InstallLocalDB.ps1"' -f $PSScriptRoot
                        Start-Process PowerShell.exe -Verb RunAs -ArgumentList $args -WorkingDirectory $PSScriptRoot -Wait 
                        
                        #Reload path to enable sqllocaldb on frist install
                        $env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User") 
                
                        $instanceName = "particular-monitoring"

                        Write-Host "Configuring LocalDB instance $instanceName"
                        $serverName = Add-LocalDbInstance -instanceName $instanceName

                        $args = [string]::Format("-Command ""{0}\support\AddDatabaseInLocalDB.ps1"" {1} {2}", $PSScriptRoot, $instanceName, $databaseName)                        
                        Start-Process PowerShell.exe -ArgumentList $args -WorkingDirectory $PSScriptRoot -Wait 
                        
                        $connectionString = New-ConnectionString -server $serverName -databaseName $databaseName

                        Write-Host "Configuring transport"
                        Set-SqlTransport -connectionString $connectionString
                
                        Start-Demo

                        Read-Host
                } 
                'q' {
                        return
                }
        }
}
catch
{
        Write-Error -Message "Could not connect to Sql Server. $PSItem"
        Write-Exception $_

        Read-Host
}
