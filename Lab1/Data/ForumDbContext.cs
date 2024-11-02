using Microsoft.EntityFrameworkCore;
using Lab1.Data.Entities;
using Lab1.Auth.Model;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace Lab1.Data;

public class ForumDbContext : IdentityDbContext<ForumRestUser>
{
    private readonly IConfiguration _configuration;
    public DbSet<Meal> Meals { get; set; }
    public DbSet<Recipie> Recipies { get; set; }
    public DbSet<Comment> Comments { get; set; }

    public ForumDbContext(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(_configuration.GetConnectionString("PostgreSQL"));
    }
}