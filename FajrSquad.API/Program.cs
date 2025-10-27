using System.Text;
using System.Text.Json.Serialization;
using FajrSquad.Core.Config;
using FajrSquad.Infrastructure.Data;
using FajrSquad.Infrastructure.Services;
using FajrSquad.Infrastructure.Services.Adhkar;
using FajrSquad.API.Jobs;
using FajrSquad.Core.Profiles;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Quartz;
using DotNetEnv; // 👈 aggiunto
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.IIS;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
var provider = configuration["DatabaseProvider"];

// 🔹 Carica .env SOLO in Development
if (builder.Environment.IsDevelopment())
{
    DotNetEnv.Env.Load();
    Console.WriteLine("✅ .env file loaded in Development");
}

// 🔹 Log variabili R2 (debug)
Console.WriteLine("🌍 Environment: " + builder.Environment.EnvironmentName);
Console.WriteLine("✅ R2_BUCKET_NAME: " + Environment.GetEnvironmentVariable("R2_BUCKET_NAME"));
Console.WriteLine("✅ R2_PUBLIC_URL: " + Environment.GetEnvironmentVariable("R2_PUBLIC_URL"));

// 🔹 Firebase Admin SDK Initialization
try
{
    var firebaseCredentialsPath = Environment.GetEnvironmentVariable("FIREBASE_CONFIG");
    if (!string.IsNullOrEmpty(firebaseCredentialsPath) && File.Exists(firebaseCredentialsPath))
    {
        FirebaseApp.Create(new AppOptions()
        {
            Credential = GoogleCredential.FromFile(firebaseCredentialsPath)
        });
        Console.WriteLine("✅ Firebase Admin SDK initialized with credentials file");
    }
    else
    {
        // Fallback to default credentials (useful for cloud deployments)
        FirebaseApp.Create();
        Console.WriteLine("✅ Firebase Admin SDK initialized with default credentials");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Firebase initialization failed: {ex.Message}");
    // Don't fail the app startup, but log the error
}

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

// Adhkar services
builder.Services.AddScoped<IAdhkarService, AdhkarService>();

// Notification services
builder.Services.AddScoped<INotificationSender, FcmNotificationSender>();
builder.Services.AddScoped<IMessageBuilder, MessageBuilder>();
builder.Services.AddScoped<INotificationScheduler, NotificationScheduler>();
builder.Services.AddScoped<INotificationPrivacyService, NotificationPrivacyService>();
builder.Services.AddScoped<INotificationMetricsService, NotificationMetricsService>();

// 🔹 Quartz
builder.Services.AddQuartz(q =>
{
    // UseMicrosoftDependencyInjectionJobFactory() è obsoleto - è già il default

    // Legacy jobs (keep for backward compatibility)
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

    // New comprehensive notification jobs
    q.ScheduleJob<MorningReminderJob>(t => t
        .WithIdentity("morningReminder")
        .WithCronSchedule("0 */10 * * * ?")); // Every 10 minutes

    q.ScheduleJob<EveningReminderJob>(t => t
        .WithIdentity("eveningReminder")
        .WithCronSchedule("0 */10 * * * ?")); // Every 10 minutes

    q.ScheduleJob<FajrMissCheckJob>(t => t
        .WithIdentity("fajrMissCheck")
        .WithCronSchedule("0 30 8 * * ?")); // 08:30 daily

    q.ScheduleJob<EscalationMidMorningJob>(t => t
        .WithIdentity("escalationMidMorning")
        .WithCronSchedule("0 30 11 * * ?")); // 11:30 daily

    q.ScheduleJob<DailyHadithJob>(t => t
        .WithIdentity("dailyHadithNew")
        .WithCronSchedule("0 0 8 * * ?")); // 08:00 daily

    q.ScheduleJob<DailyMotivationJob>(t => t
        .WithIdentity("dailyMotivationNew")
        .WithCronSchedule("0 5 8 * * ?")); // 08:05 daily

    q.ScheduleJob<EventReminderSweepJob>(t => t
        .WithIdentity("eventReminderSweep")
        .WithCronSchedule("0 */15 * * * ?")); // Every 15 minutes

    q.ScheduleJob<ProcessScheduledNotificationsJob>(t => t
        .WithIdentity("processScheduledNotifications")
        .WithCronSchedule("0 */5 * * * ?")); // Every 5 minutes

    q.ScheduleJob<NotificationCleanupJob>(t => t
        .WithIdentity("notificationCleanup")
        .WithCronSchedule("0 0 2 * * ?")); // Daily at 02:00
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
                Console.WriteLine("❌ Authentication failed: " + ctx.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = ctx =>
            {
                Console.WriteLine("✅ Token validato correttamente.");
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

// 🔹 Configurazione per file upload
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 10 * 1024 * 1024; // 10MB
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10MB
    options.ValueLengthLimit = int.MaxValue;
    options.ValueCountLimit = int.MaxValue;
    options.KeyLengthLimit = int.MaxValue;
});

// 🔹 Extra
builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();
builder.Services.AddHealthChecks();
builder.Services.AddAutoMapper(typeof(FajrProfile), typeof(AdhkarProfile));

// 🔹 Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// 🔹 CORS
var originsFromEnv = Environment.GetEnvironmentVariable("CORS_ORIGINS");
string[] allowedOrigins = !string.IsNullOrWhiteSpace(originsFromEnv)
    ? originsFromEnv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    : new[]
    {
        "http://localhost:8100",
        "http://localhost:8101",
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
              .AllowAnyMethod();
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
app.UseRouting();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

// 🔹 Global exception wrapper per mantenere CORS anche sui 500
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

app.MapControllers();
app.MapHealthChecks("/health");

// Notification health check
app.MapGet("/health/notifications", async (INotificationMetricsService metricsService) =>
{
    try
    {
        var last24Hours = DateTimeOffset.UtcNow.AddDays(-1);
        var metrics = await metricsService.GetMetricsAsync(last24Hours, DateTimeOffset.UtcNow);
        
        return Results.Ok(new
        {
            status = "healthy",
            last24Hours = new
            {
                totalSent = metrics.TotalSent,
                totalFailed = metrics.TotalFailed,
                successRate = metrics.SuccessRate,
                sentByType = metrics.SentByType
            },
            timestamp = DateTimeOffset.UtcNow
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: ex.Message,
            statusCode: 500,
            title: "Notification system unhealthy"
        );
    }
});

app.Run();
