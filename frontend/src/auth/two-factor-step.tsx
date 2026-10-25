import type { ParseKeys } from 'i18next'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { TwoFactorMethod } from '@/api/generated/model'
import type { LoginRequest } from '@/api/generated/model'
import { AuthCard } from '@/auth/auth-card'
import { loginFailureReason } from '@/auth/login-failure'
import { ResendTwoFactorCodeButton } from '@/auth/resend-two-factor-code-button'
import { TwoFactorForm } from '@/auth/two-factor-form'
import type { CodeKind } from '@/auth/two-factor-form'
import { useAuth } from '@/auth/use-auth'
import { Button } from '@/components/ui/button'
import { FormError } from '@/lib/server-errors'

const stepTexts: Record<
  CodeKind,
  { title: ParseKeys; description: ParseKeys; wrongCode: ParseKeys }
> = {
  email: {
    title: 'twoFactor.title',
    description: 'twoFactor.description',
    wrongCode: 'twoFactor.wrongCode',
  },
  app: {
    title: 'twoFactor.appTitle',
    description: 'twoFactor.appDescription',
    wrongCode: 'twoFactor.wrongAppCode',
  },
  recovery: {
    title: 'twoFactor.recoveryTitle',
    description: 'twoFactor.recoveryDescription',
    wrongCode: 'twoFactor.wrongRecoveryCode',
  },
}

type TwoFactorStepProps = {
  credentials: LoginRequest
  method: TwoFactorMethod
  onCancel: () => void
}

/**
 * The login's second step, once the password is right: asks for the code of the user's way of finishing it, sending
 * the same credentials again along with the code. A user with an authenticator app can give one of their recovery
 * codes instead.
 */
export function TwoFactorStep({
  credentials,
  method,
  onCancel,
}: TwoFactorStepProps) {
  const { t } = useTranslation()
  const { login } = useAuth()
  const [kind, setKind] = useState<CodeKind>(
    method === TwoFactorMethod.Authenticator ? 'app' : 'email',
  )
  const texts = stepTexts[kind]

  async function submit(code: string) {
    try {
      await login({
        ...credentials,
        ...(kind === 'recovery'
          ? { twoFactorRecoveryCode: code }
          : { twoFactorCode: code }),
      })
    } catch (error) {
      const reason = loginFailureReason(error)
      if (reason === undefined) {
        throw error
      }
      throw new FormError(
        t(reason === 'LockedOut' ? 'login.lockedOut' : texts.wrongCode),
      )
    }
  }

  return (
    <AuthCard
      title={t(texts.title)}
      description={t(texts.description, { email: credentials.email })}
    >
      <div className="flex flex-col gap-4">
        <TwoFactorForm
          key={kind}
          kind={kind}
          submitLabel={t('twoFactor.submit')}
          onSubmit={submit}
        />
        {kind === 'email' && (
          <ResendTwoFactorCodeButton credentials={credentials} />
        )}
        {kind !== 'email' && (
          <Button
            variant="outline"
            onClick={() => setKind(kind === 'app' ? 'recovery' : 'app')}
          >
            {t(
              kind === 'app'
                ? 'twoFactor.useRecoveryCode'
                : 'twoFactor.useAppCode',
            )}
          </Button>
        )}
        <Button variant="ghost" onClick={onCancel}>
          {t('twoFactor.useAnotherAccount')}
        </Button>
      </div>
    </AuthCard>
  )
}
