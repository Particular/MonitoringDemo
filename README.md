# MonitoringDemo
Self-contained demo showing all of the monitoring components working together. 

## Prerequisites
Running the demo requires .Net Framework 4.5.1 or newer and Sql Managed Objects 2016. Depending on the options choosen SQL Server installation might be requried as well. See `Runnig` section for details.

## Runnig
To run the demo execute `run.bat` and follow the menu options:

```
================ NSB Montoring Setup ================
1: Use existing SQL Server instance.
2: Use LocalDB (may require LocalDB installation).
Q: Quit.

Please make a selection and press <ENTER>:
```

Option 1 enables running demo using existing SQL Server instance. After choosing it, it is requried to provide SQL Server instance name and login credentials if not using Integrated Security.

Option 2 enables running demo using dedicated LocalDB instance i.e. `particular-monitoring`. If no installation is found **LocalDB will be installed automatically**.


## Folders
- `/Platform` contains all of the platform components
- `/Solution` contains Visual Studio solution
- `/support` contains powershell helper modules.
