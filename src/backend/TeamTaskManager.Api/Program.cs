using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TeamTaskManager.Application.Authentication;
using TeamTaskManager.Infrastructure;
using TeamTaskManager.Api.Contracts;
using TeamTaskManager.Api.Middleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeamTaskManager.Infrastructure.Persistence;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy => policy
    .WithOrigins("http://localhost:5173")
    .AllowAnyHeader()
    .AllowAnyMethod()));
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .SelectMany(entry => entry.Value!.Errors.Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage) ? $"{entry.Key} alanı geçersiz." : error.ErrorMessage))
            .ToArray();
        return new BadRequestObjectResult(ApiResponse<object>.Fail("İstek doğrulanamadı.", errors));
    };
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Login endpointinden alınan JWT access token."
    });
});
builder.Services.AddInfrastructure(builder.Configuration);

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Jwt yapılandırması eksik.");

if (string.IsNullOrWhiteSpace(jwtOptions.Key) || jwtOptions.Key.Length < 32)
{
    throw new InvalidOperationException("Jwt:Key en az 32 karakter olmalıdır.");
}

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail("Kimlik doğrulaması gerekli.", "Geçerli bir access token gönderin."));
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail("Bu işlem için yetkiniz yok.", "Kullanıcı rolü bu endpoint için yeterli değil."));
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (builder.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    await app.Services.SeedAdminAsync(builder.Configuration);
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("Frontend");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
    .AllowAnonymous();

app.Run();
