import { useState, useEffect, useRef, useMemo } from 'react';
import { NavLink, Link, Outlet, useLocation } from 'react-router-dom';
import {
  LayoutDashboard, Search, MessageCircle, Camera, Fish, LogOut, ChevronRight,
  Droplets, TestTube, Calendar, Heart, History, BookOpen, ChevronDown,
  Shield, Home, Settings, FileText, ImageIcon, User, LogIn, Menu, X, Globe, Bell, MoreHorizontal,
  Users, Waves, Trophy
} from 'lucide-react';
import { cn, useLogout, useTranslation, setLanguage, useAuthStore } from '@fishlover/shared';

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
  product?: string;
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
  const [isUserMenuOpen, setIsUserMenuOpen] = useState(false);
  const [isLangOpen, setIsLangOpen] = useState(false);
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);
  const userMenuRef = useRef<HTMLDivElement>(null);
  const langMenuRef = useRef<HTMLDivElement>(null);

  const { i18n } = useTranslation();
  const currentLang = i18n.language as 'en' | 'vi';
  const LANGUAGES: { code: 'en' | 'vi'; label: string }[] = [
    { code: 'en', label: 'English' },
    { code: 'vi', label: 'Tiếng Việt' },
  ];

  const [selectedAvatar] = useState(() => localStorage.getItem('user_avatar') || '');

  // Close mobile menu on route change
  useEffect(() => { setIsMobileMenuOpen(false); }, [location.pathname]);

  // Lock body scroll when mobile drawer is open
  useEffect(() => {
    document.body.style.overflow = isMobileMenuOpen ? 'hidden' : '';
    return () => { document.body.style.overflow = ''; };
  }, [isMobileMenuOpen]);

  // Close menus on outside click
  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (userMenuRef.current && !userMenuRef.current.contains(e.target as Node))
        setIsUserMenuOpen(false);
      if (langMenuRef.current && !langMenuRef.current.contains(e.target as Node))
        setIsLangOpen(false);
    }
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const navItems = useMemo<NavItem[]>(() => [
    {
      icon: LayoutDashboard,
      label: t('nav.overviewGroup'),
      product: 'Overview',
      requireAuth: true,
      subItems: [
        { to: '/dashboard', icon: LayoutDashboard, label: t('nav.dashboard') },
        { to: '/articles',  icon: FileText,         label: t('nav.article') },
      ],
    },
    {
      icon: Home,
      label: t('nav.aquahomeGroup'),
      product: 'AquaHome',
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
      product: 'FishDex',
      subItems: [
        { to: '/fish',      icon: Search,   label: t('nav.fishSearch') },
        { to: '/favorites', icon: Heart,    label: t('nav.favorites'), requireAuth: true },
        { to: '/history',   icon: History,  label: t('nav.history'),   requireAuth: true },
        { to: '/my-fish',   icon: BookOpen, label: t('nav.myFish'),    requireAuth: true },
        { to: '/submit-species', icon: Fish, label: t('nav.submitSpecies'), requireAuth: true },
      ],
    },
    {
      icon: Users,
      label: t('nav.communityGroup'),
      product: 'Community',
      subItems: [
        { to: '/public/tanks', icon: Waves,  label: t('nav.publicTanks') },
        { to: '/contests',     icon: Trophy, label: t('nav.contests') },
        { to: '/my-published-tanks', icon: Globe, label: t('nav.myPublishedTanks'), requireAuth: true },
        { to: '/my-contributions', icon: BookOpen, label: t('nav.myContributions'), requireAuth: true },
      ],
    },
    {
      icon: Settings,
      label: t('nav.utilitiesGroup'),
      product: 'Utilities',
      requireAuth: true,
      subItems: [
        { to: '/ai-chat',      icon: MessageCircle, label: t('nav.aiChat') },
        { to: '/image-search', icon: Camera,        label: t('nav.imageSearch') },
      ],
    },
    {
      icon: Shield,
      label: t('nav.contribution'),
      product: 'Admin',
      requireRoles: ['SystemAdmin', 'ContentAdmin'],
      subItems: [
        { to: '/admin/articles', icon: FileText,  label: t('nav.articlesManager') },
        { to: '/admin/media',    icon: ImageIcon, label: t('nav.mediaManager') },
        { to: '/admin/community', icon: Users,    label: t('nav.communityModeration') },
        { to: '/admin/contests', icon: Trophy,    label: t('nav.contestsManager'), requireRoles: ['SystemAdmin'] },
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
      if (item.subItems?.some((sub) => location.pathname.startsWith(sub.to))) {
        setExpandedMenus((prev) => ({ ...prev, [item.label]: true }));
      }
    });
  }, [location.pathname, authorizedNavItems]);


  const toggleExpand = (label: string) => {
    setExpandedMenus((prev) => ({ ...prev, [label]: !prev[label] }));
  };

  // ── Sidebar nav content ──────────────────────────────────────
  const SidebarNav = () => (
    <nav className="flex-1 px-3 py-4 overflow-y-auto custom-scrollbar space-y-1">
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
                  'group flex items-center justify-between px-3 py-2.5 rounded-xl text-sm font-semibold transition-all duration-200 w-full',
                  isChildActive
                    ? 'text-primary bg-primary/5'
                    : 'text-slate-300 hover:bg-white/5 hover:text-slate-100'
                )}
              >
                <div className="flex items-center gap-3">
                  <Icon className={cn('h-[18px] w-[18px]', isChildActive ? 'stroke-[2.5]' : 'stroke-2')} />
                  <span>{item.label}</span>
                </div>
                <div className="flex items-center gap-1.5">
                  {item.requireRoles && <Shield className="w-3 h-3 text-emerald-500 opacity-50" />}
                  <ChevronDown className={cn('h-3.5 w-3.5 opacity-50 transition-transform duration-300', isExpanded && 'rotate-180')} />
                </div>
              </button>

              <div className={cn(
                'grid transition-all duration-300 ease-in-out',
                isExpanded ? 'grid-rows-[1fr] opacity-100 mt-1' : 'grid-rows-[0fr] opacity-0'
              )}>
                <div className="overflow-hidden flex flex-col gap-0.5 pl-4 pr-2">
                  {item.subItems!.map((sub) => {
                    const SubIcon = sub.icon;
                    return (
                      <NavLink
                        key={sub.to}
                        to={sub.to}
                        className={({ isActive }) => cn(
                          'relative flex items-center justify-between px-3 py-2 rounded-xl text-sm font-medium transition-all duration-200',
                          isActive
                            ? 'bg-primary/10 text-primary'
                            : 'text-slate-400 hover:bg-white/5 hover:text-slate-200'
                        )}
                      >
                        {({ isActive }) => (
                          <>
                            <div className="flex items-center gap-3">
                              <SubIcon className={cn('h-[16px] w-[16px]', isActive ? 'stroke-[2.5]' : 'stroke-2')} />
                              <span>{sub.label}</span>
                            </div>
                            <div className={cn(
                              'absolute left-0 top-1/2 -translate-y-1/2 w-1 h-1/2 rounded-r-full bg-primary transition-all duration-300',
                              isActive ? 'opacity-100' : 'opacity-0 -translate-x-full'
                            )} />
                            {isActive && <ChevronRight className="h-3.5 w-3.5 opacity-40" />}
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
            className={({ isActive }) => cn(
              'relative flex items-center justify-between px-3 py-2.5 rounded-xl text-sm font-semibold transition-all duration-200',
              isActive
                ? 'bg-primary/10 text-primary'
                : 'text-slate-300 hover:bg-white/5 hover:text-slate-100'
            )}
          >
            {({ isActive }) => (
              <>
                <div className="flex items-center gap-3">
                  <Icon className={cn('h-[18px] w-[18px]', isActive ? 'stroke-[2.5]' : 'stroke-2')} />
                  <span>{item.label}</span>
                </div>
                <div className={cn(
                  'absolute left-0 top-1/2 -translate-y-1/2 w-1.5 h-1/2 rounded-r-full bg-primary transition-all duration-300',
                  isActive ? 'opacity-100' : 'opacity-0 -translate-x-full'
                )} />
                {isActive && <ChevronRight className="h-3.5 w-3.5 opacity-40" />}
              </>
            )}
          </NavLink>
        );
      })}
    </nav>
  );

  return (
    <div className="flex flex-col h-screen w-full bg-[#0F172A] text-slate-300 selection:bg-primary/20">

      {/* ── Full-width Top Header ── */}
      <header className="flex items-center justify-between px-4 h-14 bg-[#0A0F1A] border-b border-slate-800/60 shrink-0 z-30 w-full">
        {/* Left: Logo + hamburger (mobile) */}
        <div className="flex items-center gap-3">
          <button
            onClick={() => setIsMobileMenuOpen(true)}
            className="md:hidden flex items-center justify-center w-9 h-9 rounded-xl text-slate-400 hover:text-white hover:bg-white/5 transition-colors"
          >
            <Menu className="w-5 h-5" />
          </button>
          <div className="flex items-center gap-2.5">
            <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-gradient-to-br from-primary to-blue-600 shadow-sm shadow-primary/30">
              <Fish className="h-4 w-4 text-white stroke-[2.5]" />
            </div>
            <span className="font-bold text-base tracking-tight text-white">The FishLover</span>
          </div>
        </div>

          {/* Right: Language + Bell + User */}
          <div className="flex items-center gap-1">
            {/* Language dropdown */}
            <div className="relative" ref={langMenuRef}>
              <button
                onClick={() => setIsLangOpen((v) => !v)}
                className={cn(
                  'flex items-center gap-1.5 px-2 sm:px-2.5 py-1.5 rounded-xl transition-colors min-h-[36px]',
                  isLangOpen ? 'bg-white/5' : 'hover:bg-white/5'
                )}
              >
                <Globe className="w-4 h-4 text-slate-400" />
                {/* mobile: abbreviation only; desktop: full label */}
                <span className="text-sm font-semibold text-slate-300 sm:hidden">
                  {currentLang.toUpperCase()}
                </span>
                <span className="text-sm font-semibold text-slate-300 hidden sm:inline">
                  {LANGUAGES.find(l => l.code === currentLang)?.label}
                </span>
                <ChevronDown className={cn('w-3 h-3 text-slate-500 transition-transform duration-200', isLangOpen && 'rotate-180')} />
              </button>
              {isLangOpen && (
                <div className="absolute right-0 top-[calc(100%+6px)] w-40 bg-[#1E293B] border border-slate-700 rounded-xl shadow-2xl overflow-hidden z-[200] animate-in fade-in slide-in-from-top-2">
                  {LANGUAGES.map((lang) => (
                    <button
                      key={lang.code}
                      onClick={() => { setLanguage(lang.code); setIsLangOpen(false); }}
                      className={cn(
                        'w-full flex items-center px-4 py-2.5 text-sm transition-colors',
                        currentLang === lang.code
                          ? 'bg-primary/10 text-primary font-semibold'
                          : 'text-slate-300 hover:bg-white/5 hover:text-white'
                      )}
                    >
                      {lang.label}
                    </button>
                  ))}
                </div>
              )}
            </div>

            {/* Notification bell */}
            <button className="flex items-center justify-center w-9 h-9 rounded-xl text-slate-400 hover:text-white hover:bg-white/5 transition-colors relative">
              <Bell className="w-4 h-4" />
            </button>

            {isAuthenticated ? (
              <div className="relative" ref={userMenuRef}>
                <button
                  onClick={() => setIsUserMenuOpen((v) => !v)}
                  className={cn(
                    'flex items-center gap-2 px-2 sm:px-3 py-1.5 rounded-xl transition-colors border border-transparent min-h-[40px]',
                    isUserMenuOpen ? 'bg-white/5 border-slate-700/50' : 'hover:bg-white/5'
                  )}
                >
                  {/* mobile: 3-dot icon; desktop: avatar + name */}
                  <MoreHorizontal className="w-5 h-5 text-slate-400 sm:hidden" />
                  <div className="hidden sm:flex items-center gap-2">
                    {selectedAvatar ? (
                      <img src={selectedAvatar} alt="avatar" className="w-7 h-7 rounded-full border border-slate-700" />
                    ) : (
                      <div className="w-7 h-7 rounded-full bg-gradient-to-br from-primary to-blue-600 flex items-center justify-center text-white font-bold text-xs">
                        {userInitials}
                      </div>
                    )}
                    <div className="flex flex-col items-start leading-none">
                      <span className="text-sm font-semibold text-white truncate max-w-[120px]">{userName || 'User'}</span>
                      <span className="text-[11px] text-slate-400 truncate max-w-[120px]">{roles[0] ?? userEmail ?? ''}</span>
                    </div>
                    <ChevronDown className={cn('w-3.5 h-3.5 text-slate-500 transition-transform duration-300', isUserMenuOpen && 'rotate-180')} />
                  </div>
                </button>

                {isUserMenuOpen && (
                  <div className="absolute right-0 top-[calc(100%+6px)] w-52 bg-[#1E293B] border border-slate-700 rounded-xl shadow-2xl overflow-hidden z-[200] animate-in fade-in slide-in-from-top-2">
                    <div className="px-4 py-3 border-b border-slate-800">
                      <p className="text-sm font-bold text-white truncate">{userName}</p>
                      <p className="text-xs text-slate-400 truncate">{userEmail}</p>
                    </div>
                    <Link
                      to="/profile"
                      onClick={() => setIsUserMenuOpen(false)}
                      className="flex items-center gap-3 px-4 py-3 text-sm text-slate-300 hover:bg-white/5 hover:text-white transition-colors"
                    >
                      <User className="w-4 h-4" />
                      {t('nav.profile')}
                    </Link>
                    <button
                      onClick={() => setIsUserMenuOpen(false)}
                      className="w-full flex items-center gap-3 px-4 py-3 text-sm text-slate-300 hover:bg-white/5 hover:text-white transition-colors border-t border-slate-800/50 opacity-40 cursor-not-allowed"
                      disabled
                    >
                      <Settings className="w-4 h-4" />
                      {t('nav.settings')}
                    </button>
                    <button
                      onClick={() => { setIsUserMenuOpen(false); logout(); }}
                      className="w-full flex items-center gap-3 px-4 py-3 text-sm text-red-400 hover:bg-red-500/10 hover:text-red-300 transition-colors border-t border-slate-800/50"
                    >
                      <LogOut className="w-4 h-4" />
                      {t('nav.logout')}
                    </button>
                  </div>
                )}
              </div>
            ) : (
              <Link
                to="/login"
                className="flex items-center gap-2 px-3 py-1.5 rounded-xl bg-primary/10 hover:bg-primary/20 text-primary font-semibold text-sm transition-colors border border-primary/20 min-h-[36px]"
              >
                <LogIn className="w-4 h-4" />
                {t('nav.login')}
              </Link>
            )}
          </div>
      </header>

      {/* ── Body: Sidebar + Content ── */}
      <div className="flex flex-1 min-h-0 overflow-hidden">

        {/* ── Desktop Sidebar ── */}
        <aside className="hidden md:flex w-[240px] flex-shrink-0 flex-col bg-[#0A0F1A] border-r border-slate-800/60">
          <SidebarNav />
        </aside>

        {/* ── Mobile Drawer Overlay ── */}
        {isMobileMenuOpen && (
          <div className="fixed inset-0 bg-black/60 z-40 md:hidden" onClick={() => setIsMobileMenuOpen(false)} />
        )}

        {/* ── Mobile Drawer Panel ── */}
        <aside className={cn(
          'fixed top-0 left-0 h-full w-[260px] flex flex-col bg-[#0A0F1A] border-r border-slate-800/60 shadow-2xl z-50 transition-transform duration-300 ease-in-out md:hidden',
          isMobileMenuOpen ? 'translate-x-0' : '-translate-x-full'
        )}>
          <div className="flex items-center justify-between px-4 py-4 border-b border-slate-800/60">
            <div className="flex items-center gap-2.5">
              <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-gradient-to-br from-primary to-blue-600">
                <Fish className="h-4 w-4 text-white stroke-[2.5]" />
              </div>
              <span className="font-bold text-base tracking-tight text-white">The FishLover</span>
            </div>
            <button
              onClick={() => setIsMobileMenuOpen(false)}
              className="flex items-center justify-center w-9 h-9 rounded-xl text-slate-400 hover:text-white hover:bg-white/5 transition-colors"
            >
              <X className="w-4 h-4" />
            </button>
          </div>
          <SidebarNav />
        </aside>

        <main className="flex-1 overflow-auto min-w-0">
          <div className="w-full h-full">
            <Outlet />
          </div>
        </main>
      </div>

    </div>
  );
}
