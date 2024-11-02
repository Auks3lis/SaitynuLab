using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentValidation;
using Lab1.Auth;
using Lab1.Data;
using Lab1.Data.Entities;
using Microsoft.EntityFrameworkCore;
using O9d.AspNet.FluentValidation;
using Lab1.Auth.Model;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

using Microsoft.OpenApi.Writers;


JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ForumDbContext>();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddTransient<JwtTokenService>();
builder.Services.AddScoped<AuthDbSeeder>();

builder.Services.AddIdentity<ForumRestUser, IdentityRole>()
    .AddEntityFrameworkStores<ForumDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters.ValidAudience = builder.Configuration["Jwt:ValidAudience"];
    options.TokenValidationParameters.ValidIssuer = builder.Configuration["Jwt:ValidIssuer"];
    options.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]));
});
builder.Services.AddAuthorization();

var app = builder.Build();

//MEAL
var mealsGroup = app. MapGroup (prefix: "/api").WithValidationFilter();

mealsGroup.MapGet("meals", async (ForumDbContext dbContext, CancellationToken cancellationToken) =>
{
    return (await dbContext.Meals.ToListAsync(cancellationToken))
        .Select(o => new MealDto(o.Id, o.Name, o.Description, o.CreationDate));
});

mealsGroup.MapGet("meals/{mealId}", async (int mealId, ForumDbContext dbContext) =>
{
    var meal = await dbContext.Meals.FirstOrDefaultAsync(m => m.Id == mealId);
    if(meal == null)
        return Results.NotFound($"Meal with ID {mealId} not found");
    
    return Results.Ok(new MealDto(meal.Id, meal.Name, meal.Description, meal.CreationDate) );
});

mealsGroup.MapPost ("meals", [Authorize(Roles = ForumRoles.Admin)]async ([Validate]CreateMealDto CreateMealDto, ForumDbContext dbContext, HttpContext httpContext) =>
{
    //return Results.UnprocessableEntity(httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub));
    //return Results.UnprocessableEntity(httpContext.User.);
    var meal = new Meal()
    {
        //"1adc7a17-a2b3-4970-bfd5-20e57fd989a7"
        Name = CreateMealDto.Name,
        Description = CreateMealDto.Description,
        CreationDate = DateTime.UtcNow,
        UserId = httpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
    };
    dbContext.Meals.Add(meal);
    await dbContext.SaveChangesAsync();

    return Results.Created($"/api/meals/{meal.Id}",
        new MealDto(meal.Id, meal.Name, meal.Description, meal.CreationDate));
});

mealsGroup.MapPut("meals/{mealId}", [Authorize(Roles = ForumRoles.ForumUser)]async (int mealId, ForumDbContext dbContext, [Validate]UpdateMealDto dto, HttpContext httpContext) =>
{    
    var meal = await dbContext.Meals.FirstOrDefaultAsync(m => m.Id == mealId);
    if(meal == null)
        return Results.NotFound($"Meal with ID {mealId} not found");
    
    if(!httpContext.User.IsInRole(ForumRoles.Admin) && httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub) != meal.UserId)
    {
        return Results.Forbid();
    }
    
    meal.Description = dto.Description;
    dbContext.Update(meal);
    await dbContext.SaveChangesAsync();
    
    return Results.Ok(new MealDto(meal.Id, meal.Name, meal.Description, meal.CreationDate) );
});

mealsGroup.MapDelete("meals/{mealId}", async (int mealId, ForumDbContext dbContext) =>
{    
    var meal = await dbContext.Meals.FirstOrDefaultAsync(m => m.Id == mealId);
    if(meal == null)
        return Results.NotFound($"Meal with ID {mealId} not found");
    
    dbContext.Remove(meal);
    await dbContext.SaveChangesAsync();

    return Results.NoContent();
});




//RECIPIE
var recipieGroup = app.MapGroup (prefix: "/api/meals/{mealId}").WithValidationFilter();

recipieGroup.MapGet("recipies", async (int mealId, ForumDbContext dbContext, CancellationToken cancellationToken) =>
{
    var meal = await dbContext.Meals.FirstOrDefaultAsync(m => m.Id == mealId);
    if (meal == null)
        return Results.NotFound($"Meal with ID {mealId} not found");
    
    var recipies = await dbContext.Recipies
        .Where(r => r.Meal.Id == mealId)
        .ToListAsync(cancellationToken);

    return Results.Ok(recipies.Select(r => new RecipieDto(r.Id, r.Name, r.Description, r.CreationDate)));
});

recipieGroup.MapGet("recipies/{recipieId}", async (int mealId, int recipieId, ForumDbContext dbContext) =>
{
    var meal = await dbContext.Meals.FirstOrDefaultAsync(m => m.Id == mealId);
    if (meal == null)
        return Results.NotFound($"Meal with ID {mealId} not found");
    
    var recipie = await dbContext.Recipies.FirstOrDefaultAsync(r => r.Id == recipieId && r.Meal.Id == mealId);
    if (recipie == null)
        return Results.NotFound($"Recipie with ID {recipieId} not found for Meal ID {mealId}");
    
    return Results.Ok(new RecipieDto(recipie.Id, recipie.Name, recipie.Description, recipie.CreationDate) );
});

