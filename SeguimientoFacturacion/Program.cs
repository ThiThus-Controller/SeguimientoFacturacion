using Microsoft.AspNetCore.Http.Features;
using SeguimientoFacturacion.Application;
using SeguimientoFacturacion.Configurations;
using SeguimientoFacturacion.Infrastructure;
using SeguimientoFacturacion.ModelBinding;
using SeguimientoFacturacion.Services.Seguridad;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(
    options =>
    {
        options.ModelBinderProviders.Insert(
            0,
            new DecimalFlexibleModelBinderProvider());
    });

builder.Services.AddApplication();

builder.Services.AddInfrastructure(
    builder.Configuration);

builder.Services.AddAutenticacionAplicacion();

builder.Services.Configure<FormOptions>(
    options =>
    {
        options.MultipartBodyLengthLimit =
            LimitesCargaArchivos.TamanoMaximoBytes;
    });

var app = builder.Build();

if (args.Contains(
        ComandoInicializacionAdministrador.Argumento,
        StringComparer.OrdinalIgnoreCase))
{
    Environment.ExitCode =
        await ComandoInicializacionAdministrador.EjecutarAsync(
            app.Services);

    return;
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets()
    .AllowAnonymous();

app.MapControllerRoute(
        name: "default",
        pattern:
            "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
