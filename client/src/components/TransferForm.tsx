import { useState, useEffect } from 'react';
import type { Account } from '../hal/api';
import { createTransfer } from '../hal/api';
import { fxApi } from '../api/fxApi';
import { useAuth } from '../auth/AuthContext';
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
  const { accessToken, user } = useAuth();
  const [sourceAccountId, setSourceAccountId] = useState('');
  const [destinationAccountId, setDestinationAccountId] = useState('');
  const [amount, setAmount] = useState('');
  const [destinationAmount, setDestinationAmount] = useState('');
  const [memo, setMemo] = useState('');
  const [occurredOn, setOccurredOn] = useState(() => new Date().toISOString().split('T')[0]);
  const [customRate, setCustomRate] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [rate, setRate] = useState<number | null>(null);
  const [fetchingRate, setFetchingRate] = useState(false);

  const srcAcc = accounts.find(a => a.id === sourceAccountId);
  const destAcc = accounts.find(a => a.id === destinationAccountId);
  const isCrossCurrency = srcAcc && destAcc && srcAcc.currency !== destAcc.currency;

  useEffect(() => {
    let canceled = false;
    if (isCrossCurrency && accessToken) {
       setFetchingRate(true);
       fxApi.getRates(accessToken, srcAcc.currency, occurredOn).then(res => {
         if (canceled) return;
         const qt = res.rates[destAcc.currency];
         if (qt) {
            setRate(qt.rate);
            setCustomRate(qt.rate.toFixed(6));
            if (amount) {
               setDestinationAmount((parseFloat(amount) * qt.rate).toFixed(2));
            }
         } else {
            setRate(null);
         }
       }).catch(console.error).finally(() => {
         if (!canceled) setFetchingRate(false);
       });
    } else {
       setRate(null);
       setDestinationAmount('');
    }
    return () => { canceled = true; };
  }, [srcAcc?.currency, destAcc?.currency, accessToken, occurredOn]);

  const handleAmountChange = (val: string) => {
    setAmount(val);
    const r = parseFloat(customRate);
    if (!isNaN(r) && val) {
       setDestinationAmount((parseFloat(val) * r).toFixed(2));
    } else if (!val) {
       setDestinationAmount('');
    }
  };

  const handleCustomRateChange = (val: string) => {
    setCustomRate(val);
    const r = parseFloat(val);
    if (!isNaN(r) && amount) {
       setDestinationAmount((parseFloat(amount) * r).toFixed(2));
    }
  };

  const handleDestAmountChange = (val: string) => {
    setDestinationAmount(val);
    const d = parseFloat(val);
    const a = parseFloat(amount);
    if (!isNaN(d) && !isNaN(a) && a > 0) {
       setCustomRate((d / a).toFixed(6));
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (submitting) return;
    setError(null);
    setSubmitting(true);
    try {
      if (!accessToken || !user.tenantId) throw new Error("Not authenticated");
      if (!srcAcc) throw new Error("Source account invalid");
      
      await createTransfer(accessToken, user.tenantId, {
        sourceAccountId,
        destinationAccountId,
        amount: parseFloat(amount),
        currency: srcAcc.currency,
        destinationAmount: isCrossCurrency && destinationAmount ? parseFloat(destinationAmount) : undefined,
        memo: memo || undefined,
        occurredOn: new Date(occurredOn).toISOString(),
      });
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
      <Field label="Date">
        <input type="date" required value={occurredOn} onChange={(e) => setOccurredOn(e.target.value)} className="input" />
      </Field>
      <Field label={`Amount${srcAcc ? ` (${srcAcc.currency})` : ''}`}>
        <input type="number" step="0.01" min="0" required value={amount} onChange={(e) => handleAmountChange(e.target.value)} className="input" placeholder="0.00" />
      </Field>
      {isCrossCurrency && (
        <>
          <div className="flex gap-4">
            <div className="flex-1">
              <Field label={`Converted Amount${destAcc ? ` (${destAcc.currency})` : ''}`}>
                <div className="relative">
                  <input type="number" step="0.01" min="0" required value={destinationAmount} onChange={(e) => handleDestAmountChange(e.target.value)} className="input" placeholder="0.00" />
                  {fetchingRate && <div className="absolute right-3 top-1/2 -translate-y-1/2"><Spinner /></div>}
                </div>
              </Field>
            </div>
            <div className="flex-1">
              <Field label="Exchange Rate">
                 <input type="number" step="0.000001" min="0" required value={customRate} onChange={(e) => handleCustomRateChange(e.target.value)} className="input" placeholder="1.000000" />
              </Field>
            </div>
          </div>
          {rate && <p className="text-xs text-slate-500 mt-0">Market rate: 1 {srcAcc.currency} = {rate.toFixed(4)} {destAcc.currency}</p>}
        </>
      )}
      <Field label="Memo (optional)">
        <input type="text" value={memo} onChange={(e) => setMemo(e.target.value)} className="input" placeholder="e.g. Rent payment" />
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
