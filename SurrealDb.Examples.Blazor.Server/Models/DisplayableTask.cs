﻿using SurrealDb.Net.Models;

namespace SurrealDb.Examples.Blazor.Server.Models;

public class DisplayableTask
{
    public Thing Id { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
}
