using SlogEngine.Server.Models;
using System.Text.Json;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SlogEngine.Migrator;

/// <summary>
/// Hashnode 블로그 포스트를 SlogEngine으로 마이그레이션하는 클래스
/// </summary>
public class HashnodeMigrator : IDisposable
{
    private readonly IDeserializer _yamlDeserializer;
    private readonly HttpClient _httpClient;

    public HashnodeMigrator()
    {
        _yamlDeserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
        
        _httpClient = new HttpClient();
        
        // 일반적인 브라우저처럼 보이도록 헤더 설정
        _httpClient.DefaultRequestHeaders.Add("User-Agent", 
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        _httpClient.DefaultRequestHeaders.Add("Accept", 
            "image/webp,image/apng,image/*,*/*;q=0.8");
        _httpClient.DefaultRequestHeaders.Add("Accept-Language", 
            "ko-KR,ko;q=0.9,en-US;q=0.8,en;q=0.7");
        _httpClient.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
        _httpClient.DefaultRequestHeaders.Add("Pragma", "no-cache");
        
        // 타임아웃 설정
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// 블로그 포스트를 마이그레이션합니다.
    /// </summary>
    /// <param name="sourcePath">소스 md 파일들이 있는 경로</param>
    /// <param name="targetPath">대상 블로그 경로</param>
    /// <param name="username">사용자명</param>
    public async Task MigrateAsync(string sourcePath, string targetPath, string username)
    {
        if (!Directory.Exists(sourcePath))
        {
            throw new DirectoryNotFoundException($"소스 경로가 존재하지 않습니다: {sourcePath}");
        }

        // 대상 디렉토리 생성
        var postsPath = Path.Combine(targetPath, "posts");
        var imagesPath = Path.Combine(targetPath, "images");
        Directory.CreateDirectory(postsPath);
        Directory.CreateDirectory(imagesPath);

        // md 파일들 가져오기
        var mdFiles = Directory.GetFiles(sourcePath, "*.md", SearchOption.TopDirectoryOnly);
        Console.WriteLine($"발견된 마크다운 파일: {mdFiles.Length}개");

        var blogPosts = new List<BlogPost>();

        // 각 파일 처리
        foreach (var mdFile in mdFiles)
        {
            try
            {
                Console.WriteLine($"처리 중: {Path.GetFileName(mdFile)}");
                var blogPost = await ProcessMarkdownFileAsync(mdFile, username, imagesPath);
                if (blogPost != null)
                {
                    blogPosts.Add(blogPost);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"파일 처리 실패 {Path.GetFileName(mdFile)}: {ex.Message}");
            }
        }

        // 날짜 순으로 정렬 (오름차순)
        blogPosts = blogPosts
            .OrderBy(p => p.DatePublished ?? p.Date)
            .ToList();

        Console.WriteLine($"\n총 {blogPosts.Count}개의 포스트를 날짜 순으로 정렬했습니다.");

        // JSON 파일로 저장
        for (int i = 0; i < blogPosts.Count; i++)
        {
            var post = blogPosts[i];
            var jsonPath = Path.Combine(postsPath, $"{post.Id}.json");
            
            var jsonOptions = new JsonSerializerOptions 
            { 
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            
            var json = JsonSerializer.Serialize(post, jsonOptions);
            await File.WriteAllTextAsync(jsonPath, json);
            
            Console.WriteLine($"저장됨 ({i + 1}/{blogPosts.Count}): {post.Title}");
        }

        Console.WriteLine($"\n마이그레이션 완료: {blogPosts.Count}개 포스트가 처리되었습니다.");
    }

    /// <summary>
    /// 마크다운 파일을 처리하여 BlogPost 객체로 변환합니다.
    /// </summary>
    private async Task<BlogPost?> ProcessMarkdownFileAsync(string filePath, string username, string imagesBasePath)
    {
        var content = await File.ReadAllTextAsync(filePath);
        var fileName = Path.GetFileNameWithoutExtension(filePath);

        // YAML Front Matter 파싱
        var yamlContent = ExtractYamlFrontMatter(content);
        if (string.IsNullOrEmpty(yamlContent))
        {
            Console.WriteLine($"YAML front matter를 찾을 수 없습니다: {fileName}");
            return null;
        }

        var metadata = _yamlDeserializer.Deserialize<Dictionary<string, object>>(yamlContent);
        var markdownContent = content.Substring(content.IndexOf("---", 4) + 3).Trim();

        // BlogPost 객체 생성
        var blogPost = new BlogPost
        {
            Id = Guid.NewGuid().ToString(),
            OriginalId = GetValueAsString(metadata, "cuid") ?? fileName,
            Title = GetValueAsString(metadata, "title") ?? "제목 없음",
            Content = markdownContent,
            Slug = GetValueAsString(metadata, "slug"),
            Tags = GetTagsAsString(metadata),
            Author = username,
            Summary = ExtractSummary(markdownContent)
        };

        // 날짜 파싱
        var datePublishedStr = GetValueAsString(metadata, "datePublished");
        if (!string.IsNullOrEmpty(datePublishedStr))
        {
            // "Sun May 23 2021 03:28:51 GMT+0000 (Coordinated Universal Time)" 형식 처리
            if (DateTime.TryParseExact(datePublishedStr, 
                "ddd MMM dd yyyy HH:mm:ss 'GMT'zzz '(Coordinated Universal Time)'", 
                System.Globalization.CultureInfo.InvariantCulture, 
                System.Globalization.DateTimeStyles.None, 
                out var datePublished))
            {
                blogPost.DatePublished = datePublished;
                blogPost.Date = datePublished;
            }
            else if (DateTime.TryParse(datePublishedStr, out var fallbackDate))
            {
                blogPost.DatePublished = fallbackDate;
                blogPost.Date = fallbackDate;
            }
        }

        // 커버 이미지 처리
        var coverUrl = GetValueAsString(metadata, "cover");
        if (!string.IsNullOrEmpty(coverUrl))
        {
            try
            {
                var localCoverPath = await DownloadImageAsync(coverUrl, imagesBasePath, blogPost.Id, "cover");
                if (!string.IsNullOrEmpty(localCoverPath))
                {
                    blogPost.Cover = $"/blogs/{username}/images/{Path.GetFileName(localCoverPath)}";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"커버 이미지 다운로드 실패 ({coverUrl}): {ex.Message}");
            }
        }

        // 콘텐츠 내 이미지 처리
        blogPost.Content = await ProcessContentImagesAsync(markdownContent, imagesBasePath, blogPost.Id, username);

        return blogPost;
    }

    /// <summary>
    /// YAML Front Matter를 추출합니다.
    /// </summary>
    private static string ExtractYamlFrontMatter(string content)
    {
        if (!content.StartsWith("---"))
            return string.Empty;

        var endIndex = content.IndexOf("---", 3);
        if (endIndex == -1)
            return string.Empty;

        var yamlContent = content.Substring(3, endIndex - 3).Trim();
        
        // YAML 내용 정리
        return CleanYamlContent(yamlContent);
    }

    /// <summary>
    /// YAML 내용에서 파싱 문제를 일으킬 수 있는 부분을 정리합니다.
    /// </summary>
    private static string CleanYamlContent(string yamlContent)
    {
        var lines = yamlContent.Split('\n');
        var cleanedLines = new List<string>();
        
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd();
            
            // 키: 값 형식의 라인 찾기
            if (line.Contains(':') && !line.TrimStart().StartsWith('-'))
            {
                var colonIndex = line.IndexOf(':');
                var key = line.Substring(0, colonIndex).Trim();
                var value = line.Substring(colonIndex + 1).Trim();
                
                // 값이 큰따옴표로 시작하지만 끝나지 않는 경우 (여러 줄 문자열)
                if (value.StartsWith('"') && !value.EndsWith('"'))
                {
                    var multiLineValue = value;
                    
                    // 다음 줄들을 읽어서 닫는 큰따옴표를 찾기
                    while (i + 1 < lines.Length)
                    {
                        i++;
                        var nextLine = lines[i].Trim();
                        multiLineValue += " " + nextLine;
                        
                        if (nextLine.EndsWith('"'))
                        {
                            break;
                        }
                        
                        // 다음 키를 만나면 강제로 큰따옴표 닫기
                        if (nextLine.Contains(':') && !nextLine.StartsWith(' ') && !nextLine.StartsWith('\t'))
                        {
                            multiLineValue += '"';
                            i--; // 이 줄을 다시 처리하도록
                            break;
                        }
                    }
                    
                    // 아직 닫히지 않았으면 강제로 닫기
                    if (!multiLineValue.EndsWith('"'))
                    {
                        multiLineValue += '"';
                    }
                    
                    cleanedLines.Add($"{key}: {multiLineValue}");
                }
                // 제목에 큰따옴표가 들어간 경우 처리
                else if (value.StartsWith('"') && value.Count(c => c == '"') % 2 != 0)
                {
                    // 홀수 개의 큰따옴표가 있으면 마지막에 큰따옴표 추가
                    cleanedLines.Add($"{key}: {value}\"");
                }
                else
                {
                    cleanedLines.Add(line);
                }
            }
            else
            {
                cleanedLines.Add(line);
            }
        }
        
        return string.Join('\n', cleanedLines);
    }

    /// <summary>
    /// 메타데이터에서 값을 문자열로 가져옵니다.
    /// </summary>
    private static string? GetValueAsString(Dictionary<string, object> metadata, string key)
    {
        return metadata.TryGetValue(key, out var value) ? value?.ToString() : null;
    }

    /// <summary>
    /// 태그를 문자열로 변환합니다.
    /// </summary>
    private static string? GetTagsAsString(Dictionary<string, object> metadata)
    {
        if (!metadata.TryGetValue("tags", out var tagsObj))
            return null;

        if (tagsObj is List<object> tagsList)
        {
            return string.Join(", ", tagsList.Select(t => t.ToString()));
        }
        
        return tagsObj?.ToString();
    }

    /// <summary>
    /// 마크다운 콘텐츠에서 요약을 추출합니다.
    /// </summary>
    private static string ExtractSummary(string content)
    {
        // 첫 번째 문단을 요약으로 사용
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var firstParagraph = lines.FirstOrDefault(line => 
            !line.StartsWith("#") && 
            !line.StartsWith("![") && 
            !line.StartsWith("```") &&
            line.Trim().Length > 0);

        if (string.IsNullOrEmpty(firstParagraph))
            return string.Empty;

        // 200자로 제한
        return firstParagraph.Length > 200 
            ? firstParagraph.Substring(0, 200) + "..." 
            : firstParagraph;
    }

    /// <summary>
    /// 이미지를 다운로드하고 로컬 경로를 반환합니다.
    /// </summary>
    private async Task<string?> DownloadImageAsync(string imageUrl, string basePath, string postId, string imageName)
    {
        const int maxRetries = 3;
        
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, imageUrl);
                
                // 특정 사이트별 추가 헤더 설정
                if (imageUrl.Contains("discourse-dotnetdev-upload") || imageUrl.Contains("ndepend.com"))
                {
                    request.Headers.Add("Referer", "https://hashnode.com/");
                }
                else if (imageUrl.Contains("claudiobernasconi.ch"))
                {
                    request.Headers.Add("Referer", "https://www.claudiobernasconi.ch/");
                }
                
                using var response = await _httpClient.SendAsync(request);
                
                // 404나 403 오류는 재시도하지 않음
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound || 
                    response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    Console.WriteLine($"이미지 다운로드 실패 ({imageUrl}): {response.StatusCode} - 재시도하지 않음");
                    return null;
                }
                
                response.EnsureSuccessStatusCode();

                var contentType = response.Content.Headers.ContentType?.MediaType;
                var extension = GetImageExtension(contentType, imageUrl);
                
                var fileName = $"{postId}_{imageName}{extension}";
                var filePath = Path.Combine(basePath, fileName);

                using var fileStream = File.Create(filePath);
                await response.Content.CopyToAsync(fileStream);

                Console.WriteLine($"이미지 다운로드 완료: {fileName}");
                return filePath;
            }
            catch (HttpRequestException ex) when (attempt < maxRetries)
            {
                Console.WriteLine($"이미지 다운로드 시도 {attempt}/{maxRetries} 실패 ({imageUrl}): {ex.Message}");
                await Task.Delay(1000 * attempt); // 1초, 2초, 3초 대기
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                Console.WriteLine($"이미지 다운로드 타임아웃 ({imageUrl}): {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"이미지 다운로드 실패 ({imageUrl}): {ex.Message}");
                return null;
            }
        }
        
