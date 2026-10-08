import {boundedBody,BodyLimitError} from '@/lib/http-body';
import {env} from 'cloudflare:workers';
import {getChatGPTUser} from '../../chatgpt-auth';
import {normalizeOperation} from '@/lib/domain';
import {applyOperation,rowToRecord,SyncError} from '@/lib/sync-server';
export const dynamic='force-dynamic';
function reply(data:unknown,status=200){return Response.json(data,{status,headers:{'Cache-Control':'no-store'}});}
export async function GET(req:Request){
 const user=await getChatGPTUser();if(!user)return reply({error:'Connexion requise.'},401);
 const url=new URL(req.url),cursor=url.searchParams.get('cursor')||'';
 const rows=await env.DB.prepare('SELECT id,kind,project_id,data,version,deleted,updated_at FROM records WHERE owner=? AND id>? ORDER BY id LIMIT 201').bind(user.userId,cursor).all();
 const page=rows.results.slice(0,200);
 return reply({records:page.map(row=>rowToRecord(row as Parameters<typeof rowToRecord>[0])),nextCursor:rows.results.length>200?page[page.length-1].id:null});
}
export async function POST(req:Request){
 const user=await getChatGPTUser();if(!user)return reply({error:'Connexion requise.'},401);
 const origin=req.headers.get('origin');if(origin&&origin!==new URL(req.url).origin)return reply({error:'Origine refusée.'},403);
 if(Number(req.headers.get('content-length')||0)>200000)return reply({error:'Requête trop volumineuse.'},413);
 try{
  const text=new TextDecoder().decode(await boundedBody(req,200000));
  const body=JSON.parse(text);if(!Array.isArray(body.operations)||body.operations.length<1||body.operations.length>25)return reply({error:'Lot invalide.'},422);
  const results=[];
  for(const operation of body.operations){try{let normalized;try{normalized=normalizeOperation(operation);}catch(e){throw new SyncError(e instanceof Error?e.message:'Données invalides.',422);}if(normalized.kind==='photo'&&!normalized.deleted){const file=await env.BUCKET.head(`${encodeURIComponent(user.userId)}/${normalized.data.fileId}`);if(!file||file.customMetadata?.projectId!==normalized.projectId||file.size!==normalized.data.size||file.httpMetadata?.contentType!==normalized.data.mime)throw new SyncError('La photo doit être téléversée et correspondre au dossier.',422); }results.push(await applyOperation(env.DB,user.userId,operation));}catch(e){return reply({results,error:e instanceof Error?e.message:'Synchronisation indisponible.',failedId:operation.id},e instanceof SyncError?e.status:503);}}
  return reply({results});
 }catch(e){if(e instanceof BodyLimitError)return reply({error:e.message},413);return reply({error:e instanceof SyntaxError?'JSON invalide.':'Synchronisation indisponible.'},e instanceof SyntaxError?400:503);}
}
