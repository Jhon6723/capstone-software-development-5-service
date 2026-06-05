using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using PixPro.Services.Projects.Application;
using PixPro.Services.Projects.Application.Interfaces;
using PixPro.Services.Projects.Application.Services;
using PixPro.Services.Projects.Domain.Repositories;
using PixPro.Services.Projects.Infrastructure.Cloudinary;
using PixPro.Services.Projects.Infrastructure.Messaging;
using PixPro.Services.Projects.Infrastructure.Persistence;
using PixPro.Services.Projects.Infrastructure.Persistence.Repositories;

// Load .env file if it exists (for local development without Docker)
var envPath = Path.Combine(Directory.GetCurrentDirectory(), "../../../../.env");
if (File.Exists(envPath))
{
    DotNetEnv.Env.Load(envPath);
}

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks();

builder.Services.AddDbContext<ProjectsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ProjectsDb")));

builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<IImageRepository, ImageRepository>();
builder.Services.AddScoped<IUserCreditRepository, UserCreditRepository>();
builder.Services.AddSingleton<IMessagePublisher, RabbitMqMessagePublisher>();
builder.Services.AddHostedService<ProcessedImageEventConsumer>();
builder.Services.Configure<CloudinaryOptions>(
    builder.Configuration.GetSection(CloudinaryOptions.SectionName));

builder.Services.AddScoped<IStorageService, CloudinaryStorageService>();
builder.Services.AddApplicationServices();

// Configure Dual Authentication (Auth0 + Local JWT)
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer not configured");
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience not configured");
var auth0Domain = builder.Configuration["Auth0:Domain"]! ?? throw new InvalidOperationException("Auth0 Domain not configured");
var auth0Audience = builder.Configuration["Auth0:Audience"]! ?? throw new InvalidOperationException("Auth0 Audience not configured");

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
        Title = "PixPro Projects API",
        Version = "v1",
        Description = "Projects Management API for PixPro",
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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "PixPro Projects API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
