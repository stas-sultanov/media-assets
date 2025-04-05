// Created by Stas Sultanov.
// Copyright © Stas Sultanov.

namespace Stas.PowerPlatformDemo.Plugins;

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Azure.Monitor.Telemetry;

using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.PluginTelemetry;

using Stas.PowerPlatformDemo.Configuration;

/// <summary>
/// Represents an abstract base class for plugins.
/// </summary>
/// <param name="configurationEnvironmentVariableKeyName">The name of the key of environment variable that contains configuration of the plugin.</param>
public abstract partial class Plugin<PluginConfigurationType>
(
	String configurationEnvironmentVariableKeyName
)
	: IPlugin
	where PluginConfigurationType : PluginConfiguration
{
	#region Constants

	/// <summary>
	/// Error message for work execution failures.
	/// </summary>
	private const String workExecutionErrorMessage = @"Error during work execution.";

	#endregion

	#region Fields

	/// <summary>
	/// The name of the key of environment variable that contains configuration of the plugin
	/// </summary>
	private readonly String configurationEnvironmentVariableKeyName = configurationEnvironmentVariableKeyName;

	#endregion

	#region Methods: Implementation of IPlugin

	/// <inheritdoc/>
	public void Execute(IServiceProvider serviceProvider)
	{
		// get start timestamp
		// we use the stopwatch to get the start time in ticks, this is used to calculate the duration of the request
		var startTimestamp = Stopwatch.GetTimestamp();

		// get start time
		// we use the UTC time to record time when the Execute method has been invoked
		var startTime = DateTime.UtcNow;

		// create plugin context
		using var pluginContext = new PluginContext<PluginConfigurationType>(serviceProvider, configurationEnvironmentVariableKeyName);

		// generate request id
		// we use Guid to align with the request id format used by the Power Platform
		var requestId = Guid.NewGuid().ToString();

		// begin operation scope
		// all subsequent telemetry events are associated with the execute request
		pluginContext.TelemetryClient.ActivityScopeBegin(requestId, out var operation);

		var success = false;

		try
		{
			// call overloaded method to execute the plugin logic
			Execute(pluginContext);

			// set success to true
			success = true;
		}
		catch (Exception exception)
		{
			// track exception telemetry
			// the exception will be associated with the request telemetry
			pluginContext.TelemetryClient.TrackException(exception);

			// just in case log the exception using native method
			pluginContext.Logger.Log(LogLevel.Critical, exception, workExecutionErrorMessage);

			// log the exception using tracing service
			pluginContext.TracingService.Trace("{0}.Execute {1} Message {2}", GetType().Name, exception.GetType().Name, exception.Message);

			// check exception type and rethrow if it is InvalidPluginExecutionException
			if (exception is InvalidPluginExecutionException)
			{
				throw;
			}

			// wrap the exception into InvalidPluginExecutionException
			throw new InvalidPluginExecutionException(workExecutionErrorMessage, exception);
		}
		finally
		{
			// end operation scope
			// we get duration from the start timestamp and the current timestamp
			pluginContext.TelemetryClient.ActivityScopeEnd(operation, startTimestamp, out var duration);

			// create tags
			// the tags will be included into the Request telemetry
			var tags = new[]
			{
				// we use the user id to understand who made the call
				new KeyValuePair<String, String>
				(
					TelemetryTagKeys.UserAuthUserId,
					pluginContext.PluginExecutionContext.InitiatingUserAzureActiveDirectoryObjectId.ToString()
				)
			};

			var responseCode = success ? "0" : "1";

			// track request telemetry
			// we need provide an URI to identify the request, we may use the plugin name
			pluginContext.TelemetryClient.TrackRequest(startTime, duration, requestId, new Uri($"plugin:run"), responseCode, success, nameof(Execute), tags: tags);

			// publish all collected telemetry
			pluginContext.TelemetryClient.PublishAsync().Wait();
		}
	}

	#endregion

	#region Methods: Abstract

	/// <summary>
	/// Executes the plugin logic within the provided context.
	/// </summary>
	/// <param name="pluginContext">The context.</param>
	/// <exception cref="InvalidPluginExecutionException">Thrown when an error occurs during the execution.</exception>
	protected abstract void Execute(PluginContext<PluginConfigurationType> pluginContext);

	#endregion
}
