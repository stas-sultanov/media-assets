// Authored by Stas Sultanov
// Copyright © Stas Sultanov

namespace Stas.PowerPlatform;

using System;

using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

/// <summary>
/// The extension methods for <see cref="IOrganizationService"/> interface.
/// </summary>
public static class OrganizationServiceExtensions
{
	#region Static Fields

	private static readonly ColumnSet environmentVariableColumnSet = new
	(
		"defaultvalue",
		"environmentvariabledefinitionid",
		"schemaname"
	);

	private static readonly LinkEntity environmentVariableLinkEntity = new()
	{
		JoinOperator = JoinOperator.LeftOuter,
		LinkFromEntityName = "environmentvariabledefinition",
		LinkFromAttributeName = "environmentvariabledefinitionid",
		LinkToEntityName = "environmentvariablevalue",
		LinkToAttributeName = "environmentvariabledefinitionid",
		Columns = new ColumnSet("value"),
		EntityAlias = "v"
	};

	#endregion

	#region Methods

	/// <summary>
	/// Attempts to retrieve the value of an environment variable from the organization service.
	/// </summary>
	/// <param name="organizationService">The <see cref="IOrganizationService"/> instance used to query the environment variable.</param>
	/// <param name="name">The name of the environment variable to retrieve.</param>
	/// <param name="value">Contains the value of the environment variable if found; otherwise, <c>null</c>.</param>
	/// <returns><c>true</c> if the environment variable was found and its value retrieved; otherwise, <c>false</c>.</returns>
	public static Boolean TryGetEnvironmentVariable
	(
		this IOrganizationService organizationService,
		String name,
		out String? value
	)
	{
		// create query
		var query = new QueryExpression("environmentvariabledefinition")
		{
			ColumnSet = environmentVariableColumnSet,
			LinkEntities = { environmentVariableLinkEntity },
			Criteria = new FilterExpression(LogicalOperator.And)
			{
				Conditions =
				{
					new ConditionExpression("schemaname", ConditionOperator.Equal, name)
				}
			}
		};

		// execute query
		var queryResults = organizationService.RetrieveMultiple(query);

		// get entity
		var entity = queryResults.Entities.FirstOrDefault();

		if (entity == null)
		{
			value = null;

			return false;
		}

		// get value
		value = (entity.GetAttributeValue<AliasedValue>("v.value")?.Value?.ToString())
			?? entity.GetAttributeValue<String>("defaultvalue");

		return true;
	}

	#endregion
}
