// Authored by Stas Sultanov
// Copyright © Stas Sultanov

namespace Stas.PowerPlatform.Demo;

using Azure.Monitor.Telemetry;

using Stas.PowerPlatform;

/// <inheritdoc/>
public sealed class DemoPlugin : Plugin<PluginContext>
{
	#region Constants

	private const String configurationEnvironmentVariableKeyName = "[INSERT ENVIRONMENT VARIABLE NAME]";

	#endregion

	#region Methods: Implementation of Plugin

	/// <inheritdoc/>
	protected override PluginContext InitializeContext
	(
		IServiceProvider serviceProvider
	)
	{
		// create plugin context
		var context = new PluginContext(serviceProvider, configurationEnvironmentVariableKeyName);

		return context;
	}

	/// <inheritdoc/>
	protected override void Execute
	(
		PluginContext pluginContext
	)
	{
		// imitate some work
		Thread.Sleep(200);

		// track trace telemetry
		// the trace will be linked to parent request activity because it is track within the scope
		pluginContext.TelemetryClient.TrackTrace("Some work done", SeverityLevel.Information);

		// imitate some other work
		Thread.Sleep(100);
	}

	#endregion
}
