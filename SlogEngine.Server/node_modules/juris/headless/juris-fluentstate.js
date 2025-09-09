// headless/juris-fluentstate.js
function createFluentStateHeadless(props, context) {    
    class FluentState {
        constructor(jurisInstance, basePath = '', skipTracking = false) {
            this._juris = jurisInstance;
            this._path = basePath;
            this._cache = new Map();
            this._skipTracking = skipTracking;
            this._subscriptions = new Map();
            this._pendingTriggers = null;
            
            return new Proxy(this, {
                get: this._createGetHandler(),
                set: this._createSetHandler()
            });
        }

        _createGetHandler() {
            return (target, prop) => {
                if (this._isInternalProperty(prop)) {
                    return this._handleInternalProperty(target, prop);
                }
                if (this._isFluentMethod(prop)) {
                    return this._handleFluentMethod(target, prop);
                }
                return this._handleStateAccess(target, prop);
            };
        }

        _createSetHandler() {
            return (target, prop, value) => {
                if (typeof prop === 'string' && !prop.startsWith('_')) {
                    const newPath = target._path ? `${target._path}.${prop}` : prop;
                    const oldValue = this._juris.getState(newPath);
                    const label = target._skipTracking ? '$.x' : '$';
                    this._ensurePathExists(newPath);
                    target._juris.setState(newPath, value);
                    if (!target._juris.stateManager.isBatchingActive()) {
                        this._triggerDirectSubscriptions(newPath, value, oldValue);
                    } else {
                        this._queueSubscriptionTrigger(newPath, value, oldValue);
                    }                    
                    target._cache.clear();
                    return true;
                }
                target[prop] = value;
                return true;
            };
        }
        _ensurePathExists(fullPath) {
            const parts = fullPath.split('.');
            let currentPath = '';
            
            for (let i = 0; i < parts.length - 1; i++) {
                currentPath = currentPath ? `${currentPath}.${parts[i]}` : parts[i];
                const current = this._juris.getState(currentPath);
                if (current === null || current === undefined || typeof current !== 'object') {
                    this._juris.setState(currentPath, {});
                }
            }
        }
        _queueSubscriptionTrigger(path, newValue, oldValue) {
            if (!this._pendingTriggers) {
                this._pendingTriggers = new Map();
                setTimeout(() => {
                    if (this._pendingTriggers && this._pendingTriggers.size > 0) {
                        this._processPendingTriggers();
                    }
                }, 0);
            }
            this._pendingTriggers.set(path, { newValue, oldValue });
        }
        _processPendingTriggers() {
            if (!this._pendingTriggers) return;
            
            const triggers = this._pendingTriggers;
            this._pendingTriggers = null;
            triggers.forEach(({ newValue, oldValue }, path) => {
                this._triggerDirectSubscriptions(path, newValue, oldValue);
            });
        }

        _isInternalProperty(prop) {
            return prop === 'valueOf' || prop === Symbol.toPrimitive || prop === 'toString' ||
                   prop === Symbol.toStringTag || prop === 'constructor';
        }

        _handleInternalProperty(target, prop) {
            if (prop === 'valueOf' || prop === Symbol.toPrimitive) {
                return () => target._juris.getState(target._path, null, !target._skipTracking);
            }
            if (prop === 'toString') {
                return () => String(target._juris.getState(target._path, null, !target._skipTracking) || '');
            }
            if (prop === Symbol.toStringTag) {
                const value = target._juris.getState(target._path, null, false);
                return Array.isArray(value) ? 'Array' : undefined;
            }
        }

        _isFluentMethod(prop) {
            const methods = ['push', 'filterBy', 'getLength', 'removeAt', 'update', 'x', 
                           'subscribe', 'unsubscribe', 'watch', 'onChange', 'raw', 
                           'exists', 'clear', 'getType','getSubscriptionInfo'];
            return typeof prop === 'string' && (
                prop.startsWith('_') || 
                methods.includes(prop)
            );
        }

        _handleFluentMethod(target, prop) {
            if (prop === 'x') {
                return this._createNonReactiveProxy(target);
            }
            if (prop === 'subscribe' || prop === 'watch') {
                return this._createSubscribeMethod(target);
            }
            
            if (prop === 'unsubscribe') {
                return this._createUnsubscribeMethod(target);
            }
            
            if (prop === 'onChange') {
                return this._createOnChangeMethod(target);
            }
            if (prop === 'raw') {
                return () => target._juris.getState(target._path, null, !target._skipTracking);
            }
            
            if (prop === 'exists') {
                return () => {
                    const value = target._juris.getState(target._path);
                    return value !== null && value !== undefined;
                };
            }
            
            if (prop === 'clear') {
                return () => {
                    target._juris.setState(target._path, null);
                    target._cache.clear();
                };
            }
            
            if (prop === 'getType') {
                return () => {
                    const value = target._juris.getState(target._path);
                    if (value === null) return 'null';
                    if (Array.isArray(value)) return 'array';
                    return typeof value;
                };
            }
            
            if (typeof target[prop] === 'function') {
                return target[prop].bind(target);
            }
            
            return target[prop];
        }

        _createNonReactiveProxy(target) {
            if (!target._nonReactiveProxy) {
                const nonReactiveProxy = new FluentState(target._juris, target._path, true);
                nonReactiveProxy._subscriptions = target._subscriptions;
                nonReactiveProxy._pendingTriggers = target._pendingTriggers;
                target._nonReactiveProxy = nonReactiveProxy;
            }
            return target._nonReactiveProxy;
        }
        _createSubscribeMethod(target) {
            return (callback, options = {}) => {
                const subscriptionId = this._generateSubscriptionId();
                const fullPath = target._path || '';
                
                const subscription = {
                    id: subscriptionId,
                    path: fullPath,
                    callback,
                    options: {
                        immediate: options.immediate ?? false,
                        deep: options.deep ?? true,
                        once: options.once ?? false,
                        ...options
                    },
                    active: true
                };
                if (!target._subscriptions.has(fullPath)) {
                    target._subscriptions.set(fullPath, new Map());
                }
                target._subscriptions.get(fullPath).set(subscriptionId, subscription);
                if (subscription.options.immediate) {
                    const currentValue = target._juris.getState(fullPath);
                    try {
                        callback(currentValue, undefined, fullPath);
                    } catch (error) {
                        console.error('Error in immediate subscription callback:', error);
                    }
                }
                return () => this._removeSubscription(target, fullPath, subscriptionId);
            };
        }
        _createUnsubscribeMethod(target) {
            return (subscriptionId) => {
                const fullPath = target._path || '';
                return this._removeSubscription(target, fullPath, subscriptionId);
            };
        }
        _createOnChangeMethod(target) {
            return (callback, options = {}) => {
                return this._createSubscribeMethod(target)(callback, { 
                    immediate: false, 
                    ...options 
                });
            };
        }
        _generateSubscriptionId() {
            return `sub_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
        }
        
        _removeSubscription(target, path, subscriptionId) {
            const pathSubscriptions = target._subscriptions.get(path);
            if (pathSubscriptions && pathSubscriptions.has(subscriptionId)) {
                pathSubscriptions.delete(subscriptionId);
                console.log(`📡 Direct subscription removed: ${path} (${subscriptionId})`);
                if (pathSubscriptions.size === 0) {
                    target._subscriptions.delete(path);
                }
                return true;
            }
            return false;
        }

        _triggerDirectSubscriptions(changedPath, newValue, oldValue) {
            for (const [subscriptionPath, pathSubscriptions] of this._subscriptions) {
                for (const [subscriptionId, subscription] of pathSubscriptions) {
                    if (!subscription.active) continue;
                    
                    const shouldTrigger = this._shouldTriggerSubscription(
                        changedPath, 
                        subscriptionPath, 
                        subscription.options
                    );
                    
                    if (shouldTrigger) {
                        try {
                            const relevantValue = subscriptionPath === changedPath 
                                ? newValue 
                                : this._juris.getState(subscriptionPath);
                                
                            const relevantOldValue = subscriptionPath === changedPath 
                                ? oldValue 
                                : undefined;
                                
                            subscription.callback(relevantValue, relevantOldValue, changedPath);

                            if (subscription.options.once) {
                                this._removeSubscription(this, subscriptionPath, subscriptionId);
                            }
                        } catch (error) {
                            console.error(`Error in direct subscription callback for ${subscriptionPath}:`, error);
                        }
                    }
                }
            }
        }
        _shouldTriggerSubscription(changedPath, subscriptionPath, options) {
            if (changedPath === subscriptionPath) {
                return true;
            }
            if (options.deep && changedPath.startsWith(subscriptionPath + '.')) {
                return true;
            }            
            if (subscriptionPath.startsWith(changedPath + '.')) {
                return true;
            }
            
            return false;
        }
        _handleStateAccess(target, prop) {
            const newPath = target._path ? `${target._path}.${prop}` : String(prop);
            const value = target._juris.getState(newPath, null, !target._skipTracking);
            
            const label = target._skipTracking ? '$.x' : '$';
            const isTracking = !target._skipTracking && !!target._juris.stateManager.currentTracking;
            console.log(`🔍 ${label} accessing: ${newPath} =`, value, `(tracking: ${isTracking})`);
            if (value !== undefined && (typeof value !== 'object' || value === null)) {
                return value;
            }
            if (value === undefined) {
                return null;
            }
            return this._getOrCreateProxy(target, newPath, value);
        }
        
        _checkPathExists(path) {
            if (!path) return true;
            
            const parts = path.split('.');
            let current = this._juris.stateManager.state;
            
            for (let i = 0; i < parts.length; i++) {
                if (current === null || current === undefined || typeof current !== 'object') {
                    return false;
                }

                if (!current.hasOwnProperty(parts[i])) {
                    return false;
                }
                current = current[parts[i]];
            }
            
            return true;
        }

        _pathExists(path) {
            const parts = path.split('.');
            let current = this._juris.stateManager.state;
            
            for (const part of parts) {
                if (!current || !current.hasOwnProperty(part)) {
                    return false;
                }
                current = current[part];
            }
            
            return true;
        }

        _getOrCreateProxy(target, newPath, value) {
            const cacheKey = target._skipTracking ? `${newPath}_notrack` : newPath;
            
            if (target._cache.has(cacheKey)) {
                const cached = target._cache.get(cacheKey);
                if (cached.isProxy) return cached.proxy;
            }
            
            const newProxy = new FluentState(target._juris, newPath, target._skipTracking);
            newProxy._subscriptions = target._subscriptions;
            newProxy._pendingTriggers = target._pendingTriggers;
            
            target._cache.set(cacheKey, { proxy: newProxy, isProxy: true });
            return newProxy;
        }

        push(item) {
            const array = this._juris.getState(this._path, null, !this._skipTracking);
            
            if (!Array.isArray(array)) {
                console.warn(`Cannot push to non-array at ${this._path}`);

                if (array == null) {
                    const newArray = [item];
                    this._juris.setState(this._path, newArray);
                    this._triggerDirectSubscriptions(this._path, newArray, array);
                    this._cache.clear();
                    return;
                }
                return;
            }
            
            const oldValue = [...array];
            const newArray = [...array, item];
            
            this._juris.setState(this._path, newArray);
            
            if (!this._juris.stateManager.isBatchingActive()) {
                this._triggerDirectSubscriptions(this._path, newArray, oldValue);
            } else {
                this._queueSubscriptionTrigger(this._path, newArray, oldValue);
            }
            
            this._cache.clear();
        }

        filterBy(predicate) {
            const array = this._juris.getState(this._path, null, !this._skipTracking);
            if (!Array.isArray(array)) {
                console.warn(`Cannot filter non-array at ${this._path}`);
                return [];
            }
            return array.filter(predicate);
        }

        getLength() {
            const array = this._juris.getState(this._path, null, !this._skipTracking);
            if (!Array.isArray(array)) {
                console.warn(`Cannot get length of non-array at ${this._path}`);
                return 0;
            }
            return array.length;
        }

        removeAt(index) {
            const array = this._juris.getState(this._path, null, !this._skipTracking);
            if (!Array.isArray(array)) {
                console.warn(`Cannot removeAt on non-array at ${this._path}`);
                return;
            }
            
            const oldValue = [...array];
            const filtered = array.filter((_, i) => i !== index);
            
            this._juris.setState(this._path, filtered);
            
            if (!this._juris.stateManager.isBatchingActive()) {
                this._triggerDirectSubscriptions(this._path, filtered, oldValue);
            } else {
                this._queueSubscriptionTrigger(this._path, filtered, oldValue);
            }
            
            this._cache.clear();
        }

        update(updates) {
            const current = this._juris.getState(this._path, null, !this._skipTracking);

            const oldValue = current && typeof current === 'object' ? { ...current } : null;
            const baseObject = current && typeof current === 'object' ? current : {};
            const newValue = { ...baseObject, ...updates };
            
            this._juris.setState(this._path, newValue);
            
            if (!this._juris.stateManager.isBatchingActive()) {
                this._triggerDirectSubscriptions(this._path, newValue, oldValue);
            } else {
                this._queueSubscriptionTrigger(this._path, newValue, oldValue);
            }
            
            this._cache.clear();
        }


        getSubscriptionInfo() {
            const info = {};
            for (const [path, pathSubs] of this._subscriptions) {
                info[path] = Array.from(pathSubs.values()).map(sub => ({
                    id: sub.id,
                    options: sub.options,
                    active: sub.active
                }));
            }
            return info;
        }
    }


    const reactiveProxy = new FluentState(context.juris);

    return {
        api: {
        
            getFluentStates: () => {
                return reactiveProxy;
            },

            createProxy: (basePath = '', skipTracking = false) => {
                const proxy = new FluentState(context.juris, basePath, skipTracking);

                proxy._subscriptions = reactiveProxy._subscriptions;
                return proxy;
            },
            
            batch: (callback) => context.executeBatch(callback),

            debug: {
                getStats: () => ({
                    reactiveCache: reactiveProxy._cache.size,
                    hasNonReactive: !!reactiveProxy._nonReactiveProxy,
                    subscriptions: reactiveProxy._subscriptions.size,
                    totalSubscriptions: Array.from(reactiveProxy._subscriptions.values())
                        .reduce((total, pathSubs) => total + pathSubs.size, 0),
                    pendingTriggers: reactiveProxy._pendingTriggers ? reactiveProxy._pendingTriggers.size : 0
                }),
                
                getSubscriptions: () => reactiveProxy.getSubscriptionInfo(),
                
                clearCache: () => {
                    reactiveProxy._cache.clear();
                    console.log('🧹 FluentState caches cleared');
                },
                
                clearSubscriptions: () => {
                    reactiveProxy._subscriptions.clear();
                    console.log('🧹 Direct subscriptions cleared');
                }
            }
        },

        hooks: {
            onRegister: () => {
                console.log('✅ Enhanced FluentState with deep paths registered');
                if (typeof window !== 'undefined') {
                    window.$ = reactiveProxy;
                    console.log('🌍 Enhanced $ exposed globally');
                }
            },
            
            onUnregister: () => {
                console.log('🧹 Enhanced FluentState cleaning up');
                if (typeof window !== 'undefined' && window.$ === reactiveProxy) {
                    delete window.$;
                }
                reactiveProxy._cache.clear();
                reactiveProxy._subscriptions.clear();
            }
        }
    };
}

if (typeof window !== 'undefined') {
    window.createFluentStateHeadless = createFluentStateHeadless;
    Object.freeze(window.createFluentStateHeadless);
    Object.freeze(window.createFluentStateHeadless.prototype);
}
if (typeof module !== 'undefined' && module.exports) {
    module.exports.createFluentStateHeadless = createFluentStateHeadless;
    module.exports.default = createFluentStateHeadless;
}