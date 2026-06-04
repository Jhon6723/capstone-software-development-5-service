using Auth0.AspNetCore.Authentication.Api;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PixPro.Services.Auth.Application;
using PixPro.Services.Auth.Domain.Repositories;
using PixPro.Services.Auth.Infrastructure.Persistence;
using PixPro.Services.Auth.Infrastructure.Persistence.Repositories;
using System.Text;

// Load .env file if it exists (for local development without Docker)
var envPath = Path.Combine(Directory.GetCurrentDirectory(), "../../../../.env");
if (File.Exists(envPath))
{
    DotNetEnv.Env.Load(envPath);
}

var builder = WebApplication.CreateBuilder(args);

builder.Configuration["ConnectionStrings:AuthDb"] =
    Environment.GetEnvironmentVariable("AUTH_DB_CONNECTION") ?? "";
builder.Configuration["Jwt:ExpirationMinutes"] =
    Environment.GetEnvironmentVariable("JWT_EXPIRATION_MINUTES") ?? "60";

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks();

builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("AuthDb")));

// Register Infrastructure services
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Register Application services
builder.Services.AddApplicationServices();

var auth0Domain = builder.Configuration["Auth0:Domain"]! ?? throw new InvalidOperationException("Auth0 Domain not configured");
var auth0Audience = builder.Configuration["Auth0:Audience"]! ?? throw new InvalidOperationException("Auth0 Audience not configured");
var jwtSecret = builder.Configuration["Jwt:Secret"]! ?? throw new InvalidOperationException("JWT Secret not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"]! ?? throw new InvalidOperationException("JWT Issuer not configured");
var jwtAudience = builder.Configuration["Jwt:Audience"]! ?? throw new InvalidOperationException("JWT Audience not configured");

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
})

.AddJwtBearer("Local", options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSecret))
    };
})

.AddPolicyScheme("DualScheme", "Auth0 or Local JWT", options =>
{
    options.ForwardDefaultSelector = context =>
    {
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        if (authHeader?.StartsWith("Bearer ") == true)
        {
            var token = authHeader["Bearer ".Length..].Trim();
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            if (handler.CanReadToken(token))
            {
                var jwt = handler.ReadJwtToken(token);
                var issuer = jwt.Issuer;
                if (issuer.Contains(auth0Domain))
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
        Title = "PixPro Auth API",
        Version = "v1",
        Description = "Authentication and Authorization API for PixPro",
        Contact = new OpenApiContact
        {
            Name = "PixPro Team",
            Email = "support@pixpro.com"
        }
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header. It supports Auth0 tokens and local tokens.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "PixPro Auth API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();