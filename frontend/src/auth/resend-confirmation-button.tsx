import { toast } from 'sonner'
import { isTooManyRequests } from '@/api/fetcher'
import { usePostApiAuthResendConfirmationEmail } from '@/api/generated'
import { Button } from '@/components/ui/button'
import { useCooldown } from '@/lib/use-cooldown'

/** The API sends each client at most one email a minute. */
const EMAIL_COOLDOWN_SECONDS = 60

/**
 * Asks Identity to email a new confirmation link to the given address, then waits out the API's
 * email limit. Failures are toasted for every mutation, so a rate-limited one only restarts the wait.
 */
export function ResendConfirmationButton({ email }: { email: string }) {
  const cooldown = useCooldown()
  const { mutate, isPending } = usePostApiAuthResendConfirmationEmail({
    mutation: {
      onSuccess: () => {
        toast.success(`We sent a new confirmation link to ${email}.`)
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
        ? `Resend confirmation email in ${cooldown.secondsLeft}s`
        : 'Resend confirmation email'}
    </Button>
  )
}
