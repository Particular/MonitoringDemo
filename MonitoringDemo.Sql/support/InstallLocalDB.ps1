try
{
    Set-Location (Split-Path $script:MyInvocation.MyCommand.Path)

    Import-Module ./Utils.psm1

    Write-Host "Installing Prerequisites"
    Install-Msi (Get-Item ".\SQLSysClrTypes.msi")
    Install-Msi (Get-Item ".\SharedManagementObjects.msi")
    Install-Msi (Get-Item ".\SqlLocalDB.msi")

    Write-Host "Prerequisites [SMO, LocalDB] installed."
}
catch
{
    Read-Host
}