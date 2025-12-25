using Catalog.Application;
using Catalog.Application.Interfaces;
using Catalog.Application.Services;
using Catalog.Infrastructure;
using Catalog.Infrastructure.Services;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// ========== CONFIGURATION ==========
var configuration = builder.Configuration;
var redisConnectionString = configuration.GetConnectionString("Redis");

// ========== DEPENDENCY INJECTION ==========

// Infrastructure Layer
builder.Services.AddInfrastructure(configuration);

// Application Layer
builder.Services.AddApplication();

// API Layer services
builder.Services.AddScoped<IProductService, ProductService>();

// CACHE CONFIGURATION - КРИТИЧНО ВАЖЛИВО
if (!string.IsNullOrEmpty(redisConnectionString))
{
    Console.WriteLine("✅ Redis cache configured");
    
    // OPTION 1: Use StackExchange.Redis directly
    builder.Services.AddSingleton<IConnectionMultiplexer>(sp => 
    {
        try
        {
            var connection = ConnectionMultiplexer.Connect(redisConnectionString);
            Console.WriteLine($"Redis connected: {connection.IsConnected}");
            return connection;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Redis connection failed: {ex.Message}");
            throw;
        }
    });
    
    // Використовуємо RedisCacheService ТІЛЬКИ якщо Redis доступний
    builder.Services.AddSingleton<ICacheService, RedisCacheService>();
    
    // Додатково для сумісності
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
        options.InstanceName = "Catalog_";
    });
}
else
{
    Console.WriteLine("⚠️ Using in-memory cache (Redis not configured)");
    
    // OPTION 2: Use in-memory distributed cache for development
    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddSingleton<ICacheService, DistributedCacheService>();
}

// ========== API SERVICES ==========
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add logging
builder.Services.AddLogging();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ========== BUILD APP ==========
var app = builder.Build();

// ========== MIDDLEWARE PIPELINE ==========
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();

app.MapControllers();

app.MapGet("/", () => Results.Redirect("/swagger"));

app.MapGet("/api", () => Results.Ok(new
{
    Message = "Catalog API",
    Version = "1.0.0",
    Endpoints = new[]
    {
        "/api/products - CRUD for products",
        "/api/categories - Product categories",
        "/swagger - API documentation",
        "/health - Health check",
        "/api/cache-test - Test Redis cache"
    },
    Timestamp = DateTime.UtcNow
}));

// ========== HEALTH CHECKS ==========
app.MapGet("/health", async (IServiceProvider services, CancellationToken ct) =>
{
    try
    {
        // Перевірка кешу
        var cache = services.GetRequiredService<ICacheService>();
        await cache.SetAsync("health_test", "ok", TimeSpan.FromSeconds(5), ct);
        var cacheResult = await cache.GetAsync<string>("health_test", ct);
        
        return Results.Ok(new 
        { 
            Status = "Healthy",
            Cache = cacheResult == "ok" ? "Working" : "Not working",
            CacheType = cache.GetType().Name,
            Timestamp = DateTime.UtcNow,
            Environment = app.Environment.EnvironmentName
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            title: "Unhealthy",
            detail: ex.Message,
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

app.MapGet("/api/test", () => Results.Ok(new 
{ 
    Message = "Catalog API is running",
    Version = "1.0.0",
    Timestamp = DateTime.UtcNow
}));

app.MapGet("/api/cache-test", async (ICacheService cache, CancellationToken ct) =>
{
    var testKey = "cache_test_key";
    var testValue = new { Message = "Hello from cache", Time = DateTime.UtcNow };
    
    // Set cache
    await cache.SetAsync(testKey, testValue, TimeSpan.FromMinutes(1), ct);
    
    // Get cache
    var cachedValue = await cache.GetAsync<object>(testKey, ct);
    
    // Clear test cache
    await cache.RemoveAsync(testKey, ct);
    
    return Results.Ok(new
    {
        SetValue = testValue,
        RetrievedValue = cachedValue,
        CacheWorking = cachedValue != null,
        CacheType = cache.GetType().Name
    });
});

Console.WriteLine($"Environment: {app.Environment.EnvironmentName}");
Console.WriteLine($"Redis configured: {!string.IsNullOrEmpty(redisConnectionString)}");

app.Run();




/*using System.Text;
using Catalog.Application;
using Catalog.Application.Interfaces;
using Catalog.Application.Services;
using Catalog.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Catalog.Infrastructure.Persistence;
using Catalog.Infrastructure.Services;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddApplication();

builder.Services.AddScoped<IProductService, ProductService>();

var redisConnection = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrEmpty(redisConnection))
{
    builder.Services.AddSingleton<IConnectionMultiplexer>(sp => 
        ConnectionMultiplexer.Connect(redisConnection));
    
    builder.Services.AddSingleton<ICacheService, RedisCacheService>();
}

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var key = jwtSettings["Key"];

if (string.IsNullOrEmpty(key) || key.Length < 32)
{
    if (builder.Environment.IsDevelopment())
    {
        key = "DevelopmentSuperSecretKey_ChangeInProduction123!";
    }
    else
    {
        throw new InvalidOperationException("JWT Key not configured in appsettings.json");
    }
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
            {
                context.Response.Headers["Token-Expired"] = "true";
            }
            return Task.CompletedTask;
        }
    };
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString),
        mysqlOptions =>
        {
            mysqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null);
        });
    
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Catalog API",
        Version = "v1",
        Description = "API for Catalog Management System",
        Contact = new OpenApiContact
        {
            Name = "Development Team",
            Email = "dev@catalog.com"
        }
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
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

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

var corsSettings = builder.Configuration.GetSection("Cors");
var allowedOrigins = corsSettings.GetSection("AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:3000" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins",
        policy =>
        {
            policy.WithOrigins(allowedOrigins)
                   .AllowAnyMethod()
                   .AllowAnyHeader()
                   .AllowCredentials();
        });
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();

// Health check
if (connectionString != null)
    builder.Services.AddHealthChecks()
        .AddMySql(connectionString, name: "mysql-check")
        .AddDbContextCheck<ApplicationDbContext>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Catalog API v1");
        c.RoutePrefix = "swagger";
        c.DisplayRequestDuration();
        c.EnableFilter();
    });
    
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseCors("AllowSpecificOrigins");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health");

app.MapGet("/", () => Results.Redirect("/swagger"));

app.MapGet("/api/test", () => 
{
    return Results.Ok(new 
    { 
        Message = "Catalog API is running",
        Timestamp = DateTime.UtcNow,
        Version = "1.0.0",
        Environment = app.Environment.EnvironmentName
    });
}).AllowAnonymous();

app.MapGet("/api/test-db", async (ApplicationDbContext dbContext) =>
{
    try
    {
        var canConnect = await dbContext.Database.CanConnectAsync();
        var dbName = dbContext.Database.GetDbConnection().Database;
        
        return Results.Ok(new 
        { 
            Status = "Connected",
            Database = dbName,
            CanConnect = canConnect,
            dbContext.Database.GetDbConnection().ServerVersion
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            title: "Database Connection Failed",
            detail: ex.Message,
            statusCode: StatusCodes.Status503ServiceUnavailable
        );
    }
}).AllowAnonymous();

app.Run();

public partial class Program { }*/