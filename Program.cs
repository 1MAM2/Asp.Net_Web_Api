using Microsoft.EntityFrameworkCore;
using productApi.Context;

var builder = WebApplication.CreateBuilder(args);

// SQL Server bağlantısı
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
// Tanımladığımmız proplar aynı değiştirilmeden Frontend taarafına gececek.
builder.Services.AddControllers()
.AddJsonOptions(x =>
x.JsonSerializerOptions.PropertyNamingPolicy = null);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals;
        options.JsonSerializerOptions.WriteIndented = true;
    });


builder.Services.AddDbContext<productDb>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
    )
);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy => policy.WithOrigins("https://e-shop-roan-eight.vercel.app") // Vercel URL’in
                        .AllowAnyHeader()
                        .AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowReactApp");

app.UseAuthorization();
app.MapControllers();
app.Run();