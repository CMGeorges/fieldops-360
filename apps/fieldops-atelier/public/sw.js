const CACHE='fieldops-shell-v1';
self.addEventListener('install',event=>event.waitUntil(caches.open(CACHE).then(async cache=>{const response=await fetch('/offline-assets.json',{cache:'no-store'});if(!response.ok)throw new Error('Offline manifest missing');const {assets}=await response.json();await cache.addAll(['/offline','/icon.svg','/manifest.webmanifest',...assets]);}).then(()=>self.skipWaiting())));
self.addEventListener('activate',event=>event.waitUntil(caches.keys().then(keys=>Promise.all(keys.filter(key=>key!==CACHE).map(key=>caches.delete(key)))).then(()=>self.clients.claim())));
self.addEventListener('fetch',event=>{
 const req=event.request,url=new URL(req.url);
 if(req.method!=='GET'||url.origin!==self.location.origin||url.pathname.startsWith('/api/')||url.pathname.includes('signin')||url.pathname.includes('signout')||url.pathname==='/callback')return;
 if(req.mode==='navigate'){event.respondWith(fetch(req).catch(()=>caches.match('/offline').then(response=>response||Response.error())));return;}
 if(['script','style','font'].includes(req.destination)||url.pathname==='/offline'){event.respondWith(caches.match(req).then(cached=>cached||fetch(req).then(response=>{if(response.ok)caches.open(CACHE).then(cache=>cache.put(req,response.clone()));return response;})));}
});
