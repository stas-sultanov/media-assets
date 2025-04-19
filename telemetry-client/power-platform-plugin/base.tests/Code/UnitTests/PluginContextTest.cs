// Authored by Stas Sultanov
// Copyright © Stas Sultanov

namespace Stas.PowerPlatformTests;

using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.PluginTelemetry;

using Moq;

using Stas.PowerPlatform;

/// <summary>
/// Unit tests for the <see cref="PluginContext"/> class.
/// </summary>
[TestCategory("UnitTests")]
[TestClass]
public sealed class PluginContextTest
{
	#region Constants

	private const String ConfigurationValue = @"
	{
		""TelemetryClient"":
		{
			""Publishers"":
			[
				{
					""Authenticate"": true,
					""IngestionEndpoint"": ""https://dc.in.applicationinsights.azure.com/"",
					""InstrumentationKey"": ""11111111-1111-1111-1111-111111111111"",
					""ManagedIdentityId"": ""b2719b6e-8f3b-4c9b-9a2e-4f5b5e5f5e5f""
				},
				{
					""Authenticate"": true,
					""IngestionEndpoint"": ""https://dc.in.applicationinsights.azure.com/"",
					""InstrumentationKey"": ""22222222-2222-2222-2222-222222222222""
				},
				{
					""IngestionEndpoint"": ""https://dc.in.applicationinsights.azure.com/"",
					""InstrumentationKey"": ""33333333-3333-3333-3333-333333333333""
				}
			]
		}
	}";

	private const String ConfigurationKey = "TestPlugin";

	#endregion

	#region Methods Tests

	[TestMethod]
	public void Constructor_ShouldInitializeProperties_WhenServiceProviderIsValid()
	{
		var environmentMock = new PowerPlatformEnvironmentMock(ConfigurationKey, ConfigurationValue);

		var pluginContext = new PluginContext(environmentMock!.ServiceProvider.Object, ConfigurationKey);

		Assert.AreEqual(environmentMock.mock_Logger.Object, pluginContext.Logger);
		Assert.AreEqual(environmentMock.mock_ManagedIdentityService.Object, pluginContext.ManagedIdentityService);
		Assert.AreEqual(environmentMock.mock_OrganizationServiceFactory.Object, pluginContext.OrganizationServiceFactory);
		Assert.AreEqual(environmentMock.mock_PluginExecutionContext.Object, pluginContext.PluginExecutionContext);
		Assert.AreEqual(environmentMock.mock_TracingService.Object, pluginContext.TracingService);
		Assert.AreEqual(environmentMock.mock_OrganizationService_InitiatingUser.Object, pluginContext.OrganizationService_InitiatingUser);
		Assert.AreEqual(environmentMock.mock_OrganizationService_User.Object, pluginContext.OrganizationService_User);
	}

	[TestMethod]
	public void GetService_ShouldThrowException_WhenServiceNotFound()
	{
		var environmentMock = new PowerPlatformEnvironmentMock(ConfigurationKey, ConfigurationValue);

		_ = environmentMock.ServiceProvider.Setup(sp => sp.GetService(typeof(ILogger))).Returns(null!);

		_ = Assert.ThrowsExactly<InvalidPluginExecutionException>
		(
			() => _ = new PluginContext(environmentMock.ServiceProvider.Object, String.Empty)
		);
	}

	[TestMethod]
	public void CreateOrganizationService_ShouldThrowException_WhenServiceNotCreated()
	{
		var environmentMock = new PowerPlatformEnvironmentMock(ConfigurationKey, ConfigurationValue);

		_ = environmentMock.mock_OrganizationServiceFactory.Setup(factory => factory.CreateOrganizationService(It.IsAny<Guid>())).Returns((IOrganizationService) null!);

		_ = Assert.ThrowsExactly<InvalidPluginExecutionException>
		(
			() => _ = new PluginContext(environmentMock.ServiceProvider.Object, String.Empty)
		);
	}

	#endregion
}
