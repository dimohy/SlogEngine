using Microsoft.JSInterop;

namespace SlogEngine.WebAssembly.Services;

/// <summary>
/// 클립보드 관련 기능을 제공하는 서비스의 인터페이스입니다.
/// </summary>
public interface IClipboardService
{
    /// <summary>
    /// 지정된 텍스트 영역에 클립보드 이미지 붙여넣기 리스너를 등록합니다.
    /// </summary>
    /// <param name="textAreaId">텍스트 영역의 ID</param>
    /// <param name="imageUploadCallback">이미지 업로드 콜백 함수</param>
    Task RegisterPasteListenerAsync(string textAreaId, Func<string, string, string, Task> imageUploadCallback);

    /// <summary>
    /// 지정된 텍스트 영역의 클립보드 리스너를 제거합니다.
    /// </summary>
    /// <param name="textAreaId">텍스트 영역의 ID</param>
    Task RemovePasteListenerAsync(string textAreaId);

    /// <summary>
    /// 지정된 텍스트 영역의 커서 위치에 텍스트를 삽입합니다.
    /// </summary>
    /// <param name="textAreaId">텍스트 영역의 ID</param>
    /// <param name="text">삽입할 텍스트</param>
    Task InsertTextAtCursorAsync(string textAreaId, string text);

    /// <summary>
    /// 지정된 텍스트 영역에서 특정 텍스트를 다른 텍스트로 교체합니다.
    /// </summary>
    /// <param name="textAreaId">텍스트 영역의 ID</param>
    /// <param name="oldText">교체할 기존 텍스트</param>
    /// <param name="newText">새로운 텍스트</param>
    Task ReplaceTextAsync(string textAreaId, string oldText, string newText);
}
