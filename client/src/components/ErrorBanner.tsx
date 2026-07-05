export function ErrorBanner({ message, onDismiss }: { message: string; onDismiss?: () => void }) {
  return (
    <div className="rounded-xl border border-rose-200 bg-rose-50 p-4 text-sm text-rose-700 dark:border-rose-900 dark:bg-rose-950 dark:text-rose-200">
      <div className="flex items-start justify-between gap-3">
        <p className="font-medium">{message}</p>
        {onDismiss && (
          <button
            type="button"
            onClick={onDismiss}
            aria-label="Dismiss"
            className="text-rose-500 hover:text-rose-700 dark:text-rose-300 dark:hover:text-rose-100"
          >
            ×
          </button>
        )}
      </div>
    </div>
  );
}