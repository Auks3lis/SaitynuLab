namespace Lab1.Data.Entities;
using System.ComponentModel.DataAnnotations;
using Lab1.Auth.Model;

public class Comment
{
    public int Id { get; set; }
    public required string Content { get; set; }
    public DateTime? CreationDate { get; set; }
    
    public required Recipie Recipie { get; set; }
    
    [Required]
    public required string UserId { get; set; }
    public ForumRestUser User { get; set; }
}

public record CommentDto(int Id, string Content, DateTime? CreationDate);