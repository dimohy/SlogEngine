using SlogEngine.Server.Interfaces;
using SlogEngine.Server.Models;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SlogEngine.Server.Services;

public class BlogService : IBlogService
{
    private readonly string _blogsPath;

    public BlogService(IWebHostEnvironment env)
    {
        _blogsPath = Path.Combine(env.WebRootPath, "blogs");
    }

    public IReadOnlyList<BlogPost> GetPosts(string username)
    {
        var userPath = Path.Combine(_blogsPath, username);
        var postsPath = Path.Combine(userPath, "posts");

        if (!Directory.Exists(postsPath))
        {
            return new List<BlogPost>();
        }

        var posts = new List<BlogPost>();
        foreach (var file in Directory.GetFiles(postsPath, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var post = JsonSerializer.Deserialize<BlogPost>(json);
                if (post != null)
                {
                    posts.Add(post);
                }
            }
            catch
            {
                // 파일 읽기 실패 시 무시
            }
        }

        return posts.OrderByDescending(p => p.Date).ToList();
    }

    public BlogPost? GetPost(string username, string postId)
    {
        var userPath = Path.Combine(_blogsPath, username);
        var postsPath = Path.Combine(userPath, "posts");
        var postFile = Path.Combine(postsPath, $"{postId}.json");

        if (!File.Exists(postFile))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(postFile);
            return JsonSerializer.Deserialize<BlogPost>(json);
        }
        catch
        {
            return null;
        }
    }

    public void AddPost(string username, BlogPost post)
    {
        var userPath = Path.Combine(_blogsPath, username);
        var postsPath = Path.Combine(userPath, "posts");
        Directory.CreateDirectory(postsPath);

        post.Id = Guid.NewGuid().ToString();
        post.Date = DateTime.UtcNow;

        // 이미지 처리 및 콘텐츠 업데이트
        post.Content = ProcessImagesForPost(username, post.Id, post.Content ?? string.Empty);

        var postFile = Path.Combine(postsPath, $"{post.Id}.json");
        var json = JsonSerializer.Serialize(post, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(postFile, json);
    }

    public void UpdatePost(string username, BlogPost post)
    {
        var userPath = Path.Combine(_blogsPath, username);
        var postsPath = Path.Combine(userPath, "posts");
        var postFile = Path.Combine(postsPath, $"{post.Id}.json");

        if (!File.Exists(postFile))
        {
            throw new FileNotFoundException("Post not found");
        }

        post.Date = DateTime.UtcNow; // 업데이트 시간 갱신
        
        // 이미지 처리 및 콘텐츠 업데이트
        post.Content = ProcessImagesForPost(username, post.Id, post.Content ?? string.Empty);

        var json = JsonSerializer.Serialize(post, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(postFile, json);
    }

    public void DeletePost(string username, string postId)
    {
        var userPath = Path.Combine(_blogsPath, username);
        var postsPath = Path.Combine(userPath, "posts");
        var postFile = Path.Combine(postsPath, $"{postId}.json");

        if (File.Exists(postFile))
        {
            // JSON 파일 삭제
            File.Delete(postFile);
            
            // 포스트 관련 이미지 디렉토리 삭제
            var postImagesPath = Path.Combine(postsPath, postId);
            if (Directory.Exists(postImagesPath))
            {
                Directory.Delete(postImagesPath, true);
            }
        }
    }

    public BlogMeta GetBlogMeta(string username)
    {
        var userPath = Path.Combine(_blogsPath, username);
        var metaFile = Path.Combine(userPath, "meta.json");

        if (!File.Exists(metaFile))
        {
            // 기본 메타 생성
            var defaultMeta = new BlogMeta { Title = $"{username} 블로그" };
            UpdateBlogMeta(username, defaultMeta);
            return defaultMeta;
        }

        try
        {
            var json = File.ReadAllText(metaFile);
            var meta = JsonSerializer.Deserialize<BlogMeta>(json);
            if (meta != null)
            {
                // 타이틀이 비어있으면 기본값 설정
                if (string.IsNullOrEmpty(meta.Title))
                {
                    meta.Title = $"{username} 블로그";
                }
                return meta;
            }
        }
        catch
        {
            // 파일 읽기 실패 시 기본값 반환
        }

        return new BlogMeta { Title = $"{username} 블로그" };
    }

    public void UpdateBlogMeta(string username, BlogMeta meta)
    {
        var userPath = Path.Combine(_blogsPath, username);
        Directory.CreateDirectory(userPath);
        var metaFile = Path.Combine(userPath, "meta.json");

        var json = JsonSerializer.Serialize(meta, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(metaFile, json);
    }

    // 임시 이미지 저장 (클립보드 붙여넣기 시)
    public async Task<string> SaveTempImageAsync(string username, IFormFile imageFile)
    {
        Console.WriteLine($"SaveTempImageAsync 시작: username={username}, 파일명={imageFile.FileName}");
        
        var userPath = Path.Combine(_blogsPath, username);
        var tempImagesPath = Path.Combine(userPath, "images", "temp");
        Directory.CreateDirectory(tempImagesPath);

        // 파일명 생성 (타임스탬프 + 랜덤 GUID)
        var fileName = $"temp_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}{Path.GetExtension(imageFile.FileName)}";
        var filePath = Path.Combine(tempImagesPath, fileName);

        Console.WriteLine($"임시 이미지 저장 경로: {filePath}");

        // 이미지 저장
        using var stream = new FileStream(filePath, FileMode.Create);
        await imageFile.CopyToAsync(stream);

        Console.WriteLine($"임시 이미지 저장 완료, 파일 크기: {new FileInfo(filePath).Length} bytes");

        // 웹에서 접근 가능한 상대 경로 반환
        var webPath = $"/blogs/{username}/images/temp/{fileName}";
        Console.WriteLine($"반환할 웹 경로: {webPath}");
        
        return webPath;
    }

    // 포스트 저장 시 이미지 정리 및 이동
    private string ProcessImagesForPost(string username, string postId, string content)
    {
        Console.WriteLine($"ProcessImagesForPost 시작: postId={postId}");
        
        var userPath = Path.Combine(_blogsPath, username);
        var tempImagesPath = Path.Combine(userPath, "images", "temp");
        var postImagesPath = Path.Combine(userPath, "posts", postId);

        // 콘텐츠에서 사용된 이미지 URL 추출
        var imageUrls = ExtractImageUrls(content);
        var tempImageUrls = imageUrls.Where(url => url.Contains("/images/temp/")).ToList();
        var existingPostImageUrls = imageUrls.Where(url => url.Contains($"/posts/{postId}/")).ToList();

        Console.WriteLine($"발견된 이미지 URL: 임시={tempImageUrls.Count}, 기존={existingPostImageUrls.Count}");

        // 임시 이미지를 포스트 이미지 디렉토리로 이동
        if (tempImageUrls.Any())
        {
            Directory.CreateDirectory(postImagesPath);
            Console.WriteLine($"포스트 이미지 디렉토리 생성: {postImagesPath}");
            
            foreach (var tempImageUrl in tempImageUrls)
            {
                var tempFileName = Path.GetFileName(tempImageUrl);
                var tempFilePath = Path.Combine(tempImagesPath, tempFileName);
                
                Console.WriteLine($"임시 이미지 처리: {tempImageUrl} -> {tempFilePath}");
                
                if (File.Exists(tempFilePath))
                {
                    // 새 파일명 생성 (temp_ 접두사 제거)
                    var newFileName = tempFileName.StartsWith("temp_") 
                        ? tempFileName.Substring(5) // "temp_" 제거
                        : tempFileName;
                    
                    var newFilePath = Path.Combine(postImagesPath, newFileName);
                    var newImageUrl = $"/blogs/{username}/posts/{postId}/{newFileName}";
                    
                    Console.WriteLine($"파일 이동: {tempFilePath} -> {newFilePath}");
                    Console.WriteLine($"URL 업데이트: {tempImageUrl} -> {newImageUrl}");
                    
                    // 파일 이동
                    File.Move(tempFilePath, newFilePath);
                    
                    // 콘텐츠에서 URL 업데이트
                    content = content.Replace(tempImageUrl, newImageUrl);
                }
                else
                {
                    Console.WriteLine($"임시 파일이 존재하지 않음: {tempFilePath}");
                }
            }
        }

        // 포스트 이미지 디렉토리에서 사용되지 않는 이미지 삭제
        if (Directory.Exists(postImagesPath))
        {
            var allPostImages = Directory.GetFiles(postImagesPath);
            var usedImageNames = ExtractImageUrls(content) // 업데이트된 content 사용
                .Where(url => url.Contains($"/posts/{postId}/"))
                .Select(url => Path.GetFileName(url))
                .ToHashSet();
            
            Console.WriteLine($"포스트 이미지 디렉토리의 모든 이미지: {allPostImages.Length}개");
            Console.WriteLine($"사용 중인 이미지: {usedImageNames.Count}개");
            
            foreach (var imageFile in allPostImages)
            {
                var fileName = Path.GetFileName(imageFile);
                if (!usedImageNames.Contains(fileName))
                {
                    Console.WriteLine($"사용되지 않는 이미지 삭제: {imageFile}");
                    File.Delete(imageFile);
                }
                else
                {
                    Console.WriteLine($"사용 중인 이미지 유지: {fileName}");
                }
            }
            
            // 폴더가 비어있으면 삭제
            if (!Directory.GetFiles(postImagesPath).Any())
            {
                Console.WriteLine($"빈 폴더 삭제: {postImagesPath}");
                Directory.Delete(postImagesPath);
            }
        }

        // 오래된 임시 이미지 정리 (24시간 이상 된 것들)
        CleanupOldTempImages(username);

        Console.WriteLine("ProcessImagesForPost 완료");
        return content;
    }

    // 콘텐츠에서 이미지 URL 추출
    private static IReadOnlyList<string> ExtractImageUrls(string content)
    {
        if (string.IsNullOrEmpty(content))
            return new List<string>();

        // 마크다운 이미지 패턴: ![alt](url)
        var markdownPattern = @"!\[.*?\]\((.*?)\)";
        // HTML 이미지 패턴: <img src="url"
        var htmlPattern = @"<img[^>]+src=[""']([^""']+)[""']";

        var urls = new List<string>();
        
        var markdownMatches = Regex.Matches(content, markdownPattern);
        foreach (Match match in markdownMatches)
        {
            urls.Add(match.Groups[1].Value);
        }
        
        var htmlMatches = Regex.Matches(content, htmlPattern);
        foreach (Match match in htmlMatches)
        {
            urls.Add(match.Groups[1].Value);
        }

        return urls.Where(url => url.StartsWith("/blogs/")).ToList();
    }

    // 오래된 임시 이미지 정리
    private void CleanupOldTempImages(string username)
    {
        var userPath = Path.Combine(_blogsPath, username);
        var tempImagesPath = Path.Combine(userPath, "images", "temp");
        
        if (!Directory.Exists(tempImagesPath))
            return;

        var cutoffTime = DateTime.UtcNow.AddHours(-24);
        var tempFiles = Directory.GetFiles(tempImagesPath);
        
        foreach (var file in tempFiles)
        {
            var fileInfo = new FileInfo(file);
            if (fileInfo.CreationTimeUtc < cutoffTime)
            {
                File.Delete(file);
            }
        }
    }
}
