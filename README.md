# Overview

Self-contained demo showing all of the monitoring components working together. This accompanies the Particular Platform Monitoring Tutorial available at the following location:

- https://docs.particular.net/tutorials/monitoring/demo/

## Prerequisites

In order to run the downloaded sample you will need the following prerequisites.

- .NET 8.0 or higher

## Running

- Compile `src\MonitoringDemo.sln`
- Execute `src\binaries\launch.sh` or `src\binaries\launch.ps1`

## Deploying

1. Go to the [Release action page](https://github.com/Particular/MonitoringDemo/actions/workflows/release.yml).
2. Click the **Run workflow** button.
3. Leave the branch set to master and click the green **Run workflow** button.
4. Once complete, the new build should be [available for download]( https://s3.amazonaws.com/particular.downloads/MonitoringDemo/Particular.MonitoringDemo.zip).

The monitoring demo doesn't use versioning. The most recent build of the master branch is pushed to S3 storage.

The most common reason to deploy the MonitoringDemo is due to an update of [Particular.PlatformSample](https://github.com/Particular/Particular.PlatformSample). This is included via a [wildcard dependency](https://github.com/Particular/MonitoringDemo/blob/master/src/Platform/Platform.csproj#L12) so it is not necessary to merge a PR to update this dependency, but a new release must be forced as described above.
