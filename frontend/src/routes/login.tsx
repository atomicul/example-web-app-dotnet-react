import { Link, createFileRoute } from '@tanstack/react-router'
import { useState } from 'react'
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
const loginFailureMessages: Partial<Record<string, string>> = {
  NotAllowed: 'Confirm your email before logging in.',
  LockedOut: 'Too many failed attempts. Try again in a few minutes.',
  RequiresTwoFactor:
    'This account uses two-factor authentication, which this app does not support yet.',
}

function loginFailureReason(error: unknown) {
  return error instanceof ApiError && error.status === 401
    ? (error.problem?.detail ?? '')
    : undefined
}

function LoginPage() {
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
        loginFailureMessages[reason] ?? 'Invalid email or password.',
      )
    }
  }

  return (
    <AuthCard
      title="Log in"
      description="Welcome back! Log in to see your todos."
      footer={
        <p>
          New here?{' '}
          <Link to="/register" search={search} className="underline">
            Create an account
          </Link>
        </p>
      }
    >
      <div className="flex flex-col gap-4">
        <CredentialsForm
          schema={PostApiAuthLoginBody}
          submitLabel="Log in"
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
