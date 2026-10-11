(() => {
  const key = 'furigana';
  const button = document.getElementById('furigana-toggle');
  let on = localStorage.getItem(key) !== 'off';

  const apply = () => {
    document.body.classList.toggle('hide-furigana', !on);
    if (button) {
      button.classList.toggle('active', on);
      button.setAttribute('aria-pressed', String(on));
    }
  };

  apply();
  button?.addEventListener('click', () => {
    on = !on;
    localStorage.setItem(key, on ? 'on' : 'off');
    apply();
  });
})();
