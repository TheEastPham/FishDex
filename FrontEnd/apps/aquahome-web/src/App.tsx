import { RouterProvider } from 'react-router-dom';
import { router } from './router';
import { useAuthRestore, useTranslation } from '@fishlover/shared';

export default function App() {
  const isInitializing = useAuthRestore();
  const { t } = useTranslation();

  if (isInitializing) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-slate-50">
        <p className="text-slate-400 text-sm animate-pulse">{t('login.restoringSession')}</p>
      </div>
    );
  }

  return <RouterProvider router={router} />;
}
