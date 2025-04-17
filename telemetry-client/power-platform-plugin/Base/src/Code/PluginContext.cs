// Created by Stas Sultanov.
// Copyright © Stas Sultanov.

namespace Stas.PowerPlatform;

using System;
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

/// <summary>
/// Provides a collection of essential services for plugin execution in the Power Platform environment.
/// This class encapsulates common services and contexts required for plugin operations.
/// </summary>
public class PluginContext : IDisposable
{
	const String TelemetryClientConfigurationKeyName = "TelemetryClient";

	#region Static Fields

	private static readonly String[] telemetryPublisherAuthorizationScopes = [HttpTelemetryPublisher.AuthorizationScope];

	#endregion

	#region Fields

	/// <summary>
	/// The <see cref="HttpClient"/> instance used by telemetry publishers.
	/// </summary>
	private readonly HttpClient telemetryPublisherHttpClient;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes a new instance of the <see cref="PluginContext"/> class.
	/// </summary>
	/// <param name="serviceProvider">The service provider.</param>
	/// <param name="environmentVariablesConfigName">The name of the environment variable containing configuration settings.</param>
	public PluginContext
	(
		IServiceProvider serviceProvider,
		String environmentVariablesConfigName
	)
	{
		// retrieve required services
		Logger = GetService<ILogger>(serviceProvider);
		OrganizationServiceFactory = GetService<IOrganizationServiceFactory>(serviceProvider);
		PluginExecutionContext = GetService<IPluginExecutionContext7>(serviceProvider);
		ManagedIdentityService = GetService<IManagedIdentityService>(serviceProvider);
		TracingService = GetService<ITracingService>(serviceProvider);

		// create organization services
		OrganizationService_InitiatingUser = CreateOrganizationService(OrganizationServiceFactory, PluginExecutionContext.InitiatingUserId);
		OrganizationService_User = CreateOrganizationService(OrganizationServiceFactory, PluginExecutionContext.UserId);

		// retrieve configuration
		var configurationAsString = OrganizationService_User.GetEnvironmentVariable(environmentVariablesConfigName)
			?? throw new InvalidPluginExecutionException($"Environment variable '{environmentVariablesConfigName}' is not found.");

		// deserialize configuration
		using var config = JsonSerializer.Deserialize<JsonDocument>(configurationAsString);

		if (config == null)
		{
			throw new InvalidPluginExecutionException($"Cannot deserialize configuration from the environment variable '{environmentVariablesConfigName}'.");
		}

		Configuration = config.RootElement;

		if (!Configuration.TryGetProperty(TelemetryClientConfigurationKeyName, out var telemetryClientConfigurationAsJsonElement))
		{
			throw new InvalidPluginExecutionException($"Cannot get proptery from configuration '{TelemetryClientConfigurationKeyName}'.");
		}

		var telemetryClientConfiguration = telemetryClientConfigurationAsJsonElement.Deserialize<TelemetryClientConfiguration>();

		if (telemetryClientConfiguration == null)
		{
			throw new InvalidPluginExecutionException($"Cannot deserialize configuration 'TelemetryClient'.");
		}

		// initialize HTTP client for telemetry publishers
		telemetryPublisherHttpClient = new HttpClient();

		// Create telemetry tags
		// the Power Platform plugins are designed to handle only one operation type
		// thus we can set OperationId and OperationParentId tags here
		var tags = new TelemetryTags(telemetryClientConfiguration.Tags)
		{
			CloudRole = "PowerPlatform",
			CloudRoleInstance = Environment.MachineName,

			OperationId = PluginExecutionContext.CorrelationId.ToString(),
			OperationParentId = PluginExecutionContext.OperationId.ToString()
		};

		// create telemetry client
		TelemetryClient = TelemetryClientFactory.CreateTelemetryClient
		(
			telemetryClientConfiguration.Publishers,
			telemetryPublisherHttpClient,
			GetGetAccessToken,
			tags
		);
	}

	#endregion

	#region Properties

	/// <summary>
	/// The plugin configuration settings.
	/// </summary>
	protected JsonElement Configuration { get; }

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

	/// <summary>
	/// A flag that indicates whether instance is disposed.
	/// </summary>
	public Boolean IsDisposed { get; private set; }

	#endregion

	#region Methods: Implementation of IDisposable

	/// <inheritdoc/>
	public void Dispose()
	{
		// Release the resources
		Dispose(true);

		// Request system not to call the finalize method for a specified object
		GC.SuppressFinalize(this);
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
	private static IOrganizationService CreateOrganizationService
	(
		IOrganizationServiceFactory organizationServiceFactory,
		Guid userId
	)
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
	private static T GetService<T>
	(
		IServiceProvider serviceProvider
	)
	{
		var result = serviceProvider.Get<T>() ?? throw new InvalidPluginExecutionException($"Cannot get instance of {typeof(T).FullName} type from the {nameof(serviceProvider)}.");

		return result;
	}

	/// <summary>
	/// Retrieves a delegate for obtaining an access token for the specified managed identity.
	/// </summary>
	/// <param name="managedIdentityId">The ID of the managed identity.</param>
	/// <returns>A delegate that retrieves an access token.</returns>
	private Func<CancellationToken, Task<BearerToken>> GetGetAccessToken
	(
		Guid? managedIdentityId
	)
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

	#region Methods: Protected

	/// <summary>
	/// Releases resources associated with the instance.
	/// </summary>
	/// <param name="fromDispose">Value indicating whether method was called from the <see cref="Dispose()" /> method.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected virtual void Dispose
	(
		Boolean fromDispose
	)
	{
		// Check if object is disposed already
		if (IsDisposed)
		{
			return;
		}

		// Check if the call is from Dispose() method
		if (fromDispose)
		{
			telemetryPublisherHttpClient.Dispose();
		}

		// Set disposed
		IsDisposed = true;
	}

	#endregion
}
