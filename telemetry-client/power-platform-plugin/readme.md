# Power Platform Plugin Demo

This folder contains sources of sample Microsoft Power Platform Plugin that demonstrates how to use the [Azure.Monitor.Telemetry](https://github.com/stas-sultanov/azure-monitor-telemetry) library to track and publish telemetry data.

The demo is created for people with solid expertise in creating plugins for Microsoft Power Platform.

## 🚀 Features

- Tracks:
  - Exceptions
  - Requests
- Adds contextual information from the plugin execution context
- Publishes telemetry directly to Application Insights

## 🛠 Prerequisites

- A Power Platform environment
- An [Azure Subscription][azure_subscription]
- An [Application Insights resource(s)][app_insights_info]

## ⚙️ Setup

### 1. Application Insights Resource
Create as many instances of Application Insights resource as needed.

### 2. Authentication

In case of using [Entra authentication][app_insights_entra_auth] ensure that:
1. The Demo Plugin Package is Signed with a certificate.
2. The certificate Thumbprint is specified within the Entra Identity, which will be used by Power Platform plugin to publish telemetry.
3. The Entra Identity from step 2 is granted with the [Monitoring Metrics Publisher][azure_rbac_monitoring_metrics_publisher] role on specific instance of Application Insights resource.
5. The Power Platform Managed Identity created using Entra Identity from step 2.
6. The Demo Plugin Package is bind with the Power Platform Managed Identity from step 5.

### 3. Environment Variables

Create environment variable of text type with the following content:

```json
{
  "TelemetryClient":
  {
    "Publishers":
    [
      {
        "Authenticate": true,
	"IngestionEndpoint": "[INSERT HERE THE INGESTION ENDPOINT #A]",
        "InstrumentationKey": "[INSERT HERE THE INSTRUMENTATION KEY #A]"
      },
      {
        "IngestionEndpoint": "[INSERT HERE THE INGESTION ENDPOINT #B]",
        "InstrumentationKey": "[INSERT HERE THE INSTRUMENTATION KEY #B]"
      }
    ],
    "Tags":
    {
      "ai.application.ver": "1.0.0"
    }
  }
}
```

The schema is pretty simple and should be clear to understand how to specify required telemetry publishers with and without authentication.

### 4. The Code

Adjust the code of [DemoPlugin](/telemetry-client/power-platform-plugin/Demo/src/Code/DemoPlugin.cs) class, to use name of the environment variable that holds the configuration from [step 3](#3-environment-variables).

## Pack & Sign

To pack the project use the following script, replace X.Y.Z to required package version.

```powershell
dotnet pack demo.src\Plugins.csproj `
	-c Release `
	-o out `
	-p:AssemblyVersion=X.Y.Z
```

To sign the NuGet package, use the following script:
```powershell
$certificateFile = '.\your-certificate-file.pfx';
$passwordFile = '.\your-certificate-password.txt';
$path = '.out\*.nupkg';

$certificatePassword = Get-Content -Path $passwordFile;

dotnet nuget sign $path `
	--certificate-path $certificateFile `
	--certificate-password $certificatePassword `
	--hash-algorithm SHA256 `
	--overwrite `
	--timestamp-hash-algorithm SHA256 `
	--timestamper http://timestamp.digicert.com `
	--verbosity Detailed
```

## 📌 Notes

- This plugin is for demonstration purposes but can be used as a base for production.

[azure_subscription]: https://azure.microsoft.com/free/dotnet/
[azure_rbac_monitoring_metrics_publisher]: https://learn.microsoft.com/azure/role-based-access-control/built-in-roles/monitor#monitoring-metrics-publisher
[app_insights_info]: https://learn.microsoft.com/azure/azure-monitor/app/app-insights-overview
[app_insights_entra_auth]: https://learn.microsoft.com/azure/azure-monitor/app/azure-ad-authentication
