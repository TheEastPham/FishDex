import { NavLink, Outlet } from 'react-router-dom';
import { LayoutDashboard, Search, MessageCircle, Camera, Fish, LogOut, ChevronRight } from 'lucide-react';
import { cn, useLogout, useTranslation, LanguageSwitcher } from '@fishlover/shared';

export default function AppShell() {
  const logout = useLogout();
  const { t } = useTranslation();

  const navItems = [
    { to: '/dashboard',    icon: LayoutDashboard, label: t('nav.dashboard') },
    { to: '/fish',         icon: Search,          label: t('nav.fishSearch') },
    { to: '/ai-chat',      icon: MessageCircle,   label: t('nav.aiChat') },
    { to: '/image-search', icon: Camera,          label: t('nav.imageSearch') },
  ];

  return (
    <div className="flex h-screen w-full bg-slate-50/80 selection:bg-primary/20">

      {/* ── Sidebar ── */}
      <aside className="w-[260px] flex-shrink-0 flex flex-col bg-white border-r border-slate-200/60 shadow-[4px_0_24px_rgba(0,0,0,0.01)] z-10 transition-all duration-300">
        
        {/* Logo */}
        <div className="flex items-center gap-3 px-6 py-6 border-b border-slate-100">
          <div className="flex h-9 w-9 items-center justify-center rounded-xl bg-gradient-to-br from-primary to-blue-600 shadow-sm shadow-primary/30">
            <Fish className="h-5 w-5 text-white stroke-[2.5]" />
          </div>
          <span className="font-bold text-lg tracking-tight bg-gradient-to-r from-slate-900 to-slate-700 bg-clip-text text-transparent">
            AquaHome
          </span>
        </div>

        {/* Nav items */}
        <nav className="flex-1 px-3 py-5 space-y-1.5 overflow-y-auto">
          {navItems.map(({ to, icon: Icon, label }) => (
            <NavLink
              key={to}
              to={to}
              className={({ isActive }) =>
                cn(
                  'group relative flex items-center justify-between px-3 py-2.5 rounded-xl text-sm font-medium transition-all duration-200 overflow-hidden',
                  isActive
                    ? 'bg-primary/5 text-primary'
                    : 'text-slate-500 hover:bg-slate-50 hover:text-slate-900'
                )
              }
            >
              {({ isActive }) => (
                <>
                  <div className="flex items-center gap-3">
                    <div className={cn(
                      "flex items-center justify-center transition-transform duration-200",
                      isActive ? "scale-110" : "group-hover:scale-110"
                    )}>
                      <Icon className={cn(
                        "h-[18px] w-[18px] transition-colors duration-200",
                        isActive ? "stroke-[2.5]" : "stroke-2"
                      )} />
                    </div>
                    <span>{label}</span>
                  </div>
                  
                  {/* Active Indicator Line */}
                  <div className={cn(
                    "absolute left-0 top-1/2 -translate-y-1/2 w-1 h-1/2 rounded-r-full bg-primary transition-all duration-300",
                    isActive ? "opacity-100" : "opacity-0 -translate-x-full"
                  )} />
                  
                  {isActive && (
                    <ChevronRight className="h-4 w-4 opacity-50" />
                  )}
                </>
              )}
            </NavLink>
          ))}
        </nav>

        {/* Footer: language switcher + logout */}
        <div className="p-4 border-t border-slate-100 flex flex-col gap-2">
          <div className="flex items-center gap-2">
            <LanguageSwitcher
              className="flex-1 flex items-center justify-center rounded-xl border border-slate-200 bg-white px-3 py-2 text-xs font-bold tracking-wider text-slate-600 hover:bg-slate-50 hover:text-slate-900 transition-all shadow-sm"
            />
            <button
              onClick={logout}
              className="flex-1 group flex items-center justify-center gap-2 rounded-xl border border-slate-200 bg-white px-3 py-2 text-xs font-bold text-slate-600 hover:border-red-200 hover:bg-red-50 hover:text-red-600 transition-all shadow-sm"
            >
              <LogOut className="h-[14px] w-[14px] group-hover:scale-110 transition-transform duration-200" />
              <span>{t('nav.logout')}</span>
            </button>
          </div>
          <div className="text-center pt-2">
            <span className="text-[10px] text-slate-400 font-mono tracking-wider">v1.0.124</span>
          </div>
        </div>
      </aside>

      {/* ── Main content ── */}
      <div className="flex flex-1 flex-col min-w-0 overflow-hidden relative">
        {/* Subtle background glow/decoration */}
        <div className="absolute top-0 left-0 w-full h-64 bg-gradient-to-b from-primary/[0.03] to-transparent pointer-events-none" />
        
        <main className="flex-1 overflow-auto p-6 md:p-8 z-0">
          <div className="mx-auto max-w-7xl h-full">
            <Outlet />
          </div>
        </main>
      </div>

    </div>
  );
}
