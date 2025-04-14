# Console Application Demo

This is a minimal .NET console application that demonstrates how to use the [Azure.Monitor.Telemetry](https://github.com/stas-sultanov/azure-monitor-telemetry) library to track and publish telemetry data.

## 🚀 Features

- Initialization of the telemetry client
- Tracking of:
  - Custom events
  - Exceptions
  - Metrics
  - Activities (operations)
- Parallel publishing of telemetry to configured publishers (e.g., Azure Monitor)

## 🛠 Prerequisites

- An [Azure Subscription](https://azure.microsoft.com/free/)
- An [Application Insights resource](https://learn.microsoft.com/en-us/azure/azure-monitor/app/create-new-resource) for telemetry publishing

## ⚙️ Configuration

To run this application, update the configuration to include your Application Insights connection string or Entra credentials depending on your telemetry publisher.

## 📂 Structure

- `Program.cs` — Entry point of the application; contains usage examples of telemetry tracking
- `TelemetryClient` setup — Shows how to instantiate and use the telemetry client
- `Sample scenarios` — Demonstrates tracking of synthetic operations and exception handling

## 💡 Purpose

This demo is designed to be as simple and focused as possible, allowing you to:

- Understand the basic integration pattern
- Quickly test changes to the telemetry library
- Validate telemetry publishing logic independently of business environments
