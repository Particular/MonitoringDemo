try
{
    Set-Location (Split-Path $script:MyInvocation.MyCommand.Path)

    Import-Module ./Utils.psm1

    Write-Host "Installing Sql Server Management Objects 2016"
    
    Install-Msi (Get-Item ".\SQLSysClrTypes.msi")
    Install-Msi (Get-Item ".\SharedManagementObjects.msi")
}
catch
{
    Read-Host
}