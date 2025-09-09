# Juris (Browser Package)

**JavaScript Unified Reactive Interface Solution - Browser Optimized**

The First and Only Non-blocking Reactive Platform, Architecturally Optimized for Next Generation Cutting-Edge Cross-Platform Application. This browser-optimized version delivers the full framework through instant deployment - no build steps, no complexity, just pure cutting-edge reactive architecture.

## Features

- **Object-First Architecture**: Express interfaces as pure JavaScript objects
- **Temporal Independence**: Component and State Independence
- **True Progressive Enhancement**: Enhance HTMLs without replacing them
- **Intentional Reactivity**: Functions explicitly define reactive behavior
- **Universal Deployment**: Works across all JavaScript environments
- **Enhanced Web Components**: Penetrates Web Component Shadow DOM with fine-grained reactivity
- **AI Collaboration Ready**: Designed for seamless AI integration
- **Native JavaScript Patterns**: Maintains simplicity and debuggability
- **Precise Control**: Fine-grained control over reactive behaviors
- **Automatic Async Support**: Built-in support for asynchronous operations all over the place with customizable placeholders
- **Progressive Enhancement done Right**: Progressively enhance your applications with reactive features without editing your HTML
- **SVG Support**: Reactive SVG
- **Async Loading Indicator**
- **Template Compilation**: Compiles your template at runtime to pure functional component
- **TypeSafe**: IntelliSence and auto-suggestion that covers HTML and CSS Semantics and Components
- **Reactive Anonymous Functions**: Use anonymous reactive functions for children, components, and layout rendering
- **CSS-in-JS Extraction**: Automatic CSS extraction with reactive style preservation, pseudo-classes and pseudo-elements support
- **ARM**: Avdance Reactive Management for managing all global events with context and state access.

## 🌐 Browser-First Architecture

This package delivers the complete Juris framework optimized for:
- **Instant Enterprise Deployment** - Production-ready applications in seconds
- **Zero-Build Pipeline** - Revolutionary architecture without compilation complexity
- **Universal CDN Distribution** - Lightning-fast global deployment
- **Progressive Enhancement Excellence** - Transform existing HTML into reactive masterpieces
- **AI-Collaboration Ready** - Seamless integration with modern AI development workflows
- **Legacy-Future Bridge** - Advanced reactive patterns that work everywhere
- **Extreamly Complex UI/UX** - Powerful patterns without complexity

## Installation

### CDN (Instant Deployment)

```html
<!-- Latest cutting-edge version, not for production -->
<script src="https://unpkg.com/juris@latest/juris.js"></script>

<!-- Specific version, Pre-Production -->
<script src="https://unpkg.com/juris@0.9.0/juris.js"></script>
<script src="https://unpkg.com/juris@0.9.0/juris.mini.js"></script>
<!-- JSDeliver version -->
<script src="https://cdn.jsdelivr.net/npm/juris@0.9.0/juris.js"></script>
<script src="https://cdn.jsdelivr.net/npm/juris@0.9.0/juris.mini.js"></script>

```

### Optional Features
```html
<!-- Optional CSS Extractor -->
<script src="https://unpkg.com/juris@0.9.0/juris-cssextractor.js"></script>
<!-- Optional Enhance for progressive enhancement -->
<script src="https://unpkg.com/juris@0.9.0/juris-enhance.js"></script>
<!-- Optional Headless Component for stateful services -->
<script src="https://unpkg.com/juris@0.9.0/juris-headless.js"></script>
<!-- Optional Template for template compilation -->
<script src="https://unpkg.com/juris@0.9.0/juris-template.js"></script>
<!-- Optional Web Component support -->
<script src="https://unpkg.com/juris@0.9.0/juris-webcomponent.js"></script>
<!-- Optional Fluent State for advanced state management -->
<script src="https://unpkg.com/juris@0.9.0/headless/juris-fluentstate.js"></script>
<!-- Optional Router for advanced routing capabilities -->
<script src="https://unpkg.com/juris@0.9.0/headless/juris-router.js"></script>

<!--JSDeliver version -->
<!-- Optional CSS Extractor -->
<script src="https://cdn.jsdelivr.net/npm/juris@0.9.0/juris-cssextractor.js"></script>
<!-- Optional Enhance for progressive enhancement -->
<script src="https://cdn.jsdelivr.net/npm/juris@0.9.0/juris-enhance.js"></script>
<!-- Optional Headless Component for stateful services -->
<script src="https://cdn.jsdelivr.net/npm/juris@0.9.0/juris-headless.js"></script>
<!-- Optional Template for template compilation -->
<script src="https://cdn.jsdelivr.net/npm/juris@0.9.0/juris-template.js"></script>
<!-- Optional Web Component support -->
<script src="https://cdn.jsdelivr.net/npm/juris@0.9.0/juris-webcomponent.js"></script>
<!-- Optional Fluent State for advanced state management -->
<script src="https://cdn.jsdelivr.net/npm/juris@0.9.0/headless/juris-fluentstate.js"></script>
<!-- Optional Router for advanced routing capabilities -->
<script src="https://cdn.jsdelivr.net/npm/juris@0.9.0/headless/juris-router.js"></script>
```

### NPM Installation

```bash
npm install juris
# or
npm install juris@0.9.0
```
### Vite App Usage
```javascript
import { HeadlessManager } from 'juris/juris-headless'
import { createFluentStateHeadless } from 'juris/headless/juris-fluentstate';
import { Router } from 'juris/headless/juris-router';
import { DOMEnhancer } from 'juris/juris-enhance';
import { WebComponentFactory } from 'juris/juris-webcomponent';
import { Juris } from 'juris/juris';

//your components
import { Counter } from './counter'
```

### Feature Activation
To activate optional features, simply include the corresponding script tags in your HTML. Each feature is modular and can be used independently based on your application's needs.

```javascript
 const juris = new Juris({
	logLevel:'warn',// Set log level to warn in production
	// Set render mode to 'fine-grained' or 'batched'
	renderMode:'fine-grained',
	states: {
		todos: [],
		counter: 0,
		user: null,
		ui: {
			loading: false,
			error: null
		}
	},
	services:{
		apiClient: new ApiClient()
	},
	layout:{AppPage:{}}
	//optional features can be activated by passing them in the options
	 features:{
		cssExtractor: CSSExtractor, // Enable CSS extraction
		enhance: DOMEnhancer, // Enable progressive enhancement
		headless: Headless, // Enable stateful services
		template: Template, // Enable template compilation
		webComponent: WebComponentFactory // Enable enhanced web components
	 },
		headlessComponents: {
			fluentState: { //fluent state plugin
				fn: createFluentStateHeadless,
				options: { autoInit: true }
			},
			router: {//router plugin
				fn: Router,
				options: {
					autoInit: true,
					config: {
						debug: true,
						mode: 'hash',
						queryStateSync: {
							enabled: true,
							stateBasePath: '__state',
							debounceMs: 150,
							parseTypes: true,
							encodeArrays: true,
							excludeEmpty: true,
							includeInHistory: true
						},
						routes: {
							'/': { name: 'Home' },
							'/products': { name: 'Products' },
							'/profile': { name: 'Profile' },
							'/404': { name: 'Not Found' }
						},
						defaultRoute: '/',
						notFoundRoute: '/404'
					}
				}
			}
		},
	 ... // other options
 });
```

## Documentation

- [Complete Framework Documentation](https://jurisjs.com)
- [GitHub Repository](https://github.com/jurisjs/juris)
- [Interactive Examples](https://codepen.io/jurisauthor)
- [Online Testing Platform](https://jurisjs.com/tests/juris_pure_test_interface.html)