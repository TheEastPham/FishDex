import { useNavigate } from 'react-router-dom';
import type { MarketSpeciesDto, AquariumDto } from '@fishlover/shared';
import { cn, useTranslation, useAuthStore, TradeStatus, LegalStatus } from '@fishlover/shared';
import { Fish, Pencil, AlertTriangle, Ruler } from 'lucide-react';
import { FavoriteButton, AddToAquariumButton } from '../../fish-search/components/SpeciesCardActions';
import AddToCountryButton from './AddToCountryButton';

interface Props {
  species: MarketSpeciesDto;
  index?: number;
  aquariums?: AquariumDto[];
  /**
   * Quốc gia đang xem (alpha-2). Quyết định câu chữ hỏi tên bản ngữ — xem danh sách Thái Lan
   * thì hỏi tên tiếng Thái, không hardcode tiếng Việt.
   */
  countryAlpha2: string;
  /** Mở modal đặt tên bản ngữ. Chỉ dùng khi loài chưa có tên. */
  onAddName?: (specCode: number) => void;
}

const GRADIENTS = [
  'from-slate-700 to-slate-900',
  'from-zinc-700 to-zinc-900',
  'from-stone-700 to-stone-900',
];

/**
 * Thẻ loài trên trang market.
 *
 * Dùng ĐÚNG ngôn ngữ thị giác của thẻ tra cứu (`SpeciesCard`) — cùng chiều cao ảnh, cùng divider,
 * cùng cách canh giữa tên, cùng hàng action. Hai trang cạnh nhau mà thẻ khác hệ thì trang mới
 * trông như chưa làm xong.
 *
 * Khác biệt duy nhất về nội dung, không phải về hình thức: **tên bản ngữ chiếm dòng vàng**
 * (chỗ mà thẻ tra cứu để tên thường gọi), và loài chưa có tên thì có thêm một nút mời đóng góp
 * dạng ghost — cố ý nhẹ để không át tên loài.
 */
