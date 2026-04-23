using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using PixPro.Services.Projects.Domain.Repositories;
using PixPro.Services.Projects.Infrastructure.Persistence;
using PixPro.Services.Projects.Infrastructure.Persistence.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddDbContext<ProjectsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ProjectsDb")));

builder.Services.AddScoped<IProjectRepository, ProjectRepository>();

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

app.Run();
