import { createBrowserRouter, Navigate } from 'react-router-dom';
import LoginPage from '@/features/auth/LoginPage';
import CallbackPage from '@/features/auth/CallbackPage';
import ProtectedRoute from '@/components/ProtectedRoute';
import AppShell from '@/layouts/AppShell';

import FishSearchPage from '@/features/fish-search/FishSearchPage';
import FishProfilePage from '@/features/fish-profile/FishProfilePage';
import PlaceholderPage from '@/features/common/PlaceholderPage';
import DashboardPage from '@/features/dashboard/DashboardPage';
import TanksPage from '@/features/tanks/TanksPage';
import FavoritesPage from '@/features/favorites/FavoritesPage';


export const router = createBrowserRouter([
  { path: '/login',    element: <LoginPage /> },
  { path: '/callback', element: <CallbackPage /> },
  {
    element: <ProtectedRoute />,
    children: [
      {
        element: <AppShell />,
        children: [
          { index: true,          element: <Navigate to="/dashboard" replace /> },
          { path: '/dashboard',   element: <DashboardPage /> },
          { path: '/tanks',       element: <TanksPage /> },
          { path: '/parameters',  element: <PlaceholderPage /> },
          { path: '/tasks',       element: <PlaceholderPage /> },
          { path: '/fish',        element: <FishSearchPage /> },
          { path: '/fish/:specCode', element: <FishProfilePage /> },
          { path: '/favorites',   element: <FavoritesPage /> },
          { path: '/history',     element: <PlaceholderPage /> },
          { path: '/my-fish',     element: <PlaceholderPage /> },
          { path: '/admin/blog/all',        element: <PlaceholderPage /> },
          { path: '/admin/blog/new',        element: <PlaceholderPage /> },
          { path: '/admin/blog/categories', element: <PlaceholderPage /> },
          { path: '/admin/media-approval',  element: <PlaceholderPage /> },
          { path: '/ai-chat',     element: <PlaceholderPage /> },
          { path: '/image-search',element: <PlaceholderPage /> },
          { path: '*',            element: <PlaceholderPage is404 /> },
        ],
      },
    ],
  },
]);
