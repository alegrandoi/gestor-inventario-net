import { PropsWithChildren } from 'react';
import { Sidebar } from './sidebar';
import { Topbar } from './topbar';

export function AppShell({ children }: PropsWithChildren) {
  return (
    <div className="flex min-h-screen bg-slate-50">
      <Sidebar />
      <div className="flex flex-1 flex-col">
        <Topbar />
        <div className="flex-1 overflow-y-auto bg-slate-50">
          <div className="mx-auto flex w-full max-w-7xl flex-col gap-6 px-6 py-8 lg:px-10">{children}</div>
        </div>
      </div>
    </div>
  );
}
