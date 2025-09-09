// juris-template.js - Standalone Template Compiler Feature
if (typeof TemplateCompiler === 'undefined') {
    class TemplateCompiler {
        parseTemplate(template) {
            const name = template.getAttribute('data-component');
            const contextConfig = template.getAttribute('data-context');
            const content = template.content;
            const script = content.querySelector('script')?.textContent.trim() || '';
            const div = document.createElement('div');
            div.appendChild(content.cloneNode(true));
            div.querySelector('script')?.remove();
            const html = div.innerHTML.trim();
            return { name, script, html, contextConfig };
        }

        htmlToObject(html) {
            const div = document.createElement('div');
            div.innerHTML = html;
            return this.convertElement(div.firstElementChild);
        }

        convertElement(element) {
            const obj = {};
            const tag = element.tagName.toLowerCase();
            obj[tag] = {};
            for (const attr of element.attributes) {
                let value = attr.value;
                if (value.startsWith('{') && value.endsWith('}')) {
                    const expr = value.slice(1, -1);
                    obj[tag][attr.name] = { __FUNCTION__: expr };
                } else {
                    obj[tag][attr.name] = value;
                }
            }
            const children = Array.from(element.childNodes);
            const processedChildren = children
                .map(child => this.convertNode(child))
                .filter(child => child !== null);
            if (processedChildren.length === 1) {
                const child = processedChildren[0];
                if (child && child.__REACTIVE_CHILDREN__) {
                    obj[tag].children = { __FUNCTION__: child.__REACTIVE_CHILDREN__ };
                } else if (child && child.__REACTIVE_TEXT__) {
                    obj[tag].text = { __FUNCTION__: child.__REACTIVE_TEXT__ };
                } else if (typeof child === 'string') {
                    obj[tag].text = child;
                } else if (child) {
                    obj[tag].children = [child];
                }
            } else if (processedChildren.length > 0) {
                obj[tag].children = processedChildren;
            }
            return obj;
        }

        convertNode(node) {
            if (node.nodeType === Node.TEXT_NODE) {
                const text = node.textContent.trim();
                if (!text) return null;
                const childrenMatch = text.match(/^\{children:(.+)\}$/s);
                if (childrenMatch) {
                    return { __REACTIVE_CHILDREN__: childrenMatch[1] };
                }
                const textMatch = text.match(/^\{text:(.+)\}$/s);
                if (textMatch) {
                    return { __REACTIVE_TEXT__: textMatch[1] };
                }
                const expressionMatch = text.match(/^\{(.+)\}$/s);
                if (expressionMatch) {
                    return { __REACTIVE_TEXT__: expressionMatch[1] };
                }
                return text;
            }
            if (node.nodeType === Node.ELEMENT_NODE) {
                return this.convertElement(node);
            }
            return null;
        }

        generateContextDestructuring(contextConfig) {
            if (!contextConfig) return '';
            const contextVars = contextConfig.split(',').map(v => v.trim());
            return `const { ${contextVars.join(', ')} } = context;`;
        }

        generateComponent(parsed) {
            const objStr = this.objectToString(this.htmlToObject(parsed.html));
            const contextDestructuring = this.generateContextDestructuring(parsed.contextConfig);
            const combinedScript = contextDestructuring ?
                `${contextDestructuring}\n${parsed.script}` :
                parsed.script;

            return `(props, context) => {
${combinedScript}
return ${objStr};
}`;
        }
        compileTemplates(templates) {
            const components = {};
            templates.forEach(template => {
                const parsed = this.parseTemplate(template);
                const componentCode = this.generateComponent(parsed);
                components[parsed.name] = eval(`(${componentCode})`);
            });
            return components;
        }
        objectToString(obj, indent = 0) {
            const spaces = '  '.repeat(indent);
            if (obj && obj.__FUNCTION__) {
                return obj.__FUNCTION__;
            }
            if (typeof obj === 'string') {
                return `'${obj.replace(/'/g, "\\'")}'`;
            }
            if (typeof obj === 'number' || typeof obj === 'boolean') {
                return String(obj);
            }
            if (Array.isArray(obj)) {
                if (obj.length === 0) return '[]';
                const items = obj.map(item =>
                    spaces + '  ' + this.objectToString(item, indent + 1)
                ).join(',\n');
                return '[\n' + items + '\n' + spaces + ']';
            }
            if (typeof obj === 'object' && obj !== null) {
                const keys = Object.keys(obj);
                if (keys.length === 0) return '{}';
                const pairs = keys.map(key => {
                    const value = this.objectToString(obj[key], indent + 1);
                    const keyStr = /^[a-zA-Z_$][a-zA-Z0-9_$]*$/.test(key) ? key : `'${key}'`;
                    return spaces + '  ' + keyStr + ': ' + value;
                }).join(',\n');

                return '{\n' + pairs + '\n' + spaces + '}';
            }
            return 'null';
        }
    }

    // Register feature automatically
    if (typeof window !== 'undefined') {
        window.TemplateCompiler = TemplateCompiler;
        Object.freeze(window.TemplateCompiler);
        Object.freeze(window.TemplateCompiler.prototype);
    }
    if (typeof module !== 'undefined' && module.exports) {
        module.exports.TemplateCompiler = TemplateCompiler;
        module.exports.default = TemplateCompiler;
    }
}