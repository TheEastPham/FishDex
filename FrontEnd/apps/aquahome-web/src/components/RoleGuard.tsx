import { Navigate, Outlet } from 'react-router-dom';
import { useAuthStore } from '@fishlover/shared';

interface Props {
  roles: string[];
}

export default function RoleGuard({ roles }: Props) {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const userRoles = useAuthStore((s) => s.roles);

  if (!isAuthenticated) return <Navigate to="/login" replace />;
  if (!roles.some((r) => userRoles.includes(r))) return <Navigate to="/dashboard" replace />;
  return <Outlet />;
}
