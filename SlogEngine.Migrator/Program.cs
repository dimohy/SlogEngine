using SlogEngine.Migrator;

namespace SlogEngine.Migrator;

/// <summary>
/// 기존 블로그 포스트를 SlogEngine으로 마이그레이션하는 프로그램
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("SlogEngine 블로그 마이그레이션 도구");
        Console.WriteLine("=====================================");

        // 소스 경로 설정
        var sourcePath = @"W:\MyWorks\SlogEngine\dimohy.slogs.dev";
        var targetPath = @"W:\MyWorks\SlogEngine\SlogEngine.Server\wwwroot\blogs\dimohy";
        var username = "dimohy";

        Console.WriteLine($"소스 경로: {sourcePath}");
        Console.WriteLine($"대상 경로: {targetPath}");
        Console.WriteLine($"사용자명: {username}");
        Console.WriteLine();

        try
        {
            using var migrator = new HashnodeMigrator();
            await migrator.MigrateAsync(sourcePath, targetPath, username);
            
            Console.WriteLine("마이그레이션이 성공적으로 완료되었습니다!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"마이그레이션 중 오류 발생: {ex.Message}");
            Console.WriteLine($"상세 정보: {ex}");
        }

        Console.WriteLine("\n아무 키나 누르면 종료됩니다...");
        Console.ReadKey();
    }
}
