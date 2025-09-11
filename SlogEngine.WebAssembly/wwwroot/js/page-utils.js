// 페이지 타이틀 업데이트 함수
window.updatePageTitle = function(title) {
    document.title = title;
};

// 페이지 아이콘 업데이트 함수  
window.updatePageIcon = function(iconPath) {
    // 기존 favicon 링크 제거
    const existingFavicons = document.querySelectorAll('link[rel*="icon"]');
    existingFavicons.forEach(link => link.remove());
    
    // SVG favicon 추가 (우선순위)
    const svgLink = document.createElement('link');
    svgLink.rel = 'icon';
    svgLink.type = 'image/svg+xml';
    svgLink.href = iconPath || 'favicon.svg';
    document.head.appendChild(svgLink);
    
    // PNG fallback 추가
    const pngLink = document.createElement('link');
    pngLink.rel = 'icon';
    pngLink.type = 'image/png';
    pngLink.href = 'favicon.png';
    document.head.appendChild(pngLink);
};

// 페이지 로드 시 기본 아이콘 설정
window.addEventListener('DOMContentLoaded', function() {
    updatePageIcon('favicon.svg');
});
