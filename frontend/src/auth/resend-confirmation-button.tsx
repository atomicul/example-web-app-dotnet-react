import { toast } from 'sonner'
import { errorMessage } from '@/api/fetcher'
import { usePostApiAuthResendConfirmationEmail } from '@/api/generated'
import { Button } from '@/components/ui/button'

/** Asks Identity to email a new confirmation link to the given address. */
export function ResendConfirmationButton({ email }: { email: string }) {
  const { mutate, isPending } = usePostApiAuthResendConfirmationEmail({
    mutation: {
      onSuccess: () =>
        toast.success(`We sent a new confirmation link to ${email}.`),
      onError: (error) => toast.error(errorMessage(error)),
    },
  })

  return (
    <Button
      variant="outline"
      disabled={isPending}
      onClick={() => mutate({ data: { email } })}
    >
      Resend confirmation email
    </Button>
  )
}
