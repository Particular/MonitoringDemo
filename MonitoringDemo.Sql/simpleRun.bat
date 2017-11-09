@ECHO OFF
PowerShell.exe -NoProfile -Command "& {Start-Process PowerShell.exe -ArgumentList '-NoProfile -ExecutionPolicy Bypass -File ""%~dp0\SimpleRun.ps1""' -WorkingDirectory %~dp0 -Verb runAs }"