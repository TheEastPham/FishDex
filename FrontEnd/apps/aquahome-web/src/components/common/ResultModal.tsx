import { useTranslation } from '@fishlover/shared';
import { Check, AlertTriangle, X } from 'lucide-react';

interface Props {
  /** 'success' = check xanh (mặc định), 'error' = tam giác cảnh báo đỏ. */
  variant?: 'success' | 'error';
  title: string;
  /** Dòng phụ, vd cảnh báo 1 bước con bị lỗi dù thao tác chính vẫn thành công. */
  message?: string;
  onClose: () => void;
}

/**
 * Popup kết quả dùng chung cho mọi form submit/update/delete trong app — thay cho mỗi modal
 * tự vẽ lại 1 "done screen" riêng. Chỉ có 1 nút Đóng, không có action phụ.
 */
export default function ResultModal({ variant = 'success', title, message, onClose }: Props) {
  const { t } = useTranslation();
  const isSuccess = variant === 'success';

  return (
    <>
      <div className="fixed inset-0 bg-black/60 z-[60]" onClick={onClose} />
      <div className="fixed inset-0 z-[60] flex items-center justify-center p-4 pointer-events-none">
        <div className="w-full max-w-sm bg-[#172033] border border-slate-700 rounded-2xl pointer-events-auto p-6 text-center relative">
          <button
            onClick={onClose}
            className="absolute top-3 right-3 p-2 rounded-lg hover:bg-white/10 text-slate-500 min-w-[36px] min-h-[36px] flex items-center justify-center"
          >
            <X className="w-4 h-4" />
          </button>

          <div
            className={`w-14 h-14 rounded-2xl border flex items-center justify-center mx-auto mb-4 ${
              isSuccess ? 'bg-emerald-500/10 border-emerald-500/30' : 'bg-red-500/10 border-red-500/30'
            }`}
          >
            {isSuccess ? <Check className="w-7 h-7 text-emerald-400" /> : <AlertTriangle className="w-7 h-7 text-red-400" />}
          </div>

          <p className="text-white font-bold">{title}</p>
          {message && <p className="text-sm text-slate-400 mt-2">{message}</p>}

          <button
            onClick={onClose}
            className="px-6 py-3 rounded-xl bg-sky-500 text-white text-sm font-bold hover:bg-sky-400 min-h-[44px] mt-5"
          >
            {t('common.close')}
          </button>
        </div>
      </div>
    </>
  );
}
