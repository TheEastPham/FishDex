import { useState, useEffect } from 'react';
import { NavLink, Outlet, useLocation } from 'react-router-dom';
import { 
  LayoutDashboard, Search, MessageCircle, Camera, Fish, LogOut, ChevronRight,
  Droplets, TestTube, Calendar, Heart, History, BookOpen, ChevronDown,
  FileText, Image as ImageIcon, Shield
} from 'lucide-react';
import { cn, useLogout, useTranslation, LanguageSwitcher } from '@fishlover/shared';

// -- Mock Roles for RBAC Demonstration --
// In reality, this would come from a global store/auth context (e.g., useAuth().roles)
type Role = 'ADMIN' | 'CONTENT_ADMIN' | 'USER';
const MOCK_USER_ROLES: Role[] = ['USER', 'CONTENT_ADMIN']; // Change this array to test RBAC

interface SubMenuItem {
  to: string;
  label: string;
  roles?: Role[];
}

interface NavItem {
  to?: string;
  icon: React.ElementType;
  label: string;
  roles?: Role[];
  subItems?: SubMenuItem[];
}

interface NavGroup {
  label: string | null;
  roles?: Role[];
  items: NavItem[];
}

export default function AppShell() {
  const logout = useLogout();
  const { t } = useTranslation();
  const location = useLocation();
  const [expandedMenus, setExpandedMenus] = useState<Record<string, boolean>>({});

  const navGroups: NavGroup[] = [
    {
      label: null,
      items: [
        { to: '/dashboard',    icon: LayoutDashboard, label: t('nav.dashboard') },
      ]
    },
    {
      label: t('nav.aquahomeGroup'),
      items: [
        { to: '/tanks',        icon: Droplets,        label: t('nav.myTanks') },
        { to: '/parameters',   icon: TestTube,        label: t('nav.parameters') },
        { to: '/tasks',        icon: Calendar,        label: t('nav.tasks') },
      ]
    },
    {
      label: t('nav.fishdexGroup'),
      items: [
        { to: '/fish',         icon: Search,          label: t('nav.fishSearch') },
        { to: '/favorites',    icon: Heart,           label: t('nav.favorites') },
        { to: '/history',      icon: History,         label: t('nav.history') },
        { to: '/my-fish',      icon: BookOpen,        label: t('nav.myFish') },
      ]
    },
    {
      label: t('nav.contentAdminGroup'),
      roles: ['ADMIN', 'CONTENT_ADMIN'], // Only visible to Admins/Content Admins
      items: [
        { 
          icon: FileText, 
          label: t('nav.blog'),
          subItems: [
            { to: '/admin/blog/all', label: 'All Articles' },
            { to: '/admin/blog/new', label: 'Create New' },
            { to: '/admin/blog/categories', label: 'Categories' },
          ]
        },
        { to: '/admin/media-approval', icon: ImageIcon, label: t('nav.mediaApproval') },
      ]
    },
    {
      label: t('nav.utilitiesGroup'),
      items: [
        { to: '/ai-chat',      icon: MessageCircle,   label: t('nav.aiChat') },
        { to: '/image-search', icon: Camera,          label: t('nav.imageSearch') },
      ]
    }
  ];

  // Helper to check role access
  const hasAccess = (requiredRoles?: Role[]) => {
    if (!requiredRoles || requiredRoles.length === 0) return true;
    return requiredRoles.some(role => MOCK_USER_ROLES.includes(role));
  };

  // Filter navigation by Roles
  const authorizedNavGroups = navGroups
    .filter(group => hasAccess(group.roles))
    .map(group => ({
      ...group,
      items: group.items
        .filter(item => hasAccess(item.roles))
        .map(item => ({
          ...item,
          subItems: item.subItems?.filter(sub => hasAccess(sub.roles))
        }))
    }))
    .filter(group => group.items.length > 0);

  // Auto-expand menus based on current route
  useEffect(() => {
    authorizedNavGroups.forEach(group => {
      group.items.forEach(item => {
        if (item.subItems) {
          const isChildActive = item.subItems.some(sub => location.pathname.startsWith(sub.to));
          if (isChildActive) {
            setExpandedMenus(prev => ({ ...prev, [item.label]: true }));
          }
        }
      });
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [location.pathname]);

  const toggleExpand = (label: string) => {
    setExpandedMenus(prev => ({ ...prev, [label]: !prev[label] }));
  };

  return (
    <div className="flex h-screen w-full bg-[#141518] text-slate-300 selection:bg-primary/20">

      {/* ── Sidebar ── */}
      <aside className="w-[260px] flex-shrink-0 flex flex-col bg-[#0e0f11] border-r border-slate-800/60 shadow-[4px_0_24px_rgba(0,0,0,0.5)] z-10 transition-all duration-300">
        
        {/* Logo */}
        <div className="flex items-center gap-3 px-6 py-6 border-b border-slate-800/60">
          <div className="flex h-9 w-9 items-center justify-center rounded-xl bg-gradient-to-br from-primary to-blue-600 shadow-sm shadow-primary/30">
            <Fish className="h-5 w-5 text-white stroke-[2.5]" />
          </div>
          <span className="font-bold text-lg tracking-tight text-white">
            The FishLover
          </span>
        </div>

        {/* Nav items */}
        <nav className="flex-1 px-3 py-5 overflow-y-auto custom-scrollbar">
          {authorizedNavGroups.map((group, i) => (
            <div key={i} className="mb-6 last:mb-0">
              {group.label && (
                <div className="px-3 mb-2 flex items-center justify-between text-[10px] font-bold tracking-widest text-slate-500 uppercase">
                  <span>{group.label}</span>
                  {group.roles && (
                    <span title="Protected by RBAC" className="flex items-center justify-center">
                      <Shield className="w-3 h-3 text-emerald-500/50" />
                    </span>
                  )}
                </div>
              )}
              <div className="space-y-1">
                {group.items.map((item) => {
                  const Icon = item.icon;
                  const hasSub = !!item.subItems?.length;
                  const isExpanded = expandedMenus[item.label];
                  
                  // Check if any child is active
                  const isChildActive = hasSub && item.subItems!.some(sub => location.pathname.startsWith(sub.to));

                  if (hasSub) {
                    return (
                      <div key={item.label} className="flex flex-col">
                        <button
                          onClick={() => toggleExpand(item.label)}
                          className={cn(
                            'group relative flex items-center justify-between px-3 py-2.5 rounded-xl text-sm font-medium transition-all duration-200 overflow-hidden w-full',
                            isChildActive 
                              ? 'text-primary'
                              : 'text-slate-400 hover:bg-white/5 hover:text-slate-200'
                          )}
                        >
                          <div className="flex items-center gap-3">
                            <div className="flex items-center justify-center transition-transform duration-200 group-hover:scale-110">
                              <Icon className={cn(
                                "h-[18px] w-[18px] transition-colors duration-200",
                                isChildActive ? "stroke-[2.5]" : "stroke-2"
                              )} />
                            </div>
                            <span>{item.label}</span>
                          </div>
                          <ChevronDown className={cn(
                            "h-4 w-4 transition-transform duration-300",
                            isExpanded ? "rotate-180" : ""
                          )} />
                        </button>
                        
                        {/* SubItems Container */}
                        <div className={cn(
                          "grid transition-all duration-300 ease-in-out",
                          isExpanded ? "grid-rows-[1fr] opacity-100 mt-1" : "grid-rows-[0fr] opacity-0"
                        )}>
                          <div className="overflow-hidden flex flex-col gap-1 pl-10 pr-2">
                            {item.subItems!.map(sub => (
                              <NavLink
                                key={sub.to}
                                to={sub.to}
                                className={({ isActive }) =>
                                  cn(
                                    'relative flex items-center justify-between px-3 py-2 rounded-lg text-sm font-medium transition-all duration-200',
                                    isActive
                                      ? 'bg-primary/10 text-primary'
                                      : 'text-slate-500 hover:bg-white/5 hover:text-slate-300'
                                  )
                                }
                              >
                                {({ isActive }) => (
                                  <>
                                    <span>{sub.label}</span>
                                    {isActive && <div className="absolute left-0 top-1/2 -translate-y-1/2 w-1 h-1/2 rounded-r-full bg-primary" />}
                                  </>
                                )}
                              </NavLink>
                            ))}
                          </div>
                        </div>
                      </div>
                    );
                  }

                  return (
                    <NavLink
                      key={item.to}
                      to={item.to!}
                      className={({ isActive }) =>
                        cn(
                          'group relative flex items-center justify-between px-3 py-2.5 rounded-xl text-sm font-medium transition-all duration-200 overflow-hidden',
                          isActive
                            ? 'bg-primary/10 text-primary'
                            : 'text-slate-400 hover:bg-white/5 hover:text-slate-200'
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
                            <span>{item.label}</span>
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
                  );
                })}
              </div>
            </div>
          ))}
        </nav>

        {/* Footer: language switcher + logout */}
        <div className="p-4 border-t border-slate-800/60 flex flex-col gap-2">
          <div className="flex items-center gap-2">
            <LanguageSwitcher
              className="flex-1 flex items-center justify-center rounded-xl border border-slate-800 bg-[#141518] px-3 py-2 text-xs font-bold tracking-wider text-slate-400 hover:bg-slate-800 hover:text-white transition-all shadow-sm"
            />
            <button
              onClick={logout}
              className="flex-1 group flex items-center justify-center gap-2 rounded-xl border border-slate-800 bg-[#141518] px-3 py-2 text-xs font-bold text-slate-400 hover:border-red-500/30 hover:bg-red-500/10 hover:text-red-400 transition-all shadow-sm"
            >
              <LogOut className="h-[14px] w-[14px] group-hover:scale-110 transition-transform duration-200" />
              <span>{t('nav.logout')}</span>
            </button>
          </div>
          <div className="text-center pt-2">
            <span className="text-[10px] text-slate-600 font-mono tracking-wider">v1.0.124</span>
          </div>
        </div>
      </aside>

      {/* ── Main content ── */}
      <div className="flex flex-1 flex-col min-w-0 overflow-hidden relative">
        <main className="flex-1 overflow-auto p-0 md:p-0 z-0">
          <div className="w-full h-full">
            <Outlet />
          </div>
        </main>
      </div>

    </div>
  );
}
