using System.ComponentModel.DataAnnotations;

namespace SlogEngine.Server.Models;

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
}
