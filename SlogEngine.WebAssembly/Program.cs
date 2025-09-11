using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SlogEngine.WebAssembly;
using SlogEngine.WebAssembly.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// HTTP 클라이언트 설정
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// 서비스 등록
builder.Services.AddScoped<BlogApiService>();
builder.Services.AddScoped<IMarkdownService, MarkdownService>();
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<IClipboardService, ClipboardService>();

await builder.Build().RunAsync();
