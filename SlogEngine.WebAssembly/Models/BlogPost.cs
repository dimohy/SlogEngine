using System.ComponentModel.DataAnnotations;

namespace SlogEngine.WebAssembly.Models;

public class BlogPost
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required(ErrorMessage = "제목은 필수입니다.")]
    public string? Title { get; set; }
    
    [Required(ErrorMessage = "내용은 필수입니다.")]
    public string? Content { get; set; }
    
    public DateTime Date { get; set; }
    public string? Summary { get; set; }
    public string? Author { get; set; }
    
    /// <summary>
    /// 원본 게시물의 고유 ID (마이그레이션된 경우)
    /// </summary>
    public string? OriginalId { get; set; }
    
    /// <summary>
    /// 게시물 슬러그 (URL에 사용)
    /// </summary>
    public string? Slug { get; set; }
    
    /// <summary>
    /// 커버 이미지 URL
    /// </summary>
    public string? Cover { get; set; }
    
    /// <summary>
    /// 태그 목록 (쉼표로 구분)
    /// </summary>
    public string? Tags { get; set; }
    
    /// <summary>
    /// 게시물 게시 날짜 (원본)
    /// </summary>
    public DateTime? DatePublished { get; set; }
}
