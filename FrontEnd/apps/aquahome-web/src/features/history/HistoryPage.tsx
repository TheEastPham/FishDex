import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { useTranslation, getRecentlyViewed } from '@fishlover/shared';
import type { RecentlyViewedDto } from '@fishlover/shared';
import { Clock, Fish } from 'lucide-react';

export default function HistoryPage() {
  const { t, i18n } = useTranslation();
  const [items, setItems] = useState<RecentlyViewedDto[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    getRecentlyViewed()
      .then(setItems)
      .catch(() => setItems([]))
      .finally(() => setLoading(false));
  }, []);

  const formatDate = (iso: string) =>
    new Intl.DateTimeFormat(i18n.language, {
      day: '2-digit', month: '2-digit', year: 'numeric',
      hour: '2-digit', minute: '2-digit',
    }).format(new Date(iso));

  return (
    <div className="max-w-2xl mx-auto px-4 py-8">
      <div className="flex items-center gap-3 mb-6">
        <Clock className="w-6 h-6 text-primary" />
        <h1 className="text-2xl font-bold">{t('history.title')}</h1>
      </div>

      {loading ? (
        <div className="space-y-3">
          {Array.from({ length: 5 }).map((_, i) => (
            <div key={i} className="h-16 rounded-lg bg-muted animate-pulse" />
          ))}
        </div>
      ) : items.length === 0 ? (
        <div className="flex flex-col items-center gap-3 py-20 text-muted-foreground">
          <Fish className="w-12 h-12 opacity-30" />
          <p className="font-medium">{t('history.empty')}</p>
          <p className="text-sm">{t('history.emptyHint')}</p>
          <Link to="/fish" className="mt-2 text-sm text-primary underline underline-offset-2">
            {t('nav.fishSearch')}
          </Link>
        </div>
      ) : (
        <div className="space-y-2">
          {items.map((item) => (
            <div
              key={item.specCode}
              className="flex items-center justify-between rounded-lg border bg-card px-4 py-3 hover:bg-muted/50 transition-colors"
            >
              <div className="flex items-center gap-3">
                <Fish className="w-5 h-5 text-muted-foreground shrink-0" />
                <div>
                  <p className="font-medium text-sm">SpecCode #{item.specCode}</p>
                  <p className="text-xs text-muted-foreground">
                    {t('history.viewedAt')}: {formatDate(item.viewedAt)}
                  </p>
                </div>
              </div>
              <Link
                to={`/fish/${item.specCode}`}
                className="text-xs text-primary hover:underline underline-offset-2 shrink-0 ml-4"
              >
                {t('history.viewDetail')} →
              </Link>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
