function setTheme(className) {
    // 정확한 테마 클래스명 사용
    if (className.includes('dark')) {
        document.body.className = 'dark-theme';
    } else {
        document.body.className = 'light-theme';
    }
    
    // 테마 변경 시 마크다운 콘텐츠 재처리
    if (typeof window.onThemeChange === 'function') {
        window.onThemeChange();
    }
}

// 초기 테마 설정
document.addEventListener('DOMContentLoaded', function() {
    const currentTheme = document.body.className;
    console.log('초기 테마:', currentTheme);
    if (currentTheme) {
        setTheme(currentTheme);
    }
});
