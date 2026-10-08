export class BodyLimitError extends Error {}
export async function boundedBody(req:Request,limit:number):Promise<Uint8Array>{
 if(!req.body)return new Uint8Array();
 const reader=req.body.getReader();const chunks:Uint8Array[]=[];let size=0;
 try{while(true){const {done,value}=await reader.read();if(done)break;size+=value.byteLength;if(size>limit){await reader.cancel();throw new BodyLimitError('Requête trop volumineuse.');}chunks.push(value);}}finally{reader.releaseLock();}
 const body=new Uint8Array(size);let offset=0;for(const part of chunks){body.set(part,offset);offset+=part.byteLength;}return body;
}
