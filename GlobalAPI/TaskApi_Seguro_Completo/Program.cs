using Microsoft.OpenApi.Models;
using TaskApi_EOLO;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
c.SwaggerDoc("v1", new OpenApiInfo { Title = "Task API", Version = "v1" });    
});
var app = builder.Build();

app.UseHsts();
app.UseHttpsRedirection();

    app.UseSwagger();
    app.UseSwaggerUI();

app.MapGet("/", context =>
{
    context.Response.Redirect("/swagger");
    return Task.CompletedTask;
});

app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.MapControllers();

app.Run();
