import { readFileSync } from 'node:fs';
import { join } from 'node:path';

const schemaPath = join(process.cwd(), '..', '..', '..', 'docs', 'database-schema.json');
const schema = JSON.parse(readFileSync(schemaPath, 'utf-8'));

type Column = {
  name: string;
  type: string;
  description: string;
  primaryKey?: boolean;
  foreignKey?: boolean;
  nullable: boolean;
  references?: string;
};

type Table = {
  description: string;
  columns: Column[];
};

type DatabaseSchema = {
  metadata: {
    title: string;
    description: string;
    source: string;
    lastUpdated: string;
  };
  tables: Record<string, Table>;
};

export const databaseSchema = schema as DatabaseSchema;

export const featuredTables = ['Products', 'Variants', 'InventoryStock', 'SalesOrders', 'PurchaseOrders'];
