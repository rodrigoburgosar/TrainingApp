using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;
using Serilog.Context;
using SportFlow.Application.Abstractions;
using SportFlow.Application.Identity.Commands;
using SportFlow.Application.Identity.DTOs;
using SportFlow.Application.Identity.Queries;
using SportFlow.Application.Identity.Validators;
using SportFlow.Domain.Identity;
using SportFlow.Infrastructure.Persistence;
using SportFlow.Infrastructure.Persistence.Repositories;
using SportFlow.Infrastructure.Services;
using SportFlow.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog ─────────────────────────────────────────────────────────────────
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
.WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {TenantId} {UserId} {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/sportflow-.log",
        rollingInterval: RollingInterval.Day,
        outputTemplate:
        "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {TenantId} {UserId} {Message:lj}{NewLine}{Exception}"));

// ── Database ─────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<SportFlowDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("SportFlow.Infrastructure")));

// ── JWT Authentication ───────────────────────────────────────────────────────
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"]!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

// ── Authorization Policies ───────────────────────────────────────────────────
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireSuperAdmin", policy =>
        policy.RequireClaim("role", SystemRoles.SuperAdmin));

    options.AddPolicy("RequireTenantOwner", policy =>
        policy.RequireClaim("role",
            SystemRoles.SuperAdmin,
            SystemRoles.TenantOwner));

    options.AddPolicy("RequireStaffOrAbove", policy =>
        policy.RequireClaim("role",
            SystemRoles.SuperAdmin,
            SystemRoles.TenantOwner,
            SystemRoles.TenantManager,
            SystemRoles.Staff,
            SystemRoles.Coach));

    options.AddPolicy("RequireCoachOrAbove", policy =>
        policy.RequireClaim("role",
            SystemRoles.SuperAdmin,
            SystemRoles.TenantOwner,
            SystemRoles.TenantManager,
            SystemRoles.Coach));
});

// ── Repositories & Services ─────────────────────────────────────────────────
builder.Services.AddScoped<TenantContext>();
builder.Services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<ITenantRepository, StubTenantRepository>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<SportFlowDbContext>());
builder.Services.AddScoped<IEmailService, StubEmailService>();

// ── Command & Query Handlers ─────────────────────────────────────────────────
builder.Services.AddScoped<ICommandHandler<LoginRequest, TokenResponse>, LoginCommandHandler>();
builder.Services.AddScoped<ICommandHandler<RefreshTokenRequest, TokenResponse>, RefreshTokenCommandHandler>();
builder.Services.AddScoped<ICommandHandler<LogoutRequest>, LogoutCommandHandler>();
builder.Services.AddScoped<ICommandHandler<ChangePasswordRequest>, ChangePasswordCommandHandler>();
builder.Services.AddScoped<ICommandHandler<ForgotPasswordRequest>, ForgotPasswordCommandHandler>();
builder.Services.AddScoped<ICommandHandler<ResetPasswordRequest>, ResetPasswordCommandHandler>();
builder.Services.AddScoped<ICommandHandler<RevokeSessionCommand>, RevokeSessionCommandHandler>();
builder.Services.AddScoped<IQueryHandler<GetMeQuery, MeResponse>, GetMeQueryHandler>();
builder.Services.AddScoped<IQueryHandler<GetSessionsQuery, IReadOnlyList<SessionResponse>>, GetSessionsQueryHandler>();

// ── FluentValidation ─────────────────────────────────────────────────────────
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();

// ── Swagger ───────────────────────────────────────────────────────────────────
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SportFlow API",
        Version = "v1",
        Description = "Multi-tenant SaaS platform for gym management"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer"),
            new List<string>()
        }
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// ── Middleware Pipeline ──────────────────────────────────────────────────────
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SportFlow API v1");
        c.DisplayRequestDuration();
    });
}

app.UseAuthentication();
app.UseMiddleware<TenantResolutionMiddleware>();

// Enrich Serilog with TenantId and UserId per request
app.Use(async (context, next) =>
{
    var tenantCtx = context.RequestServices.GetService<ITenantContext>();
    using (LogContext.PushProperty("TenantId", tenantCtx?.TenantId?.Value.ToString() ?? "-"))
    using (LogContext.PushProperty("UserId", tenantCtx?.UserId.Value.ToString() ?? "-"))
    {
        await next();
    }
});

app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
