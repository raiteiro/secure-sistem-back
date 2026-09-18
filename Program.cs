using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SecureSistem.Data;
using SecureSistem.Services;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? new[] { "http://localhost:4200" })
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Services
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger with JWT support
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "SecureSistem API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token"
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
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

// Two things on every genuinely authenticated request (never on /auth/refresh itself,
// which carries no Bearer token):
// 1. Rejects the request immediately if this JWT's session (its "sessionId" claim, the
//    RefreshToken it was issued alongside) has been revoked — e.g. because the same user
//    logged in elsewhere. Without this, a revoked RefreshToken only blocks future renewals;
//    an already-issued access token would otherwise keep working until it expires (up to
//    Jwt:ExpirationHours). Tokens issued before this check existed have no "sessionId"
//    claim — they're let through untouched rather than force-logged-out on deploy.
// 2. Stamps User.LastSeenAt — real inactivity signal for AuthController's idle-session
//    check, independent of how often the refresh token silently renews itself.
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var sessionIdClaim = context.User.FindFirstValue("sessionId");

        if (userIdClaim is not null && int.TryParse(userIdClaim, out var userId))
        {
            var db = context.RequestServices.GetRequiredService<ApplicationDbContext>();

            if (sessionIdClaim is not null && int.TryParse(sessionIdClaim, out var sessionId))
            {
                var isRevoked = await db.RefreshTokens
                    .Where(rt => rt.Id == sessionId)
                    .Select(rt => (bool?)(rt.RevokedAt != null))
                    .FirstOrDefaultAsync() ?? true;

                if (isRevoked)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        message = "Tu sesión se cerró porque iniciaste sesión en otro lugar."
                    });
                    return;
                }
            }

            await db.Users.Where(u => u.Id == userId)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.LastSeenAt, DateTime.UtcNow));
        }
    }

    await next();
});

app.MapControllers();

app.Run();
