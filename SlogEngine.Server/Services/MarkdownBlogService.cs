using SlogEngine.Server.Interfaces;
using SlogEngine.Server.Models;
using System.Text;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;

namespace SlogEngine.Server.Services;

/// <summary>
/// Markdown 형식으로 블로그 포스트를 저장하고 읽는 서비스입니다.
/// </summary>
public class MarkdownBlogService : IBlogService
{
    private readonly string _blogsPath;
    private readonly IDeserializer _yamlDeserializer;
    private readonly ISerializer _yamlSerializer;

    public MarkdownBlogService(IWebHostEnvironment env)
    {
        _blogsPath = Path.Combine(env.WebRootPath, "blogs");
        _yamlDeserializer = new DeserializerBuilder().Build();
        _yamlSerializer = new SerializerBuilder().Build();
    }

    /// <summary>
    /// 사용자의 모든 블로그 포스트를 조회합니다.
    /// </summary>
    /// <param name="username">사용자명</param>
    /// <returns>블로그 포스트 목록</returns>
    public IReadOnlyList<BlogPost> GetPosts(string username)
    {
        var userPath = Path.Combine(_blogsPath, username);
        var postsPath = Path.Combine(userPath, "posts");

        if (!Directory.Exists(postsPath))
        {
            return new List<BlogPost>();
        }

        var posts = new List<BlogPost>();
        foreach (var file in Directory.GetFiles(postsPath, "*.md"))
        {
            try
            {
                var post = ReadMarkdownPost(file);
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

    /// <summary>
    /// 페이징된 블로그 포스트 목록을 조회합니다.
    /// </summary>
    /// <param name="username">사용자명</param>
    /// <param name="request">페이징 요청 정보</param>
    /// <returns>페이징된 블로그 포스트 결과</returns>
    public PagedResult<BlogPost> GetPagedPosts(string username, PagedRequest request)
    {
        var userPath = Path.Combine(_blogsPath, username);
        var postsPath = Path.Combine(userPath, "posts");

        if (!Directory.Exists(postsPath))
        {
            return new PagedResult<BlogPost>
            {
                Items = new List<BlogPost>(),
                TotalCount = 0,
                CurrentPage = request.Page,
                PageSize = request.PageSize
            };
        }

        var posts = new List<BlogPost>();
        foreach (var file in Directory.GetFiles(postsPath, "*.md"))
        {
            try
            {
                var post = ReadMarkdownPost(file);
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

        // OriginalId 기준으로 중복 제거 (OriginalId가 null이거나 비어있는 경우는 제외)
        IEnumerable<BlogPost> query = posts
            .GroupBy(p => string.IsNullOrEmpty(p.OriginalId) ? p.Id : p.OriginalId)
            .Select(g => g.OrderByDescending(x => x.Date).First())
            .OrderByDescending(p => p.Date);

        // 검색어 필터링
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchTerm = request.Search.ToLower();
            query = query.Where(p => 
                (p.Title != null && p.Title.ToLower().Contains(searchTerm)) ||
                (p.Content != null && p.Content.ToLower().Contains(searchTerm)) ||
                (p.Summary != null && p.Summary.ToLower().Contains(searchTerm))
            );
        }

        // 태그 필터링
        if (!string.IsNullOrWhiteSpace(request.Tag))
        {
            var tagFilter = request.Tag.ToLower();
            query = query.Where(p => 
                p.Tags != null && p.Tags.ToLower().Contains(tagFilter)
            );
        }

        var totalCount = query.Count();
        var pagedPosts = query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return new PagedResult<BlogPost>
        {
            Items = pagedPosts,
            TotalCount = totalCount,
            CurrentPage = request.Page,
            PageSize = request.PageSize
        };
    }

    /// <summary>
    /// 특정 블로그 포스트를 조회합니다.
    /// </summary>
    /// <param name="username">사용자명</param>
    /// <param name="postId">포스트 ID</param>
    /// <returns>블로그 포스트</returns>
    public BlogPost? GetPost(string username, string postId)
    {
        var userPath = Path.Combine(_blogsPath, username);
        var postsPath = Path.Combine(userPath, "posts");
        var postFile = Path.Combine(postsPath, $"{postId}.md");

        if (!File.Exists(postFile))
        {
            return null;
        }

        try
        {
            return ReadMarkdownPost(postFile);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 새 블로그 포스트를 추가합니다.
    /// </summary>
    /// <param name="username">사용자명</param>
    /// <param name="post">블로그 포스트</param>
    public void AddPost(string username, BlogPost post)
    {
        var userPath = Path.Combine(_blogsPath, username);
        var postsPath = Path.Combine(userPath, "posts");
        Directory.CreateDirectory(postsPath);

        post.Id = Guid.NewGuid().ToString();
        post.Date = DateTime.UtcNow;

        // 이미지 처리 및 콘텐츠 업데이트
        post.Content = ProcessImagesForPost(username, post.Id, post.Content ?? string.Empty);

        var postFile = Path.Combine(postsPath, $"{post.Id}.md");
        WriteMarkdownPost(postFile, post);
    }

    /// <summary>
    /// 기존 블로그 포스트를 수정합니다.
    /// </summary>
    /// <param name="username">사용자명</param>
    /// <param name="post">블로그 포스트</param>
    public void UpdatePost(string username, BlogPost post)
    {
        var userPath = Path.Combine(_blogsPath, username);
        var postsPath = Path.Combine(userPath, "posts");
        var postFile = Path.Combine(postsPath, $"{post.Id}.md");

        if (!File.Exists(postFile))
        {
            throw new FileNotFoundException("Post not found");
        }

        post.Date = DateTime.UtcNow; // 업데이트 시간 갱신
        
        // 이미지 처리 및 콘텐츠 업데이트
        post.Content = ProcessImagesForPost(username, post.Id, post.Content ?? string.Empty);

        WriteMarkdownPost(postFile, post);
    }

    /// <summary>
    /// 블로그 포스트를 삭제합니다.
    /// </summary>
    /// <param name="username">사용자명</param>
    /// <param name="postId">포스트 ID</param>
    public void DeletePost(string username, string postId)
    {
        var userPath = Path.Combine(_blogsPath, username);
        var postsPath = Path.Combine(userPath, "posts");
        var postFile = Path.Combine(postsPath, $"{postId}.md");

        if (File.Exists(postFile))
        {
            // Markdown 파일 삭제
            File.Delete(postFile);
            
            // 포스트 관련 이미지 디렉토리 삭제
            var postImagesPath = Path.Combine(postsPath, postId);
            if (Directory.Exists(postImagesPath))
            {
                Directory.Delete(postImagesPath, true);
            }
        }
    }

    /// <summary>
    /// 블로그 메타데이터를 조회합니다.
    /// </summary>
    /// <param name="username">사용자명</param>
    /// <returns>블로그 메타데이터</returns>
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
            var meta = System.Text.Json.JsonSerializer.Deserialize<BlogMeta>(json);
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

    /// <summary>
    /// 블로그 메타데이터를 업데이트합니다.
    /// </summary>
    /// <param name="username">사용자명</param>
    /// <param name="meta">블로그 메타데이터</param>
    public void UpdateBlogMeta(string username, BlogMeta meta)
    {
        var userPath = Path.Combine(_blogsPath, username);
        Directory.CreateDirectory(userPath);
        var metaFile = Path.Combine(userPath, "meta.json");

        var json = System.Text.Json.JsonSerializer.Serialize(meta, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(metaFile, json);
    }

    /// <summary>
    /// 임시 이미지를 저장합니다.
    /// </summary>
    /// <param name="username">사용자명</param>
    /// <param name="imageFile">이미지 파일</param>
    /// <returns>임시 이미지 URL</returns>
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

    /// <summary>
    /// Markdown 파일에서 블로그 포스트를 읽습니다.
    /// </summary>
    /// <param name="filePath">파일 경로</param>
    /// <returns>블로그 포스트</returns>
    private BlogPost? ReadMarkdownPost(string filePath)
    {
        var content = File.ReadAllText(filePath, Encoding.UTF8);
        
        // YAML Front Matter 추출
        var frontMatterMatch = Regex.Match(content, @"^---\s*\n(.*?)\n---\s*\n(.*)", RegexOptions.Singleline);
        
        if (!frontMatterMatch.Success)
        {
            return null;
        }

        var frontMatter = frontMatterMatch.Groups[1].Value;
        var markdownContent = frontMatterMatch.Groups[2].Value;

        try
        {
            var metadata = _yamlDeserializer.Deserialize<Dictionary<string, object>>(frontMatter);
            var fileName = Path.GetFileNameWithoutExtension(filePath);

            var post = new BlogPost
            {
                Id = fileName,
                Title = GetStringValue(metadata, "title"),
                Content = markdownContent,
                Summary = GetStringValue(metadata, "summary"),
                Author = GetStringValue(metadata, "author"),
                OriginalId = GetStringValue(metadata, "originalId"),
                Slug = GetStringValue(metadata, "slug"),
                Cover = GetStringValue(metadata, "cover"),
                Tags = GetStringValue(metadata, "tags"),
                Date = GetDateTimeValue(metadata, "date") ?? DateTime.MinValue,
                DatePublished = GetDateTimeValue(metadata, "datePublished")
            };

            return post;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 블로그 포스트를 Markdown 파일에 씁니다.
    /// </summary>
    /// <param name="filePath">파일 경로</param>
    /// <param name="post">블로그 포스트</param>
    private void WriteMarkdownPost(string filePath, BlogPost post)
    {
        var metadata = new Dictionary<string, object>
        {
            ["title"] = post.Title ?? string.Empty,
            ["date"] = post.Date.ToString("yyyy-MM-ddTHH:mm:ssK"),
            ["summary"] = post.Summary ?? string.Empty,
            ["author"] = post.Author ?? string.Empty,
            ["originalId"] = post.OriginalId ?? string.Empty,
            ["slug"] = post.Slug ?? string.Empty,
            ["cover"] = post.Cover ?? string.Empty,
            ["tags"] = post.Tags ?? string.Empty
        };

        if (post.DatePublished.HasValue)
        {
            metadata["datePublished"] = post.DatePublished.Value.ToString("yyyy-MM-ddTHH:mm:ssK");
        }

        var frontMatter = _yamlSerializer.Serialize(metadata);
        var content = $"---\n{frontMatter}---\n{post.Content ?? string.Empty}";

        File.WriteAllText(filePath, content, Encoding.UTF8);
    }

    /// <summary>
    /// 딕셔너리에서 문자열 값을 안전하게 추출합니다.
    /// </summary>
    /// <param name="dict">딕셔너리</param>
    /// <param name="key">키</param>
    /// <returns>문자열 값</returns>
    private static string? GetStringValue(IReadOnlyDictionary<string, object> dict, string key)
    {
        return dict.TryGetValue(key, out var value) ? value?.ToString() : null;
    }

    /// <summary>
    /// 딕셔너리에서 DateTime 값을 안전하게 추출합니다.
    /// </summary>
    /// <param name="dict">딕셔너리</param>
    /// <param name="key">키</param>
    /// <returns>DateTime 값</returns>
    private static DateTime? GetDateTimeValue(IReadOnlyDictionary<string, object> dict, string key)
    {
        if (!dict.TryGetValue(key, out var value))
            return null;

        if (value is DateTime dateTime)
            return dateTime;

        if (DateTime.TryParse(value?.ToString(), out var parsedDate))
            return parsedDate;

        return null;
    }

    /// <summary>
    /// 포스트 저장 시 이미지를 처리하고 이동합니다.
    /// </summary>
    /// <param name="username">사용자명</param>
    /// <param name="postId">포스트 ID</param>
    /// <param name="content">포스트 내용</param>
    /// <returns>처리된 포스트 내용</returns>
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

    /// <summary>
    /// 콘텐츠에서 이미지 URL을 추출합니다.
    /// </summary>
    /// <param name="content">콘텐츠</param>
    /// <returns>이미지 URL 목록</returns>
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

    /// <summary>
    /// 오래된 임시 이미지를 정리합니다.
    /// </summary>
    /// <param name="username">사용자명</param>
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
