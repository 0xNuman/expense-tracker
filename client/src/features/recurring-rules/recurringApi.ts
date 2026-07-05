import { halFetch } from '../../hal/api';

export interface RecurringRule {
  id: string;
  name: string;
  enabled: boolean;
  kind: string;
  cadence: string;
  interval: number;
  nextRunUtc: string;
  amount: number;
  currency: string;
}

export interface CreateRecurringRuleDto {
  name: string;
  kind: string;
  cadence: string;
  interval?: number;
  startDateUtc?: string;
  accountId: string;
  amount: number;
  currency: string;
  dayOfMonth?: number;
  categoryId?: string;
  memo?: string;
  counterpartAccountId?: string;
}

export const recurringApi = {
  getRules: (token: string, tenantId: string, enabledOnly?: boolean) => 
    halFetch<RecurringRule[]>(`/api/tenants/${tenantId}/recurring-rules${enabledOnly !== undefined ? `?enabledOnly=${enabledOnly}` : ''}`, { method: 'GET' }, token),
    
  createRule: (token: string, tenantId: string, data: CreateRecurringRuleDto) =>
    halFetch<RecurringRule>(`/api/tenants/${tenantId}/recurring-rules`, { method: 'POST', body: JSON.stringify(data) }, token),
    
  pauseRule: (token: string, ruleId: string) => 
    halFetch(`/api/recurring-rules/${ruleId}/pause`, { method: 'POST' }, token),
    
  resumeRule: (token: string, ruleId: string) =>
    halFetch(`/api/recurring-rules/${ruleId}/resume`, { method: 'POST' }, token),
    
  postNow: (token: string, ruleId: string, asOfUtc?: string) =>
    halFetch(`/api/recurring-rules/${ruleId}/post-now`, { method: 'POST', body: JSON.stringify({ asOfUtc }) }, token),
    
  getForecast: (token: string, ruleId: string, horizonDays?: number) =>
    halFetch<{date: string, amount: number}[]>(`/api/recurring-rules/${ruleId}/forecast${horizonDays ? `?horizonDays=${horizonDays}` : ''}`, { method: 'GET' }, token)
};
