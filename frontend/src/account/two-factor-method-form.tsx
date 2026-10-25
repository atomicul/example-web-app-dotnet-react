import type { ParseKeys } from 'i18next'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import {
  getGetPendingEmailQueryKey,
  getGetTwoFactorMethodQueryKey,
  useGetTwoFactorMethod,
  usePostApiAuthManage2fa,
} from '@/api/generated'
import { TwoFactorMethod } from '@/api/generated/model'
import { Button } from '@/components/ui/button'
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card'
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import {
  Field,
  FieldContent,
  FieldDescription,
  FieldLabel,
  FieldTitle,
} from '@/components/ui/field'
import { RadioGroup, RadioGroupItem } from '@/components/ui/radio-group'
import { AuthenticatorSetupDialog } from './authenticator-setup-dialog'

const methods: {
  value: TwoFactorMethod
  title: ParseKeys
  description: ParseKeys
}[] = [
  {
    value: TwoFactorMethod.Email,
    title: 'account.twoFactorEmail',
    description: 'account.twoFactorEmailDescription',
  },
  {
    value: TwoFactorMethod.Authenticator,
    title: 'account.twoFactorApp',
    description: 'account.twoFactorAppDescription',
  },
]

/**
 * Picks how the user finishes a login: with a code emailed to them, or one an authenticator app shows. Picking the
 * app sets it up first, with a new key, so the choice changes only once a code from it checks out; picking the email
 * asks first, as the app and its recovery codes stop working. The choice is disabled while logins ask for no code.
 */
export function TwoFactorMethodForm() {
  const { t } = useTranslation()
  const { data } = useGetTwoFactorMethod()
  const [sharedKey, setSharedKey] = useState('')
  const [isSettingUp, setIsSettingUp] = useState(false)
  const [isConfirmingEmail, setIsConfirmingEmail] = useState(false)
  const { mutate: change, isPending } = usePostApiAuthManage2fa({
    mutation: {
      meta: {
        invalidates: [
          getGetTwoFactorMethodQueryKey(),
          getGetPendingEmailQueryKey(),
        ],
      },
    },
  })

  function setUpApp() {
    change(
      { data: { resetSharedKey: true } },
      {
        onSuccess: (setup) => {
          setSharedKey(setup.sharedKey)
          setIsSettingUp(true)
        },
      },
    )
  }

  function switchToEmail() {
    change(
      {
        data: { enable: false, resetSharedKey: true, resetRecoveryCodes: true },
      },
      {
        onSuccess: () => {
          setIsConfirmingEmail(false)
          toast.success(t('account.switchedToEmail'))
        },
      },
    )
  }

  function choose(method: TwoFactorMethod) {
    if (method === TwoFactorMethod.Authenticator) {
      setUpApp()
    } else {
      setIsConfirmingEmail(true)
    }
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>{t('account.twoFactor')}</CardTitle>
        <CardDescription>
          {t(
            data?.isAvailable
              ? 'account.twoFactorDescription'
              : 'account.twoFactorUnavailable',
          )}
        </CardDescription>
      </CardHeader>
      <CardContent>
        <RadioGroup
          value={data?.method}
          onValueChange={(method) => choose(method as TwoFactorMethod)}
          disabled={!data?.isAvailable || isPending}
        >
          {methods.map(({ value, title, description }) => (
            <FieldLabel key={value} htmlFor={`twoFactor${value}`}>
              <Field
                orientation="horizontal"
                data-disabled={!data?.isAvailable}
              >
                <RadioGroupItem value={value} id={`twoFactor${value}`} />
                <FieldContent>
                  <FieldTitle>{t(title)}</FieldTitle>
                  <FieldDescription>{t(description)}</FieldDescription>
                </FieldContent>
              </Field>
            </FieldLabel>
          ))}
        </RadioGroup>
      </CardContent>
      <AuthenticatorSetupDialog
        open={isSettingUp}
        onOpenChange={setIsSettingUp}
        sharedKey={sharedKey}
      />
      <Dialog open={isConfirmingEmail} onOpenChange={setIsConfirmingEmail}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('account.useEmail')}</DialogTitle>
            <DialogDescription>
              {t('account.useEmailDescription')}
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <DialogClose render={<Button variant="outline" />}>
              {t('account.cancel')}
            </DialogClose>
            <Button disabled={isPending} onClick={switchToEmail}>
              {t('account.switchToEmail')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </Card>
  )
}
