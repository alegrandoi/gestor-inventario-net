import type { Metadata } from 'next';
import { Inter } from 'next/font/google';
import clsx from 'clsx';
import './globals.css';
import { AuthProvider } from '../components/providers/auth-provider';

const inter = Inter({ subsets: ['latin'], variable: '--font-inter' });

export const metadata: Metadata = {
  title: 'Gestor de Inventario',
  description:
    'Suite web para controlar productos, inventario, pedidos y planificación de la demanda mediante una API .NET 8.'
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="es" className={clsx(inter.variable)}>
      <body className="bg-slate-50 text-slate-900 antialiased">
        <AuthProvider>
          <main className="flex min-h-screen flex-col">{children}</main>
        </AuthProvider>
      </body>
    </html>
  );
}
