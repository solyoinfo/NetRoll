// Category drag & drop (simplified single-event version)
window.netrollDragVersion = 'drag-1.0.0';
window.netrollDrag = {
  init: function(rootSelector){
    try{
      const root = document.querySelector(rootSelector);
      if(!root) return;
      function ensureSortable(cb){ if(window.Sortable){ cb(); return; } const s=document.createElement('script'); s.src='https://cdn.jsdelivr.net/npm/sortablejs@1.15.2/Sortable.min.js'; s.onload=cb; document.head.appendChild(s); }
      ensureSortable(function(){
        root.querySelectorAll('.sortable').forEach(function(ul){
          if(ul.__sortable){ try{ ul.__sortable.option('disabled', false);}catch(e){} return; }
          ul.__sortable = new Sortable(ul, {
            group:{ name:'cats', pull:true, put:true },
            animation:150,
            handle: '.drag-handle, .handle',
            draggable:'li',
            dataIdAttr:'data-id',
            direction:'vertical',
            chosenClass:'sortable-chosen',
            ghostClass:'drag-ghost',
            dragClass:'sortable-drag',
            filter:'.btn, .btn *',
            preventOnFilter:true,
            onEnd:function(evt){
              try{
                if(window.__catsBusy) return;
                if(evt && evt.from===evt.to && evt.oldIndex===evt.newIndex) return;
                const list = evt && evt.to ? evt.to : ul;
                const parentAttr = list.getAttribute('data-parent-id');
                const newParentId = (parentAttr===''||parentAttr===null)? null : parseInt(parentAttr,10);
                const movedEl = evt && evt.item ? evt.item : null;
                const movedId = movedEl ? parseInt(movedEl.getAttribute('data-id')||'0',10):0;
                const newIndex = (typeof evt.newIndex==='number')? evt.newIndex : 0;
                if(!isNaN(movedId) && movedId>0){
                  window.__catsBusy = true;
                  window.dispatchEvent(new CustomEvent('cats:drop', { detail:{ movedItemId:movedId, newParentId, newIndex } }));
                  setTimeout(()=>{ window.__catsBusy=false; },300);
                }
              }catch(e){ console.warn('drag onEnd failed', e); }
            }
          });
        });
      });
    }catch(e){}
  },
  bindReorder: function(dotNetRef){
    try{
      if(!dotNetRef) return;
      window.__catsDotNetRef = dotNetRef;
      if(window.__catsDropListener){ try{ window.removeEventListener('cats:drop', window.__catsDropListener);}catch(_){} }
      window.__catsDropListener = function(e){
        try{ const d=e.detail||{}; if(d && typeof d.movedItemId==='number'){ (window.__catsDotNetRef||dotNetRef).invokeMethodAsync('OnCatsDrop', d.movedItemId, d.newParentId, d.newIndex); } }catch(err){ console.warn('cats:drop invoke failed', err); }
      };
      window.addEventListener('cats:drop', window.__catsDropListener);
    }catch(e){}
  }
};
