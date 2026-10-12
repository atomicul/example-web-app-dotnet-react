import { Link, createFileRoute } from '@tanstack/react-router'
import { useState } from 'react'
import { ApiError, isUnauthorized } from '@/api/fetcher'
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

/** Identity refuses to sign in a user whose email is not confirmed with a 401 detailing "NotAllowed". */
function isEmailUnconfirmed(error: unknown) {
  return (
    error instanceof ApiError &&
    error.status === 401 &&
    error.problem?.detail === 'NotAllowed'
  )
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
      if (isEmailUnconfirmed(error)) {
        setUnconfirmedEmail(credentials.email)
        throw new FormError('Confirm your email before logging in.')
      }
      throw isUnauthorized(error)
        ? new FormError('Invalid email or password.')
        : error
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
