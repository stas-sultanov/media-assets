// Authored by Stas Sultanov
// Copyright © Stas Sultanov

namespace Stas.PowerPlatformTests;

using System;

using Azure.Core;
using Azure.Identity;

using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.PluginTelemetry;
using Microsoft.Xrm.Sdk.Query;

using Moq;

/// <summary>
/// Provides a mock of Power Platform Environment for testing purposes.
/// </summary>
public sealed class PowerPlatformEnvironmentMock
{
	#region Data

	internal readonly String managedIdentityTokenValue = "BH5BHYB45BU9G5B9GBUG54BUB59GU54GBU9G4BUBC3B394BUC43BUC43B9UB943CBUC";

	internal readonly Guid contextInitiatingUserId = Guid.NewGuid();
	internal readonly Guid contextUserId = Guid.NewGuid();
	internal readonly Mock<ILogger> mock_Logger;
	internal readonly Mock<IManagedIdentityService> mock_ManagedIdentityService;
	internal readonly Mock<IOrganizationServiceFactory> mock_OrganizationServiceFactory;
	internal readonly Mock<IPluginExecutionContext7> mock_PluginExecutionContext;
	internal readonly Mock<ITracingService> mock_TracingService;
	internal readonly Mock<IOrganizationService> mock_OrganizationService_InitiatingUser;
	internal readonly Mock<IOrganizationService> mock_OrganizationService_User;

	public Mock<IServiceProvider> ServiceProvider { get; }

	#endregion

	public PowerPlatformEnvironmentMock
	(
		String configurationKey,
		String configurationValue
	)
	{
		ServiceProvider = new Mock<IServiceProvider>();
		mock_Logger = new Mock<ILogger>();
		mock_ManagedIdentityService = new Mock<IManagedIdentityService>();
		mock_OrganizationServiceFactory = new Mock<IOrganizationServiceFactory>();
		mock_PluginExecutionContext = new Mock<IPluginExecutionContext7>();
		mock_TracingService = new Mock<ITracingService>();
		mock_OrganizationService_InitiatingUser = new Mock<IOrganizationService>();
		mock_OrganizationService_User = new Mock<IOrganizationService>();

		_ = ServiceProvider.Setup(serviceProvider => serviceProvider.GetService(typeof(ILogger))).Returns(mock_Logger.Object);
		_ = ServiceProvider.Setup(serviceProvider => serviceProvider.GetService(typeof(IManagedIdentityService))).Returns(mock_ManagedIdentityService.Object);
		_ = ServiceProvider.Setup(serviceProvider => serviceProvider.GetService(typeof(IOrganizationServiceFactory))).Returns(mock_OrganizationServiceFactory.Object);
		_ = ServiceProvider.Setup(serviceProvider => serviceProvider.GetService(typeof(IPluginExecutionContext7))).Returns(mock_PluginExecutionContext.Object);
		_ = ServiceProvider.Setup(serviceProvider => serviceProvider.GetService(typeof(ITracingService))).Returns(mock_TracingService.Object);

		_ = mock_ManagedIdentityService.Setup(m => m.AcquireToken(It.IsAny<IList<String>>())).Returns<IList<String>>
		(
			(scopes) =>
			{
				var tokenCredential = new DefaultAzureCredential();

				var context = new TokenRequestContext([.. scopes]);

				var token = tokenCredential.GetToken(context);

				return token.Token;
			}
		);

		_ = mock_PluginExecutionContext.Setup(context => context.CorrelationId).Returns(Guid.NewGuid());
		_ = mock_PluginExecutionContext.Setup(context => context.OperationId).Returns(Guid.NewGuid());
		_ = mock_PluginExecutionContext.SetupGet(context => context.InitiatingUserId).Returns(contextInitiatingUserId);
		_ = mock_PluginExecutionContext.SetupGet(context => context.UserId).Returns(contextUserId);

		_ = mock_OrganizationServiceFactory.Setup(factory => factory.CreateOrganizationService(It.IsAny<Guid>())).Returns<Guid>
		(
			(id) =>
			{
				if (id == contextInitiatingUserId)
				{
					return mock_OrganizationService_InitiatingUser.Object;
				}
				else if (id == contextUserId)
				{
					return mock_OrganizationService_User.Object;
				}
				else
				{
					throw new ArgumentException("Invalid user ID");
				}
			}
		);

		_ = mock_OrganizationService_User
			.Setup(service => service.RetrieveMultiple(It.IsAny<QueryExpression>()))
			.Returns(() => new EntityCollection
			(
				[
					new Entity("environmentvariabledefinition")
					{
						["schemaname"] = configurationKey,
						["defaultvalue"] = null,
						["v.value"] = new AliasedValue(null, "value", configurationValue)
					},
				])
			);
	}
}
