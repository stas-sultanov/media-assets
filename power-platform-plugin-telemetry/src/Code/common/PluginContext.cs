// Created by Stas Sultanov.
// Copyright © Stas Sultanov.

namespace Stas.PowerPlatformDemo.Plugins;

using System;
using System.Collections.Generic;
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
/// Provides a collection of essential services for plugin execution in the Power Platform environment.
/// This class encapsulates common services and contexts required for plugin operations.
/// </summary>
public sealed class PluginContext<PluginConfigurationType> : IDisposable
	where PluginConfigurationType : PluginConfiguration
{
	#region Static Fields

	private static readonly String[] telemetryPublisherAuthorizationScopes = [HttpTelemetryPublisher.AuthorizationScope];

	#endregion

	#region Fields

	/// <summary>
	/// The <see cref="HttpClient"/> instance used by telemetry publishers.
	/// </summary>
	private readonly HttpClient telemetryPublisherHttpClient;

	#endregion

	#region Properties

	/// <summary>
	/// The plugin configuration settings.
	/// </summary>
	public PluginConfigurationType Configuration { get; }

	/// <summary>
	/// The platform logger.
	/// </summary>
	public ILogger Logger { get; }

	/// <summary>
	/// The service for obtaining access tokens for managed identities.
	/// </summary>
	public IManagedIdentityService ManagedIdentityService { get; }

	/// <summary>
	/// The factory for creating instances of <see cref="IOrganizationService"/>.
	/// </summary>
	public IOrganizationServiceFactory OrganizationServiceFactory { get; }

	/// <summary>
	/// The organization service for the user who initiated the plugin execution.
	/// </summary>
	public IOrganizationService OrganizationService_InitiatingUser { get; }

	/// <summary>
	/// The organization service for the user under which the plugin is registered to run.
	/// </summary>
	public IOrganizationService OrganizationService_User { get; }

	/// <summary>
	/// Contextual information about the current operation being processed.
	/// </summary>
	public IPluginExecutionContext7 PluginExecutionContext { get; }

	/// <summary>
	/// The Azure Monitor telemetry client.
	/// </summary>
	public TelemetryClient TelemetryClient { get; }

	/// <summary>
	/// The tracing service for basic logging and diagnostics.
	/// </summary>
	public ITracingService TracingService { get; }

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes a new instance of the <see cref="PluginContext"/> class.
	/// </summary>
	/// <param name="serviceProvider">The service provider.</param>
	/// <param name="environmentVariablesConfigName">The name of the environment variable containing configuration settings.</param>
	/// <exception cref="InvalidPluginExecutionException">Thrown if the service provider is null or configuration cannot be retrieved.</exception>
	public PluginContext(IServiceProvider serviceProvider, String environmentVariablesConfigName)
	{
		// validate input parameter
		if (serviceProvider == null)
		{
			throw new InvalidPluginExecutionException($"Parameter is null. Name: {nameof(serviceProvider)}.");
		}

		// retrieve required services
		Logger = GetService<ILogger>(serviceProvider);
		OrganizationServiceFactory = GetService<IOrganizationServiceFactory>(serviceProvider);
		PluginExecutionContext = GetService<IPluginExecutionContext7>(serviceProvider);
		ManagedIdentityService = GetService<IManagedIdentityService>(serviceProvider);
		TracingService = GetService<ITracingService>(serviceProvider);

		// create organization services
		OrganizationService_InitiatingUser = CreateOrganizationService(OrganizationServiceFactory, PluginExecutionContext.InitiatingUserId);
		OrganizationService_User = CreateOrganizationService(OrganizationServiceFactory, PluginExecutionContext.UserId);

		// retrieve and deserialize configuration
		var configurationAsString = OrganizationService_User.GetEnvironmentVariable(environmentVariablesConfigName)
			?? throw new InvalidPluginExecutionException($"Environment variable '{environmentVariablesConfigName}' is not found.");
		Configuration = JsonSerializer.Deserialize<PluginConfigurationType>(configurationAsString)
			?? throw new InvalidPluginExecutionException($"Cannot deserialize configuration from the environment variable '{environmentVariablesConfigName}'.");

		// initialize telemetry client
		telemetryPublisherHttpClient = new HttpClient();
		var telemetryClientConfiguration = Configuration.TelemetryClient;

		// form telemetry tags
		var tags = new List<KeyValuePair<String, String>>(telemetryClientConfiguration.Tags?.ToArray() ?? Array.Empty<KeyValuePair<String, String>>())
		{
			new(TelemetryTagKeys.CloudRole, "PowerPlatform"),
			new(TelemetryTagKeys.CloudRoleInstance, Environment.MachineName)
		};

		// create telemetry client
		TelemetryClient = TelemetryClientFactory.CreateTelemetryClient
		(
			telemetryClientConfiguration.Publishers,
			telemetryPublisherHttpClient,
			GetGetAccessToken,
			tags
		);

		// Set telemetry operation details
		TelemetryClient.Operation = new TelemetryOperation
		{
			Id = PluginExecutionContext.CorrelationId.ToString(),
			Name = PluginExecutionContext.MessageName,
			ParentId = PluginExecutionContext.OperationId.ToString()
		};
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
	/// <param name="userId">The ID of the user for whom the service is created.</param>
	/// <returns>An instance of <see cref="IOrganizationService"/> for the specified user.</returns>
	/// <exception cref="InvalidPluginExecutionException">Thrown if the organization service cannot be created.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static IOrganizationService CreateOrganizationService(IOrganizationServiceFactory organizationServiceFactory, Guid userId)
	{
		var result = organizationServiceFactory.CreateOrganizationService(userId) ?? throw new InvalidPluginExecutionException($"Cannot create instance of the OrganizationService for userId: {userId}.");

		return result;
	}

	/// <summary>
	/// Retrieves a service of type <typeparamref name="T"/> from the provided service provider.
	/// </summary>
	/// <typeparam name="T">The type of service to retrieve.</typeparam>
	/// <param name="serviceProvider">The service provider to retrieve the service from.</param>
	/// <returns>An instance of type <typeparamref name="T"/>.</returns>
	/// <exception cref="InvalidPluginExecutionException">Thrown if the requested service cannot be retrieved.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static T GetService<T>(IServiceProvider serviceProvider)
	{
		var result = serviceProvider.Get<T>() ?? throw new InvalidPluginExecutionException($"Cannot get instance of {typeof(T).FullName} type from the {nameof(serviceProvider)}.");

		return result;
	}

	/// <summary>
	/// Retrieves a delegate for obtaining an access token for the specified managed identity.
	/// </summary>
	/// <param name="managedIdentityId">The ID of the managed identity.</param>
	/// <returns>A delegate that retrieves an access token.</returns>
	private Func<CancellationToken, Task<BearerToken>> GetGetAccessToken(Guid? managedIdentityId)
	{
		// Default token expiration is 24 hours
		var tokenExpiresOn = DateTime.UtcNow.AddHours(24);

		// Acquire token from the Managed Identity service
		var tokenValue = managedIdentityId.HasValue
			? ManagedIdentityService.AcquireToken(managedIdentityId.Value, telemetryPublisherAuthorizationScopes)
			: ManagedIdentityService.AcquireToken(telemetryPublisherAuthorizationScopes);

		// Create a Bearer token
		var token = new BearerToken { ExpiresOn = tokenExpiresOn, Value = tokenValue };

		Task<BearerToken> getAccessToken(CancellationToken cancellationToken)
		{
			return Task.FromResult(token);
		}

		return getAccessToken;
	}

	#endregion
}
