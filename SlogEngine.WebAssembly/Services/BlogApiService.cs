using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using SlogEngine.WebAssembly.Models;

namespace SlogEngine.WebAssembly.Services;

public class BlogApiService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public BlogApiService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _baseUrl = configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5023";
    }

    // Meta 관련 메소드
    public async Task<BlogMeta?> GetBlogMetaAsync(string username)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<BlogMeta>($"{_baseUrl}/blog/{username}/meta");
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> UpdateBlogMetaAsync(string username, BlogMeta meta)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"{_baseUrl}/blog/{username}/meta", meta);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    // Post 관련 메소드
    public async Task<List<BlogPost>?> GetPostsAsync(string username)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<BlogPost>>($"{_baseUrl}/blog/{username}");
        }
        catch
        {
            return new List<BlogPost>();
        }
    }

    public async Task<bool> AddPostAsync(string username, BlogPost post)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/blog/{username}", post);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdatePostAsync(string username, string postId, BlogPost post)
    {
        try
        {
            Console.WriteLine($"BlogApiService.UpdatePostAsync: username={username}, postId={postId}, post.Title={post?.Title}");
            var response = await _httpClient.PutAsJsonAsync($"{_baseUrl}/blog/{username}/{postId}", post);
            Console.WriteLine($"BlogApiService.UpdatePostAsync: Response status: {response.StatusCode}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"BlogApiService.UpdatePostAsync: Exception: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeletePostAsync(string username, string postId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"{_baseUrl}/blog/{username}/{postId}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
