# Overview

Self-contained demo showing all of the monitoring components working together. This accompanies the Particular Platform Monitoring Tutorial available at the following location:

- https://docs.particular.net/tutorials/monitoring/demo/

# Prerequisites

Running the demo requires .Net Framework 4.5.1 or newer. 

In order to run the downloaded sample you will need the following prerequisites.
 
- Windows 8 or higher: the Particular Service Platform requires the Windows operating system
- Powershell 3.0 or higher
- .NET Framework 4.6.1 (check version)

## Option 1: Existing SQL Server instance

- Requires Microsoft SQL Server 2012 or higher and login setup  

## Option 2: New LocalDB instance

- Requires [LocalDB](https://www.microsoft.com/en-us/download/details.aspx?id=29062),
- [SQL Server Command Line Utilities](https://www.microsoft.com/en-us/download/details.aspx?id=36433) 
- [Microsoft ODBC Driver 11 for SQL Server](https://www.microsoft.com/en-us/download/details.aspx?id=36434).

# Running 

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

**In this option the script will create a new catalog in the instance specified by the user**

### Use LocalDB

This option runs the demo using existing LocalDB installation.

**In this option the script will create a new LocalDB instance `particular-monitoring` and a new catalog called `ParticularMonitoringDemo`.**

# Structure

- `/Platform` contains all of the platform components
- `/Solution` contains Visual Studio solution
- `/support` contains powershell helper modules.
