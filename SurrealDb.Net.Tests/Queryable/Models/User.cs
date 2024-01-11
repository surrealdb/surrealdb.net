namespace SurrealDb.Net.Tests.Queryable.Models;

public class User : SurrealDbRecord
{
    public string Username { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public bool IsActive { get; set; }
    public bool IsOwner { get; set; }
    public int Age { get; set; }
}
