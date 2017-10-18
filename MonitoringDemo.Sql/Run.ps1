#Requires -RunAsAdministrator

Start-Process PowerShell.exe -ArgumentList '-Command "& .\Install.ps1"' -WorkingDirectory $PSScriptRoot -Wait

#Reload path to enable sqllocaldb on frist install
$env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User") 

Start-Process PowerShell.exe -ArgumentList '-Command "& .\Start.ps1"' -WorkingDirectory $PSScriptRoot -Wait