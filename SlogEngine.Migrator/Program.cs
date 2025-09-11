using SlogEngine.Migrator;

namespace SlogEngine.Migrator;

/// <summary>
/// 기존 블로그 포스트를 SlogEngine으로 마이그레이션하는 프로그램
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== SlogEngine 블로그 포스트 마이그레이션 도구 ===");
        Console.WriteLine("JSON 형식의 블로그 포스트를 Markdown 형식으로 변환합니다.");
        Console.WriteLine();

        // 옵션 선택
        Console.WriteLine("실행할 작업을 선택하세요:");
        Console.WriteLine("1. JSON -> Markdown 마이그레이션");
        Console.WriteLine("2. Hashnode 마이그레이션 (기존)");
        Console.Write("선택 (1 또는 2): ");
        
        var choice = Console.ReadLine();
        
        if (choice == "1")
        {
            RunJsonToMarkdownMigration();
        }
        else if (choice == "2")
        {
            RunHashnodeMigration().Wait();
        }
        else
        {
            Console.WriteLine("잘못된 선택입니다.");
            return;
        }
    }

    static void RunJsonToMarkdownMigration()
    {
        Console.WriteLine("\n🚀 JSON -> Markdown 마이그레이션을 시작합니다...");
        
        // wwwroot/blogs 경로 설정
        var blogsPath = @"W:\MyWorks\SlogEngine\SlogEngine.Server\wwwroot\blogs";

        if (!Directory.Exists(blogsPath))
        {
            Console.WriteLine($"❌ 블로그 디렉토리를 찾을 수 없습니다: {blogsPath}");
            Console.WriteLine("올바른 경로에서 실행하고 있는지 확인해주세요.");
            return;
        }

        Console.WriteLine($"📁 블로그 디렉토리: {blogsPath}");
        Console.WriteLine();

        var migrationService = new BlogMigrationService(blogsPath);

        try
        {
            migrationService.MigrateAllPosts();

            Console.WriteLine();
            Console.WriteLine("✅ 마이그레이션이 성공적으로 완료되었습니다!");
            Console.WriteLine();
            Console.WriteLine("변경 사항:");
            Console.WriteLine("- JSON 파일들이 Markdown 파일로 변환되었습니다");
            Console.WriteLine("- 이미지들이 각 포스트별 폴더로 이동되었습니다");
            Console.WriteLine("- 이미지 URL들이 새로운 경로로 업데이트되었습니다");
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine($"❌ 마이그레이션 중 오류가 발생했습니다: {ex.Message}");
            Console.WriteLine($"상세 정보: {ex}");
        }

        Console.WriteLine();
        Console.WriteLine("아무 키나 눌러서 종료하세요...");
        Console.ReadKey();
    }

    static async Task RunHashnodeMigration()
    {
        Console.WriteLine("\n🚀 Hashnode 마이그레이션을 시작합니다...");
        
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
