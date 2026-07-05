import { useState, useEffect } from 'react';
import type { Account, Transaction } from '../hal/api';
import { createTransaction, updateTransaction } from '../hal/api';
import { ErrorBanner } from './ErrorBanner';
import { Spinner } from './Spinner';
import { fetchCategories } from '../features/categories/CategoriesTree';
import type { Category } from '../features/categories/CategoriesTree';

const TYPES = ['Expense', 'Income'] as const;

export function AddTransactionForm({
  token,
  accounts,
  defaultAccountId,
  initialData,
  onCreated,
  onCancel,
}: {
  token: string;
  accounts: Account[];
  defaultAccountId?: string;
  initialData?: Transaction;
  onCreated?: (txn: Transaction) => void;
  onCancel?: () => void;
}) {
  const [type, setType] = useState<(typeof TYPES)[number]>(initialData?.type ?? 'Expense');
  const [amount, setAmount] = useState(initialData?.amount.toString() ?? '');
  const [accountId, setAccountId] = useState(initialData?.accountId ?? defaultAccountId ?? accounts[0]?.id ?? '');
  const [occurredOn, setOccurredOn] = useState(() => initialData?.occurredOn.slice(0, 10) ?? new Date().toISOString().slice(0, 10));
  const [memo, setMemo] = useState(initialData?.memo ?? '');
  const [categoryId, setCategoryId] = useState(initialData?.categoryId ?? '');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [categories, setCategories] = useState<Category[]>([]);

  useEffect(() => {
    if (token) fetchCategories(token, false).then(setCategories).catch(console.error);
  }, [token]);

  const selectedAccount = accounts.find((a) => a.id === accountId);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (submitting) return;
    if (!accountId) {
      setError('Create an account first.');
      return;
    }
    setError(null);
    setSubmitting(true);
    try {
      let txn;
      if (initialData) {
        txn = await updateTransaction(token, initialData.id, {
          type,
          amount: Number(amount) || 0,
          currency: selectedAccount?.currency ?? 'USD',
          occurredOn,
          memo: memo.trim() || null,
          categoryId: categoryId || null,
        });
      } else {
        txn = await createTransaction(token, accountId, {
          type,
          amount: Number(amount) || 0,
          currency: selectedAccount?.currency ?? 'USD',
          occurredOn,
          memo: memo.trim() || null,
          categoryId: categoryId || null,
        });
      }
      onCreated?.(txn);
      setAmount('');
      setMemo('');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not create transaction');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-4">
      {error && <ErrorBanner message={error} onDismiss={() => setError(null)} />}
      <div className="grid grid-cols-2 gap-3">
        <Field label="Type">
          <select value={type} onChange={(e) => setType(e.target.value as (typeof TYPES)[number])} className="input">
            {TYPES.map((t) => (
              <option key={t} value={t}>
                {t}
              </option>
            ))}
          </select>
        </Field>
        <Field label="Amount">
          <input
            type="number"
            step="0.01"
            min="0"
            required
            value={amount}
            onChange={(e) => setAmount(e.target.value)}
            className="input"
          />
        </Field>
      </div>
      <Field label="Account">
        <select value={accountId} onChange={(e) => setAccountId(e.target.value)} className="input" disabled={!!initialData}>
          {accounts.length === 0 && <option value="">No accounts yet</option>}
          {accounts.map((a) => (
            <option key={a.id} value={a.id}>
              {a.name} ({a.currency})
            </option>
          ))}
        </select>
      </Field>
      <Field label="Date">
        <input type="date" required value={occurredOn} onChange={(e) => setOccurredOn(e.target.value)} className="input" />
      </Field>
      <Field label="Category">
        <select value={categoryId} onChange={(e) => setCategoryId(e.target.value)} className="input">
          <option value="">None</option>
          {categories.filter(c => c.kind === type || c.kind === 'Either').map((c) => (
            <option key={c.id} value={c.id}>
              {c.name}
            </option>
          ))}
        </select>
      </Field>
      <Field label="Memo">
        <input type="text" value={memo} onChange={(e) => setMemo(e.target.value)} placeholder="Optional" className="input" />
      </Field>
      <div className="flex items-center gap-2">
        <button
          type="submit"
          disabled={submitting || !accountId}
          className="inline-flex items-center gap-2 rounded-lg bg-sky-600 px-4 py-2 text-white font-medium hover:bg-sky-700 disabled:opacity-60"
        >
          {submitting && <Spinner />}
          {initialData ? 'Update transaction' : 'Save transaction'}
        </button>
        {onCancel && (
          <button type="button" onClick={onCancel} className="rounded-lg px-3 py-2 text-sm text-slate-500 hover:bg-slate-100 dark:hover:bg-slate-800">
            Cancel
          </button>
        )}
      </div>
    </form>
  );
}

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <label className="flex flex-col gap-1 text-sm">
      <span className="font-medium text-slate-700 dark:text-slate-300">{label}</span>
      {children}
    </label>
  );
}