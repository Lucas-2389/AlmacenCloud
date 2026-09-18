using System.IdentityModel.Tokens.Jwt;
using System.Text;
using AlmacenCloud.API.Middleware;
using AlmacenCloud.API.Services;
using AlmacenCloud.Application.Abstractions;
using AlmacenCloud.Application.Features.Auth;
using AlmacenCloud.Application.Features.Inventory;
using AlmacenCloud.Application.Features.Sales;
using AlmacenCloud.Infrastructure.Identity;
using AlmacenCloud.Infrastructure.Persistence;
using AlmacenCloud.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.SnakeCaseUpper)));
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CurrentUser>();
builder.Services.AddScoped<ICurrentUser>(sp => sp.GetRequiredService<CurrentUser>());
builder.Services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<CurrentUser>());
builder.Services.AddScoped<IIdentityRepository, IdentityRepository>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IInventoryCoreRepository, InventoryCoreRepository>();
builder.Services.AddScoped<IInventoryCoreService, InventoryCoreService>();
builder.Services.AddScoped<ISalesRepository, SalesRepository>();
builder.Services.AddScoped<ISalesService, SalesService>();
var taxRate = builder.Configuration.GetValue<decimal?>("SalesTax:Rate") ?? 0.18m;
builder.Services.AddSingleton<ISalesTaxCalculator>(new SalesTaxCalculator(taxRate));

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.AddDbContext<AlmacenCloudDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("MySql");
    if (string.IsNullOrWhiteSpace(connectionString))
        throw new InvalidOperationException("Configure ConnectionStrings:MySql mediante user-secrets o variables de entorno.");
    options.UseMySQL(connectionString);
});

JwtSecurityTokenHandler.DefaultMapInboundClaims = false;
var jwt = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var principal = context.Principal;
                if (!Guid.TryParse(principal?.FindFirst("sub")?.Value, out _) ||
                    !Guid.TryParse(principal?.FindFirst("empresa_id")?.Value, out _))
                    context.Fail("El token no contiene un contexto tenant válido.");
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddCors(options => options.AddPolicy("FrontendDevelopment", policy =>
    policy.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "AlmacenCloud API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document, null)] = []
    });
});

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors("FrontendDevelopment");
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/api/v1/health", () => Results.Ok(new { status = "ok", message = "AlmacenCloud API is running" }));

app.Run();

public partial class Program;
