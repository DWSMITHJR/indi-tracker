using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Tracker.API.Data;
using Tracker.API.Data.Seeders;
using Tracker.API.Services;
using Tracker.Infrastructure.Data;
using Tracker.Infrastructure.Models;
using Tracker.Shared.Auth;
using Tracker.Shared.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

builder.Services.AddSwaggerGen();

// Add configuration settings
builder.Services.Configure<AppSettings>(
    builder.Configuration.GetSection("AppSettings"));
    
// Add JWT settings
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("JwtSettings"));

// Register AppSettingsService
builder.Services.AddScoped<AppSettingsService>();

// Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Identity
builder.Services.AddIdentity<User, IdentityRole<Guid>>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Add Authentication
var appSettings = builder.Configuration.GetSection("AppSettings").Get<AppSettings>();
if (string.IsNullOrEmpty(appSettings?.Secret))
{
    // Use a default secret for development if not configured
    appSettings ??= new AppSettings();
    appSettings.Secret = "YOUR_SECRET_KEY_HERE_AT_LEAST_32_CHARACTERS_LONG";
    builder.Services.Configure<AppSettings>(options => options.Secret = appSettings.Secret);
}

var key = Encoding.ASCII.GetBytes(appSettings.Secret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        // Set clock skew to zero so tokens expire exactly at token expiration time
        ClockSkew = TimeSpan.Zero
    };
});

// Authentication is already configured above

// Register DataSeeder and related services
builder.Services.AddScoped<IDataSeeder, DataSeeder>();
builder.Services.AddScoped<ICsvDataReader, CsvDataReader>();

// Register AuthService
builder.Services.AddScoped<IAuthService, AuthService>();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

// Configure API versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});

// Register Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Tracker API", Version = "v1" });
    
    // Add JWT Authentication
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "JWT Authentication",
        Description = "Enter JWT Bearer token **_only_**",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };
    c.AddSecurityDefinition(securityScheme.Reference.Id, securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {securityScheme, Array.Empty<string>()}
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Enable CORS
app.UseCors("AllowAll");

// Enable Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Seed initial data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    
    try
    {
        logger.LogInformation("Initializing database...");
        var context = services.GetRequiredService<ApplicationDbContext>();
        
        // Ensure database is created and apply any pending migrations
        logger.LogInformation("Ensuring database is created and applying migrations...");
        await context.Database.EnsureCreatedAsync();
        
        // Check if we should seed data (only if the database is empty)
        if (!await context.Users.AnyAsync())
        {
            logger.LogInformation("Database is empty. Seeding initial data...");
            try
            {
                var seeder = services.GetRequiredService<IDataSeeder>();
                await seeder.SeedAsync();
                logger.LogInformation("Database seeding completed successfully.");
            }
            catch (Exception seederEx)
            {
                logger.LogError(seederEx, "An error occurred while seeding the database. The application will continue without seeded data.");
                // Continue running the application even if seeding fails
            }
        }
        else
        {
            logger.LogInformation("Database already contains data. Skipping seeding.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while initializing the database");
        // Re-throw to stop the application if there's a critical database error
        throw;
    }
}

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/api/appsettings", async (AppSettingsService appSettingsService) =>
{
    var settings = await appSettingsService.GetAppSettingsAsync();
    return Results.Ok(settings);
})
.WithName("GetAppSettings")
.Produces<AppSettings>(StatusCodes.Status200OK);

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
