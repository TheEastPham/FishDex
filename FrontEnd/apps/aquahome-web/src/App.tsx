import { RouterProvider } from 'react-router-dom';
import { router } from './router';
import { useAuthRestore } from '@fishlover/shared';

export default function App() {
  const isInitializing = useAuthRestore();

  if (isInitializing) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-slate-50">
        <p className="text-slate-400 text-sm animate-pulse">Đang khôi phục phiên đăng nhập…</p>
      </div>
    );
  }

  return <RouterProvider router={router} />;
}
