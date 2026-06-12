using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PixPro.Services.Notifications.Application;
using PixPro.Services.Notifications.Infrastructure;
using PixPro.Services.Notifications.Infrastructure.Persistence;

// Load .env file if it exists (for local development without Docker)
var envPath = Path.Combine(Directory.GetCurrentDirectory(), "../../../../.env");
if (File.Exists(envPath))
{
    DotNetEnv.Env.Load(envPath);
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks();

// Register Application services
builder.Services.AddApplicationServices();

// Register Infrastructure services (MongoDB, RabbitMQ)
builder.Services.AddInfrastructureServices(builder.Configuration);

// Configure Dual Authentication (Auth0 + Local JWT)
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer not configured");
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience not configured");
var auth0Domain = builder.Configuration["Auth0:Domain"] ?? throw new InvalidOperationException("Auth0 Domain not configured");
var auth0Audience = builder.Configuration["Auth0:Audience"] ?? throw new InvalidOperationException("Auth0 Audience not configured");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "DualScheme";
    options.DefaultChallengeScheme = "DualScheme";
})

.AddJwtBearer("Auth0", options =>
{
    options.Authority = $"https://{auth0Domain}";
    options.Audience = auth0Audience;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = $"https://{auth0Domain}/",
        ValidateAudience = true,
        ValidAudience = auth0Audience,
        ValidateLifetime = true
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) &&
                (path.StartsWithSegments("/api/websocket") || path.StartsWithSegments("/hubs")))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
})

.AddJwtBearer("Local", options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) &&
                (path.StartsWithSegments("/api/websocket") || path.StartsWithSegments("/hubs")))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
})

.AddPolicyScheme("DualScheme", "Auth0 or Local JWT", options =>
{
    options.ForwardDefaultSelector = context =>
    {
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        string? token = null;

        if (authHeader?.StartsWith("Bearer ") == true)
            token = authHeader["Bearer ".Length..].Trim();
        else
            token = context.Request.Query["access_token"].FirstOrDefault();

        if (token != null)
        {
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            if (handler.CanReadToken(token))
            {
                var jwt = handler.ReadJwtToken(token);
                if (jwt.Issuer.Contains(auth0Domain))
                    return "Auth0";
            }
        }
        return "Local";
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy =>
        policy.RequireAuthenticatedUser()
              .RequireRole("Admin"));
});

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PixPro Notifications API",
        Version = "v1",
        Description = "Notifications and Messaging API for PixPro",
        Contact = new OpenApiContact
        {
            Name = "PixPro Team",
            Email = "support@pixpro.com"
        }
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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

var allowedOriginsNotifications = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:3000" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("WebSocketPolicy", policy =>
    {
        policy.WithOrigins(allowedOriginsNotifications)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Create MongoDB indexes on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
    await dbContext.CreateIndexesAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Enable WebSocket support
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromMinutes(2)
});

app.UseCors("WebSocketPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
