import { test, expect } from '@playwright/test';

test.describe('Phase 1 MVP Onboarding', () => {
  // We allow 3 minutes (180s) for the full onboarding flow as per Acceptance Criteria
  test.setTimeout(180_000);

  test('New user onboards and logs 10 txns in < 180 s', async ({ page }) => {
    // 1. Visit Login Page
    await page.goto('/login');
    const uniqueEmail = `test-user-${Date.now()}@example.com`;

    await page.fill('input[type="email"]', uniqueEmail);
    await page.click('button:has-text("Send magic link")');

    try {
      await expect(page.locator('text=Check your email')).toBeVisible({ timeout: 15000 });
    } catch {
      console.log('Page content on error:', await page.content());
      throw new Error('Failed to see Check your email. See log for details.');
    }

    // 2. Fetch the magic link from Mailpit API (running on localhost:8025)
    // Mailpit REST API: GET /api/v1/messages
    let magicLink = '';
    
    // Poll Mailpit until the message arrives
    for (let i = 0; i < 10; i++) {
      await page.waitForTimeout(1000);
      try {
        const res = await page.request.get('http://localhost:8025/api/v1/messages');
        const data = await res.json();
        
        // Find the latest message to this email
        const message = data.messages.find((m: any) => m.To.some((t: any) => t.Address.includes(uniqueEmail)));
        if (message) {
          // Fetch message details to get the body
          const msgRes = await page.request.get(`http://localhost:8025/api/v1/message/${message.ID}`);
          const msgData = await msgRes.json();
          // Extract URL from body
          const match = msgData.HTML?.match(/http:\/\/localhost:5173\/login-complete\?token=[^\s'"]+/) || 
                        msgData.Text?.match(/http:\/\/localhost:5173\/login-complete\?token=[^\s'"]+/);
          
          if (match) {
            magicLink = match[0];
            break;
          }
        }
      } catch (e) {
        // Ignore fetch errors during polling
      }
    }

    // If Papercut isn't running or email isn't sent, we fail the test.
    expect(magicLink).not.toBe('');

    // 3. Complete Login
    await page.goto(magicLink);
    
    // Should redirect to dashboard
    await expect(page).toHaveURL('/');
    await expect(page.getByRole('heading', { name: 'Accounts' })).toBeVisible();

    // 4. Create an Account
    await page.locator('section').filter({ hasText: 'Accounts' }).locator('button:has-text("+ Add")').click();
    
    await expect(page.locator('h3:has-text("Add account")')).toBeVisible();
    await page.getByLabel('Name').fill('Checking E2E');
    await page.getByLabel('Opening balance').fill('1000');
    await page.click('button:has-text("Save account")');

    // Account should be visible
    await expect(page.locator('text=Checking E2E')).toBeVisible();

    // 5. Log 10 transactions rapidly
    for (let i = 1; i <= 10; i++) {
      // Click Add Transaction in the Recent activity section
      await page.locator('section').filter({ hasText: 'Recent activity' }).locator('button:has-text("+ Add")').first().click();
      
      await expect(page.locator('h3:has-text("Add transaction")')).toBeVisible();
      
      await page.getByLabel('Amount').fill((10 * i).toString());
      await page.getByLabel('Memo').fill(`E2E Txn ${i}`);
      
      await page.click('button:has-text("Save transaction")');
      
      // Wait for it to appear
      await expect(page.locator(`text=E2E Txn ${i}`)).toBeVisible();
    }
  });
});
