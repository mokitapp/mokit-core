// Mokit Theme and Sidebar Management
// Uses data attributes for strongest CSS specificity

(function() {
    'use strict';
    
    var html = document.documentElement;

    // ===== SIDEBAR STATE =====
    function applySidebarState(isCollapsed) {
        // Data attribute for CSS
        html.setAttribute('data-sidebar', isCollapsed ? 'collapsed' : 'expanded');
        
        // Classes for backward compatibility
        html.classList.remove('sidebar-collapsed', 'sidebar-expanded');
        html.classList.add(isCollapsed ? 'sidebar-collapsed' : 'sidebar-expanded');
        
        // Container element
        var container = document.querySelector('.app-container');
        if (container) {
            container.classList.toggle('sidebar-collapsed', isCollapsed);
        }
    }

    // ===== THEME STATE =====
    function applyTheme(isLight) {
        html.classList.toggle('light-theme', isLight);
        
        // Update icons
        document.querySelectorAll('.sun-icon').forEach(function(el) { 
            el.style.display = isLight ? 'block' : 'none'; 
        });
        document.querySelectorAll('.moon-icon').forEach(function(el) { 
            el.style.display = isLight ? 'none' : 'block'; 
        });
    }

    // ===== TOGGLE FUNCTIONS =====
    window.toggleSidebar = function(e) {
        if (e) e.preventDefault();
        
        var current = localStorage.getItem('Mokit-sidebar') || 'expanded';
        var next = current === 'collapsed' ? 'expanded' : 'collapsed';
        localStorage.setItem('Mokit-sidebar', next);
        
        applySidebarState(next === 'collapsed');
    };

    window.toggleTheme = function(e) {
        if (e) e.preventDefault();
        
        var current = localStorage.getItem('Mokit-theme') || 'dark';
        var next = current === 'light' ? 'dark' : 'light';
        localStorage.setItem('Mokit-theme', next);
        
        applyTheme(next === 'light');
    };

    // ===== INITIALIZATION =====
    function init() {
        var isCollapsed = localStorage.getItem('Mokit-sidebar') === 'collapsed';
        var isLight = localStorage.getItem('Mokit-theme') === 'light';
        
        applySidebarState(isCollapsed);
        applyTheme(isLight);
    }

    // Apply when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

    // ===== BLAZOR NAVIGATION HANDLER =====
    if (typeof Blazor !== 'undefined') {
        Blazor.addEventListener('enhancedload', function() {
            requestAnimationFrame(init);
        });
    }

    // ===== MUTATION OBSERVER =====
    var observer = new MutationObserver(function() {
        var container = document.querySelector('.app-container');
        if (container && !container.hasAttribute('data-init')) {
            container.setAttribute('data-init', 'true');
            var isCollapsed = localStorage.getItem('Mokit-sidebar') === 'collapsed';
            container.classList.toggle('sidebar-collapsed', isCollapsed);
        }
    });

    if (document.body) {
        observer.observe(document.body, { childList: true, subtree: true });
    } else {
        document.addEventListener('DOMContentLoaded', function() {
            observer.observe(document.body, { childList: true, subtree: true });
        });
    }

    // Blazor interop
    window.initializeLayoutFromBlazor = init;
})();