recipieGroup.MapPost("recipies", async (int mealId, [Validate] CreateRecipieDto createRecipieDto, ForumDbContext dbContext, HttpContext httpContext) =>
{
    var meal = await dbContext.Meals.FirstOrDefaultAsync(m => m.Id == mealId);
    if (meal == null)
        return Results.NotFound($"Meal with ID {mealId} not found");

    var recipie = new Recipie()
    {
        Name = createRecipieDto.Name,
        Description = createRecipieDto.Description,
        CreationDate = DateTime.UtcNow,
        Meal = meal,
        UserId = httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub)
    };
    
    dbContext.Recipies.Add(recipie);
    await dbContext.SaveChangesAsync();

    return Results.Created($"/api/meals/{meal.Id}/recipies/{recipie.Id}",
        new RecipieDto(recipie.Id, recipie.Name, recipie.Description, recipie.CreationDate));
});

recipieGroup.MapPut("recipies/{recipieId}",async (int mealId, int recipieId, ForumDbContext dbContext, [Validate]UpdateRecipieDto dto) =>
{    
    var meal = await dbContext.Meals.FirstOrDefaultAsync(m => m.Id == mealId);
    if (meal == null)
        return Results.NotFound($"Meal with ID {mealId} not found");
    
    var recipie = await dbContext.Recipies.FirstOrDefaultAsync(r => r.Id == recipieId && r.Meal.Id == mealId);
    if (recipie == null)
        return Results.NotFound($"Recipie with ID {recipieId} not found for Meal ID {mealId}");
    
    recipie.Description = dto.Description;
    dbContext.Update(recipie);
    await dbContext.SaveChangesAsync();
    
    return Results.Ok(new RecipieDto(recipie.Id, recipie.Name, recipie.Description, recipie.CreationDate) );
});

recipieGroup.MapDelete("recipies/{recipieId}", async (int mealId, int recipieId, ForumDbContext dbContext) =>
{ 
    var meal = await dbContext.Meals.FirstOrDefaultAsync(m => m.Id == mealId);
    if (meal == null)
        return Results.NotFound($"Meal with ID {mealId} not found");
    
    var recipie = await dbContext.Recipies.FirstOrDefaultAsync(r => r.Id == recipieId && r.Meal.Id == mealId);
    if (recipie == null)
        return Results.NotFound($"Recipie with ID {recipieId} not found for Meal ID {mealId}");
    
    dbContext.Remove(recipie);
    await dbContext.SaveChangesAsync();

    return Results.NoContent();
});





//COMMENT
var commentGroup = app.MapGroup (prefix: "/api/meals/{mealId}/recipies/{recipieid}").WithValidationFilter();

commentGroup.MapGet("comments", async (int mealId, int recipieId, ForumDbContext dbContext, CancellationToken cancellationToken) =>
{
    var meal = await dbContext.Meals.FirstOrDefaultAsync(m => m.Id == mealId);
    if (meal == null)
        return Results.NotFound($"Meal with ID {mealId} not found");
    
    var recipie = await dbContext.Recipies.FirstOrDefaultAsync(r => r.Id == recipieId && r.Meal.Id == mealId);
    if (recipie == null)
        return Results.NotFound($"Recipie with ID {recipieId} not found for Meal ID {mealId}");
    
    var comments = await dbContext.Comments
        .Where(c => c.Recipie.Id == recipieId)
        .ToListAsync(cancellationToken);

    return Results.Ok(comments.Select(c => new CommentDto(c.Id, c.Content, c.CreationDate)));
});

commentGroup.MapGet("comments/{commentId}", async (int mealId, int recipieId, int commentId, ForumDbContext dbContext) =>
{
    var meal = await dbContext.Meals.FirstOrDefaultAsync(m => m.Id == mealId);
    if (meal == null)
        return Results.NotFound($"Meal with ID {mealId} not found");
    
    var recipie = await dbContext.Recipies.FirstOrDefaultAsync(r => r.Id == recipieId && r.Meal.Id == mealId);
    if (recipie == null)
        return Results.NotFound($"Recipie with ID {recipieId} not found for Meal ID {mealId}");
    
    var comment = await dbContext.Comments.FirstOrDefaultAsync(c => c.Id == commentId && c.Recipie.Id == recipieId);
    if(comment == null)
        return Results.NotFound($"Comment with ID {commentId} not found for Recipie ID {recipieId}");

    return Results.Ok(new CommentDto(comment.Id, comment.Content, comment.CreationDate));
});


