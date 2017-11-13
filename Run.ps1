function Show-Menu
{
     param (
           [string]$Title = 'NSB Monitoring Setup'
     )

     Clear-Host
     Write-Host "================ $Title ================"
     
     Write-Host "1: Use existing SQL Server database."
     Write-Host "2: Use LocalDB (requires LocalDB and elevated permissions)."
     Write-Host "Q: Quit."
     Write-Host
}

try
{
        Show-Menu

        $input = Read-Host "Please make a selection and press <ENTER>"

        switch ($input)
        {
                '1' {
                        $args = [string]::Format('-NoExit -NoProfile -ExecutionPolicy Bypass -File ""{0}\existingDB.ps1""', $PSScriptRoot)                        
                        Start-Process PowerShell.exe -ArgumentList $args -WorkingDirectory $PSScriptRoot -Verb runAs
                        
                        return
                } 
                '2' {
                        $args = [string]::Format('-NoExit -NoProfile -ExecutionPolicy Bypass -File ""{0}\localDB.ps1""', $PSScriptRoot)                        
                        Start-Process PowerShell.exe -ArgumentList $args -WorkingDirectory $PSScriptRoot -Verb runAs
                        
                        return
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
