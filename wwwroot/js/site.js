// Enhanced DevTools Suite - Main JavaScript File
class DevToolsSuite {
    constructor() {
        this.themeManager = null;
        this.eventListeners = [];
        this.intervals = [];
        this.timeouts = [];
        this.init();
    }

    init() {
        try {
            this.setupThemeManager();
            this.setupEventListeners();
            this.setupAnimations();
            this.setupServiceWorker();
            this.setupPerformanceMonitoring();
            this.setupCollaboration();
        } catch (error) {
            console.error('DevToolsSuite initialization failed:', error);
        }
    }

    // Enhanced Theme Management
    setupThemeManager() {
        this.themeManager = {
            currentTheme: localStorage.getItem('devtools-theme') || 'dark',

            init() {
                this.applyTheme(this.currentTheme);
                this.bindEvents();
            },

            applyTheme(theme) {
                try {
                    document.documentElement.setAttribute('data-bs-theme', theme);
                    localStorage.setItem('devtools-theme', theme);
                    this.currentTheme = theme;

                    // Update theme switch
                    const themeSwitch = document.getElementById('themeSwitch');
                    if (themeSwitch) {
                        const icon = themeSwitch.querySelector('i');
                        if (icon) {
                            icon.className = theme === 'dark' ? 'fas fa-sun' : 'fas fa-moon';
                        }
                        themeSwitch.setAttribute('aria-label', `Switch to ${theme === 'dark' ? 'light' : 'dark'} mode`);
                    }

                    // Dispatch theme change event
                    document.dispatchEvent(new CustomEvent('themeChanged', { detail: { theme } }));
                } catch (error) {
                    console.error('Theme application failed:', error);
                }
            },

            toggleTheme() {
                const newTheme = this.currentTheme === 'dark' ? 'light' : 'dark';
                this.applyTheme(newTheme);
            },

            bindEvents() {
                const themeSwitch = document.getElementById('themeSwitch');
                if (themeSwitch) {
                    const handler = () => this.toggleTheme();
                    themeSwitch.addEventListener('click', handler);
                    window.devToolsSuite?.eventListeners.push({ element: themeSwitch, type: 'click', handler });
                }

                if (window.matchMedia) {
                    const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
                    const handleChange = (e) => {
                        if (!localStorage.getItem('devtools-theme')) {
                            this.applyTheme(e.matches ? 'dark' : 'light');
                        }
                    };

                    mediaQuery.addEventListener('change', handleChange);
                    window.devToolsSuite?.eventListeners.push({ element: mediaQuery, type: 'change', handler: handleChange });

                    if (!localStorage.getItem('devtools-theme')) {
                        this.applyTheme(mediaQuery.matches ? 'dark' : 'light');
                    }
                }
            }
        };

        this.themeManager.init();
    }

    // Enhanced Utility Functions - COMPLETE ORIGINAL VERSION
    static utils = {
        // Enhanced JSON formatting with error recovery
        formatJSON: function (jsonString, options = {}) {
            try {
                if (!jsonString || jsonString.trim() === '') {
                    return {
                        success: false,
                        error: 'Empty JSON string'
                    };
                }

                const parsed = JSON.parse(jsonString);
                const indent = options.indent || 2;

                let formatted = JSON.stringify(parsed, null, indent);

                if (options.prettify) {
                    formatted = formatted.replace(/\n\s+/g, '\n');
                }

                return {
                    success: true,
                    data: formatted,
                    size: new Blob([formatted]).size,
                    lines: formatted.split('\n').length
                };
            } catch (error) {
                const fixed = this.tryFixJSON(jsonString);
                if (fixed) {
                    return this.formatJSON(fixed, options);
                }

                return {
                    success: false,
                    error: error.message,
                    position: this.findJSONErrorPosition(jsonString, error),
                    suggestions: this.getJSONErrorSuggestions(error)
                };
            }
        },

        tryFixJSON: function (jsonString) {
            if (!jsonString || typeof jsonString !== 'string') return null;

            let fixed = jsonString
                .replace(/,\s*([}\]])/g, '$1')
                .replace(/([{,]\s*)([a-zA-Z_$][a-zA-Z0-9_$]*)(\s*:)/g, '$1"$2"$3')
                .replace(/'([^']*)'/g, '"$1"')
                .replace(/\/\/.*$/gm, '')
                .replace(/\/\*[\s\S]*?\*\//g, '');

            try {
                JSON.parse(fixed);
                return fixed;
            } catch {
                return null;
            }
        },

