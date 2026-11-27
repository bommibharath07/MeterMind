// Development-only PostgreSQL runtime. Production uses provisioned PostgreSQL.
import {existsSync} from 'node:fs';
import {fileURLToPath} from 'node:url';
const {default:EmbeddedPostgres}=await import('../.tools/node_modules/embedded-postgres/dist/index.js');
const directory=fileURLToPath(new URL('../.local/postgres',import.meta.url));
const postgres=new EmbeddedPostgres({databaseDir:directory,user:'metermind',password:'local-only',port:55432,persistent:true,postgresFlags:['-h','127.0.0.1'],onLog:()=>{},onError:console.error});
if(!existsSync(new URL('../.local/postgres/PG_VERSION',import.meta.url)))await postgres.initialise();
await postgres.start();const client=postgres.getPgClient('postgres','127.0.0.1');await client.connect();
for(const name of ['metermind','metermind_test'])if(!(await client.query('select 1 from pg_database where datname=$1',[name])).rowCount)await postgres.createDatabase(name);
await client.end();console.log('Isolated PostgreSQL ready on 127.0.0.1:55432. Ctrl+C to stop; data persists in .local/postgres.');
process.on('SIGINT',async()=>{await postgres.stop();process.exit()});setInterval(()=>{},60000);
