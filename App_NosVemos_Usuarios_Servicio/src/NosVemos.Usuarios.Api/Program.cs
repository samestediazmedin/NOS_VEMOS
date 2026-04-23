using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var servicePort = builder.Configuration.GetValue<int?>("ServicePort") ?? 7002;
builder.WebHost.UseUrls($"http://0.0.0.0:{servicePort}");
var useInMemory = builder.Configuration.GetValue<bool?>("UseInMemoryDatabase") ?? true;
builder.Services.AddDbContext<UsuariosDbContext>(options =>
{
    if (useInMemory)
    {
        options.UseInMemoryDatabase("UsuariosDB");
    }
    else
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer"));
    }
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var jwtKey = builder.Configuration["Jwt:SecretKey"]
    ?? throw new InvalidOperationException("Missing Jwt:SecretKey configuration.");
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = "NosVemos.Auth",
            ValidAudience = "NosVemos.Client",
            IssuerSigningKey = signingKey,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

UsuariosDatabaseInitializer.Initialize(app.Services, useInMemory);

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "usuarios" }));
app.MapUsuariosEndpoints();

app.Run();

public partial class Program;
