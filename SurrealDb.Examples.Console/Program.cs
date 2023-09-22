using SurrealDb;
using SurrealDb.Models;
using SurrealDb.Models.Auth;
using System.Text.Json;

var db = new SurrealDbClient("ws://localhost:8000/rpc");

await db.SignIn(new RootAuth { Username = "root", Password = "root" });
await db.Use("test", "test");

var person = new Person
{
	Title = "Founder & CEO",
	Name = new() { FirstName = "Tobie", LastName = "Morgan Hitchcock" },
	Marketing = true
};
var created = await db.Create("person", person);
Console.WriteLine(ToJsonString(created));

var updated = await db.Merge<ResponsibilityMerge, Person>(
	new() { Id = new Thing("person", "jaime"), Marketing = true }
);
Console.WriteLine(ToJsonString(updated));

var people = await db.Select<Person>("person");
Console.WriteLine(ToJsonString(people));

var queryResponse = await db.Query(
	"SELECT marketing, count() FROM type::table($table) GROUP BY marketing",
	new Dictionary<string, object> { { "table", "person" } } 
);
var groups = queryResponse.GetValue<List<Group>>(0);
Console.WriteLine(ToJsonString(groups));

static string ToJsonString(object? o)
{
	return JsonSerializer.Serialize(o, new JsonSerializerOptions
	{
		WriteIndented = true,
	});
}

public class Person : Record
{
	public string? Title { get; set; }
	public Name? Name { get; set; }
	public bool Marketing { get; set; }
}
public class Name
{
	public string? FirstName { get; set; }
	public string? LastName { get; set; }
}
public class ResponsibilityMerge : Record
{
	public bool Marketing { get; set; }
}
public class Group
{
	public bool Marketing { get; set; }
	public int Count { get; set; }
}
