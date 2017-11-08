Function Install-Msi {
    param (
        [System.IO.FileInfo]$file
    )
    $DataStamp = get-date -Format yyyyMMddTHHmmss
    $logFile = '{0}-{1}.log' -f $file.fullname,$DataStamp
    $MSIArguments = @(
        "/i"
        ('"{0}"' -f $file.fullname)
        "/qn"
        "/norestart"
        "/L*v"
        $logFile
        "IACCEPTSQLLOCALDBLICENSETERMS=YES"
    )
    
    $process = Start-Process "msiexec.exe" -ArgumentList $MSIArguments -Wait -NoNewWindow -PassThru
    
    if($process.ExitCode -ne 0){
        Write-Error -Message "Failed installing ($file.Path)"
        Read-Host
    }
    
}

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