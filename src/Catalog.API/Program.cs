using System.Text;
using Catalog.Application;
using Catalog.Application.Interfaces;
using Catalog.Application.Services;
using Catalog.Infrastructure;
using Catalog.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
var environment = builder.Environment;

builder.Services.AddInfrastructure(configuration);
builder.Services.AddApplication();

builder.Services.AddScoped<IProductService, ProductService>();

var redisConnectionString = configuration.GetConnectionString("Redis");

if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    Console.WriteLine("Redis configured");

    builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
        ConnectionMultiplexer.Connect(redisConnectionString));

    builder.Services.AddSingleton<ICacheService, RedisCacheService>();

    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
        options.InstanceName = "Catalog_";
    });
}
else
{
    Console.WriteLine("Redis not configured — using InMemory cache");

    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddSingleton<ICacheService, DistributedCacheService>();
}

var jwtSettings = configuration.GetSection("JwtSettings");
var jwtKey = jwtSettings["Key"];

if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
{
    if (environment.IsDevelopment())
    {
        jwtKey = "DevelopmentSuperSecretKey_ChangeInProduction123!";
        Console.WriteLine("Using DEVELOPMENT JWT key");
    }
    else
    {
        throw new InvalidOperationException("JWT Key is not configured");
    }
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"] ?? "catalog-api",
            ValidAudience = jwtSettings["Audience"] ?? "catalog-client",
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = ctx =>
            {
                if (ctx.Exception is SecurityTokenExpiredException)
                    ctx.Response.Headers["Token-Expired"] = "true";
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Catalog API",
        Version = "v1",
        Description = "Catalog Management API"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Bearer {your JWT token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var connectionString = configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddHealthChecks()
        .AddMySql(connectionString, name: "mysql");
}

var app = builder.Build();

if (environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/", () => Results.Redirect("/swagger"));

app.MapGet("/api/test", () => Results.Ok(new
{
    Message = "Catalog API is running",
    Environment = environment.EnvironmentName,
    Timestamp = DateTime.UtcNow
})).AllowAnonymous();

app.MapGet("/api/cache-test", async (ICacheService cache, CancellationToken ct) =>
{
    const string key = "cache_test";
    await cache.SetAsync(key, "ok", TimeSpan.FromSeconds(10), ct);
    var value = await cache.GetAsync<string>(key, ct);

    return Results.Ok(new
    {
        CacheType = cache.GetType().Name,
        Result = value
    });
}).AllowAnonymous();

app.MapHealthChecks("/health");

Console.WriteLine($"Catalog API started ({environment.EnvironmentName})");

app.Run();

public partial class Program { }