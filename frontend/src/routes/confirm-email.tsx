import { Link, createFileRoute } from '@tanstack/react-router'
import type { z } from 'zod'
import { isUnauthorized } from '@/api/fetcher'
import { mapIdentityApiApiAuthConfirmEmail } from '@/api/generated'
import { MapIdentityApiApiAuthConfirmEmailQueryParams } from '@/api/generated/index.zod'
import { AuthCard } from '@/auth/auth-card'
import { buttonVariants } from '@/components/ui/button'

const confirmEmailSearchSchema =
  MapIdentityApiApiAuthConfirmEmailQueryParams.partial()

/**
 * Confirms the email from the link's search params. Identity answers 401 for an unknown user
 * or a bad code. It calls the API directly because a 401 from a cached query re-runs the guards.
 */
async function confirmEmail({
  userId,
  code,
  changedEmail,
}: z.infer<typeof confirmEmailSearchSchema>) {
  if (!userId || !code) {
    return false
  }
  try {
    await mapIdentityApiApiAuthConfirmEmail({ userId, code, changedEmail })
    return true
  } catch (error) {
    if (isUnauthorized(error)) {
      return false
    }
    throw error
  }
}

export const Route = createFileRoute('/confirm-email')({
  validateSearch: confirmEmailSearchSchema,
  loaderDeps: ({ search }) => search,
  loader: ({ deps }) => confirmEmail(deps),
  component: ConfirmEmailPage,
})

function ConfirmEmailPage() {
  const confirmed = Route.useLoaderData()

  return confirmed ? (
    <AuthCard
      title="Email confirmed"
      description="Thanks for confirming your email. You can now log in."
    >
      <Link to="/login" className={buttonVariants({ className: 'w-full' })}>
        Log in
      </Link>
    </AuthCard>
  ) : (
    <AuthCard
      title="Link invalid or expired"
      description="We could not confirm your email with this link. Try logging in to get a new one."
    >
      <Link to="/login" className={buttonVariants({ className: 'w-full' })}>
        Back to log in
      </Link>
    </AuthCard>
  )
}
