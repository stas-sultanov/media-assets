// Authored by Stas Sultanov
// Copyright © Stas Sultanov

using Azure.Monitor.Telemetry;
using Azure.Monitor.Telemetry.Dependency;
using Azure.Monitor.Telemetry.Publish;

internal sealed partial class Program : IDisposable
{
	private const String appInsightsIngestionUrl = "[INSERT THE INGESTION ENDPOINT HERE]";
	private const String appInsightsInstrumentationKey = "[INSERT THE INSTRUMENTATION KEY HERE]";
	private const String sampleDependencyUri = "https://www.google.com/"; // change if needed

	#region Static

	private static readonly Uri processRequestUri = new(@"exe:process");

	private static readonly IReadOnlyList<KeyValuePair<String, String>> processRequestTags =
	[
		new (TelemetryTagKeys.OperationName, @"SampleRequest")
	];

	/// <summary>
	/// The helper function that generates activity ids.
	/// </summary>
	/// <returns></returns>
	private static String GetActivityId()
	{
		return Guid.NewGuid().ToString("N");
	}

	#endregion

	#region Fields

	private readonly HttpClient sampleDependencyCallHttpClient;

	private readonly TelemetryClient telemetryClient;

	private readonly HttpClient telemetryPublisherHttpClient;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes a new instance of the <see cref="Program"/> class.
	/// </summary>
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

		// create initial telemetry context
		// this tags will be sent with each telemetry item
		var telemetryContext = new TelemetryTags()
		{
			CloudRole = "local",
			CloudRoleInstance = Environment.MachineName
		};

		// initialize a telemetry client
		telemetryClient = new TelemetryClient(telemetryPublisher, telemetryContext);

		// create a telemetry tracked HTTP client handler
		// the handler will be disposed by the HTTP client
		var handler = new TelemetryTrackedHttpClientHandler(telemetryClient, GetActivityId);

		// initialize a sample dependency call HTTP client
		sampleDependencyCallHttpClient = new HttpClient(handler);
	}

	#endregion

	#region Methods: Implementation of IDisposable

	/// <inheritdoc/>
	public void Dispose()
	{
		sampleDependencyCallHttpClient?.Dispose();

		telemetryPublisherHttpClient?.Dispose();
	}

	#endregion

	#region Methods

	public async Task<Int32> Run(CancellationToken cancellationToken)
	{
		// sample process request
		var success = await ProcessRequest(cancellationToken);

		// publish telemetry
		_ = await telemetryClient.PublishAsync(cancellationToken);

		return success ? 0 : -1;
	}

	private async Task<Boolean> ProcessRequest(CancellationToken cancellationToken)
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
		var success = await ProcessSampleInProc(cancellationToken);

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
			processRequestUri,
			responseCode,
			success,
			tags: processRequestTags
		);

		return success;
	}

	private async Task<Boolean> ProcessSampleInProc(CancellationToken cancellationToken)
	{
		// begin in-proc dependency activity
		telemetryClient.ActivityScopeBegin(GetActivityId, out var time, out var timestamp, out var activityId, out var context);

		// simulate work
		await Task.Delay(150, cancellationToken);

		// track sample Event
		// the Event will have parent operation id set to the top activityId
		telemetryClient.TrackEvent("In Proc Started");

		// make a dependency call
		// the telemetry of this dependency call will be captured by the instance of TelemetryTrackedHttpClientHandler
		var sampleDependencyCallResult = await sampleDependencyCallHttpClient.GetAsync(sampleDependencyUri);

		var success = sampleDependencyCallResult.IsSuccessStatusCode;

		// track sample Trace
		// the Trace will have parent operation id set to the top activityId
		telemetryClient.TrackTrace("In Proc Completed", SeverityLevel.Information);

		// end the request activity scope and put the context back
		telemetryClient.ActivityScopeEnd(context, timestamp, out var duration);

		// track the in-proc dependency
		telemetryClient.TrackDependencyInProc(time, duration, activityId, "Sample", success);

		return success;
	}

	#endregion
}
