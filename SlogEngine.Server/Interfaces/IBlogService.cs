using SlogEngine.Server.Models;

namespace SlogEngine.Server.Interfaces;

public interface IBlogService
{
    IReadOnlyList<BlogPost> GetPosts(string username);
    BlogPost? GetPost(string username, string postId);
    void AddPost(string username, BlogPost post);
    void UpdatePost(string username, BlogPost post);
    void DeletePost(string username, string postId);
    BlogMeta GetBlogMeta(string username);
    void UpdateBlogMeta(string username, BlogMeta meta);
    
    // 이미지 관련 메서드들
    Task<string> SaveTempImageAsync(string username, IFormFile imageFile);
}
