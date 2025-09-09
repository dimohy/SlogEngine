// 마크다운 콘텐츠 향상 기능

document.addEventListener('DOMContentLoaded', function() {
    console.log('마크다운 스크립트 로드됨');
    
    // Prism.js 설정
    setupPrismConfiguration();
    
    // 코드 블록 기능 설정
    setupCodeBlockFeatures();
    
    // 표에 반응형 래퍼 추가
    enhanceTables();
});

// Prism.js 설정 - 기존 테마 시스템과 통합
function setupPrismConfiguration() {
    // Prism 자동 로더 설정
    if (window.Prism) {
        // 자동 하이라이팅 비활성화 (수동으로 제어)
        window.Prism.manual = true;
        
        // 화이트스페이스 정규화 설정
        if (window.Prism.plugins.NormalizeWhitespace) {
            window.Prism.plugins.NormalizeWhitespace.setDefaults({
                'remove-trailing': true,
                'remove-indent': true,
                'left-trim': true,
                'right-trim': true
            });
        }
        
        // Prism.js의 기본 CSS를 무시하고 우리 커스텀 CSS만 사용
        // 기본 테마 색상을 투명하게 만들어서 우리 CSS가 우선되도록 함
        const style = document.createElement('style');
        style.textContent = `
            /* Prism.js 기본 색상 무효화 - 우리 커스텀 CSS가 우선되도록 */
            pre[class*="language-"] {
                background: transparent !important;
            }
            code[class*="language-"] {
                background: transparent !important;
                color: inherit !important;
            }
            .token {
                background: transparent !important;
            }
        `;
        document.head.appendChild(style);
    }
}

// 코드 내용을 분석하여 언어를 감지하는 함수
function detectLanguage(code) {
    // JavaScript 패턴
    if (code.includes('function') && (code.includes('const ') || code.includes('let ') || code.includes('var '))) {
        if (code.includes('console.log') || code.includes('=>') || code.includes('async ') || code.includes('await ')) {
            return 'javascript';
        }
    }
    
    // TypeScript 패턴
    if (code.includes('interface ') || code.includes(': string') || code.includes(': number') || code.includes('type ')) {
        return 'typescript';
    }
    
    // C# 패턴
    if (code.includes('public class') || code.includes('using System') || code.includes('namespace ') || code.includes('public async Task')) {
        return 'csharp';
    }
    
    // Python 패턴
    if (code.includes('def ') || code.includes('import ') || code.includes('from ') || code.includes('print(')) {
        return 'python';
    }
    
    // HTML 패턴
    if (code.includes('<html') || code.includes('<!DOCTYPE') || (code.includes('<div') && code.includes('</div>'))) {
        return 'html';
    }
    
    // CSS 패턴
    if (code.includes('{') && code.includes('}') && (code.includes(':') && code.includes(';'))) {
        if (code.includes('color:') || code.includes('background:') || code.includes('margin:')) {
            return 'css';
        }
    }
    
    // JSON 패턴
    if ((code.startsWith('{') && code.endsWith('}')) || (code.startsWith('[') && code.endsWith(']'))) {
        try {
            JSON.parse(code);
            return 'json';
        } catch (e) {
            // JSON이 아님
        }
    }
    
    // SQL 패턴
    if (code.toUpperCase().includes('SELECT ') || code.toUpperCase().includes('INSERT ') || code.toUpperCase().includes('CREATE TABLE')) {
        return 'sql';
    }
    
    // Bash/Shell 패턴
    if (code.includes('#!/bin/bash') || code.includes('cd ') || code.includes('ls ') || code.includes('mkdir ')) {
        return 'bash';
    }
    
    // 기본값
    return 'text';
}

function enhanceTables() {
    const tables = document.querySelectorAll('.post-content table, .post-summary table');
    
    tables.forEach(table => {
        if (!table.parentElement.classList.contains('table-wrapper')) {
            const wrapper = document.createElement('div');
            wrapper.className = 'table-wrapper';
            table.parentNode.insertBefore(wrapper, table);
            wrapper.appendChild(table);
        }
    });
}

