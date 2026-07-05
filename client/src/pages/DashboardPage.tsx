import { useEffect, useState } from 'react';
import { fetchAccounts, fetchTransactions, renameAccount, voidTransaction, archiveAccount } from '../hal/api';
import type { Account, Transaction } from '../hal/api';
import { useAuth } from '../auth/AuthContext';
import { Layout } from '../components/Layout';
import { AccountCard } from '../components/AccountCard';
import { TransactionList } from '../components/TransactionList';
import { AddAccountForm } from '../components/AddAccountForm';
import { AddTransactionForm } from '../components/AddTransactionForm';
import { TransferForm } from '../components/TransferForm';
import { Spinner } from '../components/Spinner';
import { ErrorBanner } from '../components/ErrorBanner';

type Modal = 'none' | 'account' | 'transaction' | 'transfer';

export function DashboardPage() {
  const { accessToken } = useAuth();
  const [accounts, setAccounts] = useState<Account[]>([]);
  const [transactions, setTransactions] = useState<Transaction[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [modal, setModal] = useState<Modal>('none');
  const [transactionToEdit, setTransactionToEdit] = useState<Transaction | undefined>(undefined);

  const load = async () => {
    if (!accessToken) return;
    setError(null);
    setLoading(true);
    try {
      const [a, t] = await Promise.all([
        fetchAccounts(accessToken),
        fetchTransactions(accessToken),
      ]);
      setAccounts(a);
      setTransactions(t);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not load data');
    } finally {
      setLoading(false);
    }
  };

  const handleRenameAccount = async (account: Account) => {
    if (!accessToken) return;
    const newName = window.prompt(`Rename account '${account.name}' to:`, account.name);
    if (!newName || newName === account.name) return;
    try {
      await renameAccount(accessToken, account.id, newName);
      void load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not rename account');
    }
  };

  const handleVoidTransaction = async (t: Transaction) => {
    if (!accessToken) return;
    if (!window.confirm(`Are you sure you want to void this ${t.amount} ${t.currency} transaction?`)) return;
    try {
      await voidTransaction(accessToken, t.id, 'Voided by user');
      void load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not void transaction');
    }
  };

  const handleArchiveAccount = async (account: Account) => {
    if (!accessToken) return;
    if (!window.confirm(`Are you sure you want to archive the account '${account.name}'?`)) return;
    try {
      await archiveAccount(accessToken, account.id);
      void load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not archive account');
    }
  };

  const handleEditTransaction = (t: Transaction) => {
    setTransactionToEdit(t);
    setModal('transaction');
  };

  useEffect(() => {
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [accessToken]);

  return (
    <Layout>
      {error && (
        <div className="mb-4">
          <ErrorBanner message={error} onDismiss={() => setError(null)} />
        </div>
      )}

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-[320px_minmax(0,1fr)]">
        {/* Accounts column */}
        <section>
          <div className="mb-3 flex items-center justify-between">
            <h2 className="text-lg font-semibold">Accounts</h2>
            <button
              type="button"
              onClick={() => setModal('account')}
              className="rounded-lg bg-slate-900 px-4 py-2 text-sm font-medium text-white shadow-sm transition-colors hover:bg-slate-800 dark:bg-white dark:text-slate-900 dark:hover:bg-slate-100"
            >
              + Add
            </button>
          </div>
          {loading && accounts.length === 0 ? (
            <div className="flex items-center gap-2 text-sm text-slate-500">
              <Spinner /> Loading accounts…
            </div>
          ) : accounts.length === 0 ? (
            <div className="rounded-xl border border-dashed border-slate-300 bg-white p-6 text-center text-sm text-slate-500 dark:border-slate-700 dark:bg-slate-900">
              No accounts yet. Add one to start tracking.
            </div>
          ) : (
            <div className="flex flex-col gap-3">
              {accounts.map((a) => (
                <AccountCard key={a.id} account={a} onRenameRequested={handleRenameAccount} onArchiveRequested={handleArchiveAccount} />
              ))}
            </div>
          )}
        </section>

        {/* Transactions column */}
        <section>
          <div className="mb-3 flex items-center justify-between">
            <h2 className="text-lg font-semibold">Recent activity</h2>
            <div className="flex gap-2">
              <button
                type="button"
                onClick={() => setModal('transfer')}
                className="rounded-lg border border-slate-200 px-4 py-2 text-sm font-medium text-slate-700 transition-colors hover:bg-slate-50 hidden md:block dark:border-slate-800 dark:text-slate-300 dark:hover:bg-slate-800/50"
              >
                Transfer
              </button>
              <button
                type="button"
                onClick={() => {
                  setTransactionToEdit(undefined);
                  setModal('transaction');
                }}
                className="rounded-lg bg-slate-900 px-4 py-2 text-sm font-medium text-white shadow-sm transition-colors hover:bg-slate-800 hidden md:block dark:bg-white dark:text-slate-900 dark:hover:bg-slate-100"
              >
                + Add
              </button>
            </div>
          </div>
          {loading && transactions.length === 0 ? (
            <div className="flex items-center gap-2 text-sm text-slate-500">
              <Spinner /> Loading transactions…
            </div>
          ) : (
            <TransactionList transactions={transactions} onVoidRequested={handleVoidTransaction} onEditRequested={handleEditTransaction} />
          )}
        </section>
      </div>

      {/* FAB (mobile) */}
      <button
        type="button"
        aria-label="Add transaction"
        onClick={() => {
          setTransactionToEdit(undefined);
          setModal('transaction');
        }}
        className="fixed bottom-20 right-5 z-30 flex h-14 w-14 items-center justify-center rounded-full bg-slate-900 text-white shadow-lg transition-transform hover:scale-105 active:scale-95 md:hidden dark:bg-white dark:text-slate-900"
      >
        +
      </button>

      {/* Modal */}
      {modal !== 'none' && (
        <div
          className="fixed inset-0 z-40 flex items-end justify-center bg-slate-900/30 backdrop-blur-sm p-0 sm:items-center sm:p-4 transition-all"
          onClick={() => setModal('none')}
        >
          <div
            className="w-full max-w-md rounded-t-3xl border border-slate-200/50 bg-white/95 p-6 shadow-2xl backdrop-blur-xl dark:border-slate-700/50 dark:bg-slate-900/95 sm:rounded-3xl"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="mb-4 flex items-center justify-between">
              <h3 className="text-base font-semibold">
                {modal === 'account' ? 'Add account' : modal === 'transfer' ? 'Transfer' : transactionToEdit ? 'Edit transaction' : 'Add transaction'}
              </h3>
              <button type="button" onClick={() => setModal('none')} className="text-slate-400 hover:text-slate-600">
                ×
              </button>
            </div>
            {modal === 'account' ? (
              <AddAccountForm
                token={accessToken ?? ''}
                onCreated={() => {
                  setModal('none');
                  void load();
                }}
                onCancel={() => setModal('none')}
              />
            ) : modal === 'transfer' ? (
              <TransferForm
                accounts={accounts}
                onCreated={() => {
                  setModal('none');
                  void load();
                }}
                onCancel={() => setModal('none')}
              />
            ) : (
              <AddTransactionForm
                token={accessToken ?? ''}
                accounts={accounts}
                initialData={transactionToEdit}
                onCreated={() => {
                  setModal('none');
                  setTransactionToEdit(undefined);
                  void load();
                }}
                onCancel={() => {
                  setModal('none');
                  setTransactionToEdit(undefined);
                }}
              />
            )}
          </div>
        </div>
      )}
    </Layout>
  );
}