# Overview

Self-contained demo showing all of the monitoring components working together. This accompanies the Particular Platform Monitoring Tutorial available at the following location:

- https://docs.particular.net/tutorials/monitoring/demo/

# Prerequisites

Running the demo requires .Net Framework 4.6.1 or newer. 

In order to run the downloaded sample you will need the following prerequisites.
 
- Window operating system, the Particular Service Platform requires the Windows operating system
  - Desktop: Windows 8 or higher
  - Server: Windows Server 2016 or higher
- Powershell 3.0 or higher
- .NET Framework 4.6.1 (check version)

# Running

- Compile `src\MonitoringDemo.sln`
- Execute `src\binaries\MonitoringDemo.exe`

# Deploying

The monitoring demo doesn't use versioning. The most recent build of the master branch is pushed to S3 storage.

The most common reason to deploy the MonitoringDemo is due to an update of [Particular.PlatformSample](https://github.com/Particular/Particular.PlatformSample). This is included via a [wildcard dependency](https://github.com/Particular/MonitoringDemo/blob/master/src/Platform/Platform.csproj#L12) so it is not necessary to merge a PR to update this dependency, but a new build must be forced.

1. Locate the latest build of the master branch on TeamCity. Force a new build of master if required to include the latest Particular.PlatformSample package.
1. Promote the build to the **S3 Deploy** step.
1. Once complete, the new build should be [available for download]( https://s3.amazonaws.com/particular.downloads/MonitoringDemo/Particular.MonitoringDemo.zip).
