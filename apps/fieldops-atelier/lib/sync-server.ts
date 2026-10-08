import {normalizeOperation,advanceClock,idleClock,type Operation,type RecordItem,type Clock} from './domain';
export class SyncError extends Error {constructor(message:string,public status=409){super(message);}}
type Row={id:string;kind:RecordItem['kind'];project_id:string|null;data:string;version:number;deleted:number;updated_at:string};
export function rowToRecord(row:Row):RecordItem{return {id:row.id,kind:row.kind,projectId:row.project_id,data:JSON.parse(row.data),version:row.version,deleted:!!row.deleted,updatedAt:row.updated_at};}
export async function applyOperation(db:D1Database,owner:string,input:unknown):Promise<{id:string;version:number;duplicate:boolean}> {
 let op:Operation;try{op=normalizeOperation(input);}catch(error){throw new SyncError(error instanceof Error?error.message:'Données invalides.',422);}
 const digest=Array.from(new Uint8Array(await crypto.subtle.digest('SHA-256',new TextEncoder().encode(JSON.stringify(op))))).map(n=>n.toString(16).padStart(2,'0')).join('');
 const previous=await db.prepare('SELECT digest FROM operations WHERE owner=? AND id=?').bind(owner,op.id).first<{digest:string}>();
 if(previous){if(previous.digest!==digest)throw new SyncError('Identifiant de synchronisation réutilisé avec des données différentes.');return {id:op.id,version:op.expectedVersion+1,duplicate:true};}
 const current=await db.prepare('SELECT kind,project_id,deleted FROM records WHERE owner=? AND id=?').bind(owner,op.entityId).first<{kind:string;project_id:string|null;deleted:number}>();
 if(current&&(current.kind!==op.kind||current.project_id!==op.projectId||current.deleted))throw new SyncError('Cet élément a changé ou est archivé.');
 if(op.projectId){const project=await db.prepare("SELECT id FROM records WHERE owner=? AND id=? AND kind='project' AND deleted=0").bind(owner,op.projectId).first();if(!project)throw new SyncError('Le chantier n’est pas disponible sur le serveur.');}
 let nextClock:Clock|null=null;
 if(op.kind==='punch'){
  if(op.data.sequence!==op.expectedClockVersion!+1)throw new SyncError('Séquence de pointage invalide.',422);
  const row=await db.prepare('SELECT version,data FROM clocks WHERE owner=?').bind(owner).first<{version:number;data:string}>();
  if((row?.version||0)!==op.expectedClockVersion)throw new SyncError('Un autre appareil a modifié le pointage. Exportez les données et vérifiez la session.');
  if(Date.parse(String(op.data.capturedAt))>Date.now()+300000)throw new SyncError('L’heure de ce pointage est dans le futur. Vérifiez le téléphone.',422);
  try{nextClock=advanceClock(row?JSON.parse(row.data):idleClock,op.projectId!,String(op.data.event),String(op.data.capturedAt));}catch(e){throw new SyncError(e instanceof Error?e.message:'Session invalide.');}
 }
 const now=new Date().toISOString();
 const guard=op.kind==='punch'?' AND COALESCE((SELECT version FROM clocks WHERE owner=?),0)=?':'';
 const guardParams=op.kind==='punch'?[owner,op.expectedClockVersion!]:[];
 const claim=db.prepare('INSERT INTO operations(owner,id,entity_id,digest,received_at) SELECT ?,?,?,?,? WHERE COALESCE((SELECT version FROM records WHERE owner=? AND id=?),0)=?'+guard+' ON CONFLICT(owner,id) DO NOTHING').bind(owner,op.id,op.entityId,digest,now,owner,op.entityId,op.expectedVersion,...guardParams);
 const write=db.prepare('INSERT INTO records(owner,id,kind,project_id,data,version,deleted,updated_at) SELECT ?,?,?,?,?,?,?,? WHERE EXISTS(SELECT 1 FROM operations WHERE owner=? AND id=? AND digest=?) ON CONFLICT(owner,id) DO UPDATE SET data=excluded.data,version=excluded.version,deleted=excluded.deleted,updated_at=excluded.updated_at WHERE records.version=?').bind(owner,op.entityId,op.kind,op.projectId,JSON.stringify(op.data),op.expectedVersion+1,op.deleted?1:0,now,owner,op.id,digest,op.expectedVersion);
 const statements=[claim,write];
 if(nextClock)statements.push(db.prepare('INSERT INTO clocks(owner,version,data) SELECT ?,?,? WHERE EXISTS(SELECT 1 FROM operations WHERE owner=? AND id=? AND digest=?) ON CONFLICT(owner) DO UPDATE SET version=excluded.version,data=excluded.data WHERE clocks.version=?').bind(owner,op.expectedClockVersion!+1,JSON.stringify(nextClock),owner,op.id,digest,op.expectedClockVersion!));
 const result=await db.batch(statements);
 if(result[0].meta.changes===0){const known=await db.prepare('SELECT digest FROM operations WHERE owner=? AND id=?').bind(owner,op.id).first<{digest:string}>();if(known?.digest===digest)return {id:op.id,version:op.expectedVersion+1,duplicate:true};throw new SyncError('Conflit de version : la modification reste sauvegardée sur cet appareil.');}
 return {id:op.id,version:op.expectedVersion+1,duplicate:false};
}
