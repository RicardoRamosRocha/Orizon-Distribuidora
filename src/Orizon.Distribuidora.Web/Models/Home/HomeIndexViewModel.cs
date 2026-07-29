namespace Orizon.Distribuidora.Web.Models.Home;

public sealed class HomeIndexViewModel
{
    public string? UserFirstName { get; init; }
    public bool IsAuthenticated { get; init; }
    public bool CanAccessAdministration { get; init; }

    public string Greeting => string.IsNullOrWhiteSpace(UserFirstName)
        ? "Bem-vindo à Orizon Distribuidora."
        : $"Olá, {UserFirstName}.";

    public static string? GetSafeFirstName(string? displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName)
            || displayName.Contains('@', StringComparison.Ordinal))
        {
            return null;
        }

        return displayName.Trim()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();
    }
}
