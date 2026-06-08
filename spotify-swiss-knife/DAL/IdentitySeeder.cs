using Microsoft.AspNetCore.Identity;
using spotify_swiss_knife.Models;

namespace spotify_swiss_knife.DAL;

public static class IdentitySeeder
{
    public static readonly string[] Roles = ["Admin", "Editor", "User"];

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        var email = config["SeedAdmin:Email"] ?? "admin@ssk.local";
        var password = config["SeedAdmin:Password"] ?? "Admin123!";

        var existingAdmin = await userManager.FindByEmailAsync(email);
        if (existingAdmin is null)
        {
            var admin = new AppUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = "Site",
                LastName = "Administrator",
                OIB = "00000000000",
                JMBAG = "0000000000"
            };

            var result = await userManager.CreateAsync(admin, password);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, "Admin");
        }
        else if (string.IsNullOrEmpty(existingAdmin.OIB) || string.IsNullOrEmpty(existingAdmin.JMBAG))
        {
            existingAdmin.OIB = "00000000000";
            existingAdmin.JMBAG = "0000000000";
            await userManager.UpdateAsync(existingAdmin);
        }
    }
}
