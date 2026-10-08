import {test} from 'node:test';import assert from 'node:assert/strict';import {createRequire} from 'node:module';
const require=createRequire(import.meta.url);const {summarizePunches,quoteTotals,normalizeOperation,escapeHTML}=require('../.test-build/domain.js');const {boundedBody,BodyLimitError}=require('../.test-build/http-body.js');
const project=crypto.randomUUID();
test('Mes heures distinguent travail, pause et déplacement avec les horodatages d’origine',()=>{
 const events=[['start','08:00'],['pause','09:00'],['resume','09:15'],['travel','10:00'],['work','10:30'],['stop','11:00']];
 const records=events.map(([event,at],i)=>({id:crypto.randomUUID(),kind:'punch',projectId:project,data:{event,capturedAt:'2026-10-07T'+at+':00.000Z',sequence:i+1},deleted:false}));
 const result=summarizePunches(records.reverse());assert.equal(result.workMs,8100000);assert.equal(result.pauseMs,900000);assert.equal(result.travelMs,1800000);assert.equal(result.clock.status,'idle');
});
test('Mes événements à la même milliseconde suivent la séquence',()=>{const at='2026-10-07T08:00:00.000Z';const items=['start','pause','resume','stop'].map((event,i)=>({kind:'punch',projectId:project,data:{event,capturedAt:at,sequence:i+1}}));assert.equal(summarizePunches(items.reverse()).workMs,0);});
test('Mon devis calcule en cents et majore seulement les matériaux',()=>{assert.deepEqual(quoteTotals({lines:[{description:'Main œuvre',quantity:2.5,unitCents:7500,category:'labor',unit:'h'},{description:'Matériel',quantity:3,unitCents:1999,category:'material',unit:'u'}],markupPercent:20}),{subtotal:24747,markup:1199,total:25946});});
test('Mes pointages ne peuvent être modifiés ou supprimés',()=>{const op={id:crypto.randomUUID(),entityId:crypto.randomUUID(),kind:'punch',projectId:project,data:{},expectedVersion:1,expectedClockVersion:1,deleted:false};assert.throws(()=>normalizeOperation(op),/immuables/);assert.throws(()=>normalizeOperation({...op,expectedVersion:0,deleted:true}),/immuables/);});
test('Mon export neutralise le HTML provenant de mes notes',()=>{assert.equal(escapeHTML('<script>"&'), '&lt;script&gt;&quot;&amp;');});
test('Mon serveur arrête un corps trop grand sans dépendre de content-length',async()=>{await assert.rejects(()=>boundedBody(new Request('https://example.test',{method:'POST',body:'123456'}),5),BodyLimitError);assert.equal(new TextDecoder().decode(await boundedBody(new Request('https://example.test',{method:'POST',body:'12345'}),5)),'12345');});
