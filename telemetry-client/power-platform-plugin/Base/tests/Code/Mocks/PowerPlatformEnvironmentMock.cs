// Created by Stas Sultanov.
// Copyright © Stas Sultanov.

namespace Stas.PowerPlatform.DemoTests;

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
internal sealed class PowerPlatformEnvironmentMock
{
	#region constants

	internal const String configurationAsString = @"
	{
		""TelemetryClient"":
		{
			""Publishers"":
			[
				{
					""Authenticate"": true,
					""IngestionEndpoint"": ""https://dc.in.applicationinsights.azure.com/"",
					""InstrumentationKey"": ""11111111-1111-1111-1111-111111111111"",
					""ManagedIdentityId"": ""b2719b6e-8f3b-4c9b-9a2e-4f5b5e5f5e5f"",
					""Tags"":
					{
						""Environment"": ""Production"",
						""Region"": ""US""
					}
				},
				{
					""Authenticate"": true,
					""IngestionEndpoint"": ""https://dc.in.applicationinsights.azure.com/"",
					""InstrumentationKey"": ""22222222-2222-2222-2222-222222222222"",
					""Tags"":
					{
					}
				},
				{
					""IngestionEndpoint"": ""https://dc.in.applicationinsights.azure.com/"",
					""InstrumentationKey"": ""33333333-3333-3333-3333-333333333333""
				}
			],
			""Tags"":
			{
				""Application"": ""MyApp"",
				""Version"": ""1.0.0""
			}
		}
	}";

	internal const String configurationKey = "Stas_TestPlugin";

	#endregion

	#region Data

	internal readonly String managedIdentityTokenValue = "bebbebeebeb";

	internal readonly Guid contextInitiatingUserId = Guid.NewGuid();
	internal readonly Guid contextUserId = Guid.NewGuid();

	internal readonly Mock<IServiceProvider> mock_ServiceProvider;
	internal readonly Mock<ILogger> mock_Logger;
	internal readonly Mock<IManagedIdentityService> mock_ManagedIdentityService;
	internal readonly Mock<IOrganizationServiceFactory> mock_OrganizationServiceFactory;
	internal readonly Mock<IPluginExecutionContext7> mock_PluginExecutionContext;
	internal readonly Mock<ITracingService> mock_TracingService;
	internal readonly Mock<IOrganizationService> mock_OrganizationService_InitiatingUser;
	internal readonly Mock<IOrganizationService> mock_OrganizationService_User;

	#endregion

	public PowerPlatformEnvironmentMock()
	{
		mock_ServiceProvider = new Mock<IServiceProvider>();
		mock_Logger = new Mock<ILogger>();
		mock_ManagedIdentityService = new Mock<IManagedIdentityService>();
		mock_OrganizationServiceFactory = new Mock<IOrganizationServiceFactory>();
		mock_PluginExecutionContext = new Mock<IPluginExecutionContext7>();
		mock_TracingService = new Mock<ITracingService>();
		mock_OrganizationService_InitiatingUser = new Mock<IOrganizationService>();
		mock_OrganizationService_User = new Mock<IOrganizationService>();

		_ = mock_ServiceProvider.Setup(serviceProvider => serviceProvider.GetService(typeof(ILogger))).Returns(mock_Logger.Object);
		_ = mock_ServiceProvider.Setup(serviceProvider => serviceProvider.GetService(typeof(IManagedIdentityService))).Returns(mock_ManagedIdentityService.Object);
		_ = mock_ServiceProvider.Setup(serviceProvider => serviceProvider.GetService(typeof(IOrganizationServiceFactory))).Returns(mock_OrganizationServiceFactory.Object);
		_ = mock_ServiceProvider.Setup(serviceProvider => serviceProvider.GetService(typeof(IPluginExecutionContext7))).Returns(mock_PluginExecutionContext.Object);
		_ = mock_ServiceProvider.Setup(serviceProvider => serviceProvider.GetService(typeof(ITracingService))).Returns(mock_TracingService.Object);

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
						["v.value"] = new AliasedValue(null, "value", configurationAsString)
					},
				])
			);
	}
}
