using System;

namespace SurrealDatabase.Net.Constants
{
	public static class SurrealDatabaseHeaderConstants
	{
		public static Tuple<string,string> Accept = new Tuple<string, string>("Accept","application/json");
		public static Tuple<string,string> ContentType = new Tuple<string, string>("Content-Type","application/json");
		public const string Namespace = "NS";
		public const string Database = "DB";
	}
}
