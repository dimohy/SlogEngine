// 이미지 갤러리 JavaScript 모듈
window.ImageGallery = (function() {
    let dotnetInstance = null;
    let keyListener = null;
    let pendingImageClicks = [];

    // 초기화 함수
    function initialize(dotnetRef) {
        console.log('ImageGallery.initialize called');
        dotnetInstance = dotnetRef;
        
        // 대기 중인 이미지 클릭 이벤트가 있다면 처리
        if (pendingImageClicks.length > 0) {
            console.log(`Processing ${pendingImageClicks.length} pending image clicks`);
            pendingImageClicks.forEach(pendingClick => {
                openImageGallery(pendingClick.clickedImage, pendingClick.allImages);
            });
            pendingImageClicks = [];
        }
        
        setupImageClickHandlers();
    }

    // 이미지 클릭 핸들러 설정
    function setupImageClickHandlers() {
        console.log('Setting up image click handlers');
        
        // 모든 이미지를 찾아서 클릭 이벤트 추가
        const allImages = document.querySelectorAll('img');
        console.log(`Found ${allImages.length} images on page`);
        
        allImages.forEach((img, index) => {
            console.log(`Processing image ${index + 1}: ${img.src}`);
            
            // 기존 이벤트 리스너 제거
            img.removeEventListener('click', handleImageClick);
            
            // 이미지에 클릭 가능한 스타일 추가
            img.style.cursor = 'pointer';
            img.style.transition = 'transform 0.2s ease, filter 0.2s ease';
            
            // 호버 효과
            img.addEventListener('mouseenter', function() {
                console.log('Image hover in');
                this.style.transform = 'scale(1.02)';
                this.style.filter = 'brightness(1.1)';
            });
            
            img.addEventListener('mouseleave', function() {
                console.log('Image hover out');
                this.style.transform = 'scale(1)';
                this.style.filter = 'brightness(1)';
            });

            // 클릭 이벤트 추가
            img.addEventListener('click', handleImageClick);
        });
    }

    // 이미지 클릭 핸들러
    function handleImageClick(e) {
        console.log('Image clicked:', e.target.src);
        e.preventDefault();
        e.stopPropagation();
        
        const clickedImage = e.target;
        const allImages = document.querySelectorAll('img');
        
        openImageGallery(clickedImage, allImages);
    }

    // 이미지 갤러리 열기
    function openImageGallery(clickedImage, allImages) {
        console.log('Opening image gallery for:', clickedImage.src);
        
        if (!dotnetInstance) {
            console.log('DotNet instance not available yet, queuing click event');
            // DotNet 인스턴스가 아직 준비되지 않았다면 대기열에 추가
            pendingImageClicks.push({
                clickedImage: clickedImage,
                allImages: allImages
            });
            return;
        }

        // 모든 이미지 정보 수집
        const imageInfos = Array.from(allImages).map(img => ({
            src: img.src,
            alt: img.alt || ''
        }));

        console.log('Image infos:', imageInfos);

        // C# 메서드 호출
        try {
            dotnetInstance.invokeMethodAsync('OpenGallery', clickedImage.src, clickedImage.alt || '', imageInfos);
        } catch (error) {
            console.error('Error calling OpenGallery:', error);
        }
    }

    // 키보드 이벤트 리스너 추가
    function addKeyListener() {
        keyListener = function(e) {
            if (dotnetInstance) {
                dotnetInstance.invokeMethodAsync('HandleKeyPress', e.key);
            }
        };
        document.addEventListener('keydown', keyListener);
    }

    // 키보드 이벤트 리스너 제거
    function removeKeyListener() {
        if (keyListener) {
            document.removeEventListener('keydown', keyListener);
            keyListener = null;
        }
    }

    // 페이지 내용이 변경될 때 이미지 핸들러 재설정
    function refreshImageHandlers() {
        console.log('Refreshing image handlers');
        setupImageClickHandlers();
    }

    // Blazor 페이지 변경 감지를 위한 MutationObserver
    function setupMutationObserver() {
        const observer = new MutationObserver(function(mutations) {
            mutations.forEach(function(mutation) {
                if (mutation.type === 'childList' && mutation.addedNodes.length > 0) {
                    // 새로운 노드가 추가되면 이미지 핸들러 재설정
                    console.log('DOM mutation detected, refreshing handlers in 100ms');
                    setTimeout(refreshImageHandlers, 100);
                }
            });
        });

        // body 전체를 관찰
        observer.observe(document.body, {
            childList: true,
            subtree: true
        });
        
        console.log('MutationObserver set up');
    }

    // DOM이 로드되면 MutationObserver 설정
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', setupMutationObserver);
    } else {
        setupMutationObserver();
    }

    // 공개 API
    return {
        initialize: initialize,
        addKeyListener: addKeyListener,
        removeKeyListener: removeKeyListener,
        refreshImageHandlers: refreshImageHandlers
    };
})();
