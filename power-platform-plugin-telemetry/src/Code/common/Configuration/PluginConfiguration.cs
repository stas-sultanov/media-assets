// Created by Stas Sultanov.
// Copyright © Stas Sultanov.

namespace Stas.PowerPlatformDemo.Configuration;

/// <summary>
/// Represents the configuration settings for the plugin.
/// </summary>
/// <remarks>
/// Can be inhereted to include additional settings.
/// </remarks>
public class PluginConfiguration
{
	/// <summary>
	/// The configuration settings for the telemetry client.
	/// </summary>
	public required TelemetryClientConfiguration Telemetry { get; init; }
}
