import { createBrowserRouter, Navigate } from 'react-router-dom';
import LoginPage from './features/auth/LoginPage';
import CallbackPage from './features/auth/CallbackPage';
import ProtectedRoute from './components/ProtectedRoute';

// Placeholders — replaced in Stories 5.2 / 5.3 / 5.4 / 5.5
const DashboardPage = () => (
  <div className="p-8">
    <h1 className="text-xl font-semibold text-slate-800">Dashboard</h1>
    <p className="text-sm text-slate-500 mt-1">Story 5.2 — coming soon</p>
  </div>
);

export const router = createBrowserRouter([
  { path: '/login',    element: <LoginPage /> },
  { path: '/callback', element: <CallbackPage /> },
  {
    element: <ProtectedRoute />,
    children: [
      { index: true,         element: <Navigate to="/dashboard" replace /> },
      { path: '/dashboard',  element: <DashboardPage /> },
    ],
  },
]);
