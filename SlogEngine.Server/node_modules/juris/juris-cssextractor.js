// juris-css-extractor.js
if (typeof CSSExtractor === 'undefined') {
    class CSSExtractor {
        constructor() {
            this.cache = new Map();
            this.reactiveCache = new Map();
            this.styleSheet = null;
            this.componentCounter = 0;
            this.reactiveCounter = 0;
            this.pseudoVarCounter = 0;
            this.patterns = {
                media: /^@media/,
                container: /^@container/,
                keyframes: /^@keyframes/,
                layer: /^@layer/,
                pseudo: /^&[:]/,
                interactive: /^\$/,
                cssVar: /^--/
            };
        }
        
        // Main entry point for processing props
        processProps(props, elementName, domRenderer) {
            if (!props.style) return props;
            
            // Skip reactive style functions - they'll be post-processed later
            if (typeof props.style === 'function') {
                return props;
            }
            
            try {
                const extraction = this.extractCSS(elementName, props.style);
                if (!extraction) return props;
                
                const modifiedProps = { ...props };
                
                if (extraction.className) {
                    modifiedProps.className = this.combineClassNames(
                        props.className, 
                        extraction.className
                    );
                }
                
                if (extraction.reactiveStyle) {
                    modifiedProps.style = extraction.reactiveStyle;
                } else {
                    delete modifiedProps.style;
                }
                
                if (extraction.handlers) {
                    Object.assign(modifiedProps, extraction.handlers);
                }
                
                return modifiedProps;
            } catch (error) {
                console.error('CSS extractor error:', error);
                return props;
            }
        }
        
        // NEW: Post-process reactive style function results
        postProcessReactiveResult(result, componentName, element) {
            if (!result || typeof result !== 'object') {
                return result;
            }
            
            // Check if result has complex CSS features
            const hasComplexFeatures = this.hasComplexFeatures(result);
            
            if (!hasComplexFeatures) {
                // Simple styles, can be applied inline
                return result;
            }
            
            // Generate cache key
            const cacheKey = this.generateReactiveCacheKey(result, componentName);
            
            let cached = this.reactiveCache.get(cacheKey);
            
            if (!cached) {
                // Process complex styles
                const separation = this.separateStyles(result);
                const className = this.generateReactiveClassName(componentName);
                
                // Build and inject CSS for complex features
                const css = this.buildReactiveCSS(className, separation);
                if (css) {
                    this.injectCSS(css);
                }
                
                cached = {
                    className: css ? className : null,
                    inlineStyles: separation.staticStyles || {}
                };
                
                this.reactiveCache.set(cacheKey, cached);
            }
            
            // Apply class to element if needed
            if (cached.className && element && element.classList) {
                element.classList.add(cached.className);
            }
            
            return cached.inlineStyles;
        }
        
        // Check if object has complex CSS features
        hasComplexFeatures(styleObj) {
            return Object.keys(styleObj).some(key => 
                key.startsWith('@') || 
                key.startsWith('&') || 
                key.startsWith('$')
            );
        }
        
        // Extract CSS from style objects
        extractCSS(componentName, styleObj) {
            const separation = this.separateStyles(styleObj);
            
            if (separation.isSimple) {
                return this.handleSimpleExtraction(componentName, separation);
            }
            
            return this.handleComplexExtraction(componentName, separation);
        }
        
        // Separate styles into categories
        separateStyles(styleObj) {
            if (typeof styleObj === 'function') {
                return {
                    staticStyles: {},
                    reactiveStyles: styleObj,
                    isFunction: true,
                    isSimple: true
                };
            }
            
            const result = {
                staticStyles: {},
                reactiveStyles: {},
                pseudoStyles: {},
                mediaQueries: {},
                containerQueries: {},
                animations: {},
                cssVariables: {},
                interactiveStates: {},
                others: {},
                isSimple: true
            };
            
            for (const [key, value] of Object.entries(styleObj)) {
                const category = this.categorizeStyle(key);
                
                if (category !== 'staticStyles' && category !== 'reactiveStyles') {
                    result.isSimple = false;
                }
                
                if (typeof value === 'function') {
                    if (key.startsWith('--')) {
                        result.cssVariables[key] = value;
                    } else {
                        result.reactiveStyles[key] = value;
                    }
                } else if (typeof value === 'object' && value !== null) {
                    result[category][key] = value;
                    if (category !== 'staticStyles') {
                        result.isSimple = false;
                    }
                } else if (value !== null && value !== undefined) {
                    result[category][key] = value;
                }
            }
            
            return result;
        }
        
        // Categorize CSS property
        categorizeStyle(key) {
            if (this.patterns.media.test(key)) return 'mediaQueries';
            if (this.patterns.container.test(key)) return 'containerQueries';
            if (this.patterns.keyframes.test(key)) return 'animations';
            if (this.patterns.pseudo.test(key) || key.includes('&')) return 'pseudoStyles';
            if (this.patterns.interactive.test(key)) return 'interactiveStates';
            if (this.patterns.cssVar.test(key)) return 'cssVariables';
            if (key.startsWith('@')) return 'others';
            return 'staticStyles';
        }
        
        // Handle simple style extraction
        handleSimpleExtraction(componentName, { staticStyles, reactiveStyles, isFunction }) {
            if (isFunction) {
                return { reactiveStyle: reactiveStyles };
            }
            
            const hasStatic = Object.keys(staticStyles).length > 0;
            const hasReactive = Object.keys(reactiveStyles).length > 0;
            
            if (!hasStatic) {
                return hasReactive ? { reactiveStyle: reactiveStyles } : null;
            }
            
            const className = this.generateClassName(componentName, staticStyles);
            
            if (!this.cache.has(className)) {
                const css = this.generateSimpleCSS(className, staticStyles);
                this.injectCSS(css);
                this.cache.set(className, true);
            }
            
            const result = { className };
            if (hasReactive) {
                result.reactiveStyle = reactiveStyles;
            }
            
            return result;
        }
        
        // Handle complex style extraction
        handleComplexExtraction(componentName, separation) {
            const className = this.generateClassName(componentName);
            const css = this.buildCompleteCSS(className, separation);
            
            if (css && !this.cache.has(className)) {
                this.injectCSS(css);
                this.cache.set(className, true);
            }
            
            const result = {};
            
            if (css) {
                result.className = className;
            }
            
            // Combine all reactive styles
            const allReactive = {
                ...separation.reactiveStyles,
                ...separation.cssVariables
            };
            
            if (Object.keys(allReactive).length > 0) {
                result.reactiveStyle = allReactive;
            }
            
            if (Object.keys(separation.interactiveStates).length > 0) {
                result.handlers = this.createInteractiveHandlers(
                    separation.interactiveStates,
                    className
                );
            }
            
            return result;
        }
        
        // Build CSS for reactive styles (complex features)
        buildReactiveCSS(className, separation) {
            let css = '';
            
            // Static styles
            if (Object.keys(separation.staticStyles).length > 0) {
                css += `.${className} {\n${this.styleObjectToCSS(separation.staticStyles)}}\n`;
            }
            
            // Pseudo selectors
            for (const [selector, styles] of Object.entries(separation.pseudoStyles)) {
                css += this.buildPseudoCSS(className, selector, styles);
            }
            
            // Media queries
            for (const [query, styles] of Object.entries(separation.mediaQueries)) {
                css += this.buildMediaQueryCSS(className, query, styles);
            }
            
            // Container queries
            for (const [query, styles] of Object.entries(separation.containerQueries)) {
                css += this.buildContainerQueryCSS(className, query, styles);
            }
            
            // Animations
            for (const [name, keyframes] of Object.entries(separation.animations)) {
                css += this.buildAnimationCSS(name, keyframes);
            }
            
            // Other at-rules
            for (const [rule, styles] of Object.entries(separation.others)) {
                css += this.buildAtRuleCSS(className, rule, styles);
            }
            
            return css;
        }
        
        // Build complete CSS for complex styles
        buildCompleteCSS(className, separation) {
            return this.buildReactiveCSS(className, separation);
        }
        
        // Build pseudo-selector CSS
        buildPseudoCSS(className, selector, styles) {
            const processedSelector = selector.replace('&', `.${className}`);
            return `${processedSelector} {\n${this.styleObjectToCSS(styles)}}\n`;
        }
        
        // Build media query CSS
        buildMediaQueryCSS(className, query, styles) {
            return `${query} {\n  .${className} {\n${this.styleObjectToCSS(styles, '    ')}  }\n}\n`;
        }
        
        // Build container query CSS
        buildContainerQueryCSS(className, query, styles) {
            return `${query} {\n  .${className} {\n${this.styleObjectToCSS(styles, '    ')}  }\n}\n`;
        }
        
        // Build animation CSS
        buildAnimationCSS(name, keyframes) {
            let css = `${name} {\n`;
            for (const [stop, styles] of Object.entries(keyframes)) {
                css += `  ${stop} {\n${this.styleObjectToCSS(styles, '    ')}  }\n`;
            }
            css += '}\n';
            return css;
        }
        
        // Build at-rule CSS
        buildAtRuleCSS(className, rule, styles) {
            if (typeof styles === 'object' && !Array.isArray(styles)) {
                return `${rule} {\n  .${className} {\n${this.styleObjectToCSS(styles, '    ')}  }\n}\n`;
            }
            return `${rule} { ${styles} }\n`;
        }
        
        // Create interactive event handlers
        createInteractiveHandlers(interactiveStates, className) {
            const handlers = {};
            
            for (const [state, styles] of Object.entries(interactiveStates)) {
                switch (state) {
                    case '$hover':
                        handlers.onMouseEnter = (e) => this.applyStyles(e.target, styles);
                        handlers.onMouseLeave = (e) => this.removeStyles(e.target, styles);
                        break;
                    case '$focus':
                        handlers.onFocus = (e) => this.applyStyles(e.target, styles);
                        handlers.onBlur = (e) => this.removeStyles(e.target, styles);
                        break;
                    case '$active':
                        handlers.onMouseDown = (e) => this.applyStyles(e.target, styles);
                        handlers.onMouseUp = (e) => this.removeStyles(e.target, styles);
                        break;
                }
            }
            
            return handlers;
        }
        
        // Apply temporary styles
        applyStyles(element, styles) {
            if (!element._originalStyles) element._originalStyles = {};
            for (const [prop, value] of Object.entries(styles)) {
                if (typeof value !== 'function') {
                    element._originalStyles[prop] = element.style[prop];
                    element.style[this.camelToKebab(prop)] = value;
                }
            }
        }
        
        // Remove temporary styles
        removeStyles(element, styles) {
            if (!element._originalStyles) return;
            for (const prop of Object.keys(styles)) {
                if (typeof styles[prop] !== 'function') {
                    element.style[this.camelToKebab(prop)] = element._originalStyles[prop] || '';
                }
            }
        }
        
        // Generate simple CSS
        generateSimpleCSS(className, styles) {
            return `.${className} {\n${this.styleObjectToCSS(styles)}}\n`;
        }
        
        // Convert style object to CSS string
        styleObjectToCSS(styles, indent = '  ') {
            return Object.entries(styles)
                .filter(([, value]) => typeof value !== 'function' && typeof value !== 'object')
                .map(([prop, value]) => {
                    if (value == null) return '';
                    const stringValue = String(value);
                    return `${indent}${this.camelToKebab(prop)}: ${stringValue};`;
                })
                .filter(Boolean)
                .join('\n') + (Object.keys(styles).length ? '\n' : '');
        }
        
        // Generate class name
        generateClassName(componentName, styles = null) {
            const cleanName = (componentName || 'element').replace(/[^a-zA-Z0-9]/g, '');
            const hash = styles ? this.hashStyles(styles) : ++this.componentCounter;
            return `j-${cleanName}-${hash}`;
        }
        
        // Generate reactive class name
        generateReactiveClassName(componentName) {
            const cleanName = (componentName || 'reactive').replace(/[^a-zA-Z0-9]/g, '');
            return `j-${cleanName}-r${++this.reactiveCounter}`;
        }
        
        // Generate cache key for reactive styles
        generateReactiveCacheKey(result, componentName) {
            const str = JSON.stringify(result, Object.keys(result).sort()) + (componentName || '');
            let hash = 0;
            for (let i = 0; i < str.length; i++) {
                hash = ((hash << 5) - hash + str.charCodeAt(i)) & 0x7fffffff;
            }
            return `reactive-${hash.toString(36)}`;
        }
        
        // Hash styles for class generation
        hashStyles(styles) {
            const str = JSON.stringify(styles, Object.keys(styles).sort());
            let hash = 0;
            for (let i = 0; i < str.length; i++) {
                hash = ((hash << 5) - hash + str.charCodeAt(i)) & 0x7fffffff;
            }
            return hash.toString(36);
        }
        
        // Convert camelCase to kebab-case
        camelToKebab(str) {
            return str.replace(/([A-Z])/g, '-$1').toLowerCase();
        }
        
        // Combine class names
        combineClassNames(...classNames) {
            return classNames.filter(Boolean).join(' ');
        }
        
        // Inject CSS into DOM
        injectCSS(css) {
            if (typeof document === 'undefined') return;
            
            try {
                if (!this.styleSheet || !this.styleSheet.ownerNode) {
                    const style = document.createElement('style');
                    style.setAttribute('data-juris-css-extractor', '');
                    
                    if (!document.head) {
                        if (document.documentElement) {
                            const head = document.createElement('head');
                            document.documentElement.insertBefore(head, document.body);
                        } else {
                            console.warn('Cannot inject CSS: document.head not available');
                            return;
                        }
                    }
                    
                    document.head.appendChild(style);
                    this.styleSheet = style.sheet;
                }
                
                const rules = this.parseIndividualRules(css);
                let hasInsertRuleFailure = false;
                
                for (const rule of rules) {
                    if (rule.trim()) {
                        try {
                            if (this.styleSheet && this.styleSheet.insertRule && !hasInsertRuleFailure) {
                                this.styleSheet.insertRule(rule, this.styleSheet.cssRules.length);
                            } else {
                                hasInsertRuleFailure = true;
                                break;
                            }
                        } catch (ruleError) {
                            console.warn('Failed to insert individual rule:', rule, ruleError);
                            hasInsertRuleFailure = true;
                            break;
                        }
                    }
                }
                
                if (hasInsertRuleFailure) {
                    const styleElement = this.styleSheet?.ownerNode;
                    if (styleElement && typeof styleElement.textContent === 'string') {
                        styleElement.textContent += css + '\n';
                    } else {
                        const newStyle = document.createElement('style');
                        newStyle.textContent = css;
                        newStyle.setAttribute('data-juris-css-extractor-fallback', '');
                        document.head.appendChild(newStyle);
                    }
                }
            } catch (error) {
                console.warn('CSS injection failed:', error);
                try {
                    const emergencyStyle = document.createElement('style');
                    emergencyStyle.textContent = css;
                    emergencyStyle.setAttribute('data-juris-css-extractor-emergency', '');
                    document.head.appendChild(emergencyStyle);
                } catch (emergencyError) {
                    console.error('Emergency CSS injection failed:', emergencyError);
                }
            }
        }
        
        // Parse CSS into individual rules
        parseIndividualRules(css) {
            const rules = [];
            let currentRule = '';
            let braceCount = 0;
            let inAtRule = false;
            let inString = false;
            let stringChar = '';
            
            for (let i = 0; i < css.length; i++) {
                const char = css[i];
                const prevChar = i > 0 ? css[i - 1] : '';
                
                if ((char === '"' || char === "'") && prevChar !== '\\') {
                    if (!inString) {
                        inString = true;
                        stringChar = char;
                    } else if (char === stringChar) {
                        inString = false;
                        stringChar = '';
                    }
                }
                
                if (!inString) {
                    if (char === '@' && braceCount === 0) {
                        if (currentRule.trim()) {
                            rules.push(currentRule.trim());
                            currentRule = '';
                        }
                        inAtRule = true;
                    }
                    
                    if (char === '{') {
                        braceCount++;
                    } else if (char === '}') {
                        braceCount--;
                        
                        if (braceCount === 0) {
                            currentRule += char;
                            if (currentRule.trim()) {
                                rules.push(currentRule.trim());
                            }
                            currentRule = '';
                            inAtRule = false;
                            continue;
                        }
                    }
                }
                
                currentRule += char;
            }
            
            if (currentRule.trim()) {
                rules.push(currentRule.trim());
            }
            
            return rules;
        }
        
        // Clear all caches and styles
        clear() {
            if (this.styleSheet?.ownerNode) {
                this.styleSheet.ownerNode.remove();
                this.styleSheet = null;
            }
            this.cache.clear();
            this.reactiveCache.clear();
            this.componentCounter = 0;
            this.reactiveCounter = 0;
            this.pseudoVarCounter = 0;
        }
        
        // Get statistics
        getStats() {
            return {
                cachedClasses: this.cache.size,
                reactiveCachedClasses: this.reactiveCache.size,
                totalRules: this.styleSheet?.cssRules?.length || 0,
                componentCounter: this.componentCounter,
                reactiveCounter: this.reactiveCounter
            };
        }
    }
    
    if (typeof window !== 'undefined') {
        window.CSSExtractor = CSSExtractor;
        Object.freeze(window.CSSExtractor);
        Object.freeze(window.CSSExtractor.prototype);
    }
    // Basic CommonJS for compatibility
    if (typeof module !== 'undefined' && module.exports) {
        module.exports.CSSExtractor = CSSExtractor;
        module.exports.default = CSSExtractor;
    }
}