        findJSONErrorPosition: function (jsonString, error) {
            const match = error.message.match(/position\s+(\d+)/);
            return match ? parseInt(match[1]) : null;
        },

        getJSONErrorSuggestions: function (error) {
            const suggestions = [];

            if (error.message.includes('Unexpected token')) {
                suggestions.push('Check for missing commas or brackets');
                suggestions.push('Verify all quotes are properly closed');
            }

            if (error.message.includes('Unexpected end')) {
                suggestions.push('Check for missing closing brackets or braces');
            }

            return suggestions;
        },

        // Enhanced Base64 with file support
        base64Encode: function (data) {
            if (data instanceof File) {
                return new Promise((resolve, reject) => {
                    const reader = new FileReader();
                    reader.onload = () => resolve(reader.result.split(',')[1]);
                    reader.onerror = () => reject(new Error('File reading failed'));
                    reader.readAsDataURL(data);
                });
            }

            if (typeof data !== 'string') {
                data = String(data);
            }

            try {
                return btoa(unescape(encodeURIComponent(data)));
            } catch (error) {
                throw new Error('Base64 encoding failed: ' + error.message);
            }
        },

        base64Decode: function (encoded, outputType = 'string') {
            if (typeof encoded !== 'string') {
                throw new Error('Input must be a string');
            }

            try {
                if (encoded.includes(',')) {
                    encoded = encoded.split(',')[1];
                }

                const decoded = decodeURIComponent(escape(atob(encoded)));

                if (outputType === 'blob') {
                    return new Blob([decoded]);
                } else if (outputType === 'uint8array') {
                    return new TextEncoder().encode(decoded);
                }

                return decoded;
            } catch (error) {
                throw new Error('Invalid Base64 string: ' + error.message);
            }
        },

        // Enhanced JWT decoding with validation
        decodeJWT: function (token) {
            if (typeof token !== 'string') {
                throw new Error('Token must be a string');
            }

            try {
                const parts = token.split('.');
                if (parts.length !== 3) {
                    throw new Error('Invalid JWT format: Expected 3 parts');
                }

                const header = JSON.parse(this.base64UrlDecode(parts[0]));
                const payload = JSON.parse(this.base64UrlDecode(parts[1]));
                const signature = parts[2];

                this.validateJWT(header, payload);

                return {
                    header,
                    payload,
                    signature,
                    isValid: this.verifyJWTExpiration(payload),
                    isExpired: this.isJWTExpired(payload)
                };
            } catch (error) {
                throw new Error(`JWT decoding failed: ${error.message}`);
            }
        },

        base64UrlDecode: function (str) {
            str = str.replace(/-/g, '+').replace(/_/g, '/');
            while (str.length % 4) {
                str += '=';
            }
            return this.base64Decode(str);
        },

        validateJWT: function (header, payload) {
            if (!header.alg || !header.typ) {
                throw new Error('Missing required JWT header fields');
            }

            if (header.typ.toUpperCase() !== 'JWT') {
                throw new Error('Invalid JWT type');
            }
        },

        verifyJWTExpiration: function (payload) {
            const now = Math.floor(Date.now() / 1000);

            if (payload.exp && payload.exp < now) {
                return false;
            }

            if (payload.nbf && payload.nbf > now) {
                return false;
            }

            return true;
        },

        isJWTExpired: function (payload) {
            const now = Math.floor(Date.now() / 1000);
            return payload.exp && payload.exp < now;
        },

        // Enhanced UUID generation with options
        generateUUID: function (options = {}) {
            const version = options.version || 4;
            const format = options.format || 'standard';

            let uuid;
            switch (version) {
                case 1:
                    const time = Date.now();
                    uuid = 'xxxxxxxx-xxxx-1xxx-yxxx-xxxxxxxxxxxx';
                    break;
                case 4:
                default:
                    uuid = 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx';
                    break;
            }

            const result = uuid.replace(/[xy]/g, function (c) {
                const r = Math.random() * 16 | 0;
                const v = c == 'x' ? r : (r & 0x3 | 0x8);
                return v.toString(16);
            });

            return format === 'standard' ? result : result.replace(/-/g, '');
        },

