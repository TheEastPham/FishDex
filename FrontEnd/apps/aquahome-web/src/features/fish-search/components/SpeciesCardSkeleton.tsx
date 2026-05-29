export default function SpeciesCardSkeleton() {
  return (
    <div className="flex flex-col gap-3 rounded-xl border border-border bg-card p-4 animate-pulse">
      <div className="space-y-1.5">
        <div className="h-3 w-20 rounded bg-muted" />
        <div className="h-4 w-36 rounded bg-muted" />
      </div>
      <div className="h-5 w-24 rounded-full bg-muted" />
      <div className="h-3 w-28 rounded bg-muted" />
      <div className="mt-auto h-8 w-full rounded-lg bg-muted" />
    </div>
  );
}
