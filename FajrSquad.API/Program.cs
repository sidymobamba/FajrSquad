using System.Text;
using System.Text.Json.Serialization;
using FajrSquad.Core.Config;
using FajrSquad.Infrastructure.Data;
using FajrSquad.Infrastructure.Services;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Quartz;
using FajrSquad.API.Jobs;
using Microsoft.Extensions.DependencyInjection;
using FajrSquad.Core.Profiles;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
var provider = configuration["DatabaseProvider"];

//// 🔹 Firebase Admin SDK (solo per FCM)
//var json = Environment.GetEnvironmentVariable("FIREBASE_CONFIG_JSON");

//if (string.IsNullOrWhiteSpace(json))
//    throw new InvalidOperationException("FIREBASE_CONFIG_JSON environment variable is not set.");

//FirebaseApp.Create(new AppOptions()
//{
//    Credential = GoogleCredential.FromJson(json)
//});


// 🔹 Database (SQL Server o PostgreSQL)
builder.Services.AddDbContext<FajrDbContext>(options =>
{
    var cs = configuration.GetConnectionString("DefaultConnection");
    if (provider == "postgres")
        options.UseNpgsql(cs);
    else
        options.UseSqlServer(cs);
});

// 🔹 Dependency Injection
builder.Services.AddScoped<IFajrService, FajrService>();
builder.Services.AddScoped<IFileUploadService, FileUploadService>();
builder.Services.AddScoped<ICacheService, MemoryCacheService>();
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<NotificationService>();

// 🔹 Quartz Jobs (notifiche programmate)
builder.Services.AddQuartz(q =>
{
    q.UseMicrosoftDependencyInjectionJobFactory();

    q.ScheduleJob<SendMorningMotivationJob>(t => t
        .WithIdentity("morningMotivation")
        .WithCronSchedule("0 0 6 * * ?")); // 06:00

    q.ScheduleJob<SendAfternoonMotivationJob>(t => t
        .WithIdentity("afternoonMotivation")
        .WithCronSchedule("0 0 14 * * ?")); // 14:00

    q.ScheduleJob<SendEveningMotivationJob>(t => t
        .WithIdentity("eveningMotivation")
        .WithCronSchedule("0 0 20 * * ?")); // 20:00

    q.ScheduleJob<SendHadithJob>(t => t
        .WithIdentity("dailyHadith")
        .WithCronSchedule("0 5 14 * * ?")); // 14:05
});
builder.Services.AddQuartzHostedService();

// 🔹 JWT Auth
builder.Services.Configure<JwtSettings>(configuration.GetSection("Jwt"));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var jwtSettings = configuration.GetSection("Jwt").Get<JwtSettings>()!;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret))
    };
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine("Authentication failed: " + context.Exception.Message);
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Console.WriteLine("Token validato correttamente.");
            return Task.CompletedTask;
        }
    };
});

// 🔹 Swagger + Auth support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "FajrSquad API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Inserisci il token JWT nel formato: Bearer {token}"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// 🔹 Controllers (System.Text.Json + Newtonsoft support)
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    })
    .AddNewtonsoftJson(); // Per JObject

// 🔹 Extra services
builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();
builder.Services.AddHealthChecks();
builder.Services.Configure<StaticFileOptions>(options => options.ServeUnknownFileTypes = false);
builder.Services.AddAutoMapper(typeof(FajrProfile)); // Scansiona tutti i profili nell’assembly


// 🔹 Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// 🔹 Dev tools
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 🔹 DB seeding (facoltativo)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FajrDbContext>();
    // db.Database.Migrate(); // attiva se vuoi migrazioni automatiche
    // await IslamicDataSeeder.SeedAsync(db); // se usi seeder
}

// 🔹 Middleware personalizzati
app.UseMiddleware<FajrSquad.API.Middleware.GlobalExceptionMiddleware>();
app.UseMiddleware<FajrSquad.API.Middleware.RateLimitingMiddleware>();

// 🔹 Middleware standard
app.UseHttpsRedirection();
var uploadsPath = Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "uploads", "avatars");
Directory.CreateDirectory(uploadsPath); // 👈 assicura che esista

app.UseStaticFiles(); // wwwroot standard
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads/avatars"
});
app.UseCors("AllowLocalhostFrontend");
app.UseCors(policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
app.UseAuthentication();
app.UseAuthorization();

// 🔹 Routing
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