        Console.WriteLine($"이미지 다운로드 최종 실패 ({imageUrl}): 최대 재시도 횟수 초과");
        return null;
    }

    /// <summary>
    /// 콘텐츠 내의 이미지들을 처리합니다.
    /// </summary>
    private async Task<string> ProcessContentImagesAsync(string content, string imagesBasePath, string postId, string username)
    {
        // 마크다운 이미지 패턴: ![alt](url)
        var imagePattern = @"!\[([^\]]*)\]\(([^)]+)\)";
        var matches = Regex.Matches(content, imagePattern);

        var imageCounter = 1;
        foreach (Match match in matches)
        {
            var altText = match.Groups[1].Value;
            var imageUrl = match.Groups[2].Value;

            // URL에서 align 등의 속성 제거
            imageUrl = CleanImageUrl(imageUrl);

            // 외부 URL인 경우만 다운로드
            if (imageUrl.StartsWith("http"))
            {
                try
                {
                    var localPath = await DownloadImageAsync(imageUrl, imagesBasePath, postId, $"img_{imageCounter:D3}");
                    if (!string.IsNullOrEmpty(localPath))
                    {
                        var localUrl = $"/blogs/{username}/images/{Path.GetFileName(localPath)}";
                        content = content.Replace(match.Value, $"![{altText}]({localUrl})");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"콘텐츠 이미지 다운로드 실패 ({imageUrl}): {ex.Message}");
                }
            }

            imageCounter++;
        }

        return content;
    }

    /// <summary>
    /// 이미지 URL에서 불필요한 속성들을 제거합니다.
    /// </summary>
    private static string CleanImageUrl(string imageUrl)
    {
        if (string.IsNullOrEmpty(imageUrl))
            return imageUrl;

        // align="left", align="center", align="right" 등의 속성 제거
        imageUrl = Regex.Replace(imageUrl, @"\s+align=""[^""]*""", "", RegexOptions.IgnoreCase);
        
        // width, height 속성 제거
        imageUrl = Regex.Replace(imageUrl, @"\s+width=""[^""]*""", "", RegexOptions.IgnoreCase);
        imageUrl = Regex.Replace(imageUrl, @"\s+height=""[^""]*""", "", RegexOptions.IgnoreCase);
        
        // class 속성 제거
        imageUrl = Regex.Replace(imageUrl, @"\s+class=""[^""]*""", "", RegexOptions.IgnoreCase);
        
        // style 속성 제거
        imageUrl = Regex.Replace(imageUrl, @"\s+style=""[^""]*""", "", RegexOptions.IgnoreCase);
        
        // 기타 HTML 속성들 제거 (alt, title 등)
        imageUrl = Regex.Replace(imageUrl, @"\s+[a-zA-Z-]+=([""'])[^""']*\1", "", RegexOptions.IgnoreCase);
        
        return imageUrl.Trim();
    }

    /// <summary>
    /// 콘텐츠 타입이나 URL에서 이미지 확장자를 가져옵니다.
    /// </summary>
    private static string GetImageExtension(string? contentType, string imageUrl)
    {
        // Content-Type에서 확장자 추출
        var extension = contentType switch
        {
            "image/jpeg" => ".jpg",
            "image/jpg" => ".jpg",
            "image/png" => ".png",
            "image/gif" => ".gif",
            "image/webp" => ".webp",
            "image/svg+xml" => ".svg",
            _ => null
        };

        if (!string.IsNullOrEmpty(extension))
            return extension;

        // URL에서 확장자 추출
        var urlExtension = Path.GetExtension(imageUrl.Split('?')[0]);
        return string.IsNullOrEmpty(urlExtension) ? ".jpg" : urlExtension;
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
