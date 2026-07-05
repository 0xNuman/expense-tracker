import { useState } from 'react';
import type { Account } from '../hal/api';
import { createAccount, updateAccount } from '../hal/api';
import { ErrorBanner } from './ErrorBanner';
import { Spinner } from './Spinner';

const ACCOUNT_TYPES = ['Cash', 'Checking', 'Savings', 'CreditCard', 'Prepaid'] as const;

export function AddAccountForm({
  token,
  onCreated,
  onCancel,
  initialData,
}: {
  token: string;
  onCreated?: (account: Account) => void;
  onCancel?: () => void;
  initialData?: Account;
}) {
  const [name, setName] = useState(initialData?.name ?? '');
  const [type, setType] = useState<(typeof ACCOUNT_TYPES)[number]>(
    (initialData?.type as any) ?? 'Checking'
  );
  const [currency, setCurrency] = useState(initialData?.currency ?? 'USD');
  const [openingBalance, setOpeningBalance] = useState(
    initialData?.openingBalance?.toString() ?? '0'
  );
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (submitting) return;
    setError(null);
    setSubmitting(true);
    try {
      let account;
      if (initialData) {
        account = await updateAccount(token, initialData.id, name.trim(), type);
      } else {
        account = await createAccount(token, {
          name: name.trim(),
          type,
          currency: currency.trim().toUpperCase(),
          openingBalance: Number(openingBalance) || 0,
        });
      }
      onCreated?.(account);
      setName('');
      setOpeningBalance('0');
    } catch (err) {
      setError(err instanceof Error ? err.message : initialData ? 'Could not update account' : 'Could not create account');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-4">
      {error && <ErrorBanner message={error} onDismiss={() => setError(null)} />}
      <Field label="Name">
        <input
          type="text"
          required
          value={name}
          onChange={(e) => setName(e.target.value)}
          placeholder="e.g. Daily spending"
          className="input"
        />
      </Field>
      <Field label="Type">
        <select value={type} onChange={(e) => setType(e.target.value as (typeof ACCOUNT_TYPES)[number])} className="input">
          {ACCOUNT_TYPES.map((t) => (
            <option key={t} value={t}>
              {t}
            </option>
          ))}
        </select>
      </Field>
      {!initialData && (
        <>
          <Field label="Currency">
            <select
              required
              value={currency}
              onChange={(e) => setCurrency(e.target.value)}
              className="input"
            >
              <option value="USD">USD - US Dollar</option>
              <option value="EUR">EUR - Euro</option>
              <option value="GBP">GBP - British Pound</option>
              <option value="JPY">JPY - Japanese Yen</option>
              <option value="INR">INR - Indian Rupee</option>
              <option value="CAD">CAD - Canadian Dollar</option>
              <option value="AUD">AUD - Australian Dollar</option>
            </select>
          </Field>
          <Field label="Opening balance">
            <input
              type="number"
              step="0.01"
              value={openingBalance}
              onChange={(e) => setOpeningBalance(e.target.value)}
              className="input"
            />
          </Field>
        </>
      )}
      <div className="flex items-center gap-2">
        <button
          type="submit"
          disabled={submitting}
          className="inline-flex items-center gap-2 rounded-lg bg-sky-600 px-4 py-2 text-white font-medium hover:bg-sky-700 disabled:opacity-60"
        >
          {submitting && <Spinner />}
          {initialData ? 'Update account' : 'Save account'}
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