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

        var roleManager = scope.ServiceProvider
            .GetRequiredService<RoleManager<ApplicationRole>>();

        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();

        await SeedRolesAsync(roleManager);

        var email = configuration["Seed:Administrator:Email"];
        var fullName = configuration["Seed:Administrator:FullName"];
        var password = configuration["Seed:Administrator:Password"];

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(fullName) ||
            string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        email = email.Trim();
        fullName = fullName.Trim();

        var administrator = await userManager.FindByEmailAsync(email);

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

            var createResult = await userManager.CreateAsync(
                administrator,
                password);

            EnsureSucceeded(
                createResult,
                "Não foi possível criar o administrador inicial");
        }
        else
        {
            await UpdateAdministratorAsync(
                userManager,
                administrator,
                email,
                fullName);

            await SynchronizeAdministratorPasswordAsync(
                userManager,
                administrator,
                password);
        }

        if (!await userManager.IsInRoleAsync(
                administrator,
                Roles.Administrator))
        {
            var roleResult = await userManager.AddToRoleAsync(
                administrator,
                Roles.Administrator);

            EnsureSucceeded(
                roleResult,
                "Não foi possível vincular o perfil de administrador");
        }
    }

    private static async Task SeedRolesAsync(
        RoleManager<ApplicationRole> roleManager)
    {
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

            EnsureSucceeded(
                roleResult,
                $"Não foi possível criar o perfil {roleName}");
        }
    }

    private static async Task UpdateAdministratorAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationUser administrator,
        string email,
        string fullName)
    {
        var shouldUpdate = false;

        if (!string.Equals(
                administrator.Email,
                email,
                StringComparison.OrdinalIgnoreCase))
        {
            administrator.Email = email;
            shouldUpdate = true;
        }

        if (!string.Equals(
                administrator.UserName,
                email,
                StringComparison.OrdinalIgnoreCase))
        {
            administrator.UserName = email;
            shouldUpdate = true;
        }

        if (!administrator.EmailConfirmed)
        {
            administrator.EmailConfirmed = true;
            shouldUpdate = true;
        }

        if (!administrator.IsActive)
        {
            administrator.IsActive = true;
            shouldUpdate = true;
        }

        if (!string.Equals(
                administrator.FullName,
                fullName,
                StringComparison.Ordinal))
        {
            administrator.FullName = fullName;
            shouldUpdate = true;
        }

        if (!shouldUpdate)
        {
            return;
        }

        var updateResult = await userManager.UpdateAsync(administrator);

        EnsureSucceeded(
            updateResult,
            "Não foi possível atualizar o administrador inicial");
    }

    private static async Task SynchronizeAdministratorPasswordAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationUser administrator,
        string configuredPassword)
    {
        var passwordIsValid = await userManager.CheckPasswordAsync(
            administrator,
            configuredPassword);

        if (passwordIsValid)
        {
            return;
        }

        var resetToken = await userManager.GeneratePasswordResetTokenAsync(
            administrator);

        var resetResult = await userManager.ResetPasswordAsync(
            administrator,
            resetToken,
            configuredPassword);

        EnsureSucceeded(
            resetResult,
            "Não foi possível redefinir a senha do administrador inicial");
    }

    private static void EnsureSucceeded(
        IdentityResult result,
        string errorMessage)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors = string.Join(
            ", ",
            result.Errors.Select(error => error.Description));

        throw new InvalidOperationException(
            $"{errorMessage}: {errors}");
    }
}