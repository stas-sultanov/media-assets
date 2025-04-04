// Created by Stas Sultanov.
// Copyright © Stas Sultanov.

namespace Stas.PowerPlatformDemo.Configuration;

using System;
using System.Collections.Generic;

/// <summary>
/// The configuration settings for <see cref="Azure.Monitor.Telemetry.TelemetryClient"/>.
/// </summary>
public sealed class TelemetryClientConfiguration
{
	/// <summary>
	/// The list of telemetry publisher configurations.
	/// </summary>
	public required IReadOnlyList<TelemetryPublisherConfiguration> Publishers { get; init; }

	/// <summary>
	/// The key-value pairs of tags to include with telemetry data.
	/// </summary>
	public IReadOnlyDictionary<String, String>? Tags { get; init; }
}
