import { createBrowserRouter, Navigate } from 'react-router-dom';
import { useAuthStore } from '@fishlover/shared';
import LoginPage from '@/features/auth/LoginPage';
import CallbackPage from '@/features/auth/CallbackPage';
import AuthGuard from '@/components/AuthGuard';
import RoleGuard from '@/components/RoleGuard';
import AppShell from '@/layouts/AppShell';

import FishSearchPage from '@/features/fish-search/FishSearchPage';
import FishProfilePage from '@/features/fish-profile/FishProfilePage';
import PlaceholderPage from '@/features/common/PlaceholderPage';
import DashboardPage from '@/features/dashboard/DashboardPage';
import TanksPage from '@/features/tanks/TanksPage';
import FavoritesPage from '@/features/favorites/FavoritesPage';

// Redirect "/" based on auth state: dashboard if logged in, fish search if not
function RootRedirect() {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  return <Navigate to={isAuthenticated ? '/dashboard' : '/fish'} replace />;
}

export const router = createBrowserRouter([
  { path: '/login',    element: <LoginPage /> },
  { path: '/callback', element: <CallbackPage /> },
  {
    element: <AppShell />,
    children: [
      { index: true, element: <RootRedirect /> },

      // ── Public routes ──────────────────────────────────────
      { path: '/fish',           element: <FishSearchPage /> },
      { path: '/fish/:specCode', element: <FishProfilePage /> },

      // ── Auth-required routes ───────────────────────────────
      {
        element: <AuthGuard />,
        children: [
          { path: '/dashboard',    element: <DashboardPage /> },
          { path: '/tanks',        element: <TanksPage /> },
          { path: '/parameters',   element: <PlaceholderPage /> },
          { path: '/tasks',        element: <PlaceholderPage /> },
          { path: '/favorites',    element: <FavoritesPage /> },
          { path: '/history',      element: <PlaceholderPage /> },
          { path: '/my-fish',      element: <PlaceholderPage /> },
          { path: '/ai-chat',      element: <PlaceholderPage /> },
          { path: '/image-search', element: <PlaceholderPage /> },
        ],
      },

      // ── Role-protected routes ──────────────────────────────
      {
        element: <RoleGuard roles={['ADMIN', 'CONTENT_ADMIN']} />,
        children: [
          { path: '/admin/blog/all',        element: <PlaceholderPage /> },
          { path: '/admin/blog/new',        element: <PlaceholderPage /> },
          { path: '/admin/blog/categories', element: <PlaceholderPage /> },
          { path: '/admin/media-approval',  element: <PlaceholderPage /> },
        ],
      },

      { path: '*', element: <PlaceholderPage is404 /> },
    ],
  },
]);
