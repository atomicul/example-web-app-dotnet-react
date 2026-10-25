import { Link, createFileRoute } from '@tanstack/react-router'
import type { ParseKeys } from 'i18next'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import type { LoginRequest, TwoFactorMethod } from '@/api/generated/model'
import { AuthCard } from '@/auth/auth-card'
import { loginFailureReason, twoFactorMethodOf } from '@/auth/login-failure'
import { LoginForm } from '@/auth/login-form'
import { publicSearchSchema, redirectWhenSignedIn } from '@/auth/public-route'
import { ResendConfirmationButton } from '@/auth/resend-confirmation-button'
import { TwoFactorStep } from '@/auth/two-factor-step'
import { useAuth } from '@/auth/use-auth'
import { FormError } from '@/lib/server-errors'

export const Route = createFileRoute('/login')({
  validateSearch: publicSearchSchema,
  beforeLoad: redirectWhenSignedIn,
  component: LoginPage,
})

/** Messages for the reasons Identity refuses a sign-in with; any other reason means wrong credentials. */
const loginFailureMessages: Partial<Record<string, ParseKeys>> = {
  NotAllowed: 'login.notAllowed',
  LockedOut: 'login.lockedOut',
}

/**
 * Logs in with an email and password, then, when the API asks for a second factor, with a code of the user's way of
 * finishing the login (see {@link TwoFactorStep}).
 */
function LoginPage() {
  const { t } = useTranslation()
  const search = Route.useSearch()
  const { login } = useAuth()
  const [unconfirmedEmail, setUnconfirmedEmail] = useState<string>()
  const [awaitingCode, setAwaitingCode] = useState<{
    credentials: LoginRequest
    method: TwoFactorMethod
  }>()

  async function submit(credentials: LoginRequest) {
    setUnconfirmedEmail(undefined)
    try {
      await login(credentials)
    } catch (error) {
      const reason = loginFailureReason(error)
      if (reason === undefined) {
        throw error
      }
      if (reason === 'RequiresTwoFactor') {
        setAwaitingCode({ credentials, method: twoFactorMethodOf(error) })
        return
      }
      if (reason === 'NotAllowed') {
        setUnconfirmedEmail(credentials.email)
      }
      throw new FormError(
        t(loginFailureMessages[reason] ?? 'login.invalidCredentials'),
      )
    }
  }

  if (awaitingCode) {
    return (
      <TwoFactorStep
        {...awaitingCode}
        onCancel={() => setAwaitingCode(undefined)}
      />
    )
  }

  return (
    <AuthCard
      title={t('login.title')}
      description={t('login.description')}
      footer={
        <div className="flex flex-col gap-1">
          <p>
            {t('login.newHere')}{' '}
            <Link to="/register" search={search} className="underline">
              {t('login.createAccount')}
            </Link>
          </p>
          <p>
            {t('login.forgotCredentials')}{' '}
            <Link to="/forgot-password" search={search} className="underline">
              {t('login.resetPassword')}
            </Link>
          </p>
        </div>
      }
    >
      <div className="flex flex-col gap-4">
        <LoginForm onSubmit={submit} />
        {unconfirmedEmail && (
          <ResendConfirmationButton email={unconfirmedEmail} />
        )}
      </div>
    </AuthCard>
  )
}
