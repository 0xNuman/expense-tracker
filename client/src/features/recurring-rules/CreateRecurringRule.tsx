import { useState } from 'react';
import { recurringApi } from './recurringApi';
import type { CreateRecurringRuleDto } from './recurringApi';
import { useAuth } from '../../auth/AuthContext';

export function CreateRecurringRule({ tenantId, onCreated }: { tenantId: string, onCreated: () => void }) {
  const { accessToken: token } = useAuth();
  const [formData, setFormData] = useState<Partial<CreateRecurringRuleDto>>({
    kind: 'Expense',
    cadence: 'Monthly',
    interval: 1,
    currency: 'USD'
  });

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      if (!token) throw new Error('Not authenticated');
      await recurringApi.createRule(token, tenantId, formData as CreateRecurringRuleDto);
      onCreated();
      // Reset form
      setFormData({ kind: 'Expense', cadence: 'Monthly', interval: 1, currency: 'USD' });
    } catch (err) {
      console.error(err);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="p-4 border rounded bg-gray-50 space-y-4">
      <h3 className="font-semibold text-lg">New Recurring Transaction</h3>
      
      <div className="grid grid-cols-2 gap-4">
        <div>
          <label className="block text-sm">Name</label>
          <input required type="text" className="w-full border rounded p-1" value={formData.name || ''} onChange={e => setFormData({...formData, name: e.target.value})} />
        </div>
        <div>
          <label className="block text-sm">Kind</label>
          <select className="w-full border rounded p-1" value={formData.kind} onChange={e => setFormData({...formData, kind: e.target.value})}>
            <option>Income</option>
            <option>Expense</option>
            <option>Transfer</option>
          </select>
        </div>
        <div>
          <label className="block text-sm">Cadence</label>
          <select className="w-full border rounded p-1" value={formData.cadence} onChange={e => setFormData({...formData, cadence: e.target.value})}>
            <option>Daily</option>
            <option>Weekly</option>
            <option>Monthly</option>
            <option>Yearly</option>
          </select>
        </div>
        <div>
          <label className="block text-sm">Interval</label>
          <input type="number" min="1" className="w-full border rounded p-1" value={formData.interval || 1} onChange={e => setFormData({...formData, interval: parseInt(e.target.value)})} />
        </div>
        <div>
          <label className="block text-sm">Amount</label>
          <input required type="number" step="0.01" className="w-full border rounded p-1" value={formData.amount || ''} onChange={e => setFormData({...formData, amount: parseFloat(e.target.value)})} />
        </div>
        <div>
          <label className="block text-sm">Account ID</label>
          <input required type="text" className="w-full border rounded p-1" value={formData.accountId || ''} onChange={e => setFormData({...formData, accountId: e.target.value})} />
        </div>
        {formData.cadence === 'Monthly' && (
          <div>
            <label className="block text-sm">Day of Month</label>
            <input type="number" min="1" max="31" className="w-full border rounded p-1" value={formData.dayOfMonth || ''} onChange={e => setFormData({...formData, dayOfMonth: parseInt(e.target.value)})} />
          </div>
        )}
      </div>
      
      <button type="submit" className="px-4 py-2 bg-blue-600 text-white rounded">Save Schedule</button>
    </form>
  );
}
