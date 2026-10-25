import { zodResolver } from '@hookform/resolvers/zod'
import type { ParseKeys } from 'i18next'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { z } from 'zod'
import { Button } from '@/components/ui/button'
import {
  Field,
  FieldError,
  FieldGroup,
  FieldLabel,
} from '@/components/ui/field'
import { Input } from '@/components/ui/input'
import { fieldMessages } from '@/lib/field-messages'
import { setServerErrors } from '@/lib/server-errors'

/** Where a code comes from: an email, an authenticator app, or the recovery codes saved when the app was set up. */
export type CodeKind = 'email' | 'app' | 'recovery'

const codeFields: Record<
  CodeKind,
  { label: ParseKeys; required: ParseKeys; isNumeric: boolean }
> = {
  email: {
    label: 'twoFactor.code',
    required: 'twoFactor.codeRequired',
    isNumeric: true,
  },
  app: {
    label: 'twoFactor.code',
    required: 'twoFactor.appCodeRequired',
    isNumeric: true,
  },
  recovery: {
    label: 'twoFactor.recoveryCode',
    required: 'twoFactor.recoveryCodeRequired',
    isNumeric: false,
  },
}

const TwoFactorCodeBody = z.object({ code: z.string().trim().min(1) })

type TwoFactorCode = z.infer<typeof TwoFactorCodeBody>

type TwoFactorFormProps = {
  kind: CodeKind
  submitLabel: string
  onSubmit: (code: string) => Promise<void>
}

/** Asks for a code of the given kind; failures show under the field. */
export function TwoFactorForm({
  kind,
  submitLabel,
  onSubmit,
}: TwoFactorFormProps) {
  const { t } = useTranslation()
  const { label, required, isNumeric } = codeFields[kind]
  const form = useForm({
    resolver: zodResolver(TwoFactorCodeBody, {
      error: fieldMessages({ code: t(required) }),
    }),
    defaultValues: { code: '' },
  })
  const { errors, isSubmitting } = form.formState

  async function submit({ code }: TwoFactorCode) {
    try {
      await onSubmit(code)
    } catch (error) {
      setServerErrors(form, error)
    }
  }

  return (
    <form onSubmit={form.handleSubmit(submit)} noValidate>
      <FieldGroup>
        <Field data-invalid={!!errors.code}>
          <FieldLabel htmlFor="code">{t(label)}</FieldLabel>
          <Input
            id="code"
            inputMode={isNumeric ? 'numeric' : 'text'}
            autoComplete={isNumeric ? 'one-time-code' : 'off'}
            autoFocus
            aria-invalid={!!errors.code}
            {...form.register('code')}
          />
          <FieldError errors={[errors.code]} />
        </Field>
        <FieldError errors={[errors.root]} />
        <Button type="submit" disabled={isSubmitting}>
          {submitLabel}
        </Button>
      </FieldGroup>
    </form>
  )
}
