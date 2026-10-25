import { zodResolver } from '@hookform/resolvers/zod'
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

const TwoFactorCodeBody = z.object({ code: z.string().trim().min(1) })

type TwoFactorCode = z.infer<typeof TwoFactorCodeBody>

type TwoFactorFormProps = {
  onSubmit: (code: string) => Promise<void>
}

/** Asks for the code emailed at login; failures show under the field. */
export function TwoFactorForm({ onSubmit }: TwoFactorFormProps) {
  const { t } = useTranslation()
  const form = useForm({
    resolver: zodResolver(TwoFactorCodeBody, {
      error: fieldMessages({ code: t('twoFactor.codeRequired') }),
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
          <FieldLabel htmlFor="code">{t('twoFactor.code')}</FieldLabel>
          <Input
            id="code"
            inputMode="numeric"
            autoComplete="one-time-code"
            autoFocus
            aria-invalid={!!errors.code}
            {...form.register('code')}
          />
          <FieldError errors={[errors.code]} />
        </Field>
        <FieldError errors={[errors.root]} />
        <Button type="submit" disabled={isSubmitting}>
          {t('twoFactor.submit')}
        </Button>
      </FieldGroup>
    </form>
  )
}
