import { createBrowserRouter, Navigate } from 'react-router-dom';
import { useAuthStore } from '@fishlover/shared';
import LoginPage from '@/features/auth/LoginPage';
import CallbackPage from '@/features/auth/CallbackPage';
import RegisterPage from '@/features/auth/RegisterPage';
import ForgotPasswordPage from '@/features/auth/ForgotPasswordPage';
import ResetPasswordPage from '@/features/auth/ResetPasswordPage';
import AuthGuard from '@/components/AuthGuard';
import RoleGuard from '@/components/RoleGuard';
import AppShell from '@/layouts/AppShell';

import FishSearchPage from '@/features/fish-search/FishSearchPage';
import FishProfilePage from '@/features/fish-profile/FishProfilePage';
import PlaceholderPage from '@/features/common/PlaceholderPage';
import DashboardPage from '@/features/dashboard/DashboardPage';
import TanksPage from '@/features/tanks/TanksPage';
import FavoritesPage from '@/features/favorites/FavoritesPage';
import ProfilePage from '@/features/profile/ProfilePage';
import HistoryPage from '@/features/history/HistoryPage';
import ReleasePage from '@/features/articles/ReleasePage';
import TasksPage from '@/features/tasks/TasksPage';
import PublicTanksPage from '@/features/public-tanks/PublicTanksPage';
import PublicTankDetailPage from '@/features/public-tanks/PublicTankDetailPage';
import ContestsPage from '@/features/contests/ContestsPage';
import AdminContestsPage from '@/features/admin-contests/AdminContestsPage';
import MyPublishedTanksPage from '@/features/my-published-tanks/MyPublishedTanksPage';
import MyContributionsPage from '@/features/community/MyContributionsPage';
import SubmitSpeciesPage from '@/features/community/SubmitSpeciesPage';
import AdminCommunityPage from '@/features/admin-community/AdminCommunityPage';

// Redirect "/" based on auth state: dashboard if logged in, fish search if not
function RootRedirect() {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  return <Navigate to={isAuthenticated ? '/dashboard' : '/fish'} replace />;
}

export const router = createBrowserRouter([
  { path: '/login',           element: <LoginPage /> },
  { path: '/register',        element: <RegisterPage /> },
  { path: '/forgot-password', element: <ForgotPasswordPage /> },
  { path: '/reset-password',  element: <ResetPasswordPage /> },
  { path: '/callback',        element: <CallbackPage /> },
  {
    element: <AppShell />,
    children: [
      { index: true, element: <RootRedirect /> },

      // ── Public routes ──────────────────────────────────────
      { path: '/fish',                element: <FishSearchPage /> },
      { path: '/fish/:specCode',      element: <FishProfilePage /> },
      { path: '/articles/release',    element: <ReleasePage /> },
      { path: '/articles',            element: <Navigate to="/articles/release" replace /> },
      { path: '/public/tanks',        element: <PublicTanksPage /> },
      { path: '/public/tanks/:slug',  element: <PublicTankDetailPage /> },
      { path: '/contests',            element: <ContestsPage /> },

      // ── Auth-required routes ───────────────────────────────
      {
        element: <AuthGuard />,
        children: [
          { path: '/dashboard',    element: <DashboardPage /> },
          { path: '/tanks',        element: <TanksPage /> },
          { path: '/my-published-tanks', element: <MyPublishedTanksPage /> },
          { path: '/my-contributions', element: <MyContributionsPage /> },
          { path: '/submit-species', element: <SubmitSpeciesPage /> },
          { path: '/parameters',   element: <PlaceholderPage /> },
          { path: '/tasks',        element: <TasksPage /> },
          { path: '/favorites',    element: <FavoritesPage /> },
          { path: '/profile',      element: <ProfilePage /> },
          { path: '/history',      element: <HistoryPage /> },
          { path: '/my-fish',      element: <PlaceholderPage /> },
          { path: '/ai-chat',      element: <PlaceholderPage /> },
          { path: '/image-search', element: <PlaceholderPage /> },
        ],
      },

      // ── Role-protected routes ──────────────────────────────
      {
        element: <RoleGuard roles={['SystemAdmin', 'ContentAdmin']} />,
        children: [
          { path: '/admin/articles', element: <PlaceholderPage /> },
          { path: '/admin/media',    element: <PlaceholderPage /> },
          { path: '/admin/community', element: <AdminCommunityPage /> },
        ],
      },
      {
        element: <RoleGuard roles={['SystemAdmin']} />,
        children: [
          { path: '/admin/contests', element: <AdminContestsPage /> },
        ],
      },

      { path: '*', element: <PlaceholderPage is404 /> },
    ],
  },
]);
