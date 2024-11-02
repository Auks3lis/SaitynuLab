using Microsoft.AspNetCore.Identity;

namespace Lab1.Auth.Model;

public class ForumRestUser : IdentityUser
{
    public bool ForceRelogin { get; set; }
}