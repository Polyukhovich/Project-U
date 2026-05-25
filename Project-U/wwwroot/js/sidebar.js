function toggleSidebar() {
    const sidebar = document.getElementById('sidebar');
    const mainContent = document.getElementById('mainContent');
    const overlay = document.getElementById('sidebarOverlay');

    if (window.innerWidth <= 991) {
        sidebar.classList.toggle('mobile-open');
        overlay.classList.toggle('active');
    } else {
        sidebar.classList.toggle('collapsed');
        mainContent.classList.toggle('expanded');
        // desktop-стан керується через клас body — без інлайнових стилів,
        // щоб не ламати мобільний режим при ресайзі вікна
        document.body.classList.toggle('sidebar-collapsed',
            sidebar.classList.contains('collapsed'));
    }
}

document.addEventListener('click', function (e) {
    const sidebar = document.getElementById('sidebar');
    const toggleBtn = document.querySelector('.toggle-btn');

    if (window.innerWidth <= 991 &&
        !sidebar.contains(e.target) &&
        !toggleBtn.contains(e.target)) {
        sidebar.classList.remove('mobile-open');
        document.getElementById('sidebarOverlay').classList.remove('active');
        document.body.classList.remove('no-scroll');
    }
});

document.querySelectorAll('.sidebar .nav-link').forEach(link => {
    if (link.href === window.location.href) {
        link.classList.add('active');
    }
});

window.addEventListener('resize', function () {
    if (window.innerWidth > 991) {
        // при поверненні на десктоп прибираємо мобільні стани
        document.getElementById('sidebar').classList.remove('mobile-open');
        document.getElementById('sidebarOverlay').classList.remove('active');
        document.body.classList.remove('no-scroll');
    } else {
        // при переході на мобільний — скидаємо десктопні стани,
        // інакше sidebar може опинитися з класом 'collapsed' разом з 'mobile-open'
        document.getElementById('sidebar').classList.remove('collapsed');
        document.getElementById('mainContent').classList.remove('expanded');
        document.body.classList.remove('sidebar-collapsed');
    }
});