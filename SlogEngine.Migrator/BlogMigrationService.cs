using SlogEngine.Server.Models;
using System.Text.Json;
using System.Text;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;

namespace SlogEngine.Migrator;

/// <summary>
/// JSON 형식의 블로그 포스트를 Markdown 형식으로 마이그레이션하는 서비스입니다.
/// </summary>
public class BlogMigrationService
{
    private readonly string _blogsPath;
    private readonly ISerializer _yamlSerializer;

    public BlogMigrationService(string blogsPath)
    {
        _blogsPath = blogsPath;
        _yamlSerializer = new SerializerBuilder().Build();
    }

    /// <summary>
    /// 특정 사용자의 JSON 포스트를 Markdown으로 변환합니다.
    /// </summary>
    /// <param name="username">사용자명</param>
    public void MigrateUserPosts(string username)
    {
        var userPath = Path.Combine(_blogsPath, username);
        var postsPath = Path.Combine(userPath, "posts");

        if (!Directory.Exists(postsPath))
        {
            Console.WriteLine($"사용자 {username}의 포스트 디렉토리가 존재하지 않습니다: {postsPath}");
            return;
        }

        var jsonFiles = Directory.GetFiles(postsPath, "*.json");
        Console.WriteLine($"사용자 {username}: {jsonFiles.Length}개의 JSON 포스트를 변환 중...");

        foreach (var jsonFile in jsonFiles)
        {
            try
            {
                MigratePostFile(jsonFile, username);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"파일 변환 실패: {jsonFile}, 오류: {ex.Message}");
            }
        }

