import { createFileRoute } from '@tanstack/react-router'
import { useTranslation } from 'react-i18next'
import { EmailForm } from '@/account/email-form'
import { PasswordForm } from '@/account/password-form'
import { TwoFactorMethodForm } from '@/account/two-factor-method-form'
import {
  getGetPendingEmailQueryOptions,
  getGetTwoFactorMethodQueryOptions,
} from '@/api/generated'
import { Page } from '@/components/page'

export const Route = createFileRoute('/_authenticated/account')({
  loader: ({ context }) =>
    Promise.all([
      context.queryClient.query({
        ...getGetPendingEmailQueryOptions(),
        staleTime: 'static',
      }),
      context.queryClient.query({
        ...getGetTwoFactorMethodQueryOptions(),
        staleTime: 'static',
      }),
    ]),
  component: AccountPage,
})

function AccountPage() {
  const { t } = useTranslation()

  return (
    <Page title={t('account.title')}>
      <EmailForm />
      <PasswordForm />
      <TwoFactorMethodForm />
    </Page>
  )
}
