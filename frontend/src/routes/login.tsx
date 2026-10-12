import { Link, createFileRoute } from '@tanstack/react-router'
import { isUnauthorized } from '@/api/fetcher'
import type { LoginRequest } from '@/api/generated/model'
import { AuthCard } from '@/auth/auth-card'
import { CredentialsForm } from '@/auth/credentials-form'
import { publicSearchSchema, redirectWhenSignedIn } from '@/auth/public-route'
import { useAuth } from '@/auth/use-auth'

export const Route = createFileRoute('/login')({
  validateSearch: publicSearchSchema,
  beforeLoad: redirectWhenSignedIn,
  component: LoginPage,
})

function LoginPage() {
  const search = Route.useSearch()
  const { login } = useAuth()

  async function submit(credentials: LoginRequest) {
    try {
      await login(credentials)
    } catch (error) {
      throw isUnauthorized(error)
        ? new Error('Invalid email or password.')
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
      <CredentialsForm submitLabel="Log in" onSubmit={submit} />
    </AuthCard>
  )
}
