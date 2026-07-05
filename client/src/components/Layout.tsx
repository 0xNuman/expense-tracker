import { NavLink, useNavigate } from 'react-router-dom';
import type { ReactNode } from 'react';
import { useAuth } from '../auth/AuthContext';

export function Layout({ children }: { children: ReactNode }) {
  const { user, logout, isAuthenticated } = useAuth();
  const navigate = useNavigate();

  const handleLogout = async () => {
    await logout();
    navigate('/login');
  };

  const navItem =
    'flex items-center gap-2 rounded-lg px-3 py-2 text-sm font-medium transition';

  return (
    <div className="min-h-full bg-slate-50 text-slate-900 dark:bg-slate-950 dark:text-slate-100">
      <header className="sticky top-0 z-20 border-b border-slate-200 bg-white/80 backdrop-blur dark:border-slate-800 dark:bg-slate-900/80">
        <div className="mx-auto flex max-w-6xl items-center justify-between px-4 py-3">
          <div className="flex items-baseline gap-2">
            <span className="text-lg font-semibold tracking-tight">Expense Tracker</span>
            {user.tenantName && (
              <span className="hidden text-xs text-slate-500 dark:text-slate-400 sm:inline">
                · {user.tenantName}
              </span>
            )}
          </div>
          <div className="flex items-center gap-4">
            <button
              type="button"
              onClick={() => {
                document.documentElement.classList.toggle('dark');
                localStorage.setItem('theme', document.documentElement.classList.contains('dark') ? 'dark' : 'light');
              }}
              className="text-slate-500 hover:text-slate-700 dark:text-slate-400 dark:hover:text-slate-300"
              aria-label="Toggle dark mode"
            >
              🌓
            </button>
            <div className="flex items-center gap-3">
              {user.email && (
              <span className="hidden text-xs text-slate-500 dark:text-slate-400 sm:inline">
                {user.email}
              </span>
            )}
            {isAuthenticated && (
              <button
                type="button"
                onClick={handleLogout}
                className="rounded-lg border border-slate-200 px-3 py-1.5 text-sm font-medium text-slate-600 hover:bg-slate-100 dark:border-slate-700 dark:text-slate-300 dark:hover:bg-slate-800"
              >
                Log out
              </button>
            )}
            </div>
          </div>
        </div>
      </header>

      <div className="mx-auto flex max-w-6xl">
        {/* Sidebar (desktop) */}
        <nav className="hidden w-56 shrink-0 border-r border-slate-200 p-4 dark:border-slate-800 md:block">
          <ul className="flex flex-col gap-1">
            <li>
              <NavLink
                to="/"
                end
                className={({ isActive }) =>
                  `${navItem} ${isActive ? 'bg-sky-50 text-sky-700 dark:bg-sky-950 dark:text-sky-300' : 'text-slate-600 hover:bg-slate-100 dark:text-slate-300 dark:hover:bg-slate-800'}`
                }
              >
                Dashboard
              </NavLink>
            </li>
            <li>
              <NavLink
                to="/settings"
                className={({ isActive }) =>
                  `${navItem} ${isActive ? 'bg-sky-50 text-sky-700 dark:bg-sky-950 dark:text-sky-300' : 'text-slate-600 hover:bg-slate-100 dark:text-slate-300 dark:hover:bg-slate-800'}`
                }
              >
                Settings
              </NavLink>
            </li>
          </ul>
        </nav>

        <main className="min-w-0 flex-1 px-4 py-6 pb-24 md:pb-6">{children}</main>
      </div>

      {/* Bottom nav (mobile) */}
      <nav className="fixed inset-x-0 bottom-0 z-20 border-t border-slate-200 bg-white/90 backdrop-blur dark:border-slate-800 dark:bg-slate-900/90 md:hidden">
        <ul className="flex">
          <li className="flex-1">
            <NavLink
              to="/"
              end
              className={({ isActive }) =>
                `flex flex-col items-center py-2 text-xs ${isActive ? 'text-sky-600 dark:text-sky-400' : 'text-slate-500 dark:text-slate-400'}`
              }
            >
              Dashboard
            </NavLink>
          </li>
          <li className="flex-1">
            <NavLink
              to="/settings"
              className={({ isActive }) =>
                `flex flex-col items-center py-2 text-xs ${isActive ? 'text-sky-600 dark:text-sky-400' : 'text-slate-500 dark:text-slate-400'}`
              }
            >
              Settings
            </NavLink>
          </li>
        </ul>
      </nav>
    </div>
  );
}