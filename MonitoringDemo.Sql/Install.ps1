#Requires -RunAsAdministrator


Import-Module ./support/Utils.psm1

Write-Host "Installing Prerequisites"
Install-Msi (Get-Item ".\support\SQLSysClrTypes.msi")
Install-Msi (Get-Item ".\support\SharedManagementObjects.msi")
Install-Msi (Get-Item ".\support\SqlLocalDB.msi")

Write-Host "Prerequisites [SMO, LocalDB] installed."