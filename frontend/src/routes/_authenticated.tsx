import { Outlet, createFileRoute, redirect } from '@tanstack/react-router'
import { useTranslation } from 'react-i18next'
import { AccountMenu } from '@/auth/account-menu'
import { fetchSession } from '@/auth/session'
import { PullToRefresh } from '@/components/pull-to-refresh'
import { RefreshButton } from '@/components/refresh-button'

export const Route = createFileRoute('/_authenticated')({
  beforeLoad: async ({ context, location }) => {
    if (!(await fetchSession(context.queryClient))) {
      throw redirect({ to: '/login', search: { redirect: location.href } })
    }
  },
  component: AuthenticatedLayout,
})

function AuthenticatedLayout() {
  const { t } = useTranslation()

  return (
    <div className="flex h-svh flex-col">
      <header className="shrink-0 border-b">
        <div className="mx-auto flex max-w-2xl items-center justify-between gap-4 p-4">
          <span className="font-heading font-semibold">
            {t('layout.todos')}
          </span>
          <div className="flex items-center gap-1">
            <RefreshButton />
            <AccountMenu />
          </div>
        </div>
      </header>
      <PullToRefresh className="flex-1">
        <main className="mx-auto max-w-2xl p-4">
          <Outlet />
        </main>
      </PullToRefresh>
    </div>
  )
}
