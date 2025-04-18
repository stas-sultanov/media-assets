// Authored by Stas Sultanov
// Copyright © Stas Sultanov

namespace Stas.PowerPlatformTests;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

using Moq;

using Stas.PowerPlatform;

/// <summary>
/// Unit tests for the <see cref="OrganizationServiceExtensions"/> class.
/// </summary>
[TestCategory("UnitTests")]
[TestClass]
public sealed class ExtensionsTest
{
	#region Methods: Tests - TryGetEnvironmentVariable

	[TestMethod]
	public void TryGetEnvironmentVariable_ShouldReturnValue_WhenVariableFoundWithValue()
	{
		var mockOrganizationService = new Mock<IOrganizationService>();

		// arrange
		var name = "TestVariable";
		var aliasedValue = new AliasedValue("v", "value", "TestValue");
		var entity = new Entity("environmentvariabledefinition")
		{
			["v.value"] = aliasedValue
		};
		var entityCollection = new EntityCollection { Entities = { entity } };

		_ = mockOrganizationService
			.Setup(service => service.RetrieveMultiple(It.IsAny<QueryExpression>()))
			.Returns(entityCollection);

		// act
		var success = mockOrganizationService.Object.TryGetEnvironmentVariable(name, out var result);

		// assert
		Assert.IsTrue(success);
		Assert.AreEqual("TestValue", result);
	}

	[TestMethod]
	public void GetEnvironmentVariable_ShouldReturnDefaultValue_WhenVariableFoundWithDefaultValue()
	{
		var mockOrganizationService = new Mock<IOrganizationService>();

		// arrange
		var name = "TestVariable";
		var entity = new Entity("environmentvariabledefinition")
		{
			["defaultvalue"] = "DefaultValue"
		};
		var entityCollection = new EntityCollection { Entities = { entity } };

		_ = mockOrganizationService
			.Setup(service => service.RetrieveMultiple(It.IsAny<QueryExpression>()))
			.Returns(entityCollection);

		// act
		var success = mockOrganizationService.Object.TryGetEnvironmentVariable(name, out var result);

		// assert
		Assert.IsTrue(success);
		Assert.AreEqual("DefaultValue", result);
	}

	[TestMethod]
	public void TryGetEnvironmentVariable_ShouldReturnFalse_WhenVariableNotFound()
	{
		var mockOrganizationService = new Mock<IOrganizationService>();

		// arrange
		var name = "NonExistentVariable";
		var entityCollection = new EntityCollection(); // No entities

		_ = mockOrganizationService
			.Setup(service => service.RetrieveMultiple(It.IsAny<QueryExpression>()))
			.Returns(entityCollection);

		// act
		var success = mockOrganizationService.Object.TryGetEnvironmentVariable(name, out var result);

		// assert
		Assert.IsFalse(success);
		Assert.IsNull(result);
	}

	#endregion
}
