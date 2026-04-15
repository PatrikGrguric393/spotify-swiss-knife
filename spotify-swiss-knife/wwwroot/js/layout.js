// Mobile navigation toggle
document.addEventListener('DOMContentLoaded', function() {
    const navToggle = document.getElementById('navToggle');
    const navList = document.getElementById('navList');
    let backdrop = document.getElementById('navBackdrop');

    // Create backdrop if it doesn't exist
    if (!backdrop) {
        backdrop = document.createElement('div');
        backdrop.id = 'navBackdrop';
        backdrop.className = 'nav-backdrop';
        document.body.insertBefore(backdrop, document.body.firstChild);
    }

    if (navToggle && navList) {
        navToggle.addEventListener('click', function() {
            navList.classList.toggle('active');
            backdrop.classList.toggle('active');
        });

        // Close menu when clicking on a link
        const navLinks = navList.querySelectorAll('.nav-link');
        navLinks.forEach(link => {
            link.addEventListener('click', function() {
                navList.classList.remove('active');
                backdrop.classList.remove('active');
            });
        });

        // Close menu when clicking outside
        backdrop.addEventListener('click', function() {
            navList.classList.remove('active');
            backdrop.classList.remove('active');
        });
        document.addEventListener('click', function(event) {
            if (!event.target.closest('.app-nav') && !event.target.closest('.nav-backdrop')) {
                navList.classList.remove('active');
                backdrop.classList.remove('active');
            }
        });
    }

    // Set active link based on current URL
    const currentPath = window.location.pathname;
    const navLinks = document.querySelectorAll('.nav-link');
    navLinks.forEach(link => {
        if (link.getAttribute('href') === currentPath || 
            (currentPath === '/' && link.getAttribute('href') === '/')) {
            link.classList.add('active');
        }
    });
});
