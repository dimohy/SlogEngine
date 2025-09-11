# 모든 포스트 디렉토리에서 이미지 파일명을 수정하는 스크립트

$postsPath = "w:\MyWorks\SlogEngine\SlogEngine.Server\wwwroot\blogs\dimohy\posts"

# 모든 포스트 디렉토리 확인
Get-ChildItem -Path $postsPath -Directory | ForEach-Object {
    $postDir = $_.FullName
    $postId = $_.Name
    
    Write-Host "처리 중: $postId"
    
    # 해당 디렉토리의 모든 이미지 파일 확인
    Get-ChildItem -Path $postDir -File | Where-Object {
        $_.Extension -match '\.(png|jpg|jpeg|gif|webp)$'
    } | ForEach-Object {
        $file = $_
        $oldName = $file.Name
        
        # 포스트 ID 접두사가 있는 경우 제거
        if ($oldName.StartsWith("$postId`_")) {
            $newName = $oldName.Substring("$postId`_".Length)
            $newPath = Join-Path $postDir $newName
            
            try {
                Move-Item -Path $file.FullName -Destination $newPath
                Write-Host "  $oldName -> $newName"
            }
            catch {
                Write-Host "  오류: $oldName - $($_.Exception.Message)" -ForegroundColor Red
            }
        }
    }
}

Write-Host "완료!"
