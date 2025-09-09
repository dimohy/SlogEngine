using Microsoft.AspNetCore.Mvc;
using SlogEngine.Server.Interfaces;
using SlogEngine.Server.Models;

namespace SlogEngine.Server.Controllers;

[ApiController]
[Route("blog")]
public class BlogController : ControllerBase
{
    private readonly IBlogService _blogService;

    public BlogController(IBlogService blogService)
    {
        _blogService = blogService;
    }

    [HttpGet("{username}")]
    public IActionResult GetPosts(string username)
    {
        var posts = _blogService.GetPosts(username);
        return Ok(posts);
    }

    [HttpGet("{username}/{postId}")]
    public IActionResult GetPost(string username, string postId)
    {
        var post = _blogService.GetPost(username, postId);
        if (post == null)
        {
            return NotFound();
        }
        return Ok(post);
    }

    [HttpPost("{username}")]
    public IActionResult AddPost(string username, [FromBody] BlogPost post)
    {
        if (post == null || string.IsNullOrEmpty(post.Title) || string.IsNullOrEmpty(post.Content))
        {
            return BadRequest("Invalid post data.");
        }
        post.Author = username;
        _blogService.AddPost(username, post);
        return CreatedAtAction(nameof(GetPost), new { username, postId = post.Id }, post);
    }

    [HttpPut("{username}/{postId}")]
    public IActionResult UpdatePost(string username, string postId, [FromBody] BlogPost post)
    {
        if (post == null || post.Id != postId)
        {
            return BadRequest("Invalid post data.");
        }
        var existingPost = _blogService.GetPost(username, postId);
        if (existingPost == null)
        {
            return NotFound();
        }
        post.Author = username;
        _blogService.UpdatePost(username, post);
        return NoContent();
    }

    [HttpDelete("{username}/{postId}")]
    public IActionResult DeletePost(string username, string postId)
    {
        var post = _blogService.GetPost(username, postId);
        if (post == null)
        {
            return NotFound();
        }
        _blogService.DeletePost(username, postId);
        return NoContent();
    }

    [HttpGet("{username}/meta")]
    public IActionResult GetBlogMeta(string username)
    {
        var meta = _blogService.GetBlogMeta(username);
        return Ok(meta);
    }

    [HttpPut("{username}/meta")]
    public IActionResult UpdateBlogMeta(string username, [FromBody] BlogMeta meta)
    {
        if (meta == null)
        {
            return BadRequest("Invalid meta data.");
        }
        _blogService.UpdateBlogMeta(username, meta);
        return NoContent();
    }

    [HttpPost("{username}/images/upload")]
    public async Task<IActionResult> UploadImage(string username, [FromForm] IFormFile image)
    {
        Console.WriteLine($"이미지 업로드 요청 받음: username={username}, image={image?.FileName}");
        
        if (image == null || image.Length == 0)
        {
            Console.WriteLine("이미지 파일이 없음");
            return BadRequest("이미지 파일이 필요합니다.");
        }

        Console.WriteLine($"이미지 정보: 파일명={image.FileName}, 크기={image.Length}, 타입={image.ContentType}");

        // 파일 형식 검증
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
        
        if (!allowedExtensions.Contains(extension))
        {
            Console.WriteLine($"지원되지 않는 파일 형식: {extension}");
            return BadRequest("지원되지 않는 이미지 형식입니다. (jpg, jpeg, png, gif, webp만 지원)");
        }

        // 파일 크기 검증 (10MB 제한)
        if (image.Length > 10 * 1024 * 1024)
        {
            Console.WriteLine($"파일 크기 초과: {image.Length} bytes");
            return BadRequest("이미지 파일 크기는 10MB를 초과할 수 없습니다.");
        }

        try
        {
            Console.WriteLine("이미지 저장 시작");
            var imageUrl = await _blogService.SaveTempImageAsync(username, image);
            Console.WriteLine($"이미지 저장 완료: {imageUrl}");
            return Ok(new { url = imageUrl });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"이미지 업로드 오류: {ex.Message}");
            return StatusCode(500, $"이미지 업로드 중 오류가 발생했습니다: {ex.Message}");
        }
    }

    [HttpGet("{username}/images/test")]
    public IActionResult TestImageEndpoint(string username)
    {
        Console.WriteLine($"이미지 테스트 엔드포인트 호출: {username}");
        return Ok(new { message = $"이미지 엔드포인트가 {username}에 대해 작동 중입니다." });
    }

    [HttpGet("debug/files/{username}")]
    public IActionResult DebugFiles(string username)
    {
        var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var userPath = Path.Combine(webRootPath, "blogs", username);
        var imagesPath = Path.Combine(userPath, "images");
        
        var result = new
        {
            WebRootPath = webRootPath,
            UserPath = userPath,
            ImagesPath = imagesPath,
            UserPathExists = Directory.Exists(userPath),
            ImagesPathExists = Directory.Exists(imagesPath),
            Files = Directory.Exists(imagesPath) ? 
                Directory.GetFiles(imagesPath, "*", SearchOption.AllDirectories).ToList() : 
                new List<string>()
        };
        
        return Ok(result);
    }
}
