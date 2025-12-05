using MenuReporteria.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Registrar servicios
builder.Services.AddTransient<CuentasPorCobrarService>();
builder.Services.AddTransient<ReporteVentasService>();

var app = builder.Build();

//// Configurar para escuchar en todas las interfaces
//app.Urls.Add("http://0.0.0.0:5000");
//app.Urls.Add("https://0.0.0.0:5001");

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();