        // Enhanced clipboard with fallback
        copyToClipboard: async function (text) {
            if (typeof text !== 'string') {
                text = String(text);
            }

            try {
                if (navigator.clipboard && window.isSecureContext) {
                    await navigator.clipboard.writeText(text);
                    return { success: true };
                } else {
                    return this.fallbackCopyToClipboard(text);
                }
            } catch (error) {
                return this.fallbackCopyToClipboard(text);
            }
        },

        fallbackCopyToClipboard: function (text) {
            return new Promise((resolve) => {
                const textArea = document.createElement('textarea');
                textArea.value = text;
                textArea.style.position = 'fixed';
                textArea.style.opacity = '0';
                textArea.style.left = '-999999px';
                textArea.style.top = '-999999px';
                document.body.appendChild(textArea);
                textArea.focus();
                textArea.select();

                try {
                    const successful = document.execCommand('copy');
                    document.body.removeChild(textArea);
                    resolve({ success: successful });
                } catch (error) {
                    document.body.removeChild(textArea);
                    resolve({ success: false, error: error.message });
                }
            });
        },

        // Enhanced file operations
        downloadText: function (text, filename, options = {}) {
            if (typeof text !== 'string') {
                text = String(text);
            }

            const blob = new Blob([text], {
                type: options.contentType || 'text/plain;charset=utf-8'
            });

            if (options.autoDownload !== false) {
                const url = URL.createObjectURL(blob);
                const a = document.createElement('a');
                a.href = url;
                a.download = filename || 'download.txt';
                a.style.display = 'none';
                document.body.appendChild(a);
                a.click();

                setTimeout(() => {
                    document.body.removeChild(a);
                    URL.revokeObjectURL(url);
                }, 100);
            }

            return blob;
        },

        // Enhanced validation
        validateEmail: function (email) {
            if (typeof email !== 'string') return false;
            const re = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
            return re.test(email.trim());
        },

        validateURL: function (url) {
            if (typeof url !== 'string') return false;

            try {
                const urlObj = new URL(url);
                return urlObj.protocol === 'http:' || urlObj.protocol === 'https:';
            } catch {
                return false;
            }
        },

        // Enhanced performance utilities
        debounce: function (func, wait, immediate = false) {
            let timeout;
            return function executedFunction(...args) {
                const context = this;
                const later = () => {
                    timeout = null;
                    if (!immediate) func.apply(context, args);
                };
                const callNow = immediate && !timeout;
                clearTimeout(timeout);
                timeout = setTimeout(later, wait);
                if (callNow) func.apply(context, args);
            };
        },

        throttle: function (func, limit) {
            let inThrottle;
            return function (...args) {
                const context = this;
                if (!inThrottle) {
                    func.apply(context, args);
                    inThrottle = true;
                    setTimeout(() => inThrottle = false, limit);
                }
            };
        },

        // File size formatting with precision
        formatFileSize: function (bytes, decimals = 2) {
            if (typeof bytes !== 'number' || bytes < 0) {
                return '0 Bytes';
            }

            if (bytes === 0) return '0 Bytes';

            const k = 1024;
            const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB'];
            const i = Math.floor(Math.log(bytes) / Math.log(k));

            return parseFloat((bytes / Math.pow(k, i)).toFixed(decimals)) + ' ' + sizes[i];
        },

        // Security utilities
        sanitizeHTML: function (html) {
            if (typeof html !== 'string') return '';

            const template = document.createElement('div');
            template.textContent = html;
            return template.innerHTML;
        },

