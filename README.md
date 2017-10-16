# MonitoringDemo
Self-contained demo showing all of the monitoring components working together

- `SetUpDatabase.ps1` - pass in server, database, username, password
  - Connects to server and creates database (if it doesn't exist)
  - Creates tables for endpoint queues in target database if they don't exist
  - Updates connection strings for SC instance, Monitoring instance, and Solution endpoints
- `RunPlatform.ps1` - Runs SC instance, Monitoring instance, and (after a brief pause for SC to start) ServicePulse
- `RunSql.ps1` - Spins up mssql linux on docker and then calls `SetUpDatabase.ps1` with the new db (after a pause)


Folders
- `/Platform` contains all of the platform components
- `/Solution` contains Visual Studio solution
- `/support` contains powershell helper modules.
