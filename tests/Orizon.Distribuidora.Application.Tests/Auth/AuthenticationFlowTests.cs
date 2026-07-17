using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Orizon.Distribuidora.Infrastructure.Identity;
using Orizon.Distribuidora.Web.Areas.Admin.Controllers;
using Orizon.Distribuidora.Web.Controllers;
using Orizon.Distribuidora.Web.Models.Account;
using Orizon.Distribuidora.Web.Services;

namespace Orizon.Distribuidora.Application.Tests.Auth;

public sealed class AuthenticationFlowTests
{
    [Fact]
    public void LoginViewModel_ShouldRequireEmailAndPassword()
    {
        var model = new LoginViewModel();
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(
            model,
            new ValidationContext(model),
            validationResults,
            validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(validationResults, result => result.MemberNames.Contains(nameof(LoginViewModel.Email)));
        Assert.Contains(validationResults, result => result.MemberNames.Contains(nameof(LoginViewModel.Password)));
    }

    [Fact]
    public void AuthRedirectService_ShouldAllowLocalReturnUrl()
    {
        var redirectUrl = AuthRedirectService.GetSafeLocalReturnUrl(
            new FakeUrlHelper(),
            "/Admin/Cadastros/categorias");

        Assert.Equal("/Admin/Cadastros/categorias", redirectUrl);
    }

    [Theory]
    [InlineData("https://example.com/Admin")]
    [InlineData("//example.com/Admin")]
    [InlineData(null)]
    public void AuthRedirectService_ShouldRejectExternalReturnUrl(string? returnUrl)
    {
        var redirectUrl = AuthRedirectService.GetSafeLocalReturnUrl(
            new FakeUrlHelper(),
            returnUrl);

        Assert.Equal(AuthRedirectService.DefaultAuthenticatedPath, redirectUrl);
    }

    [Fact]
    public void AccountController_LoginGet_ShouldAllowAnonymousAccess()
    {
        var method = typeof(AccountController).GetMethod(
            nameof(AccountController.Login),
            [typeof(string)]);

        Assert.NotNull(method);
        Assert.Contains(method.GetCustomAttributes(), attribute => attribute is AllowAnonymousAttribute);
        Assert.Contains(method.GetCustomAttributes(), attribute => attribute is HttpGetAttribute get && get.Template == "Login");
    }

    [Fact]
    public void AccountController_LoginPost_ShouldUseAntiforgery()
    {
        var method = typeof(AccountController).GetMethod(
            nameof(AccountController.Login),
            [typeof(LoginViewModel)]);

        Assert.NotNull(method);
        Assert.Contains(method.GetCustomAttributes(), attribute => attribute is AllowAnonymousAttribute);
        Assert.Contains(method.GetCustomAttributes(), attribute => attribute is ValidateAntiForgeryTokenAttribute);
        Assert.Contains(method.GetCustomAttributes(), attribute => attribute is HttpPostAttribute post && post.Template == "Login");
    }

    [Fact]
    public void AccountController_Logout_ShouldRequireAuthenticatedPostWithAntiforgery()
    {
        var method = typeof(AccountController).GetMethod(nameof(AccountController.Logout));

        Assert.NotNull(method);
        Assert.Contains(method.GetCustomAttributes(), attribute => attribute is AuthorizeAttribute);
        Assert.Contains(method.GetCustomAttributes(), attribute => attribute is ValidateAntiForgeryTokenAttribute);
        Assert.Contains(method.GetCustomAttributes(), attribute => attribute is HttpPostAttribute post && post.Template == "Logout");
    }

    [Fact]
    public void AdminControllers_ShouldRequireAdministratorRole()
    {
        AssertRequiresAdministratorRole(typeof(DashboardController));
        AssertRequiresAdministratorRole(typeof(BasicRegistrationsController));
    }

    private static void AssertRequiresAdministratorRole(Type controllerType)
    {
        var authorizeAttribute = controllerType
            .GetCustomAttributes<AuthorizeAttribute>()
            .SingleOrDefault();

        Assert.NotNull(authorizeAttribute);
        Assert.Equal(Roles.Administrator, authorizeAttribute.Roles);
    }

    private sealed class FakeUrlHelper : IUrlHelper
    {
        public ActionContext ActionContext { get; } = new();

        public string? Action(UrlActionContext actionContext) => null;

        public string? Content(string? contentPath) => contentPath;

        public bool IsLocalUrl(string? url)
        {
            return !string.IsNullOrWhiteSpace(url) &&
                url[0] == '/' &&
                (url.Length == 1 || (url[1] != '/' && url[1] != '\\'));
        }

        public string? Link(string? routeName, object? values) => null;

        public string? RouteUrl(UrlRouteContext routeContext) => null;
    }
}
