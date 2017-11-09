# MonitoringDemo
Self-contained demo showing all of the monitoring components working together. 

## Prerequisites
Running the demo requires .Net Framework 4.5.1 or newer. Option 1 requires MS SQL Server database and login setup and [Sql Managed Objects 2016](https://www.microsoft.com/en-us/download/details.aspx?id=52676). Option 2 requries [LocalDB](https://www.microsoft.com/en-us/download/details.aspx?id=29062) and [sqlcmd.exe](https://www.microsoft.com/en-us/download/details.aspx?id=53591).

## Running 
To run the demo execute `run.bat`. There are two options available:
```
================ NSB Montoring Setup ================
1: Use existing SQL Server database.
2: Use LocalDB (requires LocalDB and elevated permissions).
Q: Quit.

Please make a selection and press <ENTER>:
```

### Use existing SQL Server database
This option runs the demo using existing MS SQL Server installation. During startup existing database and login details have to provided.

### Use LocalDB
This option runs the demo using existing LocalDB installation.

## Folders
- `/Platform` contains all of the platform components
- `/Solution` contains Visual Studio solution
- `/support` contains powershell helper modules.
