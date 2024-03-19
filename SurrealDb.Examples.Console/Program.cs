using System.Text.Json;
using SurrealDb.Net;
using SurrealDb.Net.Models;
using SurrealDb.Net.Models.Auth;

var db = new SurrealDbClient("ws://127.0.0.1:8000/rpc");

await db.SignIn(new RootAuth { Username = "root", Password = "root" });
await db.Use("examples", "console");

const string TABLE = "person";

var person = new Person
{
    Title = "Founder & CEO",
    Name = new() { FirstName = "Tobie", LastName = "Morgan Hitchcock" },
    Marketing = true
};
var created = await db.Create(TABLE, person);
Console.WriteLine(ToJsonString(created));

var updated = await db.Merge<ResponsibilityMerge, Person>(
    new() { Id = (TABLE, "jaime"), Marketing = true }
);
Console.WriteLine(ToJsonString(updated));

var people = await db.Select<Person>(TABLE);
Console.WriteLine(ToJsonString(people));

var queryResponse = await db.Query(
    $"SELECT Marketing, count() AS Count FROM type::table({TABLE}) GROUP BY Marketing"
);
var groups = queryResponse.GetValue<List<Group>>(0);
Console.WriteLine(ToJsonString(groups));

static string ToJsonString(object? o)
{
    return JsonSerializer.Serialize(o, new JsonSerializerOptions { WriteIndented = true, });
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