// 코드블럭에 헤더와 Prism.js 하이라이팅을 적용하는 함수
function setupCodeBlockFeatures() {
    console.log('setupCodeBlockFeatures 함수 호출됨');
    
    // 모든 코드 블록을 찾아서 처리
    const codeBlocks = document.querySelectorAll('pre code');
    console.log('발견된 코드 블록 수:', codeBlocks.length);
    
    codeBlocks.forEach((codeElement, index) => {
        const preElement = codeElement.parentElement;
        if (preElement.tagName !== 'PRE') return;
        
        // 이미 처리된 코드 블록인지 확인
        if (preElement.querySelector('.code-block-header') || preElement.classList.contains('prism-processed')) return;
        
        // 언어 감지
        let language = 'text';
        const className = codeElement.className;
        const langMatch = className.match(/language-(\w+)/);
        
        if (langMatch) {
            language = langMatch[1];
        } else {
            // 코드 내용으로 언어 추정
            const codeText = codeElement.textContent.trim();
            language = detectLanguage(codeText);
        }
        
        // 언어 클래스 추가 (Prism.js용)
        codeElement.className = `language-${language}`;
        preElement.className = `language-${language}`;
        
        // 언어명 매핑
        const languageNames = {
            'javascript': 'JavaScript',
            'js': 'JavaScript',
            'typescript': 'TypeScript',
            'ts': 'TypeScript',
            'csharp': 'C#',
            'cs': 'C#',
            'python': 'Python',
            'py': 'Python',
            'html': 'HTML',
            'css': 'CSS',
            'json': 'JSON',
            'sql': 'SQL',
            'bash': 'Bash',
            'shell': 'Shell',
            'xml': 'XML',
            'java': 'Java',
            'cpp': 'C++',
            'c': 'C',
            'php': 'PHP',
            'go': 'Go',
            'rust': 'Rust',
            'kotlin': 'Kotlin',
            'swift': 'Swift',
            'text': 'Text'
        };
        
        const displayName = languageNames[language.toLowerCase()] || language.charAt(0).toUpperCase() + language.slice(1);
        
        // 코드 블록 헤더 생성
        const header = document.createElement('div');
        header.className = 'code-block-header';
        
        // 언어 레이블 생성
        const languageLabel = document.createElement('span');
        languageLabel.className = 'code-language-label';
        languageLabel.textContent = displayName;
        
        // 복사 버튼 생성
        const copyButton = document.createElement('button');
        copyButton.className = 'code-copy-button';
        copyButton.innerHTML = `
            <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                <path d="M16 1H4C2.9 1 2 1.9 2 3V17H4V3H16V1ZM19 5H8C6.9 5 6 5.9 6 7V21C6 22.1 6.9 23 8 23H19C20.1 23 21 22.1 21 21V7C21 5.9 20.1 5 19 5ZM19 21H8V7H19V21Z" fill="currentColor"/>
            </svg>
        `;
        copyButton.title = '코드 복사';
        copyButton.onclick = function() {
            navigator.clipboard.writeText(codeElement.textContent).then(function() {
                // 복사 완료 피드백
                const originalContent = copyButton.innerHTML;
                copyButton.innerHTML = `
                    <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                        <path d="M9 16.17L4.83 12L3.41 13.41L9 19L21 7L19.59 5.59L9 16.17Z" fill="currentColor"/>
                    </svg>
                `;
                copyButton.style.color = '#10b981';
                setTimeout(() => {
                    copyButton.innerHTML = originalContent;
                    copyButton.style.color = '';
                }, 2000);
            }).catch(function(err) {
                console.error('클립보드 복사 실패:', err);
                // 실패 피드백
                const originalContent = copyButton.innerHTML;
                copyButton.innerHTML = `
                    <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                        <path d="M19 6.41L17.59 5L12 10.59L6.41 5L5 6.41L10.59 12L5 17.59L6.41 19L12 13.41L17.59 19L19 17.59L13.41 12L19 6.41Z" fill="currentColor"/>
                    </svg>
                `;
                copyButton.style.color = '#ef4444';
                setTimeout(() => {
                    copyButton.innerHTML = originalContent;
                    copyButton.style.color = '';
                }, 2000);
            });
        };
        
        // 헤더에 요소들 추가
        header.appendChild(languageLabel);
        header.appendChild(copyButton);
        
        // pre 요소에 상대적 위치 설정
        preElement.style.position = 'relative';
        
        // pre 요소의 첫 번째 자식으로 헤더 추가
        preElement.insertBefore(header, preElement.firstChild);
        
        // Prism.js로 구문 강조 적용 (우리 커스텀 CSS 스타일 사용)
        if (window.Prism && window.Prism.highlightElement) {
            try {
                window.Prism.highlightElement(codeElement);
            } catch (error) {
                console.warn('Prism 하이라이팅 실패:', error);
            }
        }
        
        // 처리 완료 표시
        preElement.classList.add('prism-processed');
    });
}

// 마크다운 콘텐츠가 동적으로 로드될 때 호출할 수 있는 함수
window.enhanceMarkdownContent = function() {
    console.log('enhanceMarkdownContent 호출됨');
    setupCodeBlockFeatures();
    enhanceTables();
    
    // Prism.js로 모든 코드 블록 재처리
    if (window.Prism && window.Prism.highlightAll) {
        window.Prism.highlightAll();
    }
};

// 테마 변경 시 코드 블록 재처리를 위한 함수
window.onThemeChange = function() {
    console.log('테마 변경 감지, 코드 블록 재처리');
    setTimeout(() => {
        if (window.Prism && window.Prism.highlightAll) {
            window.Prism.highlightAll();
        }
    }, 100);
};

// 페이지 로드 후 일정 시간 대기 후 다시 실행 (Blazor 렌더링 대기)
setTimeout(() => {
    console.log('setTimeout으로 다시 실행');
    if (typeof window.enhanceMarkdownContent === 'function') {
        window.enhanceMarkdownContent();
    }
}, 2000);

// MutationObserver를 사용하여 DOM 변경 감지
const observer = new MutationObserver((mutations) => {
    let shouldUpdate = false;
    mutations.forEach((mutation) => {
        if (mutation.type === 'childList') {
            mutation.addedNodes.forEach((node) => {
                if (node.nodeType === 1 && (node.tagName === 'PRE' || node.querySelector('pre'))) {
                    shouldUpdate = true;
                }
            });
        }
    });
    
    if (shouldUpdate) {
        console.log('DOM 변경 감지됨, 코드 블록 재처리');
        setTimeout(() => {
            if (typeof window.enhanceMarkdownContent === 'function') {
                window.enhanceMarkdownContent();
            }
        }, 100);
    }
});

// body 요소의 변경 감지 시작
observer.observe(document.body, {
    childList: true,
    subtree: true
});
