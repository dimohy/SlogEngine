using Markdig;
using Microsoft.Extensions.Configuration;

namespace SlogEngine.WebAssembly.Services;

/// <summary>
/// 마크다운을 HTML로 변환하는 서비스
/// </summary>
public interface IMarkdownService
{
    /// <summary>
    /// 마크다운 텍스트를 HTML로 변환합니다.
    /// </summary>
    /// <param name="markdown">변환할 마크다운 텍스트</param>
    /// <returns>변환된 HTML 문자열</returns>
    string ToHtml(string markdown);
}

/// <summary>
/// Markdig를 사용한 마크다운 서비스 구현
/// </summary>
public class MarkdownService : IMarkdownService
{
    private readonly MarkdownPipeline _pipeline;
    private readonly string _baseUrl;

    public MarkdownService(IConfiguration configuration)
    {
        _baseUrl = configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7255";
        
        // 마크다운 파이프라인 설정 - 다양한 확장 기능 활성화
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions() // 고급 확장 기능들 (테이블, 각주, 수식 등)
            .UseEmojiAndSmiley() // 이모지 지원
            .UsePipeTables() // 파이프 테이블 지원 강화
            .UseGridTables() // 그리드 테이블 지원
            .UseGenericAttributes() // 일반 속성 지원 (CSS 클래스 등)
            .UseAutoIdentifiers() // 자동 ID 생성
            .Build();
    }

    public string ToHtml(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;

        // 상대 경로 이미지 URL을 절대 URL로 변환
        var processedMarkdown = ConvertImageUrlsToAbsolute(markdown);
        
        var html = Markdown.ToHtml(processedMarkdown, _pipeline);
        
        // 생성된 HTML에 갤러리 기능을 위한 CSS 클래스 추가
        html = AddGalleryClassToImages(html);
        
        return html;
    }

    private string ConvertImageUrlsToAbsolute(string markdown)
    {
        if (string.IsNullOrEmpty(markdown))
            return markdown;

        // /blogs/로 시작하는 이미지 경로를 절대 URL로 변환
        var result = System.Text.RegularExpressions.Regex.Replace(
            markdown,
            @"!\[([^\]]*)\]\((/blogs/[^)]+)\)",
            $"![$1]({_baseUrl}$2)"
        );

        Console.WriteLine($"마크다운 이미지 URL 변환: {_baseUrl}");
        
        return result;
    }

    /// <summary>
    /// HTML 이미지 태그에 갤러리 기능을 위한 CSS 클래스와 속성을 추가합니다.
    /// </summary>
    /// <param name="html">변환된 HTML 문자열</param>
    /// <returns>갤러리 클래스가 추가된 HTML 문자열</returns>
    private string AddGalleryClassToImages(string html)
    {
        if (string.IsNullOrEmpty(html))
            return html;

        // img 태그에 갤러리 관련 클래스와 속성 추가
        var result = System.Text.RegularExpressions.Regex.Replace(
            html,
            @"<img([^>]*?)>",
            @"<img$1 class=""gallery-image clickable"" data-gallery=""blog-post"">",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
        );

        return result;
    }
}
