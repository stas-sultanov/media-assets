// Created by Stas Sultanov.
// Copyright © Stas Sultanov.

namespace Stas.PowerPlatformDemo.Plugins;

using Azure.Monitor.Telemetry;

/// <summary>
/// Represents the configuration settings for a plugin.
/// </summary>
/// <remarks>
/// This class can be extended to include additional configuration settings specific to the plugin.
/// </remarks>
public class PluginConfiguration
{
	/// <summary>
	/// The configuration settings for the telemetry client.
	/// </summary>
	public required TelemetryClientConfiguration TelemetryClient { get; init; }
}
