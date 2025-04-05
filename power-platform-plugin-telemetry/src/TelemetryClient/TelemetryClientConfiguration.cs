// Created by Stas Sultanov.
// Copyright © Stas Sultanov.

namespace Azure.Monitor.Telemetry;

using System;
using System.Collections.Generic;

/// <summary>
/// Represents the configuration settings for the <see cref="TelemetryClient"/>.
/// </summary>
public sealed class TelemetryClientConfiguration
{
	/// <summary>
	/// The list of configurations for telemetry publishers.
	/// </summary>
	public required IReadOnlyList<TelemetryPublisherConfiguration> Publishers { get; init; }

	/// <summary>
	/// The optional key-value pairs of tags to include with telemetry data.
	/// </summary>
	public IReadOnlyDictionary<String, String>? Tags { get; init; }
}
