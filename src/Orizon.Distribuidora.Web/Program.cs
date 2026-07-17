using Orizon.Distribuidora.Infrastructure.DependencyInjection;
using Orizon.Distribuidora.Infrastructure.Identity.Seed;
using Orizon.Distribuidora.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddScoped<ICurrentCompanyAccessor, CurrentCompanyAccessor>();

builder.Services.AddInfrastructure(
    builder.Configuration);

var app = builder.Build();

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

app.Run();
