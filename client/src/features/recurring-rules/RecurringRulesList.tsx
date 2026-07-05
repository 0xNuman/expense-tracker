import { useState, useEffect } from 'react';
import { recurringApi } from './recurringApi';
import type { RecurringRule } from './recurringApi';
import { useAuth } from '../../auth/AuthContext';

export function RecurringRulesList({ tenantId }: { tenantId: string }) {
  const { accessToken: token } = useAuth();
  const [rules, setRules] = useState<RecurringRule[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    loadRules();
  }, [tenantId]);

  const loadRules = async () => {
    setLoading(true);
    try {
      if (!token) throw new Error('Not authenticated');
      const data = await recurringApi.getRules(token, tenantId);
      setRules(data);
    } catch (e) {
      console.error(e);
    } finally {
      setLoading(false);
    }
  };

  const togglePause = async (rule: RecurringRule) => {
    try {
      if (!token) throw new Error('Not authenticated');
      if (rule.enabled) {
        await recurringApi.pauseRule(token, rule.id);
      } else {
        await recurringApi.resumeRule(token, rule.id);
      }
      loadRules();
    } catch (e) {
      console.error(e);
    }
  };

  const postNow = async (ruleId: string) => {
    try {
      if (!token) throw new Error('Not authenticated');
      await recurringApi.postNow(token, ruleId);
      loadRules();
    } catch (e) {
      console.error(e);
    }
  };

  if (loading) return <div>Loading...</div>;

  return (
    <div className="space-y-4">
      <h2 className="text-xl font-bold">Recurring Transactions</h2>
      <div className="grid gap-4 md:grid-cols-2">
        {rules.map(rule => (
          <div key={rule.id} className="p-4 border rounded shadow-sm bg-white">
            <div className="flex justify-between items-start">
              <div>
                <h3 className="font-semibold">{rule.name}</h3>
                <p className="text-sm text-gray-500">{rule.kind} • {rule.cadence}</p>
              </div>
              <div className="text-right">
                <div className="font-medium text-lg">{rule.amount} {rule.currency}</div>
                <div className="text-xs text-gray-400">Next: {rule.nextRunUtc}</div>
              </div>
            </div>
            <div className="mt-4 flex space-x-2">
              <button 
                onClick={() => togglePause(rule)}
                className={`px-3 py-1 text-sm rounded ${rule.enabled ? 'bg-amber-100 text-amber-800' : 'bg-green-100 text-green-800'}`}
              >
                {rule.enabled ? 'Pause' : 'Resume'}
              </button>
              <button 
                onClick={() => postNow(rule.id)}
                className="px-3 py-1 text-sm rounded bg-blue-100 text-blue-800"
              >
                Post Now
              </button>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
