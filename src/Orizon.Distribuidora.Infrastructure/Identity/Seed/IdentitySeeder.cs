using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Orizon.Distribuidora.Infrastructure.Identity.Seed;

public static class IdentitySeeder
{
    public static async Task SeedAsync(
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        using var scope = serviceProvider.CreateScope();

        var roleManager =
            scope.ServiceProvider
                .GetRequiredService<RoleManager<ApplicationRole>>();

        var userManager =
            scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var roleName in Roles.All)
        {
            if (await roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            var role = new ApplicationRole
            {
                Name = roleName,
                Description = $"Perfil {roleName}"
            };

            var roleResult = await roleManager.CreateAsync(role);

            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Não foi possível criar o perfil {roleName}: " +
                    string.Join(
                        ", ",
                        roleResult.Errors.Select(error => error.Description)));
            }
        }

        var email = configuration[
            "Seed:Administrator:Email"];

        var fullName = configuration[
            "Seed:Administrator:FullName"];

        var password = configuration[
            "Seed:Administrator:Password"];

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(fullName) ||
            string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var administrator =
            await userManager.FindByEmailAsync(email);

        if (administrator is null)
        {
            administrator = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName,
                IsActive = true
            };

            var userResult =
                await userManager.CreateAsync(
                    administrator,
                    password);

            if (!userResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Não foi possível criar o administrador inicial: " +
                    string.Join(
                        ", ",
                        userResult.Errors.Select(error => error.Description)));
            }
        }

        if (!await userManager.IsInRoleAsync(
                administrator,
                Roles.Administrator))
        {
            await userManager.AddToRoleAsync(
                administrator,
                Roles.Administrator);
        }
    }
}
