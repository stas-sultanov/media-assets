// Created by Stas Sultanov.
// Copyright © Stas Sultanov.

namespace Azure.Monitor.Telemetry;

using System;
using System.Collections.Generic;

/// <summary>
/// Represents the configuration settings for a <see cref="TelemetryPublisher"/>.
/// </summary>
public sealed class TelemetryPublisherConfiguration
{
	/// <summary>
	/// The flag that indicates whether authentication is required.
	/// </summary>
	public required Boolean Authenticate { get; init; }

	/// <summary>
	/// The URI of the ingestion endpoint used for sending telemetry data.
	/// </summary>
	public required Uri IngestionEndpoint { get; init; }

	/// <summary>
	/// The unique instrumentation key used to identify the application.
	/// </summary>
	public required Guid InstrumentationKey { get; init; }

	/// <summary>
	/// The managed identity identifier used for authorization.
	/// </summary>
	/// <remarks>
	/// If not specified, the default managed identity will be used.
	/// </remarks>
	public Guid? ManagedIdentityId { get; init; }

	/// <summary>
	/// The optional key-value pairs of tags to include with telemetry data.
	/// </summary>
	public IReadOnlyDictionary<String, String>? Tags { get; init; }
}
