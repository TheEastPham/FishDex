import { useState, useEffect, useRef, useMemo } from 'react';
import { NavLink, Link, Outlet, useLocation } from 'react-router-dom';
import {
  LayoutDashboard, Search, MessageCircle, Camera, Fish, LogOut, ChevronRight,
  Droplets, TestTube, Calendar, Heart, History, BookOpen, ChevronDown,
  Shield, Home, Settings, FileText, ImageIcon, User, LogIn
} from 'lucide-react';
import { cn, useLogout, useTranslation, LanguageSwitcher, useAuthStore } from '@fishlover/shared';

interface SubMenuItem {
  to: string;
  icon: React.ElementType;
  label: string;
  requireAuth?: boolean;
  requireRoles?: string[];
}

interface NavItem {
  to?: string;
  icon: React.ElementType;
  label: string;
  requireAuth?: boolean;
  requireRoles?: string[];
  subItems?: SubMenuItem[];
}

export default function AppShell() {
  const logout = useLogout();
  const { t } = useTranslation();
  const location = useLocation();

  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const roles = useAuthStore((s) => s.roles);
  const userName = useAuthStore((s) => s.userName);
  const userEmail = useAuthStore((s) => s.userEmail);
  const userInitials = userName
    ? userName.trim().split(/\s+/).map((w) => w[0].toUpperCase()).join('').slice(0, 2)
    : '?';

  const [expandedMenus, setExpandedMenus] = useState<Record<string, boolean>>({});
  const [isProfileOpen, setIsProfileOpen] = useState(false);
  const [isAvatarPickerOpen, setIsAvatarPickerOpen] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (menuRef.current && !menuRef.current.contains(event.target as Node)) {
        setIsProfileOpen(false);
        setIsAvatarPickerOpen(false);
      }
    }
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const DEFAULT_AVATARS = [
    'https://api.dicebear.com/7.x/bottts/svg?seed=Felix&backgroundColor=0284c7',
    'https://api.dicebear.com/7.x/fun-emoji/svg?seed=Mimi&backgroundColor=059669',
    'https://api.dicebear.com/7.x/avataaars/svg?seed=Aneka&backgroundColor=c026d3',
    'https://api.dicebear.com/7.x/pixel-art/svg?seed=Bella&backgroundColor=ea580c',
    'https://api.dicebear.com/7.x/shapes/svg?seed=Coco&backgroundColor=4f46e5',
  ];

  const [selectedAvatar, setSelectedAvatar] = useState(() => {
    return localStorage.getItem('user_avatar') || '';
  });

  const handleSelectAvatar = (url: string) => {
    setSelectedAvatar(url);
    localStorage.setItem('user_avatar', url);
    setIsAvatarPickerOpen(false);
  };

  const navItems = useMemo<NavItem[]>(() => [
    {
      to: '/dashboard',
      icon: LayoutDashboard,
      label: t('nav.dashboard'),
      requireAuth: true,
    },
    {
      icon: Home,
      label: t('nav.aquahomeGroup'),
      requireAuth: true,
      subItems: [
        { to: '/tanks',      icon: Droplets, label: t('nav.myTanks') },
        { to: '/parameters', icon: TestTube, label: t('nav.parameters') },
        { to: '/tasks',      icon: Calendar, label: t('nav.tasks') },
      ],
    },
    {
      icon: Fish,
      label: t('nav.fishdexGroup'),
      subItems: [
        { to: '/fish',      icon: Search,   label: t('nav.fishSearch') },
        { to: '/favorites', icon: Heart,    label: t('nav.favorites'), requireAuth: true },
        { to: '/history',   icon: History,  label: t('nav.history'),   requireAuth: true },
        { to: '/my-fish',   icon: BookOpen, label: t('nav.myFish'),    requireAuth: true },
      ],
    },
    {
      icon: Settings,
      label: t('nav.utilitiesGroup'),
      requireAuth: true,
      subItems: [
        { to: '/ai-chat',      icon: MessageCircle, label: t('nav.aiChat') },
        { to: '/image-search', icon: Camera,        label: t('nav.imageSearch') },
      ],
    },
    {
      icon: Shield,
      label: t('nav.contentMediaAdmin'),
      requireRoles: ['ADMIN', 'CONTENT_ADMIN'],
      subItems: [
        { to: '/admin/blog/all',        icon: FileText,  label: t('nav.allArticles') },
        { to: '/admin/blog/new',        icon: FileText,  label: t('nav.createNew') },
        { to: '/admin/blog/categories', icon: FileText,  label: t('nav.categories') },
        { to: '/admin/media-approval',  icon: ImageIcon, label: t('nav.mediaApproval') },
      ],
    },
  ], [t]);

  const authorizedNavItems = useMemo(() => {
    const canAccess = (requireAuth?: boolean, requireRoles?: string[]) => {
      if (requireRoles?.length) return isAuthenticated && requireRoles.some((r) => roles.includes(r));
      if (requireAuth) return isAuthenticated;
      return true;
    };
    return navItems
      .map((item) => ({
        ...item,
        subItems: item.subItems?.filter((sub) => canAccess(sub.requireAuth, sub.requireRoles)),
      }))
      .filter((item) => {
        if (!canAccess(item.requireAuth, item.requireRoles)) return false;
        if (item.subItems !== undefined) return item.subItems.length > 0;
        return true;
      });
  }, [navItems, isAuthenticated, roles]);

  // Auto-expand the group that contains the active route
  useEffect(() => {
    authorizedNavItems.forEach((item) => {
      if (item.subItems) {
        const isChildActive = item.subItems.some((sub) => location.pathname.startsWith(sub.to));
        if (isChildActive) {
          setExpandedMenus((prev) => ({ ...prev, [item.label]: true }));
        }
      }
    });
  }, [location.pathname, authorizedNavItems]);

  const toggleExpand = (label: string) => {
    setExpandedMenus((prev) => ({ ...prev, [label]: !prev[label] }));
  };

  return (
    <div className="flex h-screen w-full bg-[#0F172A] text-slate-300 selection:bg-primary/20">

      {/* ── Sidebar ── */}
      <aside className="w-[260px] flex-shrink-0 flex flex-col bg-[#0A0F1A] border-r border-slate-800/60 shadow-[4px_0_24px_rgba(0,0,0,0.5)] z-10 transition-all duration-300">

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
        <nav className="flex-1 px-3 py-6 overflow-y-auto custom-scrollbar space-y-2">
          {authorizedNavItems.map((item) => {
            const Icon = item.icon;
            const hasSub = !!item.subItems?.length;
            const isExpanded = expandedMenus[item.label];
            const isChildActive = hasSub && item.subItems!.some((sub) => location.pathname.startsWith(sub.to));

            if (hasSub) {
              return (
                <div key={item.label} className="flex flex-col">
                  <button
                    onClick={() => toggleExpand(item.label)}
                    className={cn(
                      'group relative flex items-center justify-between px-3 py-3 rounded-xl text-sm font-semibold transition-all duration-200 overflow-hidden w-full',
                      isChildActive
                        ? 'text-primary bg-primary/5'
                        : 'text-slate-300 hover:bg-white/5 hover:text-slate-100'
                    )}
                  >
                    <div className="flex items-center gap-3">
                      <div className="flex items-center justify-center transition-transform duration-200 group-hover:scale-110">
                        <Icon className={cn(
                          'h-5 w-5 transition-colors duration-200',
                          isChildActive ? 'stroke-[2.5]' : 'stroke-2'
                        )} />
                      </div>
                      <span className="tracking-wide">{item.label}</span>
                    </div>

                    <div className="flex items-center gap-2">
                      {item.requireRoles && (
                        <span title="Protected by RBAC" className="flex items-center justify-center opacity-50 hover:opacity-100 transition-opacity">
                          <Shield className="w-3.5 h-3.5 text-emerald-500" />
                        </span>
                      )}
                      <ChevronDown className={cn(
                        'h-4 w-4 transition-transform duration-300 opacity-60',
                        isExpanded ? 'rotate-180' : ''
                      )} />
                    </div>
                  </button>

                  {/* SubItems Container */}
                  <div className={cn(
                    'grid transition-all duration-300 ease-in-out',
                    isExpanded ? 'grid-rows-[1fr] opacity-100 mt-1.5' : 'grid-rows-[0fr] opacity-0'
                  )}>
                    <div className="overflow-hidden flex flex-col gap-1 pl-4 pr-2">
                      {item.subItems!.map((sub) => {
                        const SubIcon = sub.icon;
                        return (
                          <NavLink
                            key={sub.to}
                            to={sub.to}
                            className={({ isActive }) =>
                              cn(
                                'group/sub relative flex items-center justify-between px-3 py-2.5 rounded-xl text-sm font-medium transition-all duration-200 overflow-hidden',
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
                                    'flex items-center justify-center transition-transform duration-200',
                                    isActive ? 'scale-110' : 'group-hover/sub:scale-110'
                                  )}>
                                    <SubIcon className={cn(
                                      'h-[18px] w-[18px] transition-colors duration-200',
                                      isActive ? 'stroke-[2.5]' : 'stroke-2'
                                    )} />
                                  </div>
                                  <span>{sub.label}</span>
                                </div>

                                <div className={cn(
                                  'absolute left-0 top-1/2 -translate-y-1/2 w-1 h-1/2 rounded-r-full bg-primary transition-all duration-300',
                                  isActive ? 'opacity-100' : 'opacity-0 -translate-x-full'
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
                </div>
              );
            }

            return (
              <NavLink
                key={item.to}
                to={item.to!}
                className={({ isActive }) =>
                  cn(
                    'group relative flex items-center justify-between px-3 py-3 rounded-xl text-sm font-semibold transition-all duration-200 overflow-hidden',
                    isActive
                      ? 'bg-primary/10 text-primary'
                      : 'text-slate-300 hover:bg-white/5 hover:text-slate-100'
                  )
                }
              >
                {({ isActive }) => (
                  <>
                    <div className="flex items-center gap-3">
                      <div className={cn(
                        'flex items-center justify-center transition-transform duration-200',
                        isActive ? 'scale-110' : 'group-hover:scale-110'
                      )}>
                        <Icon className={cn(
                          'h-5 w-5 transition-colors duration-200',
                          isActive ? 'stroke-[2.5]' : 'stroke-2'
                        )} />
                      </div>
                      <span className="tracking-wide">{item.label}</span>
                    </div>

                    <div className={cn(
                      'absolute left-0 top-1/2 -translate-y-1/2 w-1.5 h-1/2 rounded-r-full bg-primary transition-all duration-300',
                      isActive ? 'opacity-100' : 'opacity-0 -translate-x-full'
                    )} />

                    {isActive && (
                      <ChevronRight className="h-4 w-4 opacity-50" />
                    )}
                  </>
                )}
              </NavLink>
            );
          })}
        </nav>

        {/* Footer: Profile (authenticated) or Login button (guest) */}
        <div className="p-4 border-t border-slate-800/60 relative" ref={menuRef}>
          {isAuthenticated ? (
            <>
              {/* Floating Dropdown Menu */}
              {isProfileOpen && !isAvatarPickerOpen && (
                <div className="absolute bottom-[calc(100%-8px)] left-4 right-4 mb-2 bg-[#1E293B] border border-slate-700 rounded-xl shadow-2xl overflow-hidden animate-in fade-in slide-in-from-bottom-2 z-50">
                  <div className="px-4 py-3 border-b border-slate-800 flex items-center justify-between">
                    <span className="text-xs font-semibold text-slate-400">Language</span>
                    <LanguageSwitcher className="text-xs font-bold px-2 py-1 rounded bg-white/5 hover:bg-white/10 text-slate-300 transition-colors cursor-pointer" />
                  </div>
                  <button
                    onClick={() => setIsAvatarPickerOpen(true)}
                    className="w-full flex items-center gap-3 px-4 py-3 text-sm font-medium text-slate-300 hover:bg-white/5 hover:text-white transition-colors"
                  >
                    <User className="w-4 h-4" />
                    <span>{t('nav.profile')} (Avatar)</span>
                  </button>
                  <button className="w-full flex items-center gap-3 px-4 py-3 text-sm font-medium text-slate-300 hover:bg-white/5 hover:text-white transition-colors border-t border-slate-800/50">
                    <Settings className="w-4 h-4" />
                    <span>{t('nav.settings')}</span>
                  </button>
                  <button onClick={logout} className="w-full flex items-center gap-3 px-4 py-3 text-sm font-medium text-red-400 hover:bg-red-500/10 hover:text-red-300 transition-colors border-t border-slate-800/50">
                    <LogOut className="w-4 h-4" />
                    <span>{t('nav.logout')}</span>
                  </button>
                </div>
              )}

              {/* Avatar Picker Overlay */}
              {isAvatarPickerOpen && (
                <div className="absolute bottom-[calc(100%-8px)] left-4 right-4 mb-2 bg-[#1E293B] border border-slate-700 rounded-xl shadow-2xl overflow-hidden animate-in fade-in slide-in-from-bottom-2 z-50 p-4">
                  <div className="flex items-center justify-between mb-3">
                    <span className="text-xs font-bold text-slate-300 uppercase tracking-wider">Choose Avatar</span>
                    <button onClick={() => setIsAvatarPickerOpen(false)} className="text-slate-500 hover:text-white">✕</button>
                  </div>
                  <div className="flex flex-wrap gap-3 justify-center">
                    {DEFAULT_AVATARS.map((avatar, idx) => (
                      <button
                        key={idx}
                        onClick={() => handleSelectAvatar(avatar)}
                        className={cn(
                          'w-12 h-12 rounded-full overflow-hidden border-2 transition-transform hover:scale-110 hover:shadow-lg',
                          selectedAvatar === avatar ? 'border-primary' : 'border-transparent'
                        )}
                      >
                        <img src={avatar} alt={`Avatar ${idx}`} className="w-full h-full object-cover" />
                      </button>
                    ))}
                    <button
                      onClick={() => handleSelectAvatar('')}
                      className={cn(
                        'w-12 h-12 rounded-full flex items-center justify-center text-white font-bold bg-gradient-to-br from-primary to-blue-600 border-2 transition-transform hover:scale-110',
                        !selectedAvatar ? 'border-white' : 'border-transparent'
                      )}
                    >
                      {userInitials}
                    </button>
                  </div>
                </div>
              )}

              {/* Profile button */}
              <button
                onClick={() => {
                  if (isAvatarPickerOpen) {
                    setIsAvatarPickerOpen(false);
                    setIsProfileOpen(false);
                  } else {
                    setIsProfileOpen(!isProfileOpen);
                  }
                }}
                className={cn(
                  'w-full flex items-center justify-between p-2 rounded-xl transition-colors border border-transparent',
                  (isProfileOpen || isAvatarPickerOpen) ? 'bg-white/5 border-slate-700/50' : 'hover:bg-white/5'
                )}
              >
                <div className="flex items-center gap-3">
                  {selectedAvatar ? (
                    <img src={selectedAvatar} alt="User Avatar" className="w-10 h-10 rounded-full shadow-sm border border-slate-700" />
                  ) : (
                    <div className="w-10 h-10 rounded-full bg-gradient-to-br from-primary to-blue-600 flex items-center justify-center text-white font-bold text-sm shadow-sm">
                      {userInitials}
                    </div>
                  )}
                  <div className="flex flex-col items-start">
                    <span className="text-sm font-bold text-white truncate max-w-[120px]">
                      {userName || 'User'}
                    </span>
                    <span className="text-xs text-slate-400 font-medium truncate max-w-[120px]">
                      {userEmail ?? roles[0] ?? ''}
                    </span>
                  </div>
                </div>
                <ChevronDown className={cn('w-4 h-4 text-slate-500 transition-transform duration-300', isProfileOpen && 'rotate-180')} />
              </button>
            </>
          ) : (
            <Link
              to="/login"
              className="w-full flex items-center justify-center gap-2 px-4 py-3 rounded-xl bg-primary/10 hover:bg-primary/20 text-primary font-semibold text-sm transition-colors border border-primary/20"
            >
              <LogIn className="w-4 h-4" />
              Đăng nhập
            </Link>
          )}
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
