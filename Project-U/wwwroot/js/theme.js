function setTheme(theme) {
    document.body.classList.remove('theme-dark', 'theme-green', 'theme-blue');
    if (theme !== 'default') document.body.classList.add('theme-' + theme);
    localStorage.setItem('theme', theme);
    const btn = event.target;
    const original = btn.textContent;
    btn.textContent = '✅ Застосовано!';
    setTimeout(() => btn.textContent = original, 1500);
}