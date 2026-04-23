using Microsoft.EntityFrameworkCore;
using NosVemos.OrquestadorIA.Api.Services;

var builder = WebApplication.CreateBuilder(args);
var servicePort = builder.Configuration.GetValue<int?>("ServicePort") ?? 7004;
builder.WebHost.UseUrls($"http://0.0.0.0:{servicePort}");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<ImageAnalysisService>();
var useInMemory = builder.Configuration.GetValue<bool?>("UseInMemoryDatabase") ?? false;
builder.Services.AddDbContext<AnalisisDbContext>(options =>
{
    if (useInMemory)
    {
        options.UseInMemoryDatabase("IADB");
    }
    else
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer"));
    }
});
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

builder.Services.AddCors(options =>
{
    options.AddPolicy("open", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

AnalisisDatabaseInitializer.Initialize(app.Services);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("open");

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "orquestador-ia" }));
app.MapIaEndpoints();

app.Run();

public partial class Program;
