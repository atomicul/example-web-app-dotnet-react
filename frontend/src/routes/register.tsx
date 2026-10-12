import { Link, createFileRoute } from '@tanstack/react-router'
import { useState } from 'react'
import { PostApiAuthRegisterBody } from '@/api/generated/index.zod'
import type { LoginRequest } from '@/api/generated/model'
import { AuthCard } from '@/auth/auth-card'
import { CredentialsForm } from '@/auth/credentials-form'
import { publicSearchSchema, redirectWhenSignedIn } from '@/auth/public-route'
import { useAuth } from '@/auth/use-auth'
import { buttonVariants } from '@/components/ui/button'

export const Route = createFileRoute('/register')({
  validateSearch: publicSearchSchema,
  beforeLoad: redirectWhenSignedIn,
  component: RegisterPage,
})

function RegisterPage() {
  const search = Route.useSearch()
  const { register } = useAuth()
  const [registeredEmail, setRegisteredEmail] = useState<string>()

  async function submit(credentials: LoginRequest) {
    await register(credentials)
    setRegisteredEmail(credentials.email)
  }

  if (registeredEmail) {
    return (
      <AuthCard
        title="Check your email"
        description={`We sent a confirmation link to ${registeredEmail}. Open it to activate your account, then log in.`}
      >
        <Link
          to="/login"
          search={search}
          className={buttonVariants({ className: 'w-full' })}
        >
          Go to log in
        </Link>
      </AuthCard>
    )
  }

  return (
    <AuthCard
      title="Create an account"
      description="Sign up to start keeping track of your todos."
      footer={
        <p>
          Already have an account?{' '}
          <Link to="/login" search={search} className="underline">
            Log in
          </Link>
        </p>
      }
    >
      <CredentialsForm
        schema={PostApiAuthRegisterBody}
        submitLabel="Create account"
        passwordAutoComplete="new-password"
        onSubmit={submit}
      />
    </AuthCard>
  )
}
