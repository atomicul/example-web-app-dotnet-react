import { Link, createFileRoute } from '@tanstack/react-router'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { usePostApiAuthForgotPassword } from '@/api/generated'
import { AuthCard } from '@/auth/auth-card'
import { ForgotPasswordForm } from '@/auth/forgot-password-form'
import { publicSearchSchema, redirectWhenSignedIn } from '@/auth/public-route'
import { buttonVariants } from '@/components/ui/button'

export const Route = createFileRoute('/forgot-password')({
  validateSearch: publicSearchSchema,
  beforeLoad: redirectWhenSignedIn,
  component: ForgotPasswordPage,
})

/** Identity emails the link only to a registered, confirmed address, and answers the same either way. */
function ForgotPasswordPage() {
  const { t } = useTranslation()
  const search = Route.useSearch()
  const [sentTo, setSentTo] = useState<string>()
  const { mutateAsync: sendResetLink } = usePostApiAuthForgotPassword({
    mutation: { meta: { handlesErrors: true } },
  })

  async function submit(email: string) {
    await sendResetLink({ data: { email } })
    setSentTo(email)
  }

  if (sentTo) {
    return (
      <AuthCard
        title={t('forgotPassword.checkEmail')}
        description={t('forgotPassword.linkSent', { email: sentTo })}
      >
        <Link
          to="/login"
          search={search}
          className={buttonVariants({ className: 'w-full' })}
        >
          {t('forgotPassword.backToLogIn')}
        </Link>
      </AuthCard>
    )
  }

  return (
    <AuthCard
      title={t('forgotPassword.title')}
      description={t('forgotPassword.description')}
      footer={
        <p>
          {t('forgotPassword.remembered')}{' '}
          <Link to="/login" search={search} className="underline">
            {t('forgotPassword.logIn')}
          </Link>
        </p>
      }
    >
      <ForgotPasswordForm onSubmit={submit} />
    </AuthCard>
  )
}
