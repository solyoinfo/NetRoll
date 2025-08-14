// Core UI helpers (sidebar, toast, confirm, local store)
window.netrollVersion = 'core-1.0.0';
(function(){
  function apply(initial){
    try{ const saved = localStorage.getItem('sidebar-collapsed'); const collapsed = (initial && saved!==null)? saved==='true' : document.body.classList.contains('sidebar-collapsed'); document.body.classList.toggle('sidebar-collapsed', collapsed);}catch(e){}
  }
  window.sidebar = { toggle: function(){ try{ const collapsed = !document.body.classList.contains('sidebar-collapsed'); document.body.classList.toggle('sidebar-collapsed', collapsed); localStorage.setItem('sidebar-collapsed', collapsed?'true':'false'); }catch(e){} }, init: function(){ apply(true);} };
  document.addEventListener('DOMContentLoaded', ()=>{ window.sidebar.init(); });
})();

(function(){
  function ensureToastContainer(){ let c=document.getElementById('toast-container'); if(!c){ c=document.createElement('div'); c.id='toast-container'; c.className='toast-container position-fixed top-0 end-0 p-3'; document.body.appendChild(c);} return c; }
  window.showToast = function(o){ try{ o=o||{}; const container=ensureToastContainer(); const toastEl=document.createElement('div'); toastEl.className='toast align-items-center text-bg-'+(o.type||'primary')+' border-0'; toastEl.setAttribute('role','alert'); toastEl.setAttribute('aria-live','assertive'); toastEl.setAttribute('aria-atomic','true'); toastEl.innerHTML = '<div class="d-flex">'+
    '<div class="toast-body">'+ (o.title?'<div class="fw-bold">'+o.title+'</div>':'') + (o.body||'') +'</div>'+
    '<button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>'+
    '</div>'; container.appendChild(toastEl); if(o.undoLabel&&o.undoMethod){ try{ const b=document.createElement('button'); b.type='button'; b.className='btn btn-light btn-sm ms-2'; b.textContent=o.undoLabel; b.addEventListener('click',()=>{ try{o.undoMethod();}catch(e){} }); toastEl.querySelector('.toast-body').appendChild(b);}catch(e){} }
    try{ const bsToast = new bootstrap.Toast(toastEl, { delay:o.delay||3000 }); bsToast.show(); }catch(e){ toastEl.style.display='block'; setTimeout(()=>{ try{toastEl.remove();}catch(e){} }, o.delay||3000); }
  }catch(e){} };
})();

window.confirmModal = function(message){ return new Promise(function(resolve){ try{ if(!window.bootstrap){ resolve(window.confirm(message||'Biztos?')); return; } let m=document.getElementById('confirmModal'); if(!m){ m=document.createElement('div'); m.id='confirmModal'; m.className='modal fade'; m.innerHTML='\n<div class="modal-dialog modal-dialog-centered">\n <div class="modal-content">\n  <div class="modal-header"><h5 class="modal-title">Megerősítés</h5><button type="button" class="btn-close" data-bs-dismiss="modal"></button></div>\n  <div class="modal-body"><p id="confirmModalMessage"></p></div>\n  <div class="modal-footer">\n    <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Mégse</button>\n    <button type="button" class="btn btn-primary" id="confirmModalOk">OK</button>\n  </div>\n </div>\n</div>'; document.body.appendChild(m);} m.querySelector('#confirmModalMessage').textContent=message||''; const ok=m.querySelector('#confirmModalOk'); function cleanup(r){ try{ ok.removeEventListener('click', onOk);}catch(e){} resolve(r);} function onOk(){ cleanup(true); try{ modal.hide(); }catch(e){} } ok.addEventListener('click', onOk); const modal=new bootstrap.Modal(m); modal.show(); m.addEventListener('hidden.bs.modal', function handler(){ m.removeEventListener('hidden.bs.modal', handler); cleanup(false); }); }catch(e){ resolve(window.confirm(message||'Biztos?')); } }); };

window.netrollStore = { get: k=>{ try{return localStorage.getItem(k);}catch(e){return null;} }, set:(k,v)=>{ try{localStorage.setItem(k,String(v));}catch(e){} }, del:k=>{ try{localStorage.removeItem(k);}catch(e){} } };
