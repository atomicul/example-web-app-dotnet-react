import { Link, createFileRoute } from '@tanstack/react-router'
import { AuthCard } from '@/auth/auth-card'
import { CredentialsForm } from '@/auth/credentials-form'
import { publicSearchSchema, redirectWhenSignedIn } from '@/auth/public-route'
import { useAuth } from '@/auth/use-auth'

export const Route = createFileRoute('/register')({
  validateSearch: publicSearchSchema,
  beforeLoad: redirectWhenSignedIn,
  component: RegisterPage,
})

function RegisterPage() {
  const search = Route.useSearch()
  const { register } = useAuth()

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
      <CredentialsForm submitLabel="Create account" onSubmit={register} />
    </AuthCard>
  )
}