export default function MarketSpeciesCard({ species, index = 0, aquariums = [], countryAlpha2, onAddName }: Props) {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const bgGradient = GRADIENTS[index % GRADIENTS.length];

  const languageName = t(`languageNames.${countryAlpha2}`);
  const hasName = species.localName !== null;
  const isRisky = species.legalStatus !== LegalStatus.Legal;

  return (
    <div className={cn(
      'group relative flex flex-col rounded-xl bg-[#202226] border overflow-hidden',
      'hover:shadow-2xl hover:shadow-black/60 hover:-translate-y-1 transition-all duration-300',
      isRisky ? 'border-red-500/40' : 'border-slate-800/80',
    )}>
      {/* Ảnh — cùng chiều cao 170px với thẻ tra cứu để hai lưới đứng cạnh nhau không lệch */}
      <div className="h-[170px] w-full relative overflow-hidden bg-slate-900">
        {species.imageUrl ? (
          <img
            src={species.imageUrl}
            alt={species.scientificName}
            className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-700"
          />
        ) : (
          <div className={cn('absolute inset-0 bg-gradient-to-br opacity-80', bgGradient)}>
            <div className="absolute inset-0 flex items-center justify-center opacity-30 group-hover:opacity-50 transition-opacity duration-300">
              <Fish className="w-16 h-16 text-white stroke-[1.5]" />
            </div>
          </div>
        )}

        <FavoriteButton specCode={species.specCode} className="absolute top-2 right-2" />

        {/* Kích thước nằm đè lên ảnh chứ không chiếm một dòng riêng: nó là thuộc tính hình dáng
            của con cá nên đọc trên ảnh là tự nhiên, và thẻ tiết kiệm được nguyên một dòng —
            đáng kể ở 390px. Đặt góc dưới trái để cân với nút yêu thích ở góc trên phải. */}
        {species.lengthCm !== null && (
          <span
            title={t('market.sizeLabel')}
            className="absolute bottom-2 left-2 inline-flex items-center gap-1 rounded-md bg-black/60 backdrop-blur-sm px-2 py-1 text-[11px] font-medium text-slate-100"
          >
            <Ruler className="w-3 h-3" />
            {formatLength(species.lengthCm)}
          </span>
        )}
      </div>

      <div className="flex flex-col flex-1 px-4 pt-3 pb-4">
        {/* Hàng nhãn — CHỈ dựng khi thật sự có nhãn. Bỏ min-height cố định vì dòng sinh từ bể
            không mang TradeStatus và phần lớn loài hợp pháp, nên giữ chỗ trống là chừa một dòng
            rỗng trên gần như mọi thẻ. Có seed AdminSeed thì badge mức phổ biến sẽ tự hiện lại. */}
        {(species.tradeStatus !== null || isRisky) && (
          <div className="flex items-center gap-1.5 flex-wrap text-slate-300 mb-2.5">
            {species.tradeStatus !== null && <TradeBadge status={species.tradeStatus} />}
            {isRisky && (
              <span
                className="inline-flex items-center gap-1 rounded-md bg-red-500/15 px-2 py-0.5 text-[11px] text-red-400"
                title={species.legalNote ?? undefined}
              >
                <AlertTriangle className="w-3 h-3" />
                {t(species.legalStatus === LegalStatus.Banned ? 'market.legalBanned' : 'market.legalRestricted')}
              </span>
            )}
          </div>
        )}

        <div className="w-full h-px bg-gradient-to-r from-[#f9e5b9]/60 via-[#f9e5b9]/20 to-transparent mb-3" />

        {/* Tên — LUÔN đúng hai dòng ở cùng vị trí trên mọi thẻ: dòng vàng là tên bản ngữ,
            dòng dưới là tên khoa học. Loài chưa có tên thì dòng vàng thành chỗ mời sửa,
            không phải một nút riêng — nút riêng làm khối giữa các thẻ lệch nhau. */}
        <div className="flex flex-col items-center text-center gap-1 mb-3">
          {hasName ? (
            <h3 className="text-[17px] font-bold text-[#f9e5b9] tracking-wide leading-snug line-clamp-1">
              {species.localName}
            </h3>
          ) : (
            <button
              onClick={(e) => { e.stopPropagation(); onAddName?.(species.specCode); }}
              title={t('market.addNameHint', { language: languageName })}
              className="group/name inline-flex items-center gap-1.5 text-[17px] font-medium text-slate-500 hover:text-amber-300 leading-snug transition-colors"
            >
              <span className="line-clamp-1">{t('market.noLocalName', { language: languageName })}</span>
              <Pencil className="w-3.5 h-3.5 shrink-0 opacity-50 group-hover/name:opacity-100 transition-opacity" />
            </button>
          )}
          <p className="text-[13px] text-slate-300 italic font-light line-clamp-1">
            {species.scientificName}
          </p>
        </div>

        <div className="flex-1" />

        <div className="flex mt-auto gap-2">
          <button
            onClick={() => navigate(`/fish/${species.specCode}`)}
            className="flex-1 flex items-center justify-center bg-[#2a2d32] hover:bg-[#32363c] text-white py-2 px-3 text-sm font-bold rounded-lg transition-colors"
          >
            {t('fish.viewProfile')}
          </button>

          {isAuthenticated && <AddToAquariumButton specCode={species.specCode} aquariums={aquariums} />}
          <AddToCountryButton specCode={species.specCode} />
        </div>
      </div>
    </div>
  );
}

function TradeBadge({ status }: { status: TradeStatus }) {
  const { t } = useTranslation();

  const label: Record<TradeStatus, string> = {
    [TradeStatus.Common]: 'market.tradeCommon',
    [TradeStatus.Occasional]: 'market.tradeOccasional',
    [TradeStatus.Seasonal]: 'market.tradeSeasonal',
    [TradeStatus.Rare]: 'market.tradeRare',
  };

  return (
    <span className={cn(
      'rounded-md px-2 py-0.5 text-[11px]',
      status === TradeStatus.Common ? 'bg-emerald-500/15 text-emerald-400' : 'bg-slate-800 text-slate-400',
    )}>
      {t(label[status])}
    </span>
  );
}

/** Số đo thật của FishBase, không quy về nhãn khoảng — nhãn chỉ dùng ở bộ lọc. */
function formatLength(cm: number): string {
  return cm >= 100 ? `${(cm / 100).toFixed(1)} m` : `${Number(cm.toFixed(1))} cm`;
}
