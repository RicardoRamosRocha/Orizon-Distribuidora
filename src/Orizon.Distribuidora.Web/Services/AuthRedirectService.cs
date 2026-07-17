using Microsoft.AspNetCore.Mvc;

namespace Orizon.Distribuidora.Web.Services;

public static class AuthRedirectService
{
    public const string DefaultAuthenticatedPath = "/Admin/Dashboard";

    public static string GetSafeLocalReturnUrl(
        IUrlHelper url,
        string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) &&
            url.IsLocalUrl(returnUrl))
        {
            return returnUrl;
        }

        return DefaultAuthenticatedPath;
    }
}
