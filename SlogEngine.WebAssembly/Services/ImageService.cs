using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;

namespace SlogEngine.WebAssembly.Services;

public interface IImageService
{
    Task<string> UploadImageAsync(string username, Stream imageStream, string fileName);
}

public class ImageService : IImageService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public ImageService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _baseUrl = configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5023";
    }

    public async Task<string> UploadImageAsync(string username, Stream imageStream, string fileName)
    {
        Console.WriteLine($"ImageService.UploadImageAsync 시작: username={username}, fileName={fileName}");
        
        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(imageStream);
        
        // 파일 확장자에 따른 Content-Type 설정
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        streamContent.Headers.ContentType = extension switch
        {
            ".jpg" or ".jpeg" => new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg"),
            ".png" => new System.Net.Http.Headers.MediaTypeHeaderValue("image/png"),
            ".gif" => new System.Net.Http.Headers.MediaTypeHeaderValue("image/gif"),
            ".webp" => new System.Net.Http.Headers.MediaTypeHeaderValue("image/webp"),
            _ => new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream")
        };

        content.Add(streamContent, "image", fileName);

        var url = $"{_baseUrl}/blog/{username}/images/upload";
        Console.WriteLine($"이미지 업로드 요청: {url}");
        Console.WriteLine($"Content-Type: {streamContent.Headers.ContentType}");
        
        var response = await _httpClient.PostAsync(url, content);
        
        Console.WriteLine($"응답 상태: {response.StatusCode}");
        
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<ImageUploadResponse>();
            Console.WriteLine($"업로드 성공: {result?.Url}");
            return result?.Url ?? throw new Exception("이미지 URL을 받지 못했습니다.");
        }
        
        var errorMessage = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"업로드 실패: {response.StatusCode} - {errorMessage}");
        throw new Exception($"이미지 업로드 실패: {response.StatusCode} - {errorMessage}");
    }

    private class ImageUploadResponse
    {
        public string Url { get; set; } = string.Empty;
    }
}
