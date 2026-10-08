import {z} from 'zod';
const id = z.string().uuid();
const short = z.string().trim().min(1).max(200);
const room = id.optional();
const position = z.object({latitude:z.number().min(-90).max(90),longitude:z.number().min(-180).max(180),accuracy:z.number().min(0).max(100000)}).optional();
export const payloads = {
 project:z.object({name:short,address:z.string().max(500).default(''),client:z.string().max(200).default(''),status:z.enum(['planned','active','completed']).default('active')}),
 room:z.object({name:short,length:z.number().min(0).max(1000),width:z.number().min(0).max(1000),height:z.number().min(0).max(100)}),
 note:z.object({text:z.string().trim().min(1).max(10000),roomId:room,category:z.enum(['observation','work','damage']).default('observation')}),
 task:z.object({title:short,done:z.boolean()}),
 measurement:z.object({roomId:room,material:short,moisture:z.number().min(0).max(100),humidity:z.number().min(0).max(100),temperature:z.number().min(-50).max(80),notes:z.string().max(2000).default('')}),
 content:z.object({roomId:room,name:short,quantity:z.number().int().min(1).max(100000),condition:z.string().max(200).default('')}),
 equipment:z.object({name:short,serial:z.string().max(200).default(''),state:z.enum(['deployed','returned'])}),
 photo:z.object({fileId:id,name:short,mime:z.enum(['image/jpeg','image/png','image/webp']),size:z.number().int().min(1).max(10485760),caption:z.string().max(2000).default(''),roomId:room}),
 quote:z.object({lines:z.array(z.object({description:short,quantity:z.number().positive().max(100000),unitCents:z.number().int().min(0).max(100000000),category:z.enum(['labor','material']),unit:z.string().max(30)})).min(1).max(100),markupPercent:z.number().min(0).max(100),notes:z.string().max(10000).default('')}),
 appointment:z.object({title:short,startAt:z.string().datetime(),endAt:z.string().datetime()}).refine(v=>v.endAt>v.startAt,'La fin doit suivre le début.'),
 form:z.object({title:short,fields:z.array(z.object({label:short,value:z.string().max(2000)})).min(1).max(50)}),
 punch:z.object({event:z.enum(['start','pause','resume','travel','work','stop']),capturedAt:z.string().datetime(),sequence:z.number().int().min(1),position}),
} as const;
export type Kind = keyof typeof payloads;
export type Data = Record<string, unknown>;
export type RecordItem = {id:string;kind:Kind;projectId:string|null;data:Data;version:number;deleted:boolean;updatedAt:string};
export type Operation = {id:string;entityId:string;kind:Kind;projectId:string|null;data:Data;expectedVersion:number;expectedClockVersion?:number;deleted:boolean};
const operationSchema=z.object({id,entityId:id,kind:z.enum(Object.keys(payloads) as [Kind,...Kind[]]),projectId:id.nullable(),data:z.record(z.unknown()),expectedVersion:z.number().int().min(0),expectedClockVersion:z.number().int().min(0).optional(),deleted:z.boolean().default(false)});
export function normalizeOperation(value:unknown):Operation {
 const op=operationSchema.parse(value);
 if(op.kind==='project'&&op.projectId!==null) throw new Error('Un chantier ne dépend pas d’un autre chantier.');
 if(op.kind!=='project'&&!op.projectId) throw new Error('Le chantier est requis.');
 if(op.kind==='punch'&&(op.deleted||op.expectedVersion!==0||op.expectedClockVersion===undefined)) throw new Error('Les événements de pointage sont immuables.');
 return {...op,data:op.deleted?{}:payloads[op.kind].parse(op.data)} as Operation;
}
export type Clock = {status:'idle'|'work'|'pause'|'travel';projectId:string|null;lastAt:string|null};
export const idleClock:Clock={status:'idle',projectId:null,lastAt:null};
export function advanceClock(clock:Clock,projectId:string,event:string,at:string):Clock {
 if(!Number.isFinite(Date.parse(at))) throw new Error('Horodatage invalide.');
 if(clock.lastAt&&Date.parse(at)<Date.parse(clock.lastAt)) throw new Error('Le pointage est antérieur au précédent. Vérifiez l’heure du téléphone.');
 if(event==='start') {if(clock.status!=='idle') throw new Error('Une session est déjà ouverte.'); return {status:'work',projectId,lastAt:at};}
 if(clock.status==='idle'||clock.projectId!==projectId) throw new Error('Aucune session active sur ce chantier.');
 if(event==='stop') return {status:'idle',projectId:null,lastAt:at};
 if(event==='pause') {if(clock.status==='pause') throw new Error('La pause est déjà active.'); return {...clock,status:'pause',lastAt:at};}
 if(event==='resume') {if(clock.status!=='pause') throw new Error('Aucune pause à reprendre.');return {...clock,status:'work',lastAt:at};}
 if(event==='travel'||event==='work') {if(clock.status==='pause') throw new Error('Reprenez la session avant de changer d’activité.');return {...clock,status:event,lastAt:at};}
 throw new Error('Événement inconnu.');
}
export type TimeSummary={workMs:number;travelMs:number;pauseMs:number;clock:Clock;segments:{projectId:string;from:string;to:string;status:string;durationMs:number}[]};
export function summarizePunches(records:RecordItem[],now=new Date().toISOString()):TimeSummary {
 let clock:Clock={...idleClock};let workMs=0,travelMs=0,pauseMs=0;
 const segments:TimeSummary['segments']=[];
 const items=records.filter(r=>r.kind==='punch'&&!r.deleted).sort((a,b)=>Number(a.data.sequence)-Number(b.data.sequence));
 const add=(end:string)=>{if(clock.status==='idle'||!clock.lastAt||!clock.projectId)return;const ms=Math.max(0,Date.parse(end)-Date.parse(clock.lastAt));if(clock.status==='work')workMs+=ms;else if(clock.status==='travel')travelMs+=ms;else pauseMs+=ms;segments.push({projectId:clock.projectId,from:clock.lastAt,to:end,status:clock.status,durationMs:ms});};
 for(const r of items){add(String(r.data.capturedAt));clock=advanceClock(clock,r.projectId!,String(r.data.event),String(r.data.capturedAt));}
 add(now);return {workMs,travelMs,pauseMs,clock,segments};
}
export function quoteTotals(data:Data):{subtotal:number;markup:number;total:number} {
 const q=payloads.quote.parse(data);let subtotal=0,materials=0;
 for(const line of q.lines){const cents=Math.round(line.quantity*line.unitCents);subtotal+=cents;if(line.category==='material')materials+=cents;}
 const markup=Math.round(materials*q.markupPercent/100);return {subtotal,markup,total:subtotal+markup};
}
export function escapeHTML(value:unknown):string{return String(value??'').replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]!));}
