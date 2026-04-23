using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var servicePort = builder.Configuration.GetValue<int?>("ServicePort") ?? 7001;
builder.WebHost.UseUrls($"http://0.0.0.0:{servicePort}");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var useInMemory = builder.Configuration.GetValue<bool?>("UseInMemoryDatabase") ?? true;
builder.Services.AddDbContext<AuthDbContext>(options =>
{
    if (useInMemory)
    {
        options.UseInMemoryDatabase("AutenticacionDB");
    }
    else
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer"));
    }
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var jwtKey = app.Configuration["Jwt:SecretKey"]
    ?? throw new InvalidOperationException("Missing Jwt:SecretKey configuration.");

AuthDatabaseInitializer.Initialize(app.Services, useInMemory);

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "autenticacion" }));
app.MapAuthEndpoints(jwtKey);

app.Run();

public partial class Program;
