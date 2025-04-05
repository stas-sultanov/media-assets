// Created by Stas Sultanov.
// Copyright © Stas Sultanov.

namespace Azure.Monitor.Telemetry;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Azure.Monitor.Telemetry.Publish;

/// <summary>
/// Provides a collection of essential services for plugin execution in the Power Platform Environment.
/// This class encapsulates common services and contexts needed for plugin operations.
/// </summary>
public static class TelemetryClientFactory
{
	#region Methods

	/// <summary>
	/// Creates and configures a new instance of <see cref="TelemetryClient"/>.
	/// </summary>
	/// <remarks>
	/// This implementation uses a single <see cref="HttpClient"/> instance for all telemetry publishers.
	/// </remarks>
	/// <param name="publishersConfigurations">A list of configurations for telemetry publishers.</param>
	/// <param name="publisherHttpClient">An instance of <see cref="HttpClient"/> used for sending telemetry data.</param>
	/// <param name="getGetAccessToken">A delegate that retrieves a method for obtaining an access token for authentication.</param>
	/// <param name="tags">Optional key-value pairs of tags to include with telemetry data.</param>
	/// <returns>A configured instance of <see cref="TelemetryClient"/>.</returns>
	public static TelemetryClient CreateTelemetryClient
	(
		IReadOnlyList<TelemetryPublisherConfiguration> publishersConfigurations,
		HttpClient publisherHttpClient,
		Func<Guid?, Func<CancellationToken, Task<BearerToken>>> getGetAccessToken,
		IReadOnlyList<KeyValuePair<String, String>>? tags
	)
	{
		var telemetryPublishers = new TelemetryPublisher[publishersConfigurations.Count];

		// create telemetry publishers
		for (var index = 0; index < publishersConfigurations.Count; index++)
		{
			var publisherConfiguration = publishersConfigurations[index];

			// get a delegate to the method that will be used to get the access token
			var getAccessToken = publisherConfiguration.Authenticate ? getGetAccessToken(publisherConfiguration.ManagedIdentityId) : null;

			var publisherTags = publisherConfiguration.Tags?.ToArray();

			// create telemetry publisher
			var publisher = new HttpTelemetryPublisher
			(
				publisherHttpClient,
				publisherConfiguration.IngestionEndpoint,
				publisherConfiguration.InstrumentationKey,
				getAccessToken,
				publisherTags
			);

			// set the publisher to the telemetry publishers array
			telemetryPublishers[index] = publisher;
		}

		// create telemetry client
		var result = new TelemetryClient(telemetryPublishers, tags);

		return result;
	}

	#endregion
}
