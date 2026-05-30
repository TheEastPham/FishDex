import { useState, useRef, useEffect } from 'react';
import { Filter, ChevronDown, Check } from 'lucide-react';
import { useTranslation, cn } from '@fishlover/shared';
import type { Family } from '@fishlover/shared';

interface Props {
  families: Family[];
  value: string;
  onChange: (value: string) => void;
}

export default function FamilySelect({ families, value, onChange }: Props) {
  const { t } = useTranslation();
  const [isOpen, setIsOpen] = useState(false);
  const [searchTerm, setSearchTerm] = useState('');
  const wrapperRef = useRef<HTMLDivElement>(null);

  const selectedFamily = families.find((f) => f.id === value);
  const allFamiliesText = t('fish.allFamilies') || 'All Families';

  // Display text in the input box when NOT typing
  const displayText = selectedFamily
    ? `${selectedFamily.name} ${selectedFamily.commonName ? `(${selectedFamily.commonName})` : ''}`
    : allFamiliesText;

  // Filtered families based on search term
  const filteredFamilies = families.filter((f) => {
    const searchStr = `${f.name} ${f.commonName || ''}`.toLowerCase();
    return searchStr.includes(searchTerm.toLowerCase());
  });

  // Handle outside click to close dropdown
  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (wrapperRef.current && !wrapperRef.current.contains(event.target as Node)) {
        setIsOpen(false);
        setSearchTerm('');
      }
    }
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  return (
    <div ref={wrapperRef} className="relative w-full sm:w-1/3">
      <div
        className={cn(
          "relative flex items-center bg-slate-50/50 rounded-xl border transition-colors cursor-text h-full",
          isOpen ? "border-primary/30 bg-white ring-4 ring-primary/10" : "border-transparent hover:bg-slate-100/50"
        )}
        onClick={() => setIsOpen(true)}
      >
        <Filter className={cn("absolute left-4 h-4 w-4 pointer-events-none transition-colors", isOpen ? "text-primary" : "text-slate-400")} />
        
        <input
          type="text"
          className="w-full bg-transparent pl-11 pr-10 py-3 text-sm font-medium text-slate-700 focus:outline-none placeholder:text-slate-700"
          placeholder={displayText}
          value={isOpen ? searchTerm : ''}
          onChange={(e) => {
            setSearchTerm(e.target.value);
            if (!isOpen) setIsOpen(true);
          }}
          onFocus={() => setIsOpen(true)}
        />
        
        <div className="absolute right-4 pointer-events-none text-slate-400">
          <ChevronDown className={cn("h-4 w-4 transition-transform duration-200", isOpen && "rotate-180")} />
        </div>
      </div>

      {/* Dropdown Menu */}
      {isOpen && (
        <div className="absolute z-50 mt-2 w-[300px] max-h-64 overflow-y-auto rounded-xl border border-slate-100 bg-white p-1 shadow-xl shadow-slate-200/50">
          {/* "All Families" Option */}
          <button
            onClick={() => {
              onChange('');
              setSearchTerm('');
              setIsOpen(false);
            }}
            className={cn(
              "w-full flex items-center justify-between px-3 py-2.5 text-sm rounded-lg text-left transition-colors",
              !value ? "bg-primary/5 text-primary font-semibold" : "text-slate-700 hover:bg-slate-50"
            )}
          >
            {allFamiliesText}
            {!value && <Check className="h-4 w-4" />}
          </button>
          
          {filteredFamilies.length === 0 ? (
            <div className="px-3 py-4 text-center text-sm text-slate-500">
              No families found
            </div>
          ) : (
            filteredFamilies.map((f) => {
              const isSelected = f.id === value;
              return (
                <button
                  key={f.id}
                  onClick={() => {
                    onChange(f.id);
                    setSearchTerm('');
                    setIsOpen(false);
                  }}
                  className={cn(
                    "w-full flex items-center justify-between px-3 py-2.5 text-sm rounded-lg text-left transition-colors mt-0.5",
                    isSelected ? "bg-primary/5 text-primary font-semibold" : "text-slate-700 hover:bg-slate-50"
                  )}
                >
                  <span>
                    {f.name} {f.commonName && <span className="text-slate-400 font-normal ml-1">({f.commonName})</span>}
                  </span>
                  {isSelected && <Check className="h-4 w-4" />}
                </button>
              );
            })
          )}
        </div>
      )}
    </div>
  );
}
