using System.Text;
using BuffetDiscovery.Api.Middleware;
using BuffetDiscovery.Api.Services;
using BuffetDiscovery.Application;
using BuffetDiscovery.Application.Common.Interfaces;
using BuffetDiscovery.Infrastructure;
using BuffetDiscovery.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddApplication();

var uploadsRootPath = Path.Combine(builder.Environment.WebRootPath, "uploads");
builder.Services.AddInfrastructure(builder.Configuration, uploadsRootPath);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

var jwtSection = builder.Configuration.GetSection("Jwt");
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
        ValidIssuer = jwtSection["Issuer"],
        ValidAudience = jwtSection["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]!))
    };
});

builder.Services.AddAuthorization();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    try
    {
        await db.Database.MigrateAsync();
    }
    catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.DuplicateTable)
    {
        // The migration history was rewritten when the schema moved from Offerings to
        // Services, so a database created before that still holds the old tables while its
        // history knows nothing about the new migration. There is no incremental path
        // between the two shapes: the database has to be recreated.
        var database = db.Database.GetDbConnection().Database;
        throw new InvalidOperationException(
            $"""
            The '{database}' database was built by an older version of this project and cannot be migrated in place.

            Drop and recreate it, then start the API again — the seeder will repopulate the demo data:

                dotnet ef database drop --force --project src/BuffetDiscovery.Infrastructure --startup-project src/BuffetDiscovery.Api

            Any data you added by hand will be lost.
            """, ex);
    }

    await DbSeeder.SeedAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseStaticFiles();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
