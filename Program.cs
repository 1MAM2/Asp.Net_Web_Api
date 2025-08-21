using Microsoft.EntityFrameworkCore;
using productApi.Context;

var builder = WebApplication.CreateBuilder(args);

// SQL Server / MySQL bağlantısı
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
        options.JsonSerializerOptions.NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals;
        options.JsonSerializerOptions.WriteIndented = true;
    });

builder.Services.AddDbContext<productDb>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString)
    )
);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS - tek policy içinde hem local hem Vercel domaini
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
            "http://localhost:5173", 
            "https://e-shop-roan-eight.vercel.app"
        )
        .AllowAnyHeader()
        .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// UseCors middleware: mutlaka authorization veya controller mapping'den önce
app.UseCors("AllowFrontend");

app.UseAuthorization();
app.MapControllers();
app.Run();
