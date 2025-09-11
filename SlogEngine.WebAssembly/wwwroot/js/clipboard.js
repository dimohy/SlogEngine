// 클립보드 이미지 처리 관련 함수들
window.clipboardHelper = {
    listeners: new Map(), // 이벤트 리스너 저장
    
    // 클립보드 이벤트 리스너 등록
    addPasteListener: function(elementId, dotNetHelper) {
        console.log('Adding paste listener for element:', elementId);
        const element = document.getElementById(elementId);
        if (!element) {
            console.error('Element not found:', elementId);
            return;
        }
        
        // 기존 리스너가 있으면 제거
        this.removePasteListener(elementId);
        
        const pasteHandler = async function(e) {
            console.log('Paste event detected, clipboardData items:', e.clipboardData.items.length);
            
            const items = e.clipboardData.items;
            let hasImage = false;
            
            for (let i = 0; i < items.length; i++) {
                const item = items[i];
                console.log('Item type:', item.type, 'Kind:', item.kind);
                
                // 이미지 타입 체크
                if (item.type.indexOf('image') !== -1) {
                    console.log('Image found in clipboard');
                    hasImage = true;
                    e.preventDefault();
                    
                    const file = item.getAsFile();
                    if (file) {
                        console.log('File size:', file.size, 'Type:', file.type);
                        
                        const reader = new FileReader();
                        
                        reader.onload = function(event) {
                            try {
                                console.log('FileReader loaded, data length:', event.target.result.byteLength);
                                
                                // Base64로 인코딩하여 전달
                                const arrayBuffer = event.target.result;
                                const bytes = new Uint8Array(arrayBuffer);
                                let binary = '';
                                for (let i = 0; i < bytes.byteLength; i++) {
                                    binary += String.fromCharCode(bytes[i]);
                                }
                                const base64String = btoa(binary);
                                
                                // 파일명 생성
                                const timestamp = new Date().toISOString().replace(/[:.]/g, '-');
                                const extension = file.type.split('/')[1] || 'png';
                                const fileName = `clipboard_${timestamp}.${extension}`;
                                
                                console.log('Calling Blazor method with filename:', fileName, 'Base64 length:', base64String.length);
                                
                                // Blazor 메서드 호출 (Base64 문자열로)
                                dotNetHelper.invokeMethodAsync('OnImagePasted', elementId, base64String, fileName, file.type)
                                    .then(() => {
                                        console.log('Successfully called OnImagePasted');
                                    })
                                    .catch(error => {
                                        console.error('Error calling OnImagePasted:', error);
                                    });
                            } catch (error) {
                                console.error('Error processing image:', error);
                            }
                        };
                        
                        reader.onerror = function(error) {
                            console.error('FileReader error:', error);
                        };
                        
                        reader.readAsArrayBuffer(file);
                    }
                    break;
                }
            }
            
            if (!hasImage) {
                console.log('No image found in clipboard');
            }
        };
        
        // 이벤트 리스너 등록
        element.addEventListener('paste', pasteHandler);
        this.listeners.set(elementId, pasteHandler);
        
        console.log('Paste listener added successfully for element:', elementId);
    },
    
    // 클립보드 이벤트 리스너 제거
    removePasteListener: function(elementId) {
        const element = document.getElementById(elementId);
        const listener = this.listeners.get(elementId);
        
        if (element && listener) {
            element.removeEventListener('paste', listener);
            this.listeners.delete(elementId);
            console.log('Paste listener removed for element:', elementId);
        }
    },
    
    // 텍스트 에리어에 텍스트 삽입
    insertTextAtCursor: function(elementId, textToInsert) {
        const element = document.getElementById(elementId);
        if (element) {
            const startPos = element.selectionStart || 0;
            const endPos = element.selectionEnd || 0;
            const beforeText = element.value.substring(0, startPos);
            const afterText = element.value.substring(endPos, element.value.length);
            
            element.value = beforeText + textToInsert + afterText;
            element.selectionStart = element.selectionEnd = startPos + textToInsert.length;
            
            // 변경 이벤트 발생 (Blazor 바인딩을 위해)
            element.dispatchEvent(new Event('input', { bubbles: true }));
            element.dispatchEvent(new Event('change', { bubbles: true }));
            element.focus();
            
            console.log('Text inserted at cursor position:', startPos);
        } else {
            console.error('Element not found for text insertion:', elementId);
        }
    },
    
    // 텍스트 에리어에서 특정 텍스트를 다른 텍스트로 교체
    replaceText: function(elementId, oldText, newText) {
        const element = document.getElementById(elementId);
        if (element) {
            const currentValue = element.value;
            const newValue = currentValue.replace(oldText, newText);
            
            if (currentValue !== newValue) {
                element.value = newValue;
                
                // 변경 이벤트 발생 (Blazor 바인딩을 위해)
                element.dispatchEvent(new Event('input', { bubbles: true }));
                element.dispatchEvent(new Event('change', { bubbles: true }));
                
                console.log('Text replaced successfully:', oldText, '->', newText);
            } else {
                console.log('No text replacement occurred for:', oldText);
            }
        } else {
            console.error('Element not found for text replacement:', elementId);
        }
    }
};
