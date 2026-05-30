import { Fish } from 'lucide-react';

export default function SpeciesCardSkeleton() {
  return (
    <div className="flex flex-col rounded-2xl bg-white shadow-sm border border-slate-100 overflow-hidden animate-pulse">
      {/* Image Placeholder Skeleton */}
      <div className="h-40 w-full bg-slate-100 flex items-center justify-center">
        <Fish className="w-12 h-12 text-slate-200 stroke-[1.5]" />
      </div>

      <div className="flex flex-col flex-1 p-5 gap-3">
        {/* Scientific name */}
        <div>
          <div className="h-2.5 w-16 bg-slate-200 rounded mb-2" />
          <div className="h-5 w-40 bg-slate-200 rounded" />
        </div>

        {/* Common name badge */}
        <div className="min-h-[24px]">
          <div className="h-6 w-24 bg-slate-100 rounded-md" />
        </div>

        {/* Genus */}
        <div className="mt-auto pt-2 pb-3 border-t border-slate-50 flex justify-between items-center">
          <div className="h-3 w-12 bg-slate-100 rounded" />
          <div className="h-3 w-20 bg-slate-100 rounded" />
        </div>

        {/* Action */}
        <div className="w-full h-10 rounded-xl bg-slate-100" />
      </div>
    </div>
  );
}
