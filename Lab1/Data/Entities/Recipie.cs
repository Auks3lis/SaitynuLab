namespace Lab1.Data.Entities;
using System.ComponentModel.DataAnnotations;
using Lab1.Auth.Model;

public class Recipie
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; }
    public DateTime? CreationDate { get; set; }
    
    public required Meal Meal { get; set; }
    
    [Required]
    public required string UserId { get; set; }
    public ForumRestUser User { get; set; }
}

public record RecipieDto(int Id, string Name, string Description, DateTime? CreationDate);