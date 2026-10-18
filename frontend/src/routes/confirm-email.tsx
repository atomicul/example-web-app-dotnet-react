import { Link, createFileRoute } from '@tanstack/react-router'
import { useTranslation } from 'react-i18next'
import type { z } from 'zod'
import { isUnauthorized } from '@/api/fetcher'
import { mapIdentityApiApiAuthConfirmEmail } from '@/api/generated'
import { MapIdentityApiApiAuthConfirmEmailQueryParams } from '@/api/generated/index.zod'
import { AuthCard } from '@/auth/auth-card'
import { buttonVariants } from '@/components/ui/button'

const confirmEmailSearchSchema =
  MapIdentityApiApiAuthConfirmEmailQueryParams.partial()

/**
 * Confirms the email from the link's search params, or the change to `changedEmail` when the link was sent for one.
 * Identity answers 401 for an unknown user or a bad code. It calls the API directly because a 401 from a cached
 * query re-runs the guards.
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
  const { t } = useTranslation()
  const confirmed = Route.useLoaderData()
  const { changedEmail } = Route.useSearch()

  return confirmed ? (
    <AuthCard
      title={t(
        changedEmail ? 'confirmEmail.changed' : 'confirmEmail.confirmed',
      )}
      description={
        changedEmail
          ? t('confirmEmail.changedDescription', { email: changedEmail })
          : t('confirmEmail.confirmedDescription')
      }
    >
      <Link to="/login" className={buttonVariants({ className: 'w-full' })}>
        {t('confirmEmail.logIn')}
      </Link>
    </AuthCard>
  ) : (
    <AuthCard
      title={t('confirmEmail.invalid')}
      description={t('confirmEmail.invalidDescription')}
    >
      <Link to="/login" className={buttonVariants({ className: 'w-full' })}>
        {t('confirmEmail.backToLogIn')}
      </Link>
    </AuthCard>
  )
}
