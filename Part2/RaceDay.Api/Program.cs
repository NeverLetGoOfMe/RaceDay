using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using RaceDay.Api.Data;
using RaceDay.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger - the XML comments on the controllers become the endpoint descriptions
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RaceDay API",
        Version = "v1",
        Description = "Event management API for the South African road running, walking and cycling community. " +
                      "Log in through POST /api/auth/login first - the session cookie is then sent with every request."
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFile));
});

builder.Services.AddDbContext<RaceDayDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();

// Session management - keeps the logged-in user's id and role on the server
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".RaceDay.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

var app = builder.Build();

// Used by Docker: create/update the database when the container starts (retries while SQL Server boots)
if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    for (var attempt = 1; attempt <= 12; attempt++)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<RaceDayDbContext>().Database.Migrate();
            break;
        }
        catch (Exception) when (attempt < 12)
        {
            Thread.Sleep(TimeSpan.FromSeconds(5));
        }
    }
}

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "RaceDay API v1");
});

app.UseHttpsRedirection();
app.UseSession();
app.MapControllers();

app.Run();
