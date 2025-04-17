// Created by Stas Sultanov.
// Copyright © Stas Sultanov.

namespace Stas.PowerPlatform.DemoTests;

using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.PluginTelemetry;

using Moq;

using Stas.PowerPlatform;

/// <summary>
/// Unit tests for the <see cref="PluginContext{PluginConfigurationType}"/> class.
/// </summary>
[TestCategory("UnitTests")]
[TestClass]
public sealed class PluginContextTest
{
	#region Methods Tests

	[TestMethod]
	public void Constructor_ShouldInitializeProperties_WhenServiceProviderIsValid()
	{
		var environmentMock = new PowerPlatformEnvironmentMock();

		var pluginContext = new PluginContext(environmentMock!.mock_ServiceProvider.Object, PowerPlatformEnvironmentMock.configurationKey);

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
		var environmentMock = new PowerPlatformEnvironmentMock();

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		_ = environmentMock.mock_ServiceProvider.Setup(sp => sp.GetService(typeof(ILogger))).Returns(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		_ = Assert.ThrowsExactly<InvalidPluginExecutionException>
		(
			() => _ = new PluginContext(environmentMock.mock_ServiceProvider.Object, String.Empty)
		);
	}

	[TestMethod]
	public void CreateOrganizationService_ShouldThrowException_WhenServiceNotCreated()
	{
		var environmentMock = new PowerPlatformEnvironmentMock();

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
		_ = environmentMock.mock_OrganizationServiceFactory.Setup(factory => factory.CreateOrganizationService(It.IsAny<Guid>())).Returns((IOrganizationService) null);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		_ = Assert.ThrowsExactly<InvalidPluginExecutionException>
		(
			() => _ = new PluginContext(environmentMock.mock_ServiceProvider.Object, String.Empty)
		);
	}

	#endregion
}
