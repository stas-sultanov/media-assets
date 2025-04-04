// Created by Stas Sultanov.
// Copyright © Stas Sultanov.

namespace Stas.PowerPlatformDemo.Plugins;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Azure.Monitor.Telemetry;
using Azure.Monitor.Telemetry.Publish;

using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Extensions;
using Microsoft.Xrm.Sdk.PluginTelemetry;

using Stas.PowerPlatformDemo.Configuration;

/// <summary>
/// Provides a collection of essential services for plugin execution in the Power Platform Environment.
/// This class encapsulates common services and contexts needed for plugin operations.
/// </summary>
public sealed class PluginContext<PluginConfigurationType> : IDisposable
	where PluginConfigurationType : PluginConfiguration
{
	#region Fields

	/// <summary>
	/// HttpClient for telemetry publishers.
	/// </summary>
	private readonly HttpClient telemetryPublisherHttpClient;

	#endregion

	#region Properties

	/// <summary>
	/// The read-only dictionary where the key is the name of the environment variable and the value is the value of the environment variable.
	/// </summary>
	public PluginConfigurationType Configuration { get; }

	/// <summary>
	/// The platform logger.
	/// </summary>
	public ILogger Logger { get; }

	/// <summary>
	/// The interface to obtain access token for managed identity.
	/// </summary>
	public IManagedIdentityService ManagedIdentityService { get; }

	/// <summary>
	/// Interface to allow plug-ins to obtain IOrganizationService.
	/// </summary>
	public IOrganizationServiceFactory OrganizationServiceFactory { get; }

	/// <summary>
	/// The System User that has initiated the Execution.
	/// </summary>
	public IOrganizationService OrganizationService_InitiatingUser { get; }

	/// <summary>
	/// The System User that is registered to run this plugin.
	/// </summary>
	public IOrganizationService OrganizationService_User { get; }

	/// <summary>
	/// Contextual information about the current operation being processed.
	/// </summary>
	public IPluginExecutionContext7 PluginExecutionContext { get; }

	/// <summary>
	/// Azure Monitor telemetry client.
	/// </summary>
	public TelemetryClient TelemetryClient { get; }

	/// <summary>
	/// Basic tracing.
	/// </summary>
	public ITracingService TracingService { get; }

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes a new instance of the <see cref="PluginContext"/> class.
	/// </summary>
	/// <param name="serviceProvider">The service provider.</param>
	/// <param name="environmentVariablesConfigName">The name for environment variables config.</param>
	/// <exception cref="InvalidPluginExecutionException">Thrown when the service provider is null.</exception>
	/// <exception cref="InvalidPluginExecutionException">Environment variables when the service provider is null.</exception>
	public PluginContext(IServiceProvider serviceProvider, String environmentVariablesConfigName)
	{
		// check input parameter
		if (serviceProvider == null)
		{
			throw new InvalidPluginExecutionException($"Parameter is null. name: {nameof(serviceProvider)}.");
		}

		// get organization service factory
		Logger = GetService<ILogger>(serviceProvider);

		// get organization service factory
		OrganizationServiceFactory = GetService<IOrganizationServiceFactory>(serviceProvider);

		// get plugin execution context
		PluginExecutionContext = GetService<IPluginExecutionContext7>(serviceProvider);

		// get managed identity service
		ManagedIdentityService = GetService<IManagedIdentityService>(serviceProvider);

		// get tracing service
		TracingService = GetService<ITracingService>(serviceProvider);

		// get OrganizationService on behalf of the user that initiated the plugin
		OrganizationService_InitiatingUser = CreateOrganizationService(OrganizationServiceFactory, PluginExecutionContext.InitiatingUserId);

		// get service on behalf of user that the plugin is registered to run as
		OrganizationService_User = CreateOrganizationService(OrganizationServiceFactory, PluginExecutionContext.UserId);

		// get configuration as string
		var configurationAsString = OrganizationService_User.GetEnvironmentVariable(environmentVariablesConfigName)
			?? throw new InvalidPluginExecutionException($"Environment variable '{environmentVariablesConfigName}' is not found.");

		// deserialize configuration
		var configuration = JsonSerializer.Deserialize<PluginConfigurationType>(configurationAsString)
			?? throw new InvalidPluginExecutionException($"Cannot deserialize configuration from the environment variable '{environmentVariablesConfigName}'.");

		Configuration = configuration;

		// create telemetry publisher http client
		telemetryPublisherHttpClient = new HttpClient();

		// create telemetry client
		TelemetryClient = CreateTelemetryClient(Configuration.Telemetry, telemetryPublisherHttpClient, ManagedIdentityService, PluginExecutionContext);
	}

	#endregion

	#region Methods: Implementation of IDisposable

	/// <inheritdoc/>
	public void Dispose()
	{
		telemetryPublisherHttpClient?.Dispose();
	}

	#endregion

	#region Methods: Private

	/// <summary>
	/// Creates an instance of <see cref="IOrganizationService"/> for the specified user.
	/// </summary>
	/// <param name="organizationServiceFactory">The factory used to create the organization service.</param>
	/// <param name="userId">The ID of the user to create the service for.</param>
	/// <returns>An instance of <see cref="IOrganizationService"/> for the specified user.</returns>
	/// <exception cref="InvalidPluginExecutionException">Thrown when the organization service cannot be created for the specified user.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static IOrganizationService CreateOrganizationService(IOrganizationServiceFactory organizationServiceFactory, Guid userId)
	{
		var result = organizationServiceFactory.CreateOrganizationService(userId);

		if (result != null)
		{
			return result;
		}

		var exceptionMessage = $"Cannot create instance of the OrganizationService for userId: {userId}.";

		throw new InvalidPluginExecutionException(exceptionMessage);
	}

	/// <summary>
	/// Retrieves a service of type <typeparamref name="T"/> from the provided service provider.
	/// </summary>
	/// <typeparam name="T">The type of service to retrieve.</typeparam>
	/// <param name="serviceProvider">The service provider to get the service from.</param>
	/// <returns>An instance of type <typeparamref name="T"/>.</returns>
	/// <exception cref="InvalidPluginExecutionException">Thrown when the requested service type cannot be retrieved from the service provider.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static T GetService<T>(IServiceProvider serviceProvider)
	{
		var result = serviceProvider.Get<T>();

		if (result != null)
		{
			return result;
		}

		var exceptionMessage = $"Cannot get instance of {typeof(T).FullName} type from the {nameof(serviceProvider)}.";

		throw new InvalidPluginExecutionException(exceptionMessage);
	}

	/// <summary>
	/// Creates and configures a new instance of <see cref="TelemetryClient"/> for telemetry data collection.
	/// </summary>
	/// <returns>A configured instance of <see cref="TelemetryClient"/>.</returns>
	private static TelemetryClient CreateTelemetryClient
	(
		TelemetryClientConfiguration configuration,
		HttpClient telemetryPublisherHttpClient,
		IManagedIdentityService managedIdentityService,
		IPluginExecutionContext7 pluginExecutionContext
	)
	{
		var authorizationScopes = new[] { HttpTelemetryPublisher.AuthorizationScope };

		var telemetryPublishers = new TelemetryPublisher[configuration.Publishers.Count];

		for (var index = 0; index < configuration.Publishers.Count; index++)
		{
			var publisherConfiguration = configuration.Publishers[index];

			TelemetryPublisher publisher;

			Func<CancellationToken, Task<BearerToken>>? getAccessToken = null;

			if (publisherConfiguration.Authenticate)
			{
				// by default, on the time of writing this code ManagedIdentityService provides token which is valid for 24hrs
				var tokenExpiresOn = DateTime.UtcNow.AddHours(24);

				// get Bearer access token from the Managed Identity service
				var tokenValue = publisherConfiguration.ManagedIdentityId.HasValue
					? managedIdentityService.AcquireToken(publisherConfiguration.ManagedIdentityId.Value, authorizationScopes)
					: managedIdentityService.AcquireToken(authorizationScopes);

				// create Bearer token
				var token = new BearerToken { ExpiresOn = tokenExpiresOn, Value = tokenValue };

				getAccessToken = (cancellationToken) => Task.FromResult(token);
			}

			publisher = new HttpTelemetryPublisher
			(
				telemetryPublisherHttpClient,
				publisherConfiguration.IngestionEndpoint,
				publisherConfiguration.InstrumentationKey,
				getAccessToken,
				publisherConfiguration.Tags?.ToArray()
			);

			telemetryPublishers[index] = publisher;
		}

		var tags = new List<KeyValuePair<String, String>>(configuration.Tags == null ? [] : configuration.Tags)
		{
			new(TelemetryTagKeys.CloudRole, @"PowerPlatform"),
			new(TelemetryTagKeys.CloudRoleInstance, Environment.MachineName)
		};

		var telemetryClient = new TelemetryClient(telemetryPublishers, tags)
		{
			Operation = new TelemetryOperation
			{
				// get operation id
				Id = pluginExecutionContext.CorrelationId.ToString(),
				// get operation name
				Name = pluginExecutionContext.MessageName,
				// get operation parent id
				ParentId = pluginExecutionContext.OperationId.ToString()
			}
		};

		return telemetryClient;
	}

	#endregion
}
