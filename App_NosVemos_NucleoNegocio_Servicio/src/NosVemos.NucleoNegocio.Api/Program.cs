using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var servicePort = builder.Configuration.GetValue<int?>("ServicePort") ?? 7003;
builder.WebHost.UseUrls($"http://0.0.0.0:{servicePort}");
var useInMemory = builder.Configuration.GetValue<bool?>("UseInMemoryDatabase") ?? true;
builder.Services.AddDbContext<NucleoDbContext>(options =>
{
    if (useInMemory)
    {
        options.UseInMemoryDatabase("NucleoDB");
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

var rabbitSettings = builder.Configuration.GetSection("RabbitMq").Get<RabbitMqSettings>() ?? new RabbitMqSettings();
var enableEvents = builder.Configuration.GetValue<bool?>("Messaging:EnableDomainEvents") ?? true;
if (enableEvents)
{
    builder.Services.AddSingleton<IEventPublisher>(sp =>
        new RabbitMqEventPublisher(rabbitSettings, sp.GetRequiredService<ILogger<RabbitMqEventPublisher>>()));
}
else
{
    builder.Services.AddSingleton<IEventPublisher, NoOpEventPublisher>();
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

NucleoDatabaseInitializer.Initialize(app.Services, useInMemory);

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "nucleo-negocio" }));
app.MapNucleoEndpoints();

app.Run();

public partial class Program;
