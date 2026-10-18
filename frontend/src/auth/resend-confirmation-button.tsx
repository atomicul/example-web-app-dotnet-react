import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { isTooManyRequests } from '@/api/fetcher'
import { usePostApiAuthResendConfirmationEmail } from '@/api/generated'
import { Button } from '@/components/ui/button'
import { useCooldown } from '@/lib/use-cooldown'

/** The API sends each client at most one email a minute. */
export const EMAIL_COOLDOWN_SECONDS = 60

/**
 * Asks Identity to email a new confirmation link to the given address, then waits out the API's
 * email limit. Failures are toasted for every mutation, so a rate-limited one only restarts the wait.
 */
export function ResendConfirmationButton({ email }: { email: string }) {
  const { t } = useTranslation()
  const cooldown = useCooldown()
  const { mutate, isPending } = usePostApiAuthResendConfirmationEmail({
    mutation: {
      onSuccess: () => {
        toast.success(t('resendConfirmation.sent', { email }))
        cooldown.start(EMAIL_COOLDOWN_SECONDS)
      },
      onError: (error) => {
        if (isTooManyRequests(error)) {
          cooldown.start(error.retryAfter ?? EMAIL_COOLDOWN_SECONDS)
        }
      },
    },
  })

  return (
    <Button
      variant="outline"
      disabled={isPending || cooldown.isActive}
      onClick={() => mutate({ data: { email } })}
    >
      {cooldown.isActive
        ? t('resendConfirmation.resendIn', { seconds: cooldown.secondsLeft })
        : t('resendConfirmation.resend')}
    </Button>
  )
}
