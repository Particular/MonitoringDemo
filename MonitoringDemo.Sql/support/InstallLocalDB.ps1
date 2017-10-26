try
{
    Set-Location (Split-Path $script:MyInvocation.MyCommand.Path)

    Import-Module ./Utils.psm1

    Write-Host "Installing SQL Server LocalDB 2016"

    Install-Msi (Get-Item ".\SQLSysClrTypes.msi")
    Install-Msi (Get-Item ".\SharedManagementObjects.msi")
    Install-Msi (Get-Item ".\SqlLocalDB.msi")
}
catch
{
    Read-Host
}