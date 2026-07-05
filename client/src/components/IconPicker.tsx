import { useState, useMemo } from 'react';
import * as LucideIcons from 'lucide-react';
import { icons } from 'lucide-react';
import type { LucideIcon } from 'lucide-react';

const ALL_ICON_NAMES = Object.keys(icons);

interface IconPickerProps {
  value?: string;
  onChange: (iconName: string) => void;
}

export function IconPicker({ value, onChange }: IconPickerProps) {
  const [search, setSearch] = useState('');

  const filteredIcons = useMemo(() => {
    const q = search.toLowerCase();
    const results = ALL_ICON_NAMES.filter(name => name.toLowerCase().includes(q));
    
    // If we have a currently selected value, ensure it's in the list so the user sees it selected
    if (value && !results.includes(value)) {
      results.unshift(value);
    }
    
    return results.slice(0, 72); // limit to 72 to prevent DOM lag
  }, [search, value]);

  return (
    <div className="flex flex-col gap-2">
      <div className="relative">
        <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
          <LucideIcons.Search className="h-4 w-4 text-gray-400" />
        </div>
        <input
          type="text"
          placeholder="Search 1,400+ icons..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="w-full pl-9 pr-3 py-2 rounded-xl border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-sm text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none"
        />
      </div>
      
      <div className="grid grid-cols-6 gap-2 max-h-56 overflow-y-auto p-1 bg-gray-50/50 dark:bg-gray-800/30 rounded-xl border border-gray-100 dark:border-gray-700/50">
        {filteredIcons.map(iconName => {
          const Icon = (icons as any)[iconName] as LucideIcon;
          if (!Icon) return null;
          const isSelected = value === iconName;
          return (
            <button
              key={iconName}
              type="button"
              onClick={() => onChange(iconName)}
              className={`flex items-center justify-center p-2 rounded-xl transition-all ${
                isSelected 
                  ? 'bg-sky-100 text-sky-700 ring-2 ring-sky-500 dark:bg-sky-900/40 dark:text-sky-300 dark:ring-sky-400' 
                  : 'bg-white text-slate-600 hover:bg-slate-200 shadow-sm border border-slate-100 dark:border-slate-700 dark:bg-slate-800 dark:text-slate-400 dark:hover:bg-slate-700'
              }`}
              title={iconName}
            >
              <Icon className="w-5 h-5" />
            </button>
          );
        })}
        {filteredIcons.length === 0 && (
          <div className="col-span-6 text-center py-4 text-sm text-gray-500 dark:text-gray-400">
            No icons found
          </div>
        )}
      </div>
    </div>
  );
}

export function CategoryIconRenderer({ iconName, className }: { iconName?: string, className?: string }) {
  if (!iconName) {
    return <LucideIcons.Folder className={className || "w-5 h-5"} />;
  }
  const Icon = (icons as any)[iconName] as LucideIcon;
  if (!Icon) {
    return <LucideIcons.Folder className={className || "w-5 h-5"} />;
  }
  return <Icon className={className || "w-5 h-5"} />;
}
