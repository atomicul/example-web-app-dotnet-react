import { MailIcon } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { isTooManyRequests } from '@/api/fetcher'
import {
  getGetPendingEmailQueryKey,
  useCancelPendingEmail,
  usePostApiAuthManageInfo,
} from '@/api/generated'
import { EMAIL_COOLDOWN_SECONDS } from '@/auth/resend-confirmation-button'
import { Button } from '@/components/ui/button'
import { useCooldown } from '@/lib/use-cooldown'

/**
 * Shows the email change waiting for confirmation. Sending its link again asks for the same change, whose earlier
 * links keep working; cancelling makes every link sent for it stop working. Failures are toasted for every mutation,
 * so a rate-limited resend only restarts the wait.
 */
export function PendingEmailNotice({ email }: { email: string }) {
  const { t } = useTranslation()
  const cooldown = useCooldown()
  const meta = { invalidates: [getGetPendingEmailQueryKey()] }
  const { mutate: resend, isPending: isResending } = usePostApiAuthManageInfo({
    mutation: {
      meta,
      onSuccess: () => {
        toast.success(t('account.emailLinkSent', { email }))
        cooldown.start(EMAIL_COOLDOWN_SECONDS)
      },
      onError: (error) => {
        if (isTooManyRequests(error)) {
          cooldown.start(error.retryAfter ?? EMAIL_COOLDOWN_SECONDS)
        }
      },
    },
  })
  const { mutate: cancel, isPending: isCancelling } = useCancelPendingEmail({
    mutation: {
      meta,
      onSuccess: () =>
        toast.success(t('account.emailChangeCancelled', { email })),
    },
  })

  return (
    <div className="bg-muted flex flex-col gap-3 rounded-lg p-3">
      <p className="flex gap-2">
        <MailIcon className="mt-0.5 size-4 shrink-0" />
        {t('account.pendingEmail', { email })}
      </p>
      <div className="flex flex-wrap gap-2">
        <Button
          variant="outline"
          size="sm"
          disabled={isResending || cooldown.isActive}
          onClick={() => resend({ data: { newEmail: email } })}
        >
          {cooldown.isActive
            ? t('account.resendLinkIn', { seconds: cooldown.secondsLeft })
            : t('account.resendLink')}
        </Button>
        <Button
          variant="ghost"
          size="sm"
          disabled={isCancelling}
          onClick={() => cancel()}
        >
          {t('account.cancelEmailChange')}
        </Button>
      </div>
    </div>
  )
}