commentGroup.MapPost("comments", async (int mealId, int recipieId, [Validate] CreateCommentDto CreateCommentDto, ForumDbContext dbContext, HttpContext httpContext) =>
{
    var meal = await dbContext.Meals.FirstOrDefaultAsync(m => m.Id == mealId);
    if (meal == null)
        return Results.NotFound($"Meal with ID {mealId} not found");
    
    var recipie = await dbContext.Recipies.FirstOrDefaultAsync(r => r.Meal.Id == mealId && r.Id == recipieId);
    if (recipie == null)
        return Results.NotFound($"Recipie with ID {recipieId} not found for Meal ID {mealId}");
    
    var comment = new Comment()
    {
        Content = CreateCommentDto.Content,
        CreationDate = DateTime.UtcNow,
        Recipie = recipie,
        UserId = httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub)
    };
    
    dbContext.Comments.Add(comment);
    await dbContext.SaveChangesAsync();

    return Results.Created($"/api/meals/{meal.Id}/recipies/{recipie.Id}/comments/{comment.Id}",
        new CommentDto(comment.Id, comment.Content, comment.CreationDate));
});

commentGroup.MapPut("comments/{commentId}", async (int mealId, int recipieId, int commentId, ForumDbContext dbContext, [Validate]UpdateCommentDto dto) =>
{    
    var meal = await dbContext.Meals.FirstOrDefaultAsync(m => m.Id == mealId);
    if (meal == null)
        return Results.NotFound($"Meal with ID {mealId} not found");
    
    var recipie = await dbContext.Recipies.FirstOrDefaultAsync(r => r.Id == recipieId && r.Meal.Id == mealId);
    if (recipie == null)
        return Results.NotFound($"Recipie with ID {recipieId} not found for Meal ID {mealId}");
    
    var comment = await dbContext.Comments.FirstOrDefaultAsync(c => c.Id == commentId && c.Recipie.Id == recipieId);
    if(comment == null)
        return Results.NotFound($"Comment with ID {commentId} not found for Recipie ID {recipieId}");
    
    comment.Content = dto.Content;
    dbContext.Update(comment);
    await dbContext.SaveChangesAsync();
    
    return Results.Ok(new CommentDto(comment.Id, comment.Content, comment.CreationDate) );
});

commentGroup.MapDelete("comments/{commentId}", async (int mealId, int recipieId, int commentId, ForumDbContext dbContext) =>
{    
    var meal = await dbContext.Meals.FirstOrDefaultAsync(m => m.Id == mealId);
    if (meal == null)
        return Results.NotFound($"Meal with ID {mealId} not found");
        
    var recipie = await dbContext.Recipies.FirstOrDefaultAsync(r => r.Id == recipieId && r.Meal.Id == mealId);
    if (recipie == null)
        return Results.NotFound($"Recipie with ID {recipieId} not found for Meal ID {mealId}");
        
    var comment = await dbContext.Comments.FirstOrDefaultAsync(c => c.Id == commentId && c.Recipie.Id == recipieId);
    if(comment == null)
        return Results.NotFound($"Comment with ID {commentId} not found for Recipie ID {recipieId}");
    
    dbContext.Remove(comment);
    await dbContext.SaveChangesAsync();

    return Results.NoContent();
});

app.AddAuthApi();
app.UseAuthentication();
app.UseAuthorization();


using var scope = app.Services.CreateScope();
var dbSeeder = scope.ServiceProvider.GetRequiredService<AuthDbSeeder>();
await dbSeeder.SeedAsync();
app.Run();


//FOR MEAL
public record CreateMealDto(string Name, string Description);
public record UpdateMealDto(string Description);
public class CreateMealDtoValidator : AbstractValidator<CreateMealDto>
{
    public CreateMealDtoValidator()
    {
        RuleFor(m => m.Name).NotEmpty().NotNull().Length(min: 1, max: 20);
        RuleFor(m => m.Description).NotEmpty().NotNull().Length(min: 1, max: 100);
    }
}
public class UpdateMealDtoValidator : AbstractValidator<UpdateMealDto>
{
    public UpdateMealDtoValidator()
    {
        RuleFor(m => m.Description).NotEmpty().NotNull().Length(min: 1, max: 100);
    }
}

//FOR RECIPIE
public record CreateRecipieDto(string Name, string Description);
public record UpdateRecipieDto(string Description);
public class CreateRecipieDtoValidator : AbstractValidator<CreateRecipieDto>
{
    public CreateRecipieDtoValidator()
    {
        RuleFor(r => r.Name).NotEmpty().NotNull().Length(min: 1, max: 20);
        RuleFor(r => r.Description).NotEmpty().NotNull().Length(min: 1, max: 100);
    }
}
public class UpdateRecipieDtoValidator : AbstractValidator<UpdateRecipieDto>
{
    public UpdateRecipieDtoValidator()
    {
        RuleFor(r => r.Description).NotEmpty().NotNull().Length(min: 1, max: 100);
    }
}

//FOR COMMENT
public record CreateCommentDto(string Content);
public record UpdateCommentDto(string Content);
public class CreateCommentDtoValidator : AbstractValidator<CreateCommentDto>
{
    public CreateCommentDtoValidator()
    {
        RuleFor(c => c.Content).NotEmpty().NotNull().Length(min: 1, max: 200);
    }
}
public class UpdateCommentDtoValidator : AbstractValidator<UpdateCommentDto>
{
    public UpdateCommentDtoValidator()
    {
        RuleFor(c => c.Content).NotEmpty().NotNull().Length(min: 1, max: 200);
    }
}