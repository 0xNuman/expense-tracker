import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { halClient } from '../hal/HalClient';

interface SpendingGroup {
  groupId: string;
  totalAmount: number;
  _links: {
    'drill-down': { href: string };
  };
}

interface SpendingReportResponse {
  from: string | null;
  to: string | null;
  groupBy: string;
  _embedded: {
    item: SpendingGroup[];
  };
}

export function ReportsPage() {
  const [groupBy, setGroupBy] = useState<'category' | 'month' | 'day'>('category');
  const [from, setFrom] = useState('');
  const [to, setTo] = useState('');

  const { data, isLoading, error } = useQuery<SpendingReportResponse>({
    queryKey: ['reports', 'spending', groupBy, from, to],
    queryFn: () => {
      const params = new URLSearchParams({ groupBy });
      if (from) params.append('from', from);
      if (to) params.append('to', to);
      return halClient.get(`/api/reports/spending?${params.toString()}`);
    }
  });

  return (
    <div className="p-4 max-w-4xl mx-auto">
      <h1 className="text-2xl font-bold mb-4">Spending Reports</h1>

      <div className="flex flex-wrap gap-4 mb-6 bg-gray-50 p-4 rounded-lg">
        <div>
          <label className="block text-sm font-medium mb-1">Group By</label>
          <select 
            value={groupBy} 
            onChange={(e) => setGroupBy(e.target.value as any)}
            className="border rounded p-2"
          >
            <option value="category">Category</option>
            <option value="month">Month</option>
            <option value="day">Day</option>
          </select>
        </div>
        <div>
          <label className="block text-sm font-medium mb-1">From Date</label>
          <input 
            type="date" 
            value={from} 
            onChange={(e) => setFrom(e.target.value)} 
            className="border rounded p-2"
          />
        </div>
        <div>
          <label className="block text-sm font-medium mb-1">To Date</label>
          <input 
            type="date" 
            value={to} 
            onChange={(e) => setTo(e.target.value)} 
            className="border rounded p-2"
          />
        </div>
      </div>

      {isLoading && <p>Loading report...</p>}
      {error && <p className="text-red-500">Error loading report.</p>}
      
      {data && (
        <div className="bg-white shadow rounded-lg overflow-hidden">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Group</th>
                <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Total Amount</th>
                <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Action</th>
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-200">
              {data._embedded.item.length === 0 ? (
                <tr>
                  <td colSpan={3} className="px-6 py-4 text-center text-gray-500">No data available for the selected period.</td>
                </tr>
              ) : (
                data._embedded.item.map((item) => (
                  <tr key={item.groupId}>
                    <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                      {item.groupId}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-right text-gray-500">
                      ${item.totalAmount.toFixed(2)}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-right font-medium">
                      <a href={item._links['drill-down'].href} className="text-indigo-600 hover:text-indigo-900" target="_blank" rel="noopener noreferrer">
                        Drill Down
                      </a>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
