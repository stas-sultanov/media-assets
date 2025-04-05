// Created by Stas Sultanov.
// Copyright © Stas Sultanov.

namespace Stas.PowerPlatformDemo.Plugins;

using Azure.Monitor.Telemetry;

using Stas.PowerPlatformDemo.Configuration;

/// <inheritdoc/>
public sealed class DemoPlugin : Plugin<PluginConfiguration>
{
	#region Constructor

	/// <inheritdoc/>
	public DemoPlugin() : base(@"Stas_Demo")
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