        Console.WriteLine($"사용자 {username}의 포스트 변환 완료");
    }

    /// <summary>
    /// 모든 사용자의 JSON 포스트를 Markdown으로 변환합니다.
    /// </summary>
    public void MigrateAllPosts()
    {
        if (!Directory.Exists(_blogsPath))
        {
            Console.WriteLine($"블로그 디렉토리가 존재하지 않습니다: {_blogsPath}");
            return;
        }

        var userDirectories = Directory.GetDirectories(_blogsPath);
        Console.WriteLine($"{userDirectories.Length}명의 사용자 발견, 마이그레이션 시작...");

        foreach (var userDirectory in userDirectories)
        {
            var username = Path.GetFileName(userDirectory);
            Console.WriteLine($"\n=== 사용자 {username} 마이그레이션 시작 ===");
            
            try
            {
                MigrateUserPosts(username);
                MigrateUserImages(username);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"사용자 {username} 마이그레이션 실패: {ex.Message}");
            }
        }

        Console.WriteLine("\n모든 사용자 마이그레이션 완료!");
    }

    /// <summary>
    /// 사용자의 이미지를 새로운 구조로 이동합니다.
    /// </summary>
    /// <param name="username">사용자명</param>
    public void MigrateUserImages(string username)
    {
        var userPath = Path.Combine(_blogsPath, username);
        var oldImagesPath = Path.Combine(userPath, "images");
        var postsPath = Path.Combine(userPath, "posts");

        if (!Directory.Exists(oldImagesPath))
        {
            Console.WriteLine($"사용자 {username}의 이미지 디렉토리가 존재하지 않습니다: {oldImagesPath}");
            return;
        }

        // temp 폴더는 건드리지 않음
        var tempPath = Path.Combine(oldImagesPath, "temp");
        var imageFiles = Directory.GetFiles(oldImagesPath, "*.*", SearchOption.TopDirectoryOnly)
            .Where(file => !file.StartsWith(tempPath))
            .ToArray();

        Console.WriteLine($"사용자 {username}: {imageFiles.Length}개의 이미지 파일 발견");

        var imageGroups = GroupImagesByPostId(imageFiles);
        
        foreach (var group in imageGroups)
        {
            var postId = group.Key;
            var images = group.Value;
            
            var postImagePath = Path.Combine(postsPath, postId);
            Directory.CreateDirectory(postImagePath);
            
            Console.WriteLine($"포스트 {postId}: {images.Count}개 이미지 이동 중...");
            
            foreach (var imageFile in images)
            {
                var fileName = Path.GetFileName(imageFile);
                
                // 파일명에서 포스트 ID 접두사 제거
                var newFileName = fileName;
                if (fileName.StartsWith($"{postId}_"))
                {
                    newFileName = fileName.Substring($"{postId}_".Length);
                }
                
                var newPath = Path.Combine(postImagePath, newFileName);
                
                try
                {
                    File.Move(imageFile, newPath);
                    Console.WriteLine($"  이동: {fileName} -> {newFileName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  이동 실패 - {fileName}: {ex.Message}");
                }
            }
        }

        // 이미지 디렉토리 정리 (temp 폴더 제외하고 비어있으면 삭제)
        var remainingFiles = Directory.GetFiles(oldImagesPath, "*.*", SearchOption.TopDirectoryOnly);
        if (remainingFiles.Length == 0)
        {
            var remainingDirs = Directory.GetDirectories(oldImagesPath)
                .Where(dir => !dir.Equals(tempPath, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            
            if (remainingDirs.Length == 0)
            {
                Console.WriteLine($"기존 images 디렉토리의 파일들이 모두 이동되었습니다 (temp 폴더는 유지)");
            }
        }

        Console.WriteLine($"사용자 {username}의 이미지 마이그레이션 완료");
    }

    /// <summary>
    /// 이미지 파일들을 포스트 ID별로 그룹화합니다.
    /// </summary>
    /// <param name="imageFiles">이미지 파일 경로 배열</param>
    /// <returns>포스트 ID별로 그룹화된 이미지 파일들</returns>
    private static Dictionary<string, List<string>> GroupImagesByPostId(string[] imageFiles)
    {
        var groups = new Dictionary<string, List<string>>();

        foreach (var imageFile in imageFiles)
        {
            var fileName = Path.GetFileNameWithoutExtension(imageFile);
            
            // 파일명에서 포스트 ID 추출 (첫 번째 '_' 이전 부분)
            var parts = fileName.Split('_');
            if (parts.Length >= 2)
            {
                var postId = parts[0];
                
                // GUID 형식인지 확인
                if (Guid.TryParse(postId, out _))
                {
                    if (!groups.ContainsKey(postId))
                    {
                        groups[postId] = new List<string>();
                    }
                    groups[postId].Add(imageFile);
                }
            }
        }

        return groups;
    }

    /// <summary>
    /// 단일 JSON 파일을 Markdown으로 변환합니다.
    /// </summary>
    /// <param name="jsonFilePath">JSON 파일 경로</param>
    /// <param name="username">사용자명</param>
    private void MigratePostFile(string jsonFilePath, string username)
    {
        var json = File.ReadAllText(jsonFilePath, Encoding.UTF8);
        var post = JsonSerializer.Deserialize<BlogPost>(json);
        
        if (post == null)
        {
            Console.WriteLine($"JSON 파싱 실패: {jsonFilePath}");
            return;
        }

        // 이미지 URL 업데이트
        if (!string.IsNullOrEmpty(post.Content))
        {
            post.Content = UpdateImageUrls(post.Content, username, post.Id);
        }

        if (!string.IsNullOrEmpty(post.Cover))
        {
            post.Cover = UpdateCoverImageUrl(post.Cover, username, post.Id);
        }

        // Markdown 파일로 저장
        var markdownFilePath = Path.ChangeExtension(jsonFilePath, ".md");
        WriteMarkdownPost(markdownFilePath, post);

        Console.WriteLine($"변환 완료: {Path.GetFileName(jsonFilePath)} -> {Path.GetFileName(markdownFilePath)}");
        
        // 원본 JSON 파일 삭제
        File.Delete(jsonFilePath);
    }

    /// <summary>
    /// 콘텐츠 내의 이미지 URL을 새로운 구조로 업데이트합니다.
    /// </summary>
    /// <param name="content">포스트 내용</param>
    /// <param name="username">사용자명</param>
    /// <param name="postId">포스트 ID</param>
    /// <returns>업데이트된 포스트 내용</returns>
    private static string UpdateImageUrls(string content, string username, string postId)
    {
        // /blogs/{username}/images/{postId}_xxx 패턴을 /blogs/{username}/posts/{postId}/xxx로 변경
        var pattern = $@"/blogs/{Regex.Escape(username)}/images/{Regex.Escape(postId)}_([^)\s]+)";
        var replacement = $"/blogs/{username}/posts/{postId}/$1";
        
        return Regex.Replace(content, pattern, replacement);
    }

    /// <summary>
    /// 커버 이미지 URL을 새로운 구조로 업데이트합니다.
    /// </summary>
    /// <param name="coverUrl">커버 이미지 URL</param>
    /// <param name="username">사용자명</param>
    /// <param name="postId">포스트 ID</param>
    /// <returns>업데이트된 커버 이미지 URL</returns>
    private static string UpdateCoverImageUrl(string coverUrl, string username, string postId)
    {
        if (string.IsNullOrEmpty(coverUrl))
            return coverUrl;

        // /blogs/{username}/images/{postId}_cover.xxx 패턴을 /blogs/{username}/posts/{postId}/cover.xxx로 변경
        var pattern = $@"/blogs/{Regex.Escape(username)}/images/{Regex.Escape(postId)}_cover\.([^)\s]+)";
        var replacement = $"/blogs/{username}/posts/{postId}/cover.$1";
        
        return Regex.Replace(coverUrl, pattern, replacement);
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
}
