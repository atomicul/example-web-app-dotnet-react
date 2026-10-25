import { QRCodeSVG } from 'qrcode.react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { ApiError } from '@/api/fetcher'
import {
  getGetPendingEmailQueryKey,
  getGetTwoFactorMethodQueryKey,
  useGetApiAuthManageInfo,
  usePostApiAuthManage2fa,
} from '@/api/generated'
import { TwoFactorForm } from '@/auth/two-factor-form'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { FormError } from '@/lib/server-errors'

/** The name authenticator apps show the account under. */
const ISSUER = 'Todos'

/** The `otpauth://` URI authenticator apps read from a QR code, as Google Authenticator's key URI format has it. */
function keyUri(email: string, sharedKey: string) {
  const issuer = encodeURIComponent(ISSUER)
  return `otpauth://totp/${issuer}:${encodeURIComponent(email)}?secret=${sharedKey}&issuer=${issuer}`
}

/** The key in groups of four, as it is easier to type by hand. */
function groupKey(sharedKey: string) {
  return sharedKey.match(/.{1,4}/g)?.join(' ') ?? sharedKey
}

type AuthenticatorSetupDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  sharedKey: string
}

/**
 * Sets up an authenticator app with a new key: shows it as a QR code and as text, then turns the app on once it
 * checks a code from it, and shows the recovery codes Identity makes for it. The codes are made anew each time, so
 * those of an earlier setup stop working.
 */
export function AuthenticatorSetupDialog({
  open,
  onOpenChange,
  sharedKey,
}: AuthenticatorSetupDialogProps) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <AuthenticatorSetup
          sharedKey={sharedKey}
          onDone={() => onOpenChange(false)}
        />
      </DialogContent>
    </Dialog>
  )
}

function AuthenticatorSetup({
  sharedKey,
  onDone,
}: {
  sharedKey: string
  onDone: () => void
}) {
  const { t } = useTranslation()
  const { data: user } = useGetApiAuthManageInfo()
  const [recoveryCodes, setRecoveryCodes] = useState<string[]>()
  const { mutateAsync: turnOn } = usePostApiAuthManage2fa({
    mutation: {
      meta: {
        handlesErrors: true,
        invalidates: [
          getGetTwoFactorMethodQueryKey(),
          getGetPendingEmailQueryKey(),
        ],
      },
    },
  })

  async function submit(twoFactorCode: string) {
    try {
      const setup = await turnOn({
        data: { enable: true, twoFactorCode, resetRecoveryCodes: true },
      })
      setRecoveryCodes(setup.recoveryCodes ?? [])
    } catch (error) {
      throw isInvalidCode(error)
        ? new FormError(t('account.wrongAppCode'))
        : error
    }
  }

  async function copy(codes: string[]) {
    await navigator.clipboard.writeText(codes.join('\n'))
    toast.success(t('account.recoveryCodesCopied'))
  }

  if (recoveryCodes) {
    return (
      <>
        <DialogHeader>
          <DialogTitle>{t('account.recoveryCodes')}</DialogTitle>
          <DialogDescription>
            {t('account.recoveryCodesDescription')}
          </DialogDescription>
        </DialogHeader>
        <ul className="grid grid-cols-2 gap-2 font-mono">
          {recoveryCodes.map((code) => (
            <li key={code}>{code}</li>
          ))}
        </ul>
        <DialogFooter>
          <Button variant="outline" onClick={() => void copy(recoveryCodes)}>
            {t('account.copyRecoveryCodes')}
          </Button>
          <Button onClick={onDone}>{t('account.done')}</Button>
        </DialogFooter>
      </>
    )
  }

  return (
    <>
      <DialogHeader>
        <DialogTitle>{t('account.setUpApp')}</DialogTitle>
        <DialogDescription>
          {t('account.setUpAppDescription')}
        </DialogDescription>
      </DialogHeader>
      <div className="flex flex-col items-center gap-2">
        <QRCodeSVG
          value={keyUri(user?.email ?? '', sharedKey)}
          marginSize={2}
          className="size-40 rounded-md"
        />
        <p className="text-muted-foreground">{t('account.appKey')}</p>
        <code className="font-mono select-all">{groupKey(sharedKey)}</code>
      </div>
      <TwoFactorForm
        kind="app"
        submitLabel={t('account.turnOnApp')}
        onSubmit={submit}
      />
    </>
  )
}

/** Whether Identity refused to turn the app on because the code was wrong. */
function isInvalidCode(error: unknown) {
  return (
    error instanceof ApiError &&
    error.problem?.issues?.some(({ code }) => code === 'InvalidTwoFactorCode')
  )
}
