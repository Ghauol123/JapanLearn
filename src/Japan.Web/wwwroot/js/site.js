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

(() => {
  const box = document.getElementById('search-box');
  if (!box || !box.dataset.searchUrl) return;
  const results = document.getElementById('search-results');
  const list = document.getElementById('lesson-list');
  let timer;
  let controller;

  box.addEventListener('input', () => {
    clearTimeout(timer);
    timer = setTimeout(async () => {
      const q = box.value.trim();
      if (!q) {
        results.innerHTML = '';
        list.hidden = false;
        return;
      }
      controller?.abort();
      controller = new AbortController();
      try {
        const res = await fetch(`${box.dataset.searchUrl}&q=${encodeURIComponent(q)}`, { signal: controller.signal });
        results.innerHTML = await res.text();
        list.hidden = true;
      } catch (e) {
        if (e.name !== 'AbortError') throw e;
      }
    }, 250);
  });
})();
