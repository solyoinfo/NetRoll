// Aggregator site.js (after modular split). Provides fallback shims and version flag.
window.netrollAggregatorVersion = 'aggregator-1.0.1';
// Expect these to be loaded earlier:
//  /js/site-core.js  (sidebar, showToast, confirmModal, netrollStore)
//  /js/drag-categories.js (netrollDrag)
//  /js/cropper.js (netrollCrop)

function __nrWarn(m){ try{ console.warn('[site.js]', m); }catch(e){} }
if(!window.sidebar) __nrWarn('sidebar module missing');
if(!window.netrollDrag) __nrWarn('netrollDrag missing');
if(!window.netrollCrop) __nrWarn('netrollCrop missing');

// Fallback minimal store
window.netrollStore = window.netrollStore || { get:k=>{try{return localStorage.getItem(k);}catch(e){return null;}}, set:(k,v)=>{try{localStorage.setItem(k,String(v));}catch(e){}}, del:k=>{try{localStorage.removeItem(k);}catch(e){}} };
// Fallback toast
window.showToast = window.showToast || function(o){ try{ console.log('[toast]', o); }catch(e){} };
// Fallback confirm
window.confirmModal = window.confirmModal || function(msg){ return Promise.resolve(window.confirm(msg||'Biztos?')); };
// File helpers fallback
window.netrollFiles = window.netrollFiles || { count: el=>{ try{return (el && el.files)? el.files.length:0;}catch(e){return 0;} }, name:(el,i)=>{ try{ const f=el?.files?el.files[i]:null; return f?f.name:null;}catch(e){return null;} }, readAsBase64:(el,i)=>{ try{ const f=el?.files?el.files[i]:null; if(!f) return Promise.resolve(null); return new Promise(res=>{ const r=new FileReader(); r.onload=()=>{ const s=r.result; if(typeof s==='string'){ const p=s.indexOf(','); res(p>=0?s.substring(p+1):s);} else res(null); }; r.onerror=()=>res(null); r.readAsDataURL(f); }); }catch(e){ return Promise.resolve(null);} }, clear: el=>{ try{ if(el) el.value=''; }catch(e){} } };
// Upload/API fallbacks
window.netrollUpload = window.netrollUpload || { post: async function(inputEl,url){ try{ if(!inputEl||!inputEl.files||inputEl.files.length===0) return { ok:false,status:0,text:'Nincs fájl' }; const fd=new FormData(); for(let i=0;i<inputEl.files.length;i++){ fd.append('files', inputEl.files[i], inputEl.files[i].name);} const res=await fetch(url||'/api/images/upload',{ method:'POST', body:fd, credentials:'include'}); let text=''; if(!res.ok){ try{text=await res.text();}catch(e){text='';} } return { ok:res.ok,status:res.status,text }; }catch(e){ return { ok:false,status:0,text:String(e?.message||e) }; } } };
window.netrollApi = window.netrollApi || { post: async function(url,data){ try{ const res=await fetch(url,{ method:'POST', headers:{'Content-Type':'application/json'}, credentials:'include', body:JSON.stringify(data||{})}); let text=''; if(!res.ok){ try{text=await res.text();}catch(e){text='';} } return { ok:res.ok,status:res.status,text,url }; }catch(e){ return { ok:false,status:0,text:String(e?.message||e) }; } } };

// Signal readiness
try{ window.dispatchEvent(new CustomEvent('netroll:modules-ready')); }catch(e){}
