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


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors("AllowReactApp");

app.UseAuthorization();

app.UseHttpsRedirection();
app.MapControllers();
app.Run();