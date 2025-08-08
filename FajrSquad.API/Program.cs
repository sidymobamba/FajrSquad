using System.Text;
using System.Text.Json.Serialization;
using FajrSquad.Core.Config;
using FajrSquad.Infrastructure.Data;
using FajrSquad.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
var provider = configuration["DatabaseProvider"];

// 🔹 Database
builder.Services.AddDbContext<FajrDbContext>(options =>
{
    var cs = configuration.GetConnectionString("DefaultConnection");
    if (provider == "postgres")
        options.UseNpgsql(cs);
    else
        options.UseSqlServer(cs);
});

// 🔹 Controllers + JSON + Newtonsoft
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    })
    .AddNewtonsoftJson(); // NECESSARIO per JObject

// 🔹 Swagger + JWT auth
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

// 🔹 Jwt settings e auth
builder.Services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
builder.Services.AddScoped<JwtService>();

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

// 🔹 Dependency injection
builder.Services.AddScoped<IFajrService, FajrService>();
builder.Services.AddScoped<IFileUploadService, FileUploadService>();
builder.Services.AddScoped<ICacheService, MemoryCacheService>();

// 🔹 HttpClient per chiamate API esterne
builder.Services.AddHttpClient();

// 🔹 Caching + static files + logging
builder.Services.AddMemoryCache();
builder.Services.Configure<StaticFileOptions>(options => options.ServeUnknownFileTypes = false);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Services.AddHealthChecks();

var app = builder.Build();

// 🔹 Dev-only: Swagger + eccezioni
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage(); // Mostra eccezioni dettagliate
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 🔹 DB seeding (opzionale)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FajrDbContext>();
    // db.Database.Migrate(); // solo se hai le migration
    // await IslamicDataSeeder.SeedAsync(db); // opzionale
}

// 🔹 Middleware personalizzati
app.UseMiddleware<FajrSquad.API.Middleware.GlobalExceptionMiddleware>();
app.UseMiddleware<FajrSquad.API.Middleware.RateLimitingMiddleware>();

// 🔹 Middlewares standard
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors(policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
app.UseAuthentication();
app.UseAuthorization();

// 🔹 Routing
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
