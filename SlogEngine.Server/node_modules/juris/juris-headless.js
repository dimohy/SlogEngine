// juris-headless.js - Headless Manager
if (typeof HeadlessManager === 'undefined') {
	// Headless Manager
	class HeadlessManager {
		constructor(juris, logger) {
			this.log = logger || {
				i: (msg, ctx, cat) => console.info(`[${cat || 'headless'}] ${msg}`, ctx || ''),
				d: (msg, ctx, cat) => console.debug(`[${cat || 'headless'}] ${msg}`, ctx || ''),
				e: (msg, ctx, cat) => console.error(`[${cat || 'headless'}] ${msg}`, ctx || ''),
				w: (msg, ctx, cat) => console.warn(`[${cat || 'headless'}] ${msg}`, ctx || ''),
				ei:true,ed:true,el:true,ew:true,ee:true
			};
			this.log.ei &&console.info(this.log.i('HeadlessManager initialized', {}, 'framework'));
			this.juris = juris;
			this.components = new Map();
			this.instances = new Map();
			this.context = {};
			this.initQueue = new Set();
			this.lifecycleHooks = new Map();
		}

		register(name, componentFn, options = {}) {
			this.log.ei && console.info(this.log.i('Headless component registered', { name, hasOptions: Object.keys(options).length > 0 }, 'framework'));
			this.components.set(name, { fn: componentFn, options });
			if (options.autoInit) this.initQueue.add(name);
		}

		initialize(name, props = {}) {
			this.log.ed && console.debug(this.log.d('Initializing headless component', { name, propsKeys: Object.keys(props) }, 'framework'));
			const component = this.components.get(name);
			if (!component) {
				this.log.ee && console.error(this.log.e('Headless component not found', { name }, 'framework'));
				return null;
			}
			try {
				const context = this.juris.createHeadlessContext();
				const instance = component.fn(props, context);
				if (!instance || typeof instance !== 'object') {
					this.log.ee && console.error(this.log.e('Invalid headless component instance', { name }, 'framework'));
					return null;
				}
				this.log.ei && console.info(this.log.i('Headless component initialized', { name, hasAPI: !!instance.api, hasHooks: !!instance.hooks }, 'framework'));
				this.instances.set(name, instance);
				if (instance.hooks) this.lifecycleHooks.set(name, instance.hooks);
				if (instance.api) {
					this.context[name] = instance.api;
					if (!this.juris.headlessAPIs) this.juris.headlessAPIs = {};
					this.juris.headlessAPIs[name] = instance.api;
				}
				instance.hooks?.onRegister?.();
				return instance;
			} catch (error) {
				this.log.ee && console.error(this.log.e('Headless component initialization failed', { name, error: error.message }, 'framework'));
				return null;
			}
		}

		initializeQueued() {
			this.initQueue.forEach(name => {
				if (!this.instances.has(name)) {
					const component = this.components.get(name);
					this.initialize(name, component.options || {});
				}
			});
			this.initQueue.clear();
		}

		getInstance(name) { return this.instances.get(name); }
		getAPI(name) { return this.context[name]; }
		getAllAPIs() { return { ...this.context }; }

		reinitialize(name, props = {}) {
			const instance = this.instances.get(name);
			if (instance?.hooks?.onUnregister) {
					try { instance.hooks.onUnregister(); } catch (error) { console.error(this.log.e(`Error in onUnregister for '${name}':`, error), 'framework'); }
			}
			if (this.context[name]) delete this.context[name];
			if (this.juris.headlessAPIs?.[name]) delete this.juris.headlessAPIs[name];
			this.instances.delete(name);
			this.lifecycleHooks.delete(name);
			return this.initialize(name, props);
		}

		cleanup() {
			console.info(this.log.i('Cleaning up headless components', { instanceCount: this.instances.size }, 'framework'));
			this.instances.forEach((instance, name) => {
					if (instance.hooks?.onUnregister) {
							try { instance.hooks.onUnregister(); } catch (error) { console.error(this.log.e(`Error in onUnregister for '${name}':`, error), 'framework'); }
					}
			});
			this.instances.clear();
			this.context = {};
			this.lifecycleHooks.clear();
			if (this.juris.headlessAPIs) this.juris.headlessAPIs = {};
		}

		getStatus() {
			return {
					registered: Array.from(this.components.keys()),
					initialized: Array.from(this.instances.keys()),
					queued: Array.from(this.initQueue),
					apis: Object.keys(this.context)
			};
		}
	}

	// Register feature automatically
	if (typeof window !== 'undefined') {
		window.HeadlessManager = HeadlessManager
		Object.freeze(window.HeadlessManager);
		Object.freeze(window.HeadlessManager.prototype);
	}
	// Basic CommonJS for compatibility
	if (typeof module !== 'undefined' && module.exports) {
		module.exports.HeadlessManager = HeadlessManager;
		module.exports.default = HeadlessManager;
	}
}