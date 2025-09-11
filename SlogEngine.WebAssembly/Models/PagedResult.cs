namespace SlogEngine.WebAssembly.Models;

/// <summary>
/// 페이징된 결과를 위한 모델
/// </summary>
/// <typeparam name="T">결과 항목의 타입</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// 현재 페이지의 항목들
    /// </summary>
    public IReadOnlyList<T> Items { get; set; } = new List<T>();

    /// <summary>
    /// 전체 항목 수
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 현재 페이지 번호 (1부터 시작)
    /// </summary>
    public int CurrentPage { get; set; }

    /// <summary>
    /// 페이지당 항목 수
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// 전체 페이지 수
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>
    /// 이전 페이지가 있는지 여부
    /// </summary>
    public bool HasPreviousPage => CurrentPage > 1;

    /// <summary>
    /// 다음 페이지가 있는지 여부
    /// </summary>
    public bool HasNextPage => CurrentPage < TotalPages;
}
