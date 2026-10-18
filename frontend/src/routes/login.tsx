import { Link, createFileRoute } from '@tanstack/react-router'
import type { ParseKeys } from 'i18next'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { ApiError } from '@/api/fetcher'
import { PostApiAuthLoginBody } from '@/api/generated/index.zod'
import type { LoginRequest } from '@/api/generated/model'
import { AuthCard } from '@/auth/auth-card'
import { CredentialsForm } from '@/auth/credentials-form'
import { publicSearchSchema, redirectWhenSignedIn } from '@/auth/public-route'
import { ResendConfirmationButton } from '@/auth/resend-confirmation-button'
import { useAuth } from '@/auth/use-auth'
import { FormError } from '@/lib/server-errors'

export const Route = createFileRoute('/login')({
  validateSearch: publicSearchSchema,
  beforeLoad: redirectWhenSignedIn,
  component: LoginPage,
})

/** Identity refuses a sign-in with a 401 whose detail names the reason; any other reason means wrong credentials. */
const loginFailureMessages: Partial<Record<string, ParseKeys>> = {
  NotAllowed: 'login.notAllowed',
  LockedOut: 'login.lockedOut',
  RequiresTwoFactor: 'login.requiresTwoFactor',
}

function loginFailureReason(error: unknown) {
  return error instanceof ApiError && error.status === 401
    ? (error.problem?.detail ?? '')
    : undefined
}

function LoginPage() {
  const { t } = useTranslation()
  const search = Route.useSearch()
  const { login } = useAuth()
  const [unconfirmedEmail, setUnconfirmedEmail] = useState<string>()

  async function submit(credentials: LoginRequest) {
    setUnconfirmedEmail(undefined)
    try {
      await login(credentials)
    } catch (error) {
      const reason = loginFailureReason(error)
      if (reason === undefined) {
        throw error
      }
      if (reason === 'NotAllowed') {
        setUnconfirmedEmail(credentials.email)
      }
      throw new FormError(
        t(loginFailureMessages[reason] ?? 'login.invalidCredentials'),
      )
    }
  }

  return (
    <AuthCard
      title={t('login.title')}
      description={t('login.description')}
      footer={
        <p>
          {t('login.newHere')}{' '}
          <Link to="/register" search={search} className="underline">
            {t('login.createAccount')}
          </Link>
        </p>
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
