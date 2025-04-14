# Console Application Demo

This is a minimal .NET console application that demonstrates how to use the [Azure.Monitor.Telemetry](https://github.com/stas-sultanov/azure-monitor-telemetry) library to track and publish telemetry data.

## 🚀 Features

- Initialization of the telemetry client
- Tracking of:
  - Request
  - In-Proc Dependency
  - HTTP Dependency
- Parallel publishing of telemetry to configured publishers.

## 🛠 Prerequisites

- An [Azure Subscription](https://azure.microsoft.com/free/)
- An [Application Insights resource](https://learn.microsoft.com/en-us/azure/azure-monitor/app/create-new-resource) for telemetry publishing

## ⚙️ Configuration

To run this application, update the code with information from your Application Insights connection string.
- Ingestion Endpoint
- Instrumentation Key

## 📂 Structure

- `Main.cs` — Entry point of the application
- `Program.cs` — Shows how to instantiate and use the telemetry client

## 💡 Purpose

This demo is designed to be as simple and focused as possible, allowing you to:

- Understand the basic integration pattern
- Quickly test changes to the telemetry library
- Validate telemetry publishing logic independently of business environments
