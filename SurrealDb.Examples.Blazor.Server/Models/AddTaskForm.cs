using System.ComponentModel.DataAnnotations;

namespace SurrealDb.Examples.Blazor.Server.Models;

public class AddTaskForm
{
    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public DateTime? DueDate { get; set; }
}
