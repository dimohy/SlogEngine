using SlogEngine.Server.Interfaces;
using SlogEngine.Server.Services;
using SlogEngine.Server.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();

// Configure form options for file uploads
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10MB
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartHeadersLengthLimit = int.MaxValue;
});

// Register services
builder.Services.AddScoped<IWeatherService, WeatherService>();
builder.Services.AddScoped<IBlogService, BlogService>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// app.UseHttpsRedirection();

// Use CORS
app.UseCors("AllowAll");

// 정적 파일 서빙 (이미지 등)
app.UseStaticFiles(); // 기본 wwwroot 폴더

// blogs 폴더의 이미지를 정적 파일로 서빙 (wwwroot/blogs 경로)
var contentTypeProvider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
contentTypeProvider.Mappings[".png"] = "image/png";
contentTypeProvider.Mappings[".jpg"] = "image/jpeg";
contentTypeProvider.Mappings[".jpeg"] = "image/jpeg";
contentTypeProvider.Mappings[".gif"] = "image/gif";
contentTypeProvider.Mappings[".webp"] = "image/webp";

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
        Path.Combine(app.Environment.WebRootPath, "blogs")),
    RequestPath = "/blogs",
    ContentTypeProvider = contentTypeProvider
});

app.MapControllers();

app.MapGet("/weatherforecast", (IWeatherService weatherService) =>
{
    return weatherService.GetWeatherForecast();
})
.WithName("GetWeatherForecast");

app.MapGet("/ping", () =>
{
    return "Pong";
})
.WithName("Ping");

app.Run();
