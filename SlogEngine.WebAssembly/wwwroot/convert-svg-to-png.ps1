# SVG를 PNG로 변환하는 PowerShell 스크립트
param(
    [string]$SvgPath = "favicon.svg",
    [string]$PngPath = "favicon.png",
    [int]$Size = 32
)

# HTML 파일 생성하여 SVG를 Canvas로 렌더링
$htmlContent = @"
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8">
</head>
<body>
    <canvas id="canvas" width="$Size" height="$Size"></canvas>
    <script>
        const canvas = document.getElementById('canvas');
        const ctx = canvas.getContext('2d');
        
        fetch('$SvgPath')
            .then(response => response.text())
            .then(svgText => {
                const img = new Image();
                const blob = new Blob([svgText], {type: 'image/svg+xml'});
                const url = URL.createObjectURL(blob);
                
                img.onload = function() {
                    ctx.drawImage(img, 0, 0, $Size, $Size);
                    canvas.toBlob(function(blob) {
                        const a = document.createElement('a');
                        a.href = URL.createObjectURL(blob);
                        a.download = '$PngPath';
                        a.click();
                    }, 'image/png');
                };
                img.src = url;
            });
    </script>
</body>
</html>
"@

# 임시 HTML 파일 생성
$tempHtml = "temp_converter.html"
$htmlContent | Out-File -FilePath $tempHtml -Encoding UTF8

Write-Host "임시 HTML 파일이 생성되었습니다: $tempHtml"
Write-Host "이 파일을 브라우저에서 열면 자동으로 PNG 파일이 다운로드됩니다."
Write-Host "다운로드가 완료되면 파일을 favicon.png로 이름을 변경하고 임시 파일을 삭제하세요."
