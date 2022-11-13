using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using SurrealDatabase.Net.DTOs;

namespace SurrealDatabase.Net.Helpers
{
	public static class SurrealDatabaseResponseHelper
	{
		public static SurrealDatabaseResponse GenerateErrorResponse(string? errorMessage)
		{
			var obj = new JsonObject();
			obj.Add("Error",errorMessage);
			return new SurrealDatabaseResponse() { Time = DateTime.Now.ToString(), Status = "Error.",Result = new List<JsonObject>(){obj}};
		}
	}
}
