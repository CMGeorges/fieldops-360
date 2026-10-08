import {boundedBody,BodyLimitError} from '@/lib/http-body';
import {env} from 'cloudflare:workers';
import {getChatGPTUser} from '../../../chatgpt-auth';
const uuid=/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
function reply(error:string,status:number){return Response.json({error},{status,headers:{'Cache-Control':'no-store'}});}
export async function PUT(req:Request,context:{params:Promise<{id:string}>}){
 const user=await getChatGPTUser();if(!user)return reply('Connexion requise.',401);
 const {id}=await context.params;if(!uuid.test(id))return reply('Identifiant invalide.',400);
 const origin=req.headers.get('origin');if(origin&&origin!==new URL(req.url).origin)return reply('Origine refusée.',403);
 const projectId=req.headers.get('x-project-id');if(!projectId||!uuid.test(projectId))return reply('Chantier requis.',422);
 const project=await env.DB.prepare("SELECT id FROM records WHERE owner=? AND id=? AND kind='project' AND deleted=0").bind(user.userId,projectId).first();if(!project)return reply('Chantier indisponible.',404);
 const mime=req.headers.get('content-type')||'';if(!['image/jpeg','image/png','image/webp'].includes(mime))return reply('Format photo non pris en charge. Utilisez JPEG, PNG ou WebP.',415);
 if(Number(req.headers.get('content-length')||0)>10485760)return reply('Photo limitée à 10 Mo.',413);
 let buffer:ArrayBuffer;try{const body=await boundedBody(req,10485760);buffer=body.buffer as ArrayBuffer;}catch(e){if(e instanceof BodyLimitError)return reply('Photo limitée à 10 Mo.',413);throw e;}if(!buffer.byteLength||buffer.byteLength>10485760)return reply('Photo limitée à 10 Mo.',413);
 const b=new Uint8Array(buffer);const jpg=b[0]===255&&b[1]===216&&b[2]===255;const png=b[0]===137&&b[1]===80&&b[2]===78&&b[3]===71;const webp=new TextDecoder().decode(b.slice(0,4))==='RIFF'&&new TextDecoder().decode(b.slice(8,12))==='WEBP';if(!((mime==='image/jpeg'&&jpg)||(mime==='image/png'&&png)||(mime==='image/webp'&&webp)))return reply('Le contenu ne correspond pas au format de la photo.',415);
 const digest=Array.from(new Uint8Array(await crypto.subtle.digest('SHA-256',buffer))).map(n=>n.toString(16).padStart(2,'0')).join('');const key=`${encodeURIComponent(user.userId)}/${id}`;
 const existing=await env.BUCKET.head(key);if(existing&&existing.customMetadata?.digest!==digest)return reply('Une autre photo porte cet identifiant.',409);
 if(!existing){const saved=await env.BUCKET.put(key,buffer,{httpMetadata:{contentType:mime},customMetadata:{digest,projectId},onlyIf:{etagDoesNotMatch:'*'}});if(!saved){const competing=await env.BUCKET.head(key);if(competing?.customMetadata?.digest!==digest)return reply('Une autre photo porte cet identifiant.',409);}}
 return Response.json({id},{headers:{'Cache-Control':'no-store'}});
}
export async function GET(req:Request,context:{params:Promise<{id:string}>}){
 const user=await getChatGPTUser();if(!user)return reply('Connexion requise.',401);
 const {id}=await context.params;if(!uuid.test(id))return reply('Identifiant invalide.',400);
 const row=await env.DB.prepare("SELECT id FROM records WHERE owner=? AND kind='photo' AND deleted=0 AND json_extract(data,'$.fileId')=?").bind(user.userId,id).first();if(!row)return reply('Photo introuvable.',404);
 const object=await env.BUCKET.get(`${encodeURIComponent(user.userId)}/${id}`);if(!object)return reply('Photo introuvable.',404);
 return new Response(object.body,{headers:{'Content-Type':object.httpMetadata?.contentType||'application/octet-stream','Cache-Control':'private, no-store','X-Content-Type-Options':'nosniff'}});
}
