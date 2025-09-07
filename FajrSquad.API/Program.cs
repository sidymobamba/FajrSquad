using System.Text;
using System.Text.Json.Serialization;
using FajrSquad.Core.Config;
using FajrSquad.Infrastructure.Data;
using FajrSquad.Infrastructure.Services;
using FajrSquad.API.Jobs;
using FajrSquad.Core.Profiles;
using Google.Apis.Auth.OAuth2;
using FirebaseAdmin;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Quartz;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
var provider = configuration["DatabaseProvider"];

// 🔹 Log env utili (Firebase)
var firebaseBucket = Environment.GetEnvironmentVariable("FIREBASE_STORAGE_BUCKET");
Console.WriteLine(string.IsNullOrWhiteSpace(firebaseBucket)
    ? "⚠️ FIREBASE_STORAGE_BUCKET is NULL or EMPTY!"
    : "✅ FIREBASE_STORAGE_BUCKET: " + firebaseBucket);

var firebaseJson = Environment.GetEnvironmentVariable("FIREBASE_CONFIG_JSON");
Console.WriteLine(string.IsNullOrWhiteSpace(firebaseJson)
    ? "⚠️ FIREBASE_CONFIG_JSON is NULL or EMPTY!"
    : "✅ FIREBASE_CONFIG_JSON loaded. Length: " + firebaseJson.Length);

// 🔹 Database (SQL Server o PostgreSQL)
builder.Services.AddDbContext<FajrDbContext>(options =>
{
    var cs = configuration.GetConnectionString("DefaultConnection");
    if (provider == "postgres")
        options.UseNpgsql(cs);
    else
        options.UseSqlServer(cs);
});

// 🔹 DI
builder.Services.AddScoped<IFajrService, FajrService>();
builder.Services.AddScoped<IFileUploadService, FileUploadService>();
builder.Services.AddScoped<ICacheService, MemoryCacheService>();
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<NotificationService>();

// 🔹 Quartz
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

// 🔹 JWT
builder.Services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
builder.Services
    .AddAuthentication(options =>
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
            OnAuthenticationFailed = ctx =>
            {
                Console.WriteLine("Authentication failed: " + ctx.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = ctx =>
            {
                Console.WriteLine("Token validato correttamente.");
                return Task.CompletedTask;
            }
        };
    });

// 🔹 Swagger
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

// 🔹 Controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    })
    .AddNewtonsoftJson();

// 🔹 Extra
builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();
builder.Services.AddHealthChecks();
builder.Services.AddAutoMapper(typeof(FajrProfile));

// 🔹 Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// 🔹 CORS
// Leggi origini da env CORS_ORIGINS (separate da virgola) oppure usa default
var originsFromEnv = Environment.GetEnvironmentVariable("CORS_ORIGINS");
string[] allowedOrigins = !string.IsNullOrWhiteSpace(originsFromEnv)
    ? originsFromEnv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    : new[]
    {
        "http://localhost:8100",
        "http://localhost:4200",
        "capacitor://localhost",
        "ionic://localhost"
        // aggiungi qui il dominio web se lo hai (es: https://app.tuodominio.com)
    };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              // se usi cookie (non necessario per header Authorization):
              //.AllowCredentials()
              ;
    });
});

var app = builder.Build();

// 🔹 Dev tools
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ✅ ORDINE CORRETTO PIPELINE
app.UseHttpsRedirection();

// 1) Routing
app.UseRouting();

// 2) CORS (prima di auth/authorization)
app.UseCors("AllowFrontend");

// 2b) Fallback per preflight OPTIONS (utile se qualche proxy “mangia” il preflight)
app.Use(async (ctx, next) =>
{
    if (HttpMethods.Options.Equals(ctx.Request.Method, StringComparison.OrdinalIgnoreCase))
    {
        // Lascia a CORS middleware la priorità, ma se nessuno risponde, rispondiamo noi
        if (!ctx.Response.Headers.ContainsKey("Access-Control-Allow-Origin"))
        {
            var origin = ctx.Request.Headers["Origin"].ToString();
            if (!string.IsNullOrEmpty(origin))
            {
                ctx.Response.Headers["Access-Control-Allow-Origin"] = origin;
                ctx.Response.Headers["Vary"] = "Origin";
            }
            var reqHeaders = ctx.Request.Headers["Access-Control-Request-Headers"].ToString();
            var reqMethod = ctx.Request.Headers["Access-Control-Request-Method"].ToString();
            if (!string.IsNullOrEmpty(reqHeaders))
                ctx.Response.Headers["Access-Control-Allow-Headers"] = reqHeaders;
            if (!string.IsNullOrEmpty(reqMethod))
                ctx.Response.Headers["Access-Control-Allow-Methods"] = reqMethod;
        }
        ctx.Response.StatusCode = StatusCodes.Status204NoContent;
        return;
    }
    await next();
});

// 3) Static files (ok lasciarlo qui)
app.UseStaticFiles();

// 4) Auth
app.UseAuthentication();
app.UseAuthorization();

// 5) Global exception wrapper per mantenere CORS anche sui 500
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Unhandled exception: {ex}");
        var origin = context.Request.Headers["Origin"].ToString();
        if (!string.IsNullOrEmpty(origin))
        {
            context.Response.Headers["Access-Control-Allow-Origin"] = origin;
            context.Response.Headers["Vary"] = "Origin";
        }
        context.Response.StatusCode = 500;
        await context.Response.WriteAsJsonAsync(new { error = "Internal Server Error" });
    }
});

// 6) Endpoints
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
