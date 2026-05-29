import type { SpeciesSearchResult } from '@fishlover/shared';
import { cn } from '@fishlover/shared';

interface Props {
  species: SpeciesSearchResult;
  onAddToAquarium?: (species: SpeciesSearchResult) => void;
  /** AquaHome Service chưa sẵn sàng — disable button cho đến Story 5.2 */
  aquariumReady?: boolean;
}

export default function SpeciesCard({ species, onAddToAquarium, aquariumReady = false }: Props) {
  return (
    <div className="flex flex-col gap-3 rounded-xl border border-border bg-card p-4 shadow-sm hover:shadow-md transition-shadow">

      {/* Scientific name */}
      <div>
        <p className="text-xs font-medium text-muted-foreground uppercase tracking-wide mb-0.5">
          {species.familyName ?? '—'}
        </p>
        <h3 className="font-semibold text-foreground italic leading-snug">
          {species.speciesName}
        </h3>
      </div>

      {/* Common name badge */}
      {species.preferredCommonName && (
        <span className="inline-flex w-fit items-center rounded-full bg-primary/10 px-2.5 py-0.5 text-xs font-medium text-primary">
          {species.preferredCommonName}
        </span>
      )}

      {/* Genus */}
      {species.genusName && (
        <p className="text-xs text-muted-foreground">
          Genus: <span className="italic">{species.genusName}</span>
        </p>
      )}

      {/* Action */}
      <button
        onClick={() => onAddToAquarium?.(species)}
        disabled={!aquariumReady}
        className={cn(
          'mt-auto w-full rounded-lg px-3 py-2 text-sm font-medium transition-colors',
          aquariumReady
            ? 'bg-primary text-primary-foreground hover:bg-primary/90'
            : 'bg-muted text-muted-foreground cursor-not-allowed',
        )}
        title={aquariumReady ? undefined : 'Cần AquaHome Service (Story 5.2)'}
      >
        + Thêm vào bể
      </button>

    </div>
  );
}
