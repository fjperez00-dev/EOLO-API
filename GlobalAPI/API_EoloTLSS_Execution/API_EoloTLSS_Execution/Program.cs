using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Middleware de seguridad: API Key
app.Use(async (context, next) =>
{
    var apiKey = context.Request.Headers["X-API-KEY"].FirstOrDefault();
    var validKey = builder.Configuration["ApiKey"];

    if (apiKey == null || apiKey != validKey)
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsync("API Key inválida o no proporcionada.");
        return;
    }

    await next.Invoke();
});

app.UseAuthorization();
app.MapControllers();
app.Run();
