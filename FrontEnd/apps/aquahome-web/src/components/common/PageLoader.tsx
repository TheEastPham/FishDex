import { Loader2 } from 'lucide-react';

/** Hiện trong lúc tải chunk của một page lazy (xem router.tsx). Không có chữ nên không cần i18n. */
export default function PageLoader() {
  return (
    <div className="flex items-center justify-center py-16" role="status" aria-busy="true">
      <Loader2 className="w-6 h-6 animate-spin text-slate-500" />
    </div>
  );
}
