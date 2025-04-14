// Created by Stas Sultanov.
// Copyright © Stas Sultanov.

namespace Stas.PowerPlatformDemo.Plugins;

using Azure.Monitor.Telemetry;

/// <inheritdoc/>
public sealed class DemoPlugin : Plugin<PluginConfiguration>
{
	#region Constructor

	/// <inheritdoc/>
	public DemoPlugin()
	// TODO: provide here a name of the environment variable that holds configuration
	: base(@"gostas_DemoPlugin")
	{
	}

	#endregion

	#region Methods: Implementation of Plugin

	/// <inheritdoc/>
	protected override void Execute(PluginContext<PluginConfiguration> pluginContext)
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
