import { Navigate, Outlet } from 'react-router-dom';
import { useAuthStore } from '@fishlover/shared';

export default function AuthGuard() {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  return isAuthenticated ? <Outlet /> : <Navigate to="/login" replace />;
}
