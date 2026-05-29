import { createBrowserRouter, Navigate } from 'react-router-dom';
import LoginPage from '@/features/auth/LoginPage';
import CallbackPage from '@/features/auth/CallbackPage';
import ProtectedRoute from '@/components/ProtectedRoute';
import AppShell from '@/layouts/AppShell';

import FishSearchPage from '@/features/fish-search/FishSearchPage';

// Placeholders — replaced story by story
const DashboardPage    = () => <p className="text-muted-foreground text-sm">Dashboard — Story 5.2</p>;
const AIChatPage       = () => <p className="text-muted-foreground text-sm">AI Chat — Story 5.4</p>;
const ImageSearchPage  = () => <p className="text-muted-foreground text-sm">Image Search — Story 5.5</p>;

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
          { path: '/fish',        element: <FishSearchPage /> },
          { path: '/ai-chat',     element: <AIChatPage /> },
          { path: '/image-search',element: <ImageSearchPage /> },
        ],
      },
    ],
  },
]);