        escapeRegex: function (string) {
            return string.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
        }
    };

    // Enhanced API Client - COMPLETE ORIGINAL VERSION
    static api = {
        baseURL: '/api',
        timeout: 10000,

        async request(endpoint, options = {}) {
            const controller = new AbortController();
            const timeoutId = setTimeout(() => controller.abort(), options.timeout || this.timeout);

            const config = {
                method: 'GET',
                signal: controller.signal,
                headers: {
                    'Content-Type': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest',
                    ...options.headers
                },
                ...options
            };

            if (config.body && typeof config.body === 'object' && !(config.body instanceof FormData)) {
                config.body = JSON.stringify(config.body);
            }

            try {
                const response = await fetch(`${this.baseURL}${endpoint}`, config);
                clearTimeout(timeoutId);

                if (!response.ok) {
                    const errorText = await response.text();
                    throw new Error(`HTTP ${response.status}: ${response.statusText}. ${errorText}`);
                }

                const contentType = response.headers.get('content-type');
                if (contentType && contentType.includes('application/json')) {
                    return await response.json();
                } else {
                    return await response.text();
                }
            } catch (error) {
                clearTimeout(timeoutId);
                if (error.name === 'AbortError') {
                    throw new Error(`Request timeout after ${options.timeout || this.timeout}ms`);
                }
                throw error;
            }
        },

        async get(endpoint, options = {}) {
            return this.request(endpoint, { ...options, method: 'GET' });
        },

        async post(endpoint, data, options = {}) {
            return this.request(endpoint, { ...options, method: 'POST', body: data });
        },

        async put(endpoint, data, options = {}) {
            return this.request(endpoint, { ...options, method: 'PUT', body: data });
        },

        async delete(endpoint, options = {}) {
            return this.request(endpoint, { ...options, method: 'DELETE' });
        }
    };

    // Enhanced Form Validation - COMPLETE ORIGINAL VERSION
    static formValidator = {
        patterns: {
            email: /^[^\s@]+@[^\s@]+\.[^\s@]+$/,
            password: /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$/,
            url: /^https?:\/\/.+\..+$/,
            hexColor: /^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$/,
            phone: /^\+?[\d\s-()]{10,}$/,
            numeric: /^\d+$/,
            alpha: /^[A-Za-z]+$/
        },

        messages: {
            email: 'Please enter a valid email address',
            password: 'Password must contain at least 8 characters, one uppercase, one lowercase, one number and one special character',
            required: 'This field is required',
            url: 'Please enter a valid URL',
            phone: 'Please enter a valid phone number'
        },

        validate(field, value) {
            if (!(field instanceof HTMLElement)) {
                throw new Error('Field must be an HTML element');
            }

            const type = field.getAttribute('data-validation') || field.type;
            const customPattern = field.getAttribute('data-pattern');
            const isRequired = field.hasAttribute('required') || field.hasAttribute('data-required');

            if (isRequired && (!value || value.trim() === '')) {
                return false;
            }

            if (!value || value.trim() === '') {
                return true;
            }

            if (customPattern) {
                try {
                    return new RegExp(customPattern).test(value);
                } catch (error) {
                    console.error('Invalid custom pattern:', customPattern, error);
                    return true;
                }
            }

            switch (type) {
                case 'email':
                    return this.patterns.email.test(value);
                case 'password':
                    return this.patterns.password.test(value);
                case 'url':
                    return this.patterns.url.test(value);
                case 'tel':
                    return this.patterns.phone.test(value);
                case 'color':
                    return this.patterns.hexColor.test(value);
                case 'number':
                    return this.patterns.numeric.test(value);
                case 'text':
                    if (field.hasAttribute('data-alpha')) {
                        return this.patterns.alpha.test(value);
                    }
                    return true;
                default:
                    return true;
            }
        },

        getValidationMessage(field, isValid) {
            const type = field.getAttribute('data-validation') || field.type;
            const isRequired = field.hasAttribute('required') || field.hasAttribute('data-required');

            if (!isValid) {
                if (isRequired && (!field.value || field.value.trim() === '')) {
                    return this.messages.required;
                }
                return this.messages[type] || `Invalid ${type} format`;
            }
            return '';
        },

        markField(field, isValid, message = '') {
            if (!(field instanceof HTMLElement)) return;

            field.classList.remove('is-valid', 'is-invalid');
            field.classList.add(isValid ? 'is-valid' : 'is-invalid');

            const existingFeedback = field.parentNode?.querySelector('.invalid-feedback, .valid-feedback');
            if (existingFeedback) {
                existingFeedback.remove();
            }

            if (message) {
                const feedback = document.createElement('div');
                feedback.className = isValid ? 'valid-feedback' : 'invalid-feedback';
                feedback.textContent = message;
                field.parentNode.appendChild(feedback);
            }
        },

        validateForm(form) {
            if (!(form instanceof HTMLFormElement)) {
                throw new Error('Argument must be a form element');
            }

            let isValid = true;
            const fields = form.querySelectorAll('[data-validation], [required]');

            fields.forEach(field => {
                const fieldIsValid = this.validate(field, field.value);
                const message = this.getValidationMessage(field, fieldIsValid);
                this.markField(field, fieldIsValid, message);

                if (!fieldIsValid) {
                    isValid = false;
                }
            });

            return isValid;
        }
    };

    // Event Listeners Setup
    setupEventListeners() {
        const events = [
            { element: document, type: 'submit', handler: this.handleFormSubmit.bind(this) },
            { element: document, type: 'blur', handler: this.handleInputBlur.bind(this), options: true },
            { element: document, type: 'click', handler: this.handleButtonClick.bind(this) },
            { element: document, type: 'keydown', handler: this.handleKeyboardShortcuts.bind(this) },
            { element: document, type: 'visibilitychange', handler: this.handleVisibilityChange.bind(this) },
            { element: window, type: 'resize', handler: DevToolsSuite.utils.debounce(this.handleResize.bind(this), 250) }
        ];

        events.forEach(({ element, type, handler, options }) => {
            element.addEventListener(type, handler, options);
            this.eventListeners.push({ element, type, handler, options });
        });
    }

    handleFormSubmit(e) {
        try {
            const form = e.target;

            if (form.hasAttribute('data-validate')) {
                const isValid = DevToolsSuite.formValidator.validateForm(form);
                if (!isValid) {
                    e.preventDefault();
                    this.showFormError(form, 'Please fix the validation errors');
                    return;
                }
            }

            const submitBtn = form.querySelector('button[type="submit"]');
            if (submitBtn) {
                this.showButtonLoading(submitBtn);
            }

            if (form.hasAttribute('data-autosave')) {
                this.autoSaveForm(form);
            }
        } catch (error) {
            console.error('Form submission handling failed:', error);
        }
    }

    handleInputBlur(e) {
        try {
            if (e.target.hasAttribute('data-validation') || e.target.hasAttribute('required')) {
                const isValid = DevToolsSuite.formValidator.validate(e.target, e.target.value);
                const message = DevToolsSuite.formValidator.getValidationMessage(e.target, isValid);
                DevToolsSuite.formValidator.markField(e.target, isValid, message);
            }
        } catch (error) {
            console.error('Input blur handling failed:', error);
        }
    }

    handleButtonClick(e) {
        try {
            const button = e.target.closest('[data-loading]');
            if (button) {
                this.showButtonLoading(button);
            }

            const copyButton = e.target.closest('[data-copy]');
            if (copyButton) {
                this.handleCopyButton(copyButton);
            }
        } catch (error) {
            console.error('Button click handling failed:', error);
        }
    }

    async handleCopyButton(button) {
        try {
            const targetSelector = button.getAttribute('data-copy');
            const targetElement = document.querySelector(targetSelector);

            if (targetElement) {
                const text = targetElement.value || targetElement.textContent;
                const result = await DevToolsSuite.utils.copyToClipboard(text);

                if (result.success) {
                    this.showTempMessage(button, 'Copied!', 'success');
                } else {
                    this.showTempMessage(button, 'Copy failed', 'error');
                }
            }
        } catch (error) {
            console.error('Copy button handling failed:', error);
            this.showTempMessage(button, 'Copy failed', 'error');
        }
    }

    handleKeyboardShortcuts(e) {
        try {
            if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
                e.preventDefault();
                this.focusSearch();
            }

            if (e.key === 'Escape') {
                this.closeModals();
            }

            if ((e.ctrlKey || e.metaKey) && e.key === '/') {
                e.preventDefault();
                this.showHelp();
            }
        } catch (error) {
            console.error('Keyboard shortcut handling failed:', error);
        }
    }

    handleVisibilityChange() {
        try {
            if (document.hidden) {
                this.pauseAnimations();
            } else {
                this.resumeAnimations();
            }
        } catch (error) {
            console.error('Visibility change handling failed:', error);
        }
    }

    handleResize() {
        try {
            this.updateResponsiveElements();
        } catch (error) {
            console.error('Resize handling failed:', error);
        }
    }

    // Animation Setup
    setupAnimations() {
        this.setupScrollAnimations();
        this.setupIntersectionObserver();
        this.setupHoverAnimations();
    }

    setupScrollAnimations() {
        try {
            const animatedElements = document.querySelectorAll('[data-animate]');
            animatedElements.forEach(el => {
                el.classList.add('fade-in-up');
            });
        } catch (error) {
            console.error('Scroll animations setup failed:', error);
        }
    }

    setupIntersectionObserver() {
        try {
            if (!('IntersectionObserver' in window)) return;

            const observer = new IntersectionObserver((entries) => {
                entries.forEach(entry => {
                    if (entry.isIntersecting) {
                        entry.target.classList.add('animate-in');
                        observer.unobserve(entry.target);
                    }
                });
            }, { threshold: 0.1 });

            document.querySelectorAll('[data-observe]').forEach(el => {
                observer.observe(el);
            });
        } catch (error) {
            console.error('Intersection observer setup failed:', error);
        }
    }

    setupHoverAnimations() {
        try {
            document.querySelectorAll('.btn-magnetic').forEach(btn => {
                let animationFrameId;

                const mouseMoveHandler = (e) => {
                    if (animationFrameId) {
                        cancelAnimationFrame(animationFrameId);
                    }

                    animationFrameId = requestAnimationFrame(() => {
                        this.handleMagneticEffect(e);
                    });
                };

                const mouseLeaveHandler = (e) => {
                    if (animationFrameId) {
                        cancelAnimationFrame(animationFrameId);
                    }
                    this.resetMagneticEffect(e);
                };

                btn.addEventListener('mousemove', mouseMoveHandler);
                btn.addEventListener('mouseleave', mouseLeaveHandler);

                this.eventListeners.push(
                    { element: btn, type: 'mousemove', handler: mouseMoveHandler },
                    { element: btn, type: 'mouseleave', handler: mouseLeaveHandler }
                );
            });
        } catch (error) {
            console.error('Hover animations setup failed:', error);
        }
    }

    handleMagneticEffect(e) {
        const btn = e.currentTarget;
        const rect = btn.getBoundingClientRect();
        const x = e.clientX - rect.left;
        const y = e.clientY - rect.top;

        const centerX = rect.width / 2;
        const centerY = rect.height / 2;

        const deltaX = (x - centerX) / centerX;
        const deltaY = (y - centerY) / centerY;

        btn.style.transform = `translate3d(${deltaX * 5}px, ${deltaY * 5}px, 0)`;
    }

    resetMagneticEffect(e) {
        const btn = e.currentTarget;
        btn.style.transform = 'translate3d(0, 0, 0)';
    }

    // Service Worker Setup
    setupServiceWorker() {
        try {
            if ('serviceWorker' in navigator) {
                window.addEventListener('load', () => {
                    if (window.location.hostname !== 'localhost' && window.location.hostname !== '127.0.0.1') {
                        navigator.serviceWorker.register('/sw.js')
                            .then((registration) => {
                                console.log('SW registered: ', registration);
                                this.setupUpdateNotification(registration);
                            })
                            .catch((registrationError) => {
                                console.log('SW registration failed: ', registrationError);
                            });
                    }
                });
            }
        } catch (error) {
            console.error('Service worker setup failed:', error);
        }
    }

    setupUpdateNotification(registration) {
        try {
            registration.addEventListener('updatefound', () => {
                const newWorker = registration.installing;
                newWorker.addEventListener('statechange', () => {
                    if (newWorker.state === 'installed' && navigator.serviceWorker.controller) {
                        this.showUpdateNotification();
                    }
                });
            });

            const updateInterval = setInterval(() => {
                registration.update();
            }, 60 * 60 * 1000);

            this.intervals.push(updateInterval);
        } catch (error) {
            console.error('Update notification setup failed:', error);
        }
    }

    showUpdateNotification() {
        try {
            if (document.querySelector('.update-notification')) return;

            const notification = document.createElement('div');
            notification.className = 'update-notification alert alert-info alert-dismissible fade show';
            notification.innerHTML = `
                <strong>Update Available!</strong> A new version is available.
                <button type="button" class="btn btn-sm btn-primary ms-2" onclick="location.reload()">Reload</button>
                <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
            `;

            const container = document.querySelector('.container') || document.body;
            container.prepend(notification);
        } catch (error) {
            console.error('Update notification failed:', error);
        }
    }

    // Performance Monitoring
    setupPerformanceMonitoring() {
        try {
            if ('PerformanceObserver' in window) {
                const observer = new PerformanceObserver((list) => {
                    list.getEntries().forEach((entry) => {
                        this.trackPerformance(entry);
                    });
                });

                try {
                    observer.observe({ entryTypes: ['largest-contentful-paint', 'first-input', 'layout-shift'] });
                } catch (error) {
                    console.warn('Performance monitoring failed:', error);
                }
            }

            if (performance.memory) {
                const memoryInterval = setInterval(() => {
                    this.logMemoryUsage();
                }, 30000);
                this.intervals.push(memoryInterval);
            }

            window.addEventListener('load', () => {
                setTimeout(() => {
                    this.reportPageLoadPerformance();
                }, 0);
            });
        } catch (error) {
            console.error('Performance monitoring setup failed:', error);
        }
    }

    trackPerformance(entry) {
        const threshold = {
            'largest-contentful-paint': 2500,
            'first-input': 100,
            'layout-shift': 0.1
        }[entry.name] || 1000;

        if (entry.value > threshold) {
            console.warn(`Performance issue detected: ${entry.name} = ${entry.value}`);
            this.reportPerformanceIssue(entry);
        }
    }

    reportPerformanceIssue(entry) {
        if (window.gtag) {
            window.gtag('event', 'performance_issue', {
                event_category: 'Performance',
                event_label: entry.name,
                value: Math.round(entry.value)
            });
        }
    }

    logMemoryUsage() {
        try {
            const memory = performance.memory;
            const usedMB = Math.round(memory.usedJSHeapSize / 1048576);
            const totalMB = Math.round(memory.totalJSHeapSize / 1048576);

            console.log(`Memory: ${usedMB}MB / ${totalMB}MB (${Math.round(usedMB / totalMB * 100)}%)`);

            if (usedMB / totalMB > 0.8) {
                console.warn('High memory usage detected');
            }
        } catch (error) {
            console.error('Memory logging failed:', error);
        }
    }

    reportPageLoadPerformance() {
        try {
            const navigation = performance.getEntriesByType('navigation')[0];
            if (navigation) {
                const loadTime = navigation.loadEventEnd - navigation.navigationStart;
                console.log(`Page loaded in ${loadTime}ms`);
            }
        } catch (error) {
            console.error('Page load performance reporting failed:', error);
        }
    }

    // Collaboration Features
    setupCollaboration() {
        try {
            if (window.roomId) {
                this.connectToCollaborationRoom(window.roomId);
            }
        } catch (error) {
            console.error('Collaboration setup failed:', error);
        }
    }

    connectToCollaborationRoom(roomId) {
        console.log('Connecting to collaboration room:', roomId);
    }

    // Utility Methods
    showButtonLoading(button) {
        try {
            const originalText = button.innerHTML;
            button.setAttribute('data-original-text', originalText);
            button.innerHTML = '<span class="loading-spinner me-2"></span> Processing...';
            button.disabled = true;

            const timeout = setTimeout(() => {
                this.hideButtonLoading(button);
            }, 30000);

            this.timeouts.push(timeout);
        } catch (error) {
            console.error('Button loading failed:', error);
        }
    }

    hideButtonLoading(button) {
        try {
            const originalText = button.getAttribute('data-original-text');
            if (originalText) {
                button.innerHTML = originalText;
            }
            button.disabled = false;
        } catch (error) {
            console.error('Button loading hide failed:', error);
        }
    }

    showTempMessage(element, message, type = 'info') {
        try {
            const tempMsg = document.createElement('div');
            tempMsg.className = `alert alert-${type} alert-dismissible fade show position-fixed`;
            tempMsg.style.cssText = 'top: 20px; right: 20px; z-index: 1060; min-width: 200px;';
            tempMsg.innerHTML = `
                ${message}
                <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
            `;

            document.body.appendChild(tempMsg);

            const timeout = setTimeout(() => {
                if (tempMsg.parentNode) {
                    tempMsg.remove();
                }
            }, 3000);

            this.timeouts.push(timeout);
        } catch (error) {
            console.error('Temp message failed:', error);
        }
    }

    showFormError(form, message) {
        this.showTempMessage(form, message, 'danger');
    }

    focusSearch() {
        try {
            const searchInput = document.querySelector('[data-search]');
            if (searchInput) {
                searchInput.focus();
                searchInput.select();
            }
        } catch (error) {
            console.error('Focus search failed:', error);
        }
    }

    closeModals() {
        try {
            if (typeof bootstrap === 'undefined') return;

            const modals = document.querySelectorAll('.modal.show');
            modals.forEach(modal => {
                const modalInstance = bootstrap.Modal.getInstance(modal);
                if (modalInstance) {
                    modalInstance.hide();
                }
            });

            const dropdowns = document.querySelectorAll('.dropdown-menu.show');
            dropdowns.forEach(dropdown => {
                const dropdownInstance = bootstrap.Dropdown.getInstance(dropdown.previousElementSibling);
                if (dropdownInstance) {
                    dropdownInstance.hide();
                }
            });
        } catch (error) {
            console.error('Close modals failed:', error);
        }
    }

    showHelp() {
        console.log('Show help documentation');
    }

    pauseAnimations() {
        try {
            document.body.style.animationPlayState = 'paused';
            document.querySelectorAll('*').forEach(el => {
                if (el.style.animationPlayState !== '') {
                    el.style.animationPlayState = 'paused';
                }
            });
        } catch (error) {
            console.error('Pause animations failed:', error);
        }
    }

    resumeAnimations() {
        try {
            document.body.style.animationPlayState = 'running';
            document.querySelectorAll('*').forEach(el => {
                if (el.style.animationPlayState !== '') {
                    el.style.animationPlayState = 'running';
                }
            });
        } catch (error) {
            console.error('Resume animations failed:', error);
        }
    }

    autoSaveForm(form) {
        try {
            const formData = new FormData(form);
            const data = Object.fromEntries(formData);
            localStorage.setItem(`autosave-${form.id}`, JSON.stringify(data));
        } catch (error) {
            console.error('Auto-save failed:', error);
        }
    }

    loadAutoSave(form) {
        try {
            const saved = localStorage.getItem(`autosave-${form.id}`);
            if (saved) {
                const data = JSON.parse(saved);
                Object.keys(data).forEach(key => {
                    const input = form.querySelector(`[name="${key}"]`);
                    if (input) {
                        input.value = data[key];
                    }
                });
                return true;
            }
        } catch (error) {
            console.error('Load auto-save failed:', error);
            localStorage.removeItem(`autosave-${form.id}`);
        }
        return false;
    }

    clearAutoSave(form) {
        try {
            localStorage.removeItem(`autosave-${form.id}`);
        } catch (error) {
            console.error('Clear auto-save failed:', error);
        }
    }

    updateResponsiveElements() {
        try {
            const width = window.innerWidth;
            const isMobile = width < 768;
            document.body.classList.toggle('is-mobile', isMobile);
            document.body.classList.toggle('is-desktop', !isMobile);
        } catch (error) {
            console.error('Update responsive elements failed:', error);
        }
    }

    // Enhanced cleanup method
    destroy() {
        this.eventListeners.forEach(({ element, type, handler, options }) => {
            element.removeEventListener(type, handler, options);
        });

        this.intervals.forEach(interval => clearInterval(interval));
        this.timeouts.forEach(timeout => clearTimeout(timeout));

        this.eventListeners = [];
        this.intervals = [];
        this.timeouts = [];

        console.log('DevToolsSuite cleaned up successfully');
    }
}

// Safe initialization
function initializeDevToolsSuite() {
    try {
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', function () {
                window.devToolsSuite = new DevToolsSuite();
                window.DevTools = DevToolsSuite.utils;
                window.APIClient = DevToolsSuite.api;
                window.FormValidator = DevToolsSuite.formValidator;
            });
        } else {
            window.devToolsSuite = new DevToolsSuite();
            window.DevTools = DevToolsSuite.utils;
            window.APIClient = DevToolsSuite.api;
            window.FormValidator = DevToolsSuite.formValidator;
        }
    } catch (error) {
        console.error('DevToolsSuite initialization failed:', error);
    }
}

// Initialize
initializeDevToolsSuite();

// Global error handling
window.addEventListener('error', function (e) {
    console.error('Global error:', e.error);
});

window.addEventListener('unhandledrejection', function (e) {
    console.error('Unhandled promise rejection:', e.reason);
});

// Export for module usage
if (typeof module !== 'undefined' && module.exports) {
    module.exports = DevToolsSuite;
}