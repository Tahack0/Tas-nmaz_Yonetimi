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
using TasinmazYönetimi.Application.SpatialAnalyses;
using TasinmazYönetimi.Infrastructure.SpatialAnalyses;
using TasinmazYönetimi.Application.Locations;
using TasinmazYönetimi.Infrastructure.Locations;
using TasinmazYönetimi.Application.PropertyMedia;
using TasinmazYönetimi.Infrastructure.PropertyMedia;



var builder = WebApplication.CreateBuilder(args);

if (OperatingSystem.IsWindows())
{
    GlobalFontSettings.UseWindowsFontsUnderWindows = true;
}

builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentRequestContext, HttpCurrentRequestContext>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();
builder.Services.AddScoped<ILoginService, LoginService>();
builder.Services.AddScoped<IPropertyService, PropertyService>();
builder.Services.AddScoped<IPropertyExportService, PropertyExportService>();
builder.Services.AddScoped<IAuditLogExportService, AuditLogExportService>();
builder.Services.AddScoped<IPropertyImportService, PropertyImportService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<ISpatialAnalysisService, SpatialAnalysisService>();
builder.Services.AddScoped<ILocationService, LocationService>();
builder.Services.Configure<PropertyMediaOptions>(options =>
{
    string directory = builder.Configuration["PropertyMedia:Directory"]
        ?? Path.Combine("Storage", "PropertyPhotos");
    options.RootDirectory = Path.GetFullPath(directory, builder.Environment.ContentRootPath);
});
builder.Services.AddScoped<IPropertyMediaService, PropertyMediaService>();

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
const string FrontendCorsPolicy = "Frontend";

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        FrontendCorsPolicy,
        policy =>
        {
            policy
                .WithOrigins("http://localhost:4200")
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
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
app.UseCors(FrontendCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
//boş konum tablolarını doldurmak için
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();

    var db = scope.ServiceProvider
        .GetRequiredService<ApplicationDbContext>();

    string filePath = Path.Combine(
        app.Environment.ContentRootPath,
        "Data",
        "Locations",
        "turkiye-locations.json");

    await LocationDataSeeder.SeedAsync(db, filePath);
}
app.Run();