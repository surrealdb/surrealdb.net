using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace SurrealDatabase.Net.DTOs
{
	public class SurrealDatabaseResponse
	{
		public string Time { get; set; }
		public string Status { get; set; }
		public List<JsonObject> Result { get; set; }
	}
}
