using PdfSharp.Fonts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using TasinmazYönetimi.Api.Security;
using TasinmazYönetimi.Application.Auth;
using TasinmazYönetimi.Infrastructure.Persistence;
using TasinmazYönetimi.Infrastructure.Security;
using TasinmazYönetimi.Application.Properties;
using TasinmazYönetimi.Infrastructure.Properties;
using Microsoft.OpenApi.Models;
using TasinmazYönetimi.Api.Services;
using TasinmazYönetimi.Application.Auditing;
using TasinmazYönetimi.Application.Common;
using TasinmazYönetimi.Infrastructure.Auditing;
using Microsoft.AspNetCore.WebSockets;
using TasinmazYönetimi.Application.Users;
using TasinmazYönetimi.Infrastructure.Users;



var builder = WebApplication.CreateBuilder(args);

if (OperatingSystem.IsWindows())
{
    GlobalFontSettings.UseWindowsFontsUnderWindows = true;
}

builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentRequestContext,HttpCurrentRequestContext>();
builder.Services.AddScoped<IAuditLogService,AuditLogService>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();
builder.Services.AddScoped<ILoginService, LoginService>();
builder.Services.AddScoped<IPropertyService,PropertyService>();
builder.Services.AddScoped<IPropertyExportService,PropertyExportService>();
builder.Services.AddScoped<IAuditLogExportService,AuditLogExportService>();
builder.Services.AddScoped<IPropertyImportService,PropertyImportService>();
builder.Services.AddScoped<IUserManagementService,UserManagementService>();


string jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key Yapilandirilmamis.");

byte[] keyBytes = Convert.FromBase64String(jwtKey);

if (keyBytes.Length < 32)
{
    throw new InvalidOperationException("Jwt:Key en az 32 bayt olmali.");
}

var signingKey = new SymmetricSecurityKey(keyBytes);
builder.Services.AddSingleton(signingKey);
builder.Services.AddSingleton<JwtTokenService>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "TasinmazYonetimi.Api",
            ValidateAudience = true,
            ValidAudience = "TasinmazYonetimi.Client",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            NameClaimType = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub,
            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "JWT access token değerini girin.",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
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
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();