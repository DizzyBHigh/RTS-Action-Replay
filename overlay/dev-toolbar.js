(() => {
  const params = new URLSearchParams(window.location.search);
  if (params.get('dev') !== 'true') return;

  const bar = document.createElement('div');
  bar.id = 'rts-dev-toolbar';
  bar.innerHTML = `
    <span class="dev-label">RTS DEV</span>
    <button data-style="broadcast">Broadcast</button>
    <button data-style="cinematic">Cinematic</button>
    <button data-style="cut">Cut</button>
    <button data-style="minimal">Minimal</button>
    <button data-position="top">Top</button>
    <button data-position="bottom">Bottom</button>
    <input id="rts-dev-title" value="FIRST TEST — REPLAY CAPTURE" aria-label="Preview title">
    <button data-action="show">Show</button>
    <button data-action="hide">Hide</button>
    <span class="spacer"></span>
    <span class="hint">?dev=true</span>
  `;
  document.body.prepend(bar);

  let style = 'broadcast';
  let position = 'bottom';

  const preview = () => {
    const title = RTSReplay?.title;
    if (!title) return;
    RTSReplay.hideTitle();
    const text = document.getElementById('rts-dev-title').value.trim() || 'FIRST TEST — REPLAY CAPTURE';
    title.className = `title-${style} title-${position}`;
    title.textContent = text;
    title.classList.add('visible', 'title-enter');
  };

  bar.addEventListener('click', event => {
    const button = event.target.closest('button');
    if (!button) return;
    if (button.dataset.style) {
      style = button.dataset.style;
      bar.querySelectorAll('[data-style]').forEach(item => item.classList.toggle('active', item === button));
      preview();
    }
    if (button.dataset.position) {
      position = button.dataset.position;
      bar.querySelectorAll('[data-position]').forEach(item => item.classList.toggle('active', item === button));
      preview();
    }
    if (button.dataset.action === 'show') preview();
    if (button.dataset.action === 'hide') RTSReplay.hideTitle();
  });

  bar.querySelector('[data-style="broadcast"]').classList.add('active');
  bar.querySelector('[data-position="bottom"]').classList.add('active');
})();
