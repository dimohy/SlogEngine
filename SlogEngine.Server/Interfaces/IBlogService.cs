using SlogEngine.Server.Models;

namespace SlogEngine.Server.Interfaces;

public interface IBlogService
{
    IReadOnlyList<BlogPost> GetPosts(string username);
    
    /// <summary>
    /// 페이징된 블로그 포스트 목록을 조회합니다.
    /// </summary>
    /// <param name="username">사용자명</param>
    /// <param name="request">페이징 요청 정보</param>
    /// <returns>페이징된 블로그 포스트 결과</returns>
    PagedResult<BlogPost> GetPagedPosts(string username, PagedRequest request);
    
    BlogPost? GetPost(string username, string postId);
    void AddPost(string username, BlogPost post);
    void UpdatePost(string username, BlogPost post);
    void DeletePost(string username, string postId);
    BlogMeta GetBlogMeta(string username);
    void UpdateBlogMeta(string username, BlogMeta meta);
    
    // 이미지 관련 메서드들
    Task<string> SaveTempImageAsync(string username, IFormFile imageFile);
}
