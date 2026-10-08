import {readdirSync,writeFileSync,readFileSync} from 'node:fs';import {join} from 'node:path';import {createHash} from 'node:crypto';
const root='dist/client';const assets=[];
function visit(dir){for(const entry of readdirSync(dir,{withFileTypes:true})){const file=join(dir,entry.name);if(entry.isDirectory())visit(file);else if(/\.(js|css|woff2?)$/.test(entry.name))assets.push('/'+file.slice(root.length+1));}}
visit(root);assets.sort();if(!assets.length)throw new Error('No compiled offline assets.');
const revision=createHash('sha256').update(assets.join('\n')).digest('hex').slice(0,16);
writeFileSync(join(root,'offline-assets.json'),JSON.stringify({revision,assets}));
const sw=readFileSync(join(root,'sw.js'),'utf8').replace('fieldops-shell-v1','fieldops-shell-'+revision);writeFileSync(join(root,'sw.js'),sw);
console.log(`Offline manifest: ${assets.length} compiled assets, revision ${revision}`);
