using MeetingRoomReservation.Domain.Models.Resources;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MeetingRoomReservation.Infrastructure.Data.Seeders;

public sealed class DatabaseSeeder(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager)
{
    private static readonly string[] Roles = ["Admin", "User"];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedRolesAsync();
        await SeedUsersAsync();
        await SeedResourcesAsync(cancellationToken);
    }

    private async Task SeedRolesAsync()
    {
        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    private async Task SeedUsersAsync()
    {
        await EnsureUserAsync("admin@meetingrooms.local", "Admin#12345", "Admin");
        await EnsureUserAsync("user@meetingrooms.local", "User#12345", "User");
    }

    private async Task EnsureUserAsync(string email, string password, string role)
    {
        if (await userManager.FindByEmailAsync(email) is not null)
            return;

        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
        var result = await userManager.CreateAsync(user, password);
        if (result.Succeeded)
            await userManager.AddToRoleAsync(user, role);
    }

    private async Task SeedResourcesAsync(CancellationToken cancellationToken)
    {
        if (await dbContext.Resources.AnyAsync(cancellationToken))
            return;

        var workingHours = WorkingHours.Create(new TimeOnly(9, 0), new TimeOnly(17, 0)).Value;

        var resources = new[]
        {
            Resource.Create("Alpha Conference Room", workingHours, TimeSpan.FromMinutes(30)).Value,
            Resource.Create("Beta Meeting Room", workingHours, TimeSpan.FromMinutes(60)).Value,
        };

        dbContext.Resources.AddRange(resources);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
