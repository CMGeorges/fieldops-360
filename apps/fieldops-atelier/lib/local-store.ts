'use client';
import {normalizeOperation,summarizePunches,advanceClock,type Operation,type RecordItem,type Kind,type Data} from './domain';
export type Queued=Operation&{order:number;fileId?:string};
export type LocalPhoto={id:string;blob:Blob;synced:boolean;projectId:string};
let connection:IDBDatabase|null=null;
let account='';
export function request<T>(req:IDBRequest<T>):Promise<T>{return new Promise((resolve,reject)=>{req.onsuccess=()=>resolve(req.result);req.onerror=()=>reject(req.error);});}
export function committed(tx:IDBTransaction):Promise<void>{return new Promise((resolve,reject)=>{tx.oncomplete=()=>resolve();tx.onerror=()=>reject(tx.error);tx.onabort=()=>reject(tx.error||new Error('Enregistrement local annulé.'));});}
export async function openLocal(owner:string){
 if(connection&&account===owner)return connection;if(connection)connection.close();account=owner;
 const req=indexedDB.open('fieldops-v1-'+owner,1);
 req.onupgradeneeded=()=>{const db=req.result;db.createObjectStore('records',{keyPath:'id'});const ops=db.createObjectStore('ops',{keyPath:'id'});ops.createIndex('order','order',{unique:true});db.createObjectStore('files',{keyPath:'id'});db.createObjectStore('meta',{keyPath:'key'});};
 connection=await request(req);return connection;
}
function db(){if(!connection)throw new Error('Stockage local non initialisé.');return connection;}
export async function allRecords(){return request(db().transaction('records').objectStore('records').getAll()) as Promise<RecordItem[]>;}
export async function allQueued(){return request(db().transaction('ops').objectStore('ops').index('order').getAll()) as Promise<Queued[]>;}
export async function getPhoto(id:string){return request(db().transaction('files').objectStore('files').get(id)) as Promise<LocalPhoto|undefined>;}
export async function storePhoto(photo:LocalPhoto){const tx=db().transaction('files','readwrite');const done=committed(tx);tx.objectStore('files').put(photo);await done;}
export async function saveLocal(kind:Kind,projectId:string|null,data:Data,existing?:RecordItem,file?:LocalPhoto,deleted=false):Promise<RecordItem>{
 const tx=db().transaction(['records','ops','files','meta'],'readwrite');const done=committed(tx);
 const items=tx.objectStore('records'),ops=tx.objectStore('ops'),meta=tx.objectStore('meta');
 const id=existing?.id||crypto.randomUUID();
 const oldRequest=items.get(id),recordsRequest=items.getAll(),orderRequest=meta.get('order');
 const [old,records,last]=await Promise.all([request(oldRequest),request(recordsRequest),request(orderRequest)]);
 const sequence=(records as RecordItem[]).filter(r=>r.kind==='punch').length;
 if(kind==='punch'){try{advanceClock(summarizePunches(records).clock,projectId!,String(data.event),String(data.capturedAt));}catch(e){tx.abort();await done.catch(()=>{});throw e;}}
 const normalized=normalizeOperation({id:crypto.randomUUID(),entityId:id,kind,projectId,data:kind==='punch'?{...data,sequence:sequence+1}:data,expectedVersion:old?.version||0,expectedClockVersion:kind==='punch'?sequence:undefined,deleted});
 const order=Math.max(Date.now(),(last?.value||0)+1);
 const record:RecordItem={id,kind,projectId,data:normalized.data,version:normalized.expectedVersion+1,deleted,updatedAt:new Date().toISOString()};
 items.put(record);ops.put({...normalized,order,fileId:file?.id});meta.put({key:'order',value:order});if(file)tx.objectStore('files').put(file);
 await done;return record;
}
export async function acknowledge(ids:string[]){const tx=db().transaction('ops','readwrite');const done=committed(tx);for(const id of ids)tx.objectStore('ops').delete(id);await done;}
export async function mergeCloud(records:RecordItem[]){
 const tx=db().transaction(['records','ops'],'readwrite');const done=committed(tx);
 const pending=await request(tx.objectStore('ops').getAll()) as Queued[];
 const protectedIds=new Set(pending.map(op=>op.entityId));
 for(const record of records)if(!protectedIds.has(record.id))tx.objectStore('records').put(record);
 await done;
}
async function api(url:string,options:RequestInit={},timeout=15000){const controller=new AbortController();const timer=setTimeout(()=>controller.abort(),timeout);try{return await fetch(url,{...options,signal:controller.signal,credentials:'same-origin',cache:'no-store'});}finally{clearTimeout(timer);}}
let inFlight:Promise<{pending:number;latency:number}>|null=null;
export function synchronize():Promise<{pending:number;latency:number}>{
 if(inFlight)return inFlight;
 inFlight=runSync().finally(()=>{inFlight=null;});return inFlight;
}
async function runSync():Promise<{pending:number;latency:number}>{
 const start=performance.now();
 try{
  const queued=await allQueued();
  for(const op of queued){
   if(op.fileId){const file=await getPhoto(op.fileId);if(!file)throw new Error('La photo locale manque. La modification reste en attente.');if(!file.synced){const response=await api('/api/files/'+file.id,{method:'PUT',headers:{'Content-Type':file.blob.type,'X-Project-Id':file.projectId},body:file.blob},25000);if(!response.ok){const error=await response.json() as {error?:string};throw new Error(error.error||'Téléversement indisponible.');}await storePhoto({...file,synced:true});}}
   const {order,fileId,...operation}=op;void order;void fileId;
   const response=await api('/api/sync',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({operations:[operation]})});
   const result=await response.json() as {results?:{id:string}[];error?:string};
   if(result.results?.length)await acknowledge(result.results.map((r:{id:string})=>r.id));
   if(!response.ok)throw new Error(result.error||'Synchronisation indisponible.');
  }
  let cursor:string|null='';do{const response=await api('/api/sync'+(cursor?'?cursor='+encodeURIComponent(cursor):''));const data=await response.json() as {records:RecordItem[];nextCursor:string|null;error?:string};if(!response.ok)throw new Error(data.error||'Connexion serveur indisponible.');await mergeCloud(data.records);cursor=data.nextCursor;}while(cursor);
  return {pending:(await allQueued()).length,latency:Math.round(performance.now()-start)};
 }finally{/* pending operations remain durable until acknowledged */}
}
