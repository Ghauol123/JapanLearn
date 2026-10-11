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
  const button = document.getElementById('theme-toggle');
  button?.addEventListener('click', () => {
    const next = document.documentElement.dataset.bsTheme === 'dark' ? 'light' : 'dark';
    document.documentElement.dataset.bsTheme = next;
    localStorage.setItem('theme', next);
  });
})();

(() => {
  const typing = (el) => el instanceof HTMLElement && (el.isContentEditable || /^(INPUT|TEXTAREA|SELECT)$/.test(el.tagName));

  document.addEventListener('keydown', (e) => {
    if (e.ctrlKey || e.metaKey || e.altKey || typing(e.target)) return;
    if (e.key === '/') {
      const input = document.querySelector('input[type=search]');
      if (input) {
        e.preventDefault();
        input.focus();
        input.select();
      }
    } else if (e.key === 'ArrowLeft' || e.key === 'ArrowRight') {
      const link = document.querySelector(e.key === 'ArrowLeft' ? 'a[rel=prev]' : 'a[rel=next]');
      if (link) location.href = link.href;
    }
  });
})();

(() => {
  const links = [...document.querySelectorAll('.toc a[href^="#"]')];
  const sections = links.map((a) => document.getElementById(a.getAttribute('href').slice(1)));
  if (links.length === 0 || sections.includes(null)) return;

  let frame = 0;
  let pinnedUntil = 0;
  const setActive = (current) => links.forEach((a, i) => a.classList.toggle('active', i === current));
  const update = () => {
    frame = 0;
    if (Date.now() < pinnedUntil) return;
    let current = 0;
    sections.forEach((s, i) => {
      if (s.getBoundingClientRect().top < 140) current = i;
    });
    setActive(current);
  };

  links.forEach((a, i) => a.addEventListener('click', () => {
    pinnedUntil = Date.now() + 1000;
    setActive(i);
  }));
  addEventListener('scroll', () => { frame ||= requestAnimationFrame(update); }, { passive: true });
  update();
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
      controller?.abort();
      if (!q) {
        results.innerHTML = '';
        list.hidden = false;
        return;
      }
      controller = new AbortController();
      try {
        const res = await fetch(`${box.dataset.searchUrl}&q=${encodeURIComponent(q)}`, { signal: controller.signal });
        if (!res.ok) return;
        const html = (await res.text()).trim();
        results.innerHTML = html;
        list.hidden = html.length > 0;
      } catch (e) {
        if (e.name !== 'AbortError') throw e;
      }
    }, 250);
  });
})();
