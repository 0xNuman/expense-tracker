import type { Account } from '../hal/api';

const TYPE_GLYPH: Record<string, string> = {
  Cash: '$',
  Checking: '🏦',
  Savings: '🐷',
  CreditCard: '💳',
  Prepaid: '🎫',
};

export function AccountCard({ account, onRenameRequested }: { account: Account; onRenameRequested?: (account: Account) => void }) {
  const glyph = TYPE_GLYPH[account.type] ?? '•';
  return (
    <div className="rounded-xl border border-slate-200 bg-white p-4 dark:border-slate-800 dark:bg-slate-900">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <span aria-hidden className="text-lg">
            {glyph}
          </span>
          <div>
            <div className="flex items-center gap-2">
              <p className="font-medium leading-tight">{account.name}</p>
              {onRenameRequested && (
                <button
                  type="button"
                  onClick={() => onRenameRequested(account)}
                  className="text-[10px] uppercase tracking-wider text-slate-400 hover:text-sky-600 dark:hover:text-sky-400"
                >
                  Rename
                </button>
              )}
            </div>
            <p className="text-xs text-slate-500 dark:text-slate-400">
              {account.type} · {account.currency}
            </p>
          </div>
        </div>
        <div className="text-right">
          <p className="font-mono text-lg tabular-nums">
            {account.balance.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
          </p>
          <p className="text-xs text-slate-400">{account.currency} balance</p>
        </div>
      </div>
      {account.isArchived && (
        <span className="mt-2 inline-block rounded-full bg-amber-100 px-2 py-0.5 text-xs text-amber-700 dark:bg-amber-950 dark:text-amber-300">
          Archived
        </span>
      )}
    </div>
  );
}