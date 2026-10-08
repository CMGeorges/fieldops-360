import {sqliteTable, text, integer, primaryKey, index} from 'drizzle-orm/sqlite-core';
export const records = sqliteTable('records', {
  owner: text('owner').notNull(), id: text('id').notNull(), kind: text('kind').notNull(),
  projectId: text('project_id'), data: text('data').notNull(), version: integer('version').notNull(),
  deleted: integer('deleted').notNull().default(0), updatedAt: text('updated_at').notNull(),
}, table => [primaryKey({columns:[table.owner,table.id]}), index('records_project').on(table.owner,table.projectId)]);
export const operations = sqliteTable('operations', {
  owner: text('owner').notNull(), id: text('id').notNull(), entityId: text('entity_id').notNull(),
  digest: text('digest').notNull(), receivedAt: text('received_at').notNull(),
}, table => [primaryKey({columns:[table.owner,table.id]})]);
export const clocks = sqliteTable('clocks', {
  owner: text('owner').primaryKey(), version: integer('version').notNull(), data: text('data').notNull(),
});
