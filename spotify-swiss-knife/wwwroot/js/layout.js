document.addEventListener('DOMContentLoaded', function() {
    const navToggle = document.getElementById('navToggle');
    const navList = document.getElementById('navList');
    let backdrop = document.getElementById('navBackdrop');

    // Collect all nav dropdowns so Services and Library are handled identically.
    const allDropdowns = Array.from(document.querySelectorAll('.nav-dropdown'));

    function isMobileNav() {
        return window.innerWidth <= 1024;
    }

    // CSS now handles dropdown positioning via position:absolute / top:100%.
    // This function only clears any lingering inline styles from a previous
    // JS-positioned session (e.g. after a resize that crosses the breakpoint).
    function positionDropdownMenu(dropdown) {
        const menu = dropdown.querySelector('.dropdown-menu');
        if (menu) {
            menu.style.top = '';
            menu.style.left = '';
        }
    }

    function closeDropdown(dropdown) {
        const toggle = dropdown.querySelector('.nav-dropdown-toggle');

        dropdown.classList.remove('open');

        if (toggle) {
            toggle.setAttribute('aria-expanded', 'false');
        }
    }

    function openDropdown(dropdown) {
        const toggle = dropdown.querySelector('.nav-dropdown-toggle');

        // Close every other dropdown first.
        allDropdowns.forEach(function(d) {
            if (d !== dropdown) {
                closeDropdown(d);
            }
        });

        positionDropdownMenu(dropdown);
        dropdown.classList.add('open');

        if (toggle) {
            toggle.setAttribute('aria-expanded', 'true');
        }
    }

    function toggleDropdown(dropdown) {
        if (dropdown.classList.contains('open')) {
            closeDropdown(dropdown);
        } else {
            openDropdown(dropdown);
        }
    }

    function closeAllDropdowns() {
        allDropdowns.forEach(closeDropdown);
    }

    function closeMobileNav() {
        if (!navList || !backdrop) {
            return;
        }

        navList.classList.remove('active');
        backdrop.classList.remove('active');
    }

    if (!backdrop) {
        backdrop = document.createElement('div');
        backdrop.id = 'navBackdrop';
        backdrop.className = 'nav-backdrop';
        document.body.insertBefore(backdrop, document.body.firstChild);
    }

    // Wire up each dropdown toggle button.
    allDropdowns.forEach(function(dropdown) {
        const toggle = dropdown.querySelector('.nav-dropdown-toggle');

        if (!toggle) {
            return;
        }

        toggle.addEventListener('click', function(event) {
            event.preventDefault();
            event.stopPropagation();
            toggleDropdown(dropdown);
        });
    });

    // Re-position open dropdowns when the window resizes (e.g. crossing breakpoints).
    window.addEventListener('resize', function() {
        allDropdowns.forEach(function(dropdown) {
            if (dropdown.classList.contains('open')) {
                positionDropdownMenu(dropdown);
            }
        });
    });

    if (navToggle && navList) {
        navToggle.addEventListener('click', function() {
            navList.classList.toggle('active');
            backdrop.classList.toggle('active');
        });

        backdrop.addEventListener('click', function() {
            closeMobileNav();
            closeAllDropdowns();
        });

        navList.addEventListener('click', function(event) {
            const link = event.target.closest('a.nav-link, a.nav-submenu-link');

            if (!link) {
                return;
            }

            closeMobileNav();
        });

        navList.addEventListener('keydown', function(event) {
            if (event.key === 'ArrowDown') {
                navList.scrollBy({ top: 56, behavior: 'smooth' });
                event.preventDefault();
            }

            if (event.key === 'ArrowUp') {
                navList.scrollBy({ top: -56, behavior: 'smooth' });
                event.preventDefault();
            }
        });
    }

    document.addEventListener('click', function(event) {
        // Close any open dropdown whose container does not contain the click target.
        allDropdowns.forEach(function(dropdown) {
            if (dropdown.classList.contains('open') && !dropdown.contains(event.target)) {
                closeDropdown(dropdown);
            }
        });
    });

    document.addEventListener('keydown', function(event) {
        if (event.key === 'Escape') {
            closeAllDropdowns();
            closeMobileNav();
        }
    });

    const currentPath = window.location.pathname;
    const navLinks = document.querySelectorAll('.nav-link');
    navLinks.forEach(link => {
        if (link.getAttribute('href') === currentPath || 
            (currentPath === '/' && link.getAttribute('href') === '/')) {
            link.classList.add('active');
        }
    });

    // Matrix rain background tuned per screen size to preserve usability.
    const reduceMotionQuery = window.matchMedia('(prefers-reduced-motion: reduce)');
    let matrixCanvas = null;
    let matrixContext = null;
    let drops = [];
    let columnCount = 0;
    let animationFrameId = null;
    let lastFrameTime = 0;
    let targetFrameTime = 1000 / 22;
    let glyphSize = 17;
    const glyphs = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789#$%&*+-/<=>?[]{}';

    function resizeMatrix() {
        if (!matrixCanvas || !matrixContext) {
            return;
        }

        const dpr = window.devicePixelRatio || 1;
        const width = Math.max(1, Math.floor(window.innerWidth));
        const height = Math.max(1, Math.floor(window.innerHeight));

        const isMobile = width <= 768;
        glyphSize = isMobile ? 20 : 17;
        targetFrameTime = isMobile ? 1000 / 14 : 1000 / 22;

        matrixCanvas.width = Math.floor(width * dpr);
        matrixCanvas.height = Math.floor(height * dpr);
        matrixCanvas.style.width = width + 'px';
        matrixCanvas.style.height = height + 'px';

        matrixContext.setTransform(1, 0, 0, 1, 0, 0);
        matrixContext.scale(dpr, dpr);
        matrixContext.font = glyphSize + 'px "Source Code Pro", "Cascadia Code", monospace';
        matrixContext.textAlign = 'center';

        const nextColumnCount = Math.max(1, Math.floor(width / glyphSize));
        if (nextColumnCount !== columnCount) {
            columnCount = nextColumnCount;
            drops = Array.from({ length: columnCount }, function() {
                return Math.floor(Math.random() * Math.max(1, Math.floor(height / glyphSize)));
            });
        }
    }

    function drawMatrixFrame(timestamp) {
        if (!matrixCanvas || !matrixContext) {
            return;
        }

        if (timestamp - lastFrameTime < targetFrameTime) {
            animationFrameId = window.requestAnimationFrame(drawMatrixFrame);
            return;
        }

        lastFrameTime = timestamp;
        const width = window.innerWidth;
        const height = window.innerHeight;

        matrixContext.fillStyle = 'rgba(0, 0, 0, 0.09)';
        matrixContext.fillRect(0, 0, width, height);
        matrixContext.fillStyle = '#1fd11f';

        for (let i = 0; i < columnCount; i += 1) {
            const glyph = glyphs.charAt(Math.floor(Math.random() * glyphs.length));
            const x = i * glyphSize + glyphSize * 0.5;
            const y = drops[i] * glyphSize;

            matrixContext.fillText(glyph, x, y);

            if (y > height && Math.random() > 0.975) {
                drops[i] = 0;
            }

            drops[i] += 1;
        }

        animationFrameId = window.requestAnimationFrame(drawMatrixFrame);
    }

    function stopMatrix() {
        if (animationFrameId !== null) {
            window.cancelAnimationFrame(animationFrameId);
            animationFrameId = null;
        }

        if (matrixCanvas) {
            matrixCanvas.remove();
        }

        matrixCanvas = null;
        matrixContext = null;
        drops = [];
        columnCount = 0;
        lastFrameTime = 0;
    }

    function startMatrix() {
        if (matrixCanvas) {
            return;
        }

        matrixCanvas = document.createElement('canvas');
        matrixCanvas.id = 'matrixCanvas';
        matrixCanvas.setAttribute('aria-hidden', 'true');
        document.body.prepend(matrixCanvas);
        matrixContext = matrixCanvas.getContext('2d');

        if (!matrixContext) {
            stopMatrix();
            return;
        }

        resizeMatrix();
        animationFrameId = window.requestAnimationFrame(drawMatrixFrame);
    }

    function syncMatrixState() {
        const shouldRun = !reduceMotionQuery.matches;

        if (shouldRun) {
            startMatrix();
            return;
        }

        stopMatrix();
    }

    window.addEventListener('resize', resizeMatrix);
    reduceMotionQuery.addEventListener('change', syncMatrixState);
    syncMatrixState();
});
