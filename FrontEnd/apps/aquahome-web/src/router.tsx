import { createBrowserRouter, Navigate } from 'react-router-dom';
import LoginPage from '@/features/auth/LoginPage';
import CallbackPage from '@/features/auth/CallbackPage';
import ProtectedRoute from '@/components/ProtectedRoute';
import AppShell from '@/layouts/AppShell';

import FishSearchPage from '@/features/fish-search/FishSearchPage';
import FishProfilePage from '@/features/fish-profile/FishProfilePage';
import PlaceholderPage from '@/features/common/PlaceholderPage';

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
          { path: '/dashboard',   element: <PlaceholderPage /> },
          { path: '/tanks',       element: <PlaceholderPage /> },
          { path: '/parameters',  element: <PlaceholderPage /> },
          { path: '/tasks',       element: <PlaceholderPage /> },
          { path: '/fish',        element: <FishSearchPage /> },
          { path: '/fish/:specCode', element: <FishProfilePage /> },
          { path: '/favorites',   element: <PlaceholderPage /> },
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
