import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { errorMessage, isTooManyRequests } from '@/api/fetcher'
import type { LoginRequest } from '@/api/generated/model'
import { loginFailureReason } from '@/auth/login-failure'
import { EMAIL_COOLDOWN_SECONDS } from '@/auth/resend-confirmation-button'
import { useAuth } from '@/auth/use-auth'
import { Button } from '@/components/ui/button'
import { useCooldown } from '@/lib/use-cooldown'

/**
 * Logs in again without a code, which makes the API email a new one, then waits out the API's email limit. It starts
 * waiting at once, as the login that showed it has just sent a code.
 */
export function ResendTwoFactorCodeButton({
  credentials,
}: {
  credentials: LoginRequest
}) {
  const { t } = useTranslation()
  const { login } = useAuth()
  const cooldown = useCooldown(EMAIL_COOLDOWN_SECONDS)
  const [isSending, setIsSending] = useState(false)

  async function resend() {
    setIsSending(true)
    try {
      await login(credentials)
    } catch (error) {
      if (loginFailureReason(error) === 'RequiresTwoFactor') {
        toast.success(t('twoFactor.sent', { email: credentials.email }))
        cooldown.start(EMAIL_COOLDOWN_SECONDS)
      } else {
        if (isTooManyRequests(error)) {
          cooldown.start(error.retryAfter ?? EMAIL_COOLDOWN_SECONDS)
        }
        toast.error(errorMessage(error))
      }
    } finally {
      setIsSending(false)
    }
  }

  return (
    <Button
      variant="outline"
      disabled={isSending || cooldown.isActive}
      onClick={() => void resend()}
    >
      {cooldown.isActive
        ? t('twoFactor.resendIn', { seconds: cooldown.secondsLeft })
        : t('twoFactor.resend')}
    </Button>
  )
}
