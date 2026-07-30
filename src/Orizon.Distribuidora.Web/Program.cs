using Microsoft.AspNetCore.HttpOverrides;
using Orizon.Distribuidora.Infrastructure.DependencyInjection;
using Orizon.Distribuidora.Infrastructure.Identity.Seed;
using Orizon.Distribuidora.Web.Options;
using Orizon.Distribuidora.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHealthChecks();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto;

    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.Configure<ImportacaoOptions>(
    builder.Configuration.GetSection(ImportacaoOptions.SectionName));

builder.Services.AddScoped<ICurrentCompanyAccessor, CurrentCompanyAccessor>();
builder.Services.AddScoped<ImportacaoUploadValidator>();
builder.Services.AddSingleton<ImportacaoArquivoTemporarioService>();

builder.Services.AddInfrastructure(
    builder.Configuration);

var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

await IdentitySeeder.SeedAsync(
    app.Services,
    app.Configuration);

await BasicRegistrationSeeder.SeedAsync(
    app.Services,
    app.Configuration);

await ProductSeeder.SeedAsync(app.Services);

app.Run();
