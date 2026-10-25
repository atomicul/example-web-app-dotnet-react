import { Link, createFileRoute } from '@tanstack/react-router'
import type { ParseKeys } from 'i18next'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { PostApiAuthLoginBody } from '@/api/generated/index.zod'
import type { LoginRequest } from '@/api/generated/model'
import { AuthCard } from '@/auth/auth-card'
import { CredentialsForm } from '@/auth/credentials-form'
import { loginFailureReason } from '@/auth/login-failure'
import { publicSearchSchema, redirectWhenSignedIn } from '@/auth/public-route'
import { ResendConfirmationButton } from '@/auth/resend-confirmation-button'
import { ResendTwoFactorCodeButton } from '@/auth/resend-two-factor-code-button'
import { TwoFactorForm } from '@/auth/two-factor-form'
import { useAuth } from '@/auth/use-auth'
import { Button } from '@/components/ui/button'
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
 * Logs in with an email and password, then, when the API asks for a second factor, with the code it emailed: the
 * code step sends the same credentials again along with the code.
 */
function LoginPage() {
  const { t } = useTranslation()
  const search = Route.useSearch()
  const { login } = useAuth()
  const [unconfirmedEmail, setUnconfirmedEmail] = useState<string>()
  const [awaitingCode, setAwaitingCode] = useState<LoginRequest>()

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
        setAwaitingCode(credentials)
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

  async function submitCode(credentials: LoginRequest, twoFactorCode: string) {
    try {
      await login({ ...credentials, twoFactorCode })
    } catch (error) {
      const reason = loginFailureReason(error)
      if (reason === undefined) {
        throw error
      }
      throw new FormError(
        t(reason === 'LockedOut' ? 'login.lockedOut' : 'twoFactor.wrongCode'),
      )
    }
  }

  if (awaitingCode) {
    return (
      <AuthCard
        title={t('twoFactor.title')}
        description={t('twoFactor.description', { email: awaitingCode.email })}
      >
        <div className="flex flex-col gap-4">
          <TwoFactorForm onSubmit={(code) => submitCode(awaitingCode, code)} />
          <ResendTwoFactorCodeButton credentials={awaitingCode} />
          <Button variant="ghost" onClick={() => setAwaitingCode(undefined)}>
            {t('twoFactor.useAnotherAccount')}
          </Button>
        </div>
      </AuthCard>
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
        <CredentialsForm
          schema={PostApiAuthLoginBody}
          submitLabel={t('login.submit')}
          passwordAutoComplete="current-password"
          onSubmit={submit}
        />
        {unconfirmedEmail && (
          <ResendConfirmationButton email={unconfirmedEmail} />
        )}
      </div>
    </AuthCard>
  )
}
