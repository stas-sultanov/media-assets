// Created by Stas Sultanov.
// Copyright © Stas Sultanov.

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
		@"defaultvalue",
		@"environmentvariabledefinitionid",
		@"schemaname"
	);

	private static readonly LinkEntity environmentVariableLinkEntity = new()
	{
		JoinOperator = JoinOperator.LeftOuter,
		LinkFromEntityName = @"environmentvariabledefinition",
		LinkFromAttributeName = @"environmentvariabledefinitionid",
		LinkToEntityName = @"environmentvariablevalue",
		LinkToAttributeName = @"environmentvariabledefinitionid",
		Columns = new ColumnSet(@"value"),
		EntityAlias = @"v"
	};

	#endregion

	#region Methods

	/// <summary>
	/// Gets the environment variable with the specified name.
	/// </summary>
	/// <param name="organizationService">The instance of <see cref="IOrganizationService"/>.</param>
	/// <param name="name">The name of the environment variable.</param>
	/// <returns>The value of the environment variable, or null if not found.</returns>
	public static String? GetEnvironmentVariable
	(
		this IOrganizationService organizationService,
		String name
	)
	{
		// create query
		var query = new QueryExpression(@"environmentvariabledefinition")
		{
			ColumnSet = environmentVariableColumnSet,
			LinkEntities = { environmentVariableLinkEntity },
			Criteria = new FilterExpression(LogicalOperator.And)
			{
				Conditions =
				{
					new ConditionExpression(@"schemaname", ConditionOperator.Equal, name)
				}
			}
		};

		// execute query
		var queryResults = organizationService.RetrieveMultiple(query);

		// get entity
		var entity = queryResults.Entities.FirstOrDefault();

		if (entity == null)
		{
			return null;
		}

		// get value
		var result = (entity.GetAttributeValue<AliasedValue>(@"v.value")?.Value?.ToString())
			?? entity.GetAttributeValue<String>(@"defaultvalue");

		return result;
	}

	#endregion
}
