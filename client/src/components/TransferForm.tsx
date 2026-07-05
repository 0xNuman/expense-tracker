import { useState } from 'react';
import type { Account } from '../hal/api';
import { ErrorBanner } from './ErrorBanner';
import { Spinner } from './Spinner';

export function TransferForm({
  accounts,
  onCreated,
  onCancel,
}: {
  accounts: Account[];
  onCreated?: () => void;
  onCancel?: () => void;
}) {
  const [sourceAccountId, setSourceAccountId] = useState('');
  const [destinationAccountId, setDestinationAccountId] = useState('');
  const [amount, setAmount] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (submitting) return;
    setError(null);
    setSubmitting(true);
    try {
      // Stub: in reality call createTransfer API
      onCreated?.();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not create transfer');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-4">
      {error && <ErrorBanner message={error} onDismiss={() => setError(null)} />}
      <Field label="From Account">
        <select value={sourceAccountId} onChange={(e) => setSourceAccountId(e.target.value)} required className="input">
          <option value="">Select source account</option>
          {accounts.map((a) => (
            <option key={a.id} value={a.id}>{a.name} ({a.balance} {a.currency})</option>
          ))}
        </select>
      </Field>
      <Field label="To Account">
        <select value={destinationAccountId} onChange={(e) => setDestinationAccountId(e.target.value)} required className="input">
          <option value="">Select destination account</option>
          {accounts.filter(a => a.id !== sourceAccountId).map((a) => (
            <option key={a.id} value={a.id}>{a.name} ({a.balance} {a.currency})</option>
          ))}
        </select>
      </Field>
      <Field label="Amount">
        <input type="number" step="0.01" min="0" required value={amount} onChange={(e) => setAmount(e.target.value)} className="input" />
      </Field>
      <div className="flex items-center gap-2">
        <button type="submit" disabled={submitting || !sourceAccountId || !destinationAccountId} className="inline-flex items-center gap-2 rounded-lg bg-sky-600 px-4 py-2 text-white font-medium hover:bg-sky-700 disabled:opacity-60">
          {submitting && <Spinner />}
          Transfer
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
