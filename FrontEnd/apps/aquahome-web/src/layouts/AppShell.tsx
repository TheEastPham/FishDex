import { NavLink, Outlet } from 'react-router-dom';
import { LayoutDashboard, Search, MessageCircle, Camera, Fish, LogOut } from 'lucide-react';
import { cn } from '@/lib/utils';
import { useLogout } from '@/hooks/useLogout';

const navItems = [
  { to: '/dashboard',     icon: LayoutDashboard, label: 'Dashboard' },
  { to: '/fish',          icon: Search,          label: 'Fish Search' },
  { to: '/ai-chat',       icon: MessageCircle,   label: 'AI Chat' },
  { to: '/image-search',  icon: Camera,          label: 'Image Search' },
];

export default function AppShell() {
  const logout = useLogout();

  return (
    <div className="flex h-screen overflow-hidden bg-slate-50">

      {/* ── Sidebar ── */}
      <aside className="w-60 flex-shrink-0 flex flex-col bg-sidebar border-r border-sidebar-border">

        {/* Logo */}
        <div className="flex items-center gap-2.5 px-5 py-4 border-b border-sidebar-border">
          <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-primary">
            <Fish className="h-4 w-4 text-primary-foreground" />
          </div>
          <span className="font-semibold text-sidebar-foreground">AquaHome</span>
        </div>

        {/* Nav items */}
        <nav className="flex-1 px-3 py-3 space-y-0.5">
          {navItems.map(({ to, icon: Icon, label }) => (
            <NavLink
              key={to}
              to={to}
              className={({ isActive }) =>
                cn(
                  'flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-colors',
                  isActive
                    ? 'bg-sidebar-accent text-sidebar-accent-foreground'
                    : 'text-muted-foreground hover:bg-slate-100 hover:text-sidebar-foreground'
                )
              }
            >
              <Icon className="h-4 w-4 flex-shrink-0" />
              {label}
            </NavLink>
          ))}
        </nav>

        {/* Logout */}
        <div className="px-3 py-3 border-t border-sidebar-border">
          <button
            onClick={logout}
            className="flex w-full items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium text-muted-foreground hover:bg-slate-100 hover:text-sidebar-foreground transition-colors"
          >
            <LogOut className="h-4 w-4 flex-shrink-0" />
            Đăng xuất
          </button>
        </div>
      </aside>

      {/* ── Main content ── */}
      <div className="flex flex-1 flex-col min-w-0 overflow-hidden">
        <main className="flex-1 overflow-auto p-6">
          <Outlet />
        </main>
      </div>

    </div>
  );
}
