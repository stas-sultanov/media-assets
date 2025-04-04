// Created by Stas Sultanov.
// Copyright © Stas Sultanov.

namespace Stas.PowerPlatformDemo.Configuration;

using System;
using System.Collections.Generic;

/// <summary>
/// The configuration settings for <see cref="Azure.Monitor.Telemetry.TelemetryPublisher"/>.
/// </summary>
public sealed class TelemetryPublisherConfiguration
{
	/// <summary>
	/// The flag that indicates if authentication is required.
	/// </summary>
	public required Boolean Authenticate { get; init; }

	/// <summary>
	/// The URI of the ingestion endpoint for sending telemetry data.
	/// </summary>
	public required Uri IngestionEndpoint { get; init; }

	/// <summary>
	/// The unique instrumentation key for identifying the application.
	/// </summary>
	public required Guid InstrumentationKey { get; init; }

	/// <summary>
	/// The managed identity ID to use for authorization.
	/// </summary>
	/// <remarks>
	/// If not specified, the default managed identity will be used.
	/// </remarks>
	public Guid? ManagedIdentityId { get; init; }

	/// <summary>
	/// The key-value pairs of tags to include with telemetry data.
	/// </summary>
	public IReadOnlyDictionary<String, String>? Tags { get; init; }
}
