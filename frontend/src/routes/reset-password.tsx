import { Link, createFileRoute } from '@tanstack/react-router'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { z } from 'zod'
import { ApiError } from '@/api/fetcher'
import { usePostApiAuthResetPassword } from '@/api/generated'
import { AuthCard } from '@/auth/auth-card'
import { NewPasswordForm } from '@/auth/new-password-form'
import { buttonVariants } from '@/components/ui/button'

/** The email and code of the link the API emails for a forgotten password. */
const resetPasswordSearchSchema = z.object({
  email: z.string().optional().catch(undefined),
  code: z.string().optional().catch(undefined),
})

export const Route = createFileRoute('/reset-password')({
  validateSearch: resetPasswordSearchSchema,
  component: ResetPasswordPage,
})

/** Identity refuses a code that is wrong, used or expired, or an email it doesn't know, as an invalid token. */
function isInvalidLink(error: unknown) {
  return (
    error instanceof ApiError &&
    !!error.problem?.issues?.some(({ code }) => code === 'InvalidToken')
  )
}

function ResetPasswordPage() {
  const { t } = useTranslation()
  const { email, code } = Route.useSearch()
  const [outcome, setOutcome] = useState<'reset' | 'invalid'>()
  const { mutateAsync: resetPassword } = usePostApiAuthResetPassword({
    mutation: { meta: { handlesErrors: true } },
  })

  if (outcome === 'reset') {
    return (
      <AuthCard
        title={t('resetPassword.reset')}
        description={t('resetPassword.resetDescription')}
      >
        <Link to="/login" className={buttonVariants({ className: 'w-full' })}>
          {t('resetPassword.logIn')}
        </Link>
      </AuthCard>
    )
  }

  if (!email || !code || outcome === 'invalid') {
    return (
      <AuthCard
        title={t('resetPassword.invalid')}
        description={t('resetPassword.invalidDescription')}
      >
        <Link
          to="/forgot-password"
          className={buttonVariants({ className: 'w-full' })}
        >
          {t('resetPassword.newLink')}
        </Link>
      </AuthCard>
    )
  }

  const submit = async (newPassword: string) => {
    try {
      await resetPassword({ data: { email, resetCode: code, newPassword } })
      setOutcome('reset')
    } catch (error) {
      if (!isInvalidLink(error)) {
        throw error
      }
      setOutcome('invalid')
    }
  }

  return (
    <AuthCard
      title={t('resetPassword.title')}
      description={t('resetPassword.description', { email })}
    >
      <NewPasswordForm onSubmit={submit} />
    </AuthCard>
  )
}
