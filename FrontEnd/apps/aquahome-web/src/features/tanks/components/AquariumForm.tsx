import { useEffect, useState } from 'react';
import { cn, useTranslation, WaterType, AquariumStyle, MARKET_COUNTRIES, getStoredCountry, findCountry } from '@fishlover/shared';
import type { AquariumDto, CreateAquariumRequest } from '@fishlover/shared';
import { X, FlaskConical, Droplets, Save, Loader } from 'lucide-react';

interface Props {
  isOpen: boolean;
  onClose: () => void;
  onSave: (data: CreateAquariumRequest) => Promise<void>;
  editing?: AquariumDto | null;
}

const SELECT_CLASS = 'w-full bg-[#0F172A] border border-slate-700/60 rounded-xl px-4 py-3 text-white focus:outline-none focus:border-sky-500/60 focus:ring-1 focus:ring-sky-500/30 transition-all appearance-none';

export default function AquariumForm({ isOpen, onClose, onSave, editing }: Props) {
  const { t } = useTranslation();
  const [name, setName] = useState('');
  const [waterType, setWaterType] = useState<WaterType | ''>('');
  const [style, setStyle] = useState<AquariumStyle | ''>('');
  const [length, setLength] = useState('');
  const [width, setWidth] = useState('');
  const [height, setHeight] = useState('');
  const [description, setDescription] = useState('');
  // Quốc gia đặt bể. Gắn vào BỂ chứ không vào user nên một người có thể có bể ở nhiều nước.
  // Đây là nguồn dữ liệu cho lớp market: bể ở nước X có cá Z nghĩa là nước X bán cá Z.
  const [countryCode, setCountryCode] = useState('');
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (editing) {
      setName(editing.name);
      setWaterType(editing.waterType ?? '');
      setStyle(editing.style ?? '');
      setLength(editing.lengthCm?.toString() ?? '');
      setWidth(editing.widthCm?.toString() ?? '');
      setHeight(editing.heightCm?.toString() ?? '');
      setDescription(editing.description ?? '');
      setCountryCode(editing.countryCode ?? '');
    } else {
      setName(''); setWaterType(''); setStyle('');
      setLength(''); setWidth(''); setHeight(''); setDescription('');
      // Bể mới mặc định theo nước đang chọn ở trang market — đỡ một thao tác cho phần lớn người dùng
      setCountryCode(findCountry(getStoredCountry())?.code ?? '');
    }
  }, [editing, isOpen]);

  const l = parseFloat(length) || 0;
  const w = parseFloat(width) || 0;
  const h = parseFloat(height) || 0;
  const volume = l > 0 && w > 0 && h > 0 ? (l * w * h) / 1000 : null;

  const handleSubmit = async (e?: React.FormEvent) => {
    if (e) e.preventDefault();
    if (!name.trim()) return;
    setSaving(true);
    try {
      await onSave({
        name: name.trim(),
        waterType: waterType !== '' ? waterType : null,
        style: style !== '' ? style : null,
        lengthCm: l > 0 ? l : null,
        widthCm: w > 0 ? w : null,
        heightCm: h > 0 ? h : null,
        description: description.trim() || null,
        countryCode: countryCode || null,
      });
      onClose();
    } finally {
      setSaving(false);
    }
  };

  if (!isOpen) return null;

  const waterTypeOptions = [
    { value: WaterType.Freshwater, label: t('tanks.wt_freshwater') },
    { value: WaterType.Saltwater,  label: t('tanks.wt_saltwater')  },
    { value: WaterType.Brackish,   label: t('tanks.wt_brackish')   },
  ];

  const styleOptions = [
    { value: AquariumStyle.Nature,     label: t('tanks.st_nature')     },
    { value: AquariumStyle.Dutch,      label: t('tanks.st_dutch')      },
    { value: AquariumStyle.Iwagumi,    label: t('tanks.st_iwagumi')    },
    { value: AquariumStyle.Biotope,    label: t('tanks.st_biotope')    },
    { value: AquariumStyle.Reef,       label: t('tanks.st_reef')       },
    { value: AquariumStyle.Blackwater, label: t('tanks.st_blackwater') },
    { value: AquariumStyle.Community,  label: t('tanks.st_community')  },
    { value: AquariumStyle.Predator,   label: t('tanks.st_predator')   },
    { value: AquariumStyle.Paludarium, label: t('tanks.st_paludarium') },
  ];

  return (
    <>
      <div className="fixed inset-0 bg-black/60 backdrop-blur-sm z-40" onClick={onClose} />

      <div className="fixed inset-y-0 right-0 w-full max-w-md bg-[#172033] border-l border-slate-800/60 shadow-2xl z-50 flex flex-col animate-in slide-in-from-right duration-300">

        {/* Header */}
        <div className="flex items-center justify-between px-6 py-5 border-b border-slate-800/60">
          <div className="flex items-center gap-3">
            <div className="p-2 bg-sky-500/10 rounded-xl border border-sky-500/20">
              <Droplets className="w-5 h-5 text-sky-400" />
            </div>
            <div>
              <h2 className="text-white font-bold text-lg">
                {editing ? 'Chỉnh sửa hồ cá' : 'Thêm hồ cá mới'}
              </h2>
              <p className="text-slate-500 text-xs">Điền thông tin bên dưới</p>
            </div>
          </div>
          <button onClick={onClose} className="p-2 hover:bg-white/5 rounded-xl transition-colors">
            <X className="w-5 h-5 text-slate-400" />
          </button>
        </div>

        {/* Form body */}
        <form onSubmit={handleSubmit} className="flex-1 overflow-y-auto p-6 space-y-5">

          {/* Name */}
          <div>
            <label className="block text-xs font-bold text-slate-400 uppercase tracking-widest mb-2">
              Tên hồ cá <span className="text-red-400">*</span>
            </label>
            <input
              value={name}
              onChange={e => setName(e.target.value)}
              placeholder="Ví dụ: Hồ Amazon của tôi..."
              required
              className="w-full bg-[#0F172A] border border-slate-700/60 rounded-xl px-4 py-3 text-white placeholder-slate-600 focus:outline-none focus:border-sky-500/60 focus:ring-1 focus:ring-sky-500/30 transition-all"
            />
          </div>

          {/* Quốc gia đặt bể — nguồn dữ liệu cho lớp market bên FishDex */}
          <div>
            <label className="block text-xs font-bold text-slate-400 uppercase tracking-widest mb-2">
              {t('market.countryLabel')}
            </label>
            <select
              value={countryCode}
              onChange={e => setCountryCode(e.target.value)}
              className={cn(SELECT_CLASS, countryCode === '' ? 'text-slate-600' : 'text-white')}
            >
              <option value="">—</option>
              {MARKET_COUNTRIES.map(c => (
                <option key={c.alpha2} value={c.code} className="bg-[#0F172A]">
                  {t(`countries.${c.alpha2}`)}
                </option>
              ))}
            </select>
          </div>

          {/* Water Type */}
          <div>
            <label className="block text-xs font-bold text-slate-400 uppercase tracking-widest mb-2">
              Loại hồ
            </label>
            <select
              value={waterType}
              onChange={e => setWaterType(e.target.value !== '' ? Number(e.target.value) as WaterType : '')}
              className={cn(SELECT_CLASS, waterType === '' ? 'text-slate-600' : 'text-white')}
            >
              <option value="">{t('tanks.waterTypePlaceholder')}</option>
              {waterTypeOptions.map(o => (
                <option key={o.value} value={o.value} className="bg-[#0F172A]">{o.label}</option>
              ))}
            </select>
          </div>

          {/* Style */}
          <div>
            <label className="block text-xs font-bold text-slate-400 uppercase tracking-widest mb-2">
              Phong cách
            </label>
            <select
              value={style}
              onChange={e => setStyle(e.target.value !== '' ? Number(e.target.value) as AquariumStyle : '')}
              className={cn(SELECT_CLASS, style === '' ? 'text-slate-600' : 'text-white')}
            >
              <option value="">{t('tanks.stylePlaceholder')}</option>
              {styleOptions.map(o => (
                <option key={o.value} value={o.value} className="bg-[#0F172A]">{o.label}</option>
              ))}
            </select>
          </div>

          {/* Dimensions + Live Volume */}
          <div>
            <label className="block text-xs font-bold text-slate-400 uppercase tracking-widest mb-2">
              Kích thước (cm)
            </label>
            <div className="grid grid-cols-3 gap-2">
              {[
                { label: 'Dài', val: length, set: setLength },
                { label: 'Rộng', val: width, set: setWidth },
                { label: 'Cao', val: height, set: setHeight },
              ].map(({ label, val, set }) => (
                <div key={label}>
                  <p className="text-xs text-slate-500 mb-1.5 text-center">{label}</p>
                  <input
                    type="number"
                    min={0}
                    value={val}
                    onChange={e => set(e.target.value)}
                    placeholder="0"
                    className="w-full bg-[#0F172A] border border-slate-700/60 rounded-xl px-3 py-3 text-white text-center placeholder-slate-700 focus:outline-none focus:border-sky-500/60 focus:ring-1 focus:ring-sky-500/30 transition-all"
                  />
                </div>
              ))}
            </div>

            <div className={cn(
              'mt-3 rounded-xl p-3 border flex items-center gap-3 transition-all duration-500',
              volume != null
                ? 'bg-emerald-500/10 border-emerald-500/20 opacity-100'
                : 'bg-[#0F172A] border-slate-800/40 opacity-50'
            )}>
              <FlaskConical className={cn('w-5 h-5 shrink-0', volume != null ? 'text-emerald-400' : 'text-slate-600')} />
              <div>
                <p className="text-xs text-slate-500">Thể tích tính toán</p>
                <p className={cn('text-lg font-black transition-all', volume != null ? 'text-emerald-300' : 'text-slate-600')}>
                  {volume != null ? `${volume.toFixed(2)} Lít` : '— Lít'}
                </p>
              </div>
            </div>
          </div>

          {/* Description */}
          <div>
            <label className="block text-xs font-bold text-slate-400 uppercase tracking-widest mb-2">Mô tả</label>
            <textarea
              value={description}
              onChange={e => setDescription(e.target.value)}
              placeholder="Ghi chú về hồ cá..."
              rows={3}
              className="w-full bg-[#0F172A] border border-slate-700/60 rounded-xl px-4 py-3 text-white placeholder-slate-600 focus:outline-none focus:border-sky-500/60 focus:ring-1 focus:ring-sky-500/30 transition-all resize-none"
            />
          </div>
        </form>

        {/* Footer */}
        <div className="px-6 py-4 border-t border-slate-800/60 flex gap-3">
          <button
            type="button"
            onClick={onClose}
            className="flex-1 py-3 rounded-xl bg-white/5 hover:bg-white/10 text-slate-300 font-bold transition-colors"
          >
            Huỷ
          </button>
          <button
            onClick={() => handleSubmit()}
            disabled={saving || !name.trim()}
            className="flex-1 py-3 rounded-xl bg-sky-500 hover:bg-sky-400 disabled:opacity-50 text-white font-bold transition-colors flex items-center justify-center gap-2"
          >
            {saving ? <Loader className="w-4 h-4 animate-spin" /> : <Save className="w-4 h-4" />}
            {editing ? 'Lưu thay đổi' : 'Tạo hồ cá'}
          </button>
        </div>
      </div>
    </>
  );
}
