import {spawnSync} from 'node:child_process';
import {mkdirSync,writeFileSync,rmSync} from 'node:fs';
const dir='.test-build';rmSync(dir,{recursive:true,force:true});
const compile=spawnSync(process.execPath,['node_modules/typescript/bin/tsc','lib/domain.ts','lib/sync-server.ts','lib/http-body.ts','lib/local-store.ts','--module','commonjs','--target','ES2022','--outDir',dir,'--skipLibCheck','--types','node,@cloudflare/workers-types','--esModuleInterop','--strict'],{stdio:'inherit'});
if(compile.status)process.exit(compile.status);
mkdirSync(dir,{recursive:true});writeFileSync(dir+'/package.json','{"type":"commonjs"}');
const result=spawnSync(process.execPath,['--test','--test-concurrency=1','tests/domain.test.mjs','tests/sync.test.mjs','tests/local.test.mjs'],{stdio:'inherit'});process.exit(result.status??1);
