function getNotificationUrl(message) {
    if (message.includes('оцінку') || message.includes('оцінено')) return '/Grades';
    if (message.includes('здав роботу')) return '/Assignments';
    if (message.includes('розклад')) return '/Schedules';
    if (message.includes('завдання') || message.includes('Завдання') || message.includes('дедлайн')) return '/Assignments';
    if (message.includes('схожість') || message.includes('перевірку') || message.includes('схожості')) return '/Assignments';
    return '/';
}

function buildNotificationHtml(n) {
    const isDark = document.body.classList.contains('theme-dark');
    var bg = n.isRead
        ? (isDark ? '#16213e' : 'white')
        : (isDark ? 'rgba(108,92,231,0.2)' : 'rgba(108,92,231,0.1)');
    var textColor = isDark ? '#ffffff' : '#333333';
    var borderColor = isDark ? '#2d3561' : '#f0f0f0';
    var badge = n.isRead ? '' : '<span style="float:right; background:#6c5ce7; color:white; border-radius:10px; padding:1px 8px; font-size:0.7rem;">Нове</span>';
    var url = getNotificationUrl(n.message);
    return '<div style="padding:12px 15px; border-bottom:1px solid ' + borderColor + '; background:' + bg + '; cursor:pointer;" onclick="readAndGo(' + n.id + ', \'' + url + '\')">'
        + '<div style="font-size:0.85rem; color:' + textColor + ';">' + n.message + '</div>'
        + '<div style="font-size:0.75rem; color:#999; margin-top:3px;">🕐 ' + n.createdAt + '</div>'
        + badge
        + '</div>';
}
function loadNotifications() {
    fetch('/Notifications/GetNotifications')
        .then(r => r.json())
        .then(data => {
            const list = document.getElementById('notificationList');
            const badge = document.getElementById('unreadBadge');
            const markAllBtn = document.getElementById('markAllBtn');

            const unread = data.filter(n => !n.isRead);
            badge.style.display = unread.length > 0 ? 'inline' : 'none';
            badge.textContent = unread.length;
            if (markAllBtn) markAllBtn.style.display = unread.length > 0 ? 'inline' : 'none';
            if (unread.length === 0) {
                list.innerHTML = '<p class="text-center text-muted p-3">Немає нових сповіщень</p>';
                return;
            }
            list.innerHTML = unread.map(n => buildNotificationHtml(n)).join('');

            // Ховаємо банер якщо є з'єднання
            const banner = document.getElementById('offlineBanner');
            if (banner) banner.style.display = 'none';
        })
        .catch(() => {
            // Показуємо банер якщо немає з'єднання
            const banner = document.getElementById('offlineBanner');
            if (banner) banner.style.display = 'block';
        });
}

function toggleNotifications() {
    const dropdown = document.getElementById('notificationDropdown');
    dropdown.style.display = dropdown.style.display === 'none' ? 'block' : 'none';
    if (dropdown.style.display === 'block') loadNotifications();
}

function markAllRead() {
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    fetch('/Notifications/MarkAllRead', {
        method: 'POST',
        headers: { 'RequestVerificationToken': token || '' }
    }).then(() => {
        loadNotifications();
        document.getElementById('notificationDropdown').style.display = 'none';
    });
}

function readAndGo(id, url) {
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    fetch(`/Notifications/MarkRead/${id}`, {
        method: 'POST',
        headers: { 'RequestVerificationToken': token || '' }
    }).then(() => {
        loadNotifications();
        window.location = url;
    });
}

function showToast(message, url) {
    const toast = document.createElement('div');
    toast.className = 'toast-notification';
    toast.style.cursor = 'pointer';
    toast.innerHTML = `🔔 ${message}`;
    if (url) toast.onclick = () => window.location = url;
    document.body.appendChild(toast);
    setTimeout(() => toast.classList.add('show'), 100);
    setTimeout(() => {
        toast.classList.remove('show');
        setTimeout(() => toast.remove(), 300);
    }, 4000);
}

function sendBrowserNotification(message) {
    if ('Notification' in window && Notification.permission === 'granted') {
        new Notification('Project U 🎓', {
            body: message,
            icon: '/favicon.ico'
        });
    }
}

// Закриваємо dropdown при кліку поза ним
document.addEventListener('click', function (e) {
    const wrapper = document.querySelector('.notification-wrapper');
    if (wrapper && !wrapper.contains(e.target)) {
        const dropdown = document.getElementById('notificationDropdown');
        if (dropdown) dropdown.style.display = 'none';
    }
});

// Запит дозволу на browser notifications
if ('Notification' in window && Notification.permission === 'default') {
    Notification.requestPermission();
}

loadNotifications();
setInterval(loadNotifications, 5000);