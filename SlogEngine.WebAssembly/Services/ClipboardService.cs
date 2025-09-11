using Microsoft.JSInterop;
using SlogEngine.WebAssembly.Services;

namespace SlogEngine.WebAssembly.Services;

/// <summary>
/// 클립보드 관련 기능을 제공하는 서비스입니다.
/// </summary>
public class ClipboardService : IClipboardService, IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private readonly IImageService _imageService;
    private DotNetObjectReference<ClipboardService>? _dotNetHelper;
    private readonly Dictionary<string, Func<string, string, string, Task>> _uploadCallbacks = new();

    public ClipboardService(IJSRuntime jsRuntime, IImageService imageService)
    {
        _jsRuntime = jsRuntime;
        _imageService = imageService;
    }

    /// <summary>
    /// 지정된 텍스트 영역에 클립보드 이미지 붙여넣기 리스너를 등록합니다.
    /// </summary>
    /// <param name="textAreaId">텍스트 영역의 ID</param>
    /// <param name="imageUploadCallback">이미지 업로드 콜백 함수</param>
    public async Task RegisterPasteListenerAsync(string textAreaId, Func<string, string, string, Task> imageUploadCallback)
    {
        try
        {
            _dotNetHelper ??= DotNetObjectReference.Create(this);
            _uploadCallbacks[textAreaId] = imageUploadCallback;

            // DOM이 완전히 로드될 때까지 약간 대기
            await Task.Delay(100);

            await _jsRuntime.InvokeVoidAsync("clipboardHelper.addPasteListener", textAreaId, _dotNetHelper);
            Console.WriteLine($"클립보드 리스너가 성공적으로 등록되었습니다: {textAreaId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"클립보드 리스너 등록 실패: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 지정된 텍스트 영역의 클립보드 리스너를 제거합니다.
    /// </summary>
    /// <param name="textAreaId">텍스트 영역의 ID</param>
    public async Task RemovePasteListenerAsync(string textAreaId)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("clipboardHelper.removePasteListener", textAreaId);
            _uploadCallbacks.Remove(textAreaId);
            Console.WriteLine($"클립보드 리스너가 성공적으로 제거되었습니다: {textAreaId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"클립보드 리스너 제거 중 오류: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 지정된 텍스트 영역의 커서 위치에 텍스트를 삽입합니다.
    /// </summary>
    /// <param name="textAreaId">텍스트 영역의 ID</param>
    /// <param name="text">삽입할 텍스트</param>
    public async Task InsertTextAtCursorAsync(string textAreaId, string text)
    {
        await _jsRuntime.InvokeVoidAsync("clipboardHelper.insertTextAtCursor", textAreaId, text);
    }

    /// <summary>
    /// 지정된 텍스트 영역에서 특정 텍스트를 다른 텍스트로 교체합니다.
    /// </summary>
    /// <param name="textAreaId">텍스트 영역의 ID</param>
    /// <param name="oldText">교체할 기존 텍스트</param>
    /// <param name="newText">새로운 텍스트</param>
    public async Task ReplaceTextAsync(string textAreaId, string oldText, string newText)
    {
        await _jsRuntime.InvokeVoidAsync("clipboardHelper.replaceText", textAreaId, oldText, newText);
    }

    /// <summary>
    /// JavaScript에서 호출되는 메서드 - 이미지 붙여넣기 처리
    /// </summary>
    /// <param name="textAreaId">텍스트 영역의 ID</param>
    /// <param name="base64Data">Base64 인코딩된 이미지 데이터</param>
    /// <param name="fileName">파일명</param>
    /// <param name="mimeType">MIME 타입</param>
    [JSInvokable]
    public async Task OnImagePasted(string textAreaId, string base64Data, string fileName, string mimeType)
    {
        try
        {
            if (!_uploadCallbacks.TryGetValue(textAreaId, out var callback))
            {
                Console.WriteLine($"텍스트 영역 {textAreaId}에 대한 콜백이 등록되지 않았습니다.");
                return;
            }

            Console.WriteLine($"이미지 붙여넣기 시작: {fileName}, MIME: {mimeType}, Base64 길이: {base64Data.Length}");

            // 임시 마크다운 텍스트 생성 및 삽입
            var timestamp = DateTime.Now.ToString("HHmmss");
            var placeholder = $"![업로드 중...](uploading_{timestamp})";

            await InsertTextAtCursorAsync(textAreaId, placeholder);
            Console.WriteLine("임시 placeholder 삽입 완료");

            // Base64를 바이트 배열로 변환
            var imageData = Convert.FromBase64String(base64Data);
            Console.WriteLine($"Base64 디코딩 완료: {imageData.Length} bytes");

            // 이미지 업로드
            using var imageStream = new MemoryStream(imageData);
            var imageUrl = await _imageService.UploadImageAsync("dimohy", imageStream, fileName); // TODO: 실제 사용자명으로 변경
            Console.WriteLine($"이미지 업로드 완료: {imageUrl}");

            // placeholder를 실제 URL로 교체
            var altText = Path.GetFileNameWithoutExtension(fileName);
            var newImageMarkdown = $"![{altText}]({imageUrl})";

            await ReplaceTextAsync(textAreaId, placeholder, newImageMarkdown);
            Console.WriteLine("이미지 마크다운 교체 완료");

            // 콜백 호출하여 UI 업데이트
            await callback(placeholder, newImageMarkdown, imageUrl);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"이미지 업로드 오류: {ex.Message}");

            // 오류 발생 시 placeholder 제거
            var timestamp = DateTime.Now.ToString("HHmmss");
            var placeholder = $"![업로드 중...](uploading_{timestamp})";
            await ReplaceTextAsync(textAreaId, placeholder, "");

            // TODO: 사용자에게 오류 메시지 표시
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_dotNetHelper != null)
            {
                foreach (var textAreaId in _uploadCallbacks.Keys.ToList())
                {
                    await RemovePasteListenerAsync(textAreaId);
                }
                _dotNetHelper.Dispose();
                _dotNetHelper = null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ClipboardService 해제 중 오류: {ex.Message}");
        }
    }
}
