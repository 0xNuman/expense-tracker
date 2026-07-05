export function Spinner({ className = '' }: { className?: string }) {
  return (
    <span
      role="status"
      aria-label="Loading"
      className={`inline-block animate-spin rounded-full border-2 border-slate-300 border-t-sky-600 dark:border-slate-700 dark:border-t-sky-400 ${className}`}
      style={{ width: '1.25rem', height: '1.25rem' }}
    />
  );
}