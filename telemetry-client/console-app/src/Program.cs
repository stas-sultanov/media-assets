// Created by Stas Sultanov.
// Copyright © Stas Sultanov.

using System.Diagnostics;
using System.Net.Http;

using Azure.Monitor.Telemetry;
using Azure.Monitor.Telemetry.Publish;

internal sealed partial class Program : IDisposable
{
	private const String appInsightsIngestionUrl = "https://northeurope-2.in.applicationinsights.azure.com/";
	private const String appInsightsInstrumentationKey = "c22cc09b-d18c-464c-9c65-8059764f9f50";

	#region Static

	private static readonly Uri proccessRequestUri = new(@"exe:process");

	private static readonly IReadOnlyList<KeyValuePair<String, String>> processRequestTags =
	[
		new (TelemetryTagKeys.OperationName, @"SampleRequest")
	];

	private static String GetActivityId()
	{
		return Guid.NewGuid().ToString("N");
	}

	private static Random random = new Random();

	public static string RandomString(int length)
	{
		const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
		return new string(Enumerable.Repeat(chars, length)
			.Select(s => s[random.Next(s.Length)]).ToArray());
	}

	#endregion

	#region Fields

	private readonly TelemetryClient telemetryClient;

	private readonly HttpClient telemetryPublisherHttpClient;

	#endregion

	#region Constructors

	public Program()
	{
		// create an HTTP client for the telemetry publisher
		telemetryPublisherHttpClient = new HttpClient();

		// create a telemetry publisher with the specified ingestion endpoint and instrumentation key
		var telemetryPublisher = new HttpTelemetryPublisher
		(
			telemetryPublisherHttpClient,
			new Uri(appInsightsIngestionUrl),
			new Guid(appInsightsInstrumentationKey)
		);

		// create a telemetry client with the telemetry publisher
		telemetryClient = new TelemetryClient(telemetryPublisher)
		{
			// set initial context
			// this tags should be sent with each telemetry item
			Context = new()
			{
				CloudRole = "local",
				CloudRoleInstance = Environment.MachineName
			}
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

	#region Methods

	public async Task Run(CancellationToken cancellationToken)
	{
		telemetryClient.TrackPageView
		(
			DateTime.UtcNow,
			TimeSpan.FromMilliseconds(1),
			RandomString(512),
			"SamplePageView",
			new Uri("https://gostas.dev")
		);

		// sample process request
		//await ProccessRequest(cancellationToken);

		// publish telemetry
		_ = await telemetryClient.PublishAsync(cancellationToken);
	}

	private async Task ProccessRequest(CancellationToken cancellationToken)
	{
		// because this is a new request, we should add OperationId to the context
		telemetryClient.Context = telemetryClient.Context with
		{
			OperationId = Guid.NewGuid().ToString("N"),
		};

		// begin a request activity scope and get original context
		telemetryClient.ActivityScopeBegin(GetActivityId, out var time, out var timestamp, out var activityId, out var context);

		// simulate work
		await Task.Delay(100, cancellationToken);

		// process internal request
		var success = await ProccessSampleInProc(cancellationToken);

		// simulate work
		await Task.Delay(150, cancellationToken);

		// end the request activity scope and put the context back
		telemetryClient.ActivityScopeEnd(context, timestamp, out var duration);

		// track the request
		var responseCode = success ? "0" : "1";

		// because all telemetry will have OperationId set
		// we can add OperationName only to the request
		telemetryClient.TrackRequest
		(
			time,
			duration,
			activityId,
			proccessRequestUri,
			responseCode,
			success,
			tags: processRequestTags
		);
	}

	private async Task<Boolean> ProccessSampleInProc(CancellationToken cancellationToken)
	{
		// begin in-proc dependency activity
		telemetryClient.ActivityScopeBegin(GetActivityId, out var time, out var timestamp, out var activityId, out var context);

		// simulate work
		await Task.Delay(150, cancellationToken);

		// track sample Event
		// the Event will have parent operation id set to the top activityId
		telemetryClient.TrackEvent("In Proc Started");

		// simulate work
		await Task.Delay(200, cancellationToken);

		// track sample Trace
		// the Trace will have parent operation id set to the top activityId
		telemetryClient.TrackTrace("In Proc Completed", SeverityLevel.Information);

		// end the request activity scope and put the context back
		telemetryClient.ActivityScopeEnd(context, timestamp, out var duration);

		// track the in-proc dependency
		telemetryClient.TrackDependencyInProc(time, duration, activityId, "Sample", true);

		return true;
	}

	#endregion
}
