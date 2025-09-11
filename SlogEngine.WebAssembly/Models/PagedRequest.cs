namespace SlogEngine.WebAssembly.Models;

/// <summary>
/// 페이징 요청을 위한 모델
/// </summary>
public class PagedRequest
{
    /// <summary>
    /// 페이지 번호 (1부터 시작)
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// 페이지당 항목 수
    /// </summary>
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// 검색어 (선택적)
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// 태그 필터 (선택적)
    /// </summary>
    public string? Tag { get; set; }
}
