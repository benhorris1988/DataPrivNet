namespace DataProvisioning.WebUI.Middlewares;

using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DataProvisioning.Infrastructure.Data;
using DataProvisioning.Domain.Entities;
using DataProvisioning.Domain.Enums;
using Microsoft.Extensions.Configuration;
using System.Linq;

public class UserProvisioningMiddleware
{
    private readonly RequestDelegate _next;
    private readonly bool _testMode;

    public UserProvisioningMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _testMode = configuration.GetValue<bool>("TestMode");
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only run provisioning logic if the user is authenticated (e.g. via Windows Auth or Cookie)
        if (context.User.Identity != null && context.User.Identity.IsAuthenticated)
        {
            var dbContext = context.RequestServices.GetRequiredService<ApplicationDbContext>();
            // If in TestMode, the Cookie auth might already have the email as ClaimTypes.Name
            // If Windows Auth, context.User.Identity.Name is usually "DOMAIN\username"
            var usernameOrEmail = context.User.Identity.Name;

            if (!string.IsNullOrEmpty(usernameOrEmail))
            {
                // In TestMode we lookup by Email, in Windows Auth we lookup by Email or Name
                var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == usernameOrEmail || u.Name == usernameOrEmail);
                
                if (user == null)
                {
                    // User doesn't exist, check InitialAdmins table
                    var isInitialAdmin = await dbContext.InitialAdmins
                        .AnyAsync(a => a.Username.ToLower() == usernameOrEmail.ToLower());

                    var isInitialAdminConfig = false;
                    var config = context.RequestServices.GetRequiredService<IConfiguration>();
                    var initialAdminConfig = config.GetValue<string>("InitialAdmin");
                    if (!string.IsNullOrEmpty(initialAdminConfig) && initialAdminConfig.Equals(usernameOrEmail, StringComparison.OrdinalIgnoreCase))
                    {
                        isInitialAdminConfig = true;
                    }

                    user = new User
                    {
                        Name = usernameOrEmail,
                        Email = usernameOrEmail, // Just use the username as email placeholder if none exists
                        Role = (isInitialAdmin || isInitialAdminConfig) ? UserRole.Admin : UserRole.User
                    };

                    dbContext.Users.Add(user);
                    await dbContext.SaveChangesAsync();
                }

                // Append the role claim and user ID so Authorization policies work correctly
                if (context.User.Identity is ClaimsIdentity claimsIdentity)
                {
                    if (!claimsIdentity.HasClaim(c => c.Type == ClaimTypes.Role))
                    {
                        claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, user.Role.ToString()));
                    }
                    if (!claimsIdentity.HasClaim(c => c.Type == ClaimTypes.NameIdentifier))
                    {
                        claimsIdentity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
                    }
                }
            }
        }

        await _next(context);
    }
}
