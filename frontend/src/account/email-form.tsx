import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import {
  getGetPendingEmailQueryKey,
  useGetApiAuthManageInfo,
  useGetPendingEmail,
  usePostApiAuthManageInfo,
} from '@/api/generated'
import { PostApiAuthManageInfoBody } from '@/api/generated/index.zod'
import { Button } from '@/components/ui/button'
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card'
import {
  Field,
  FieldError,
  FieldGroup,
  FieldLabel,
} from '@/components/ui/field'
import { Input } from '@/components/ui/input'
import { fieldMessages } from '@/lib/field-messages'
import { setServerErrors } from '@/lib/server-errors'
import { PendingEmailNotice } from './pending-email-notice'

const EmailChangeBody = PostApiAuthManageInfoBody.pick({
  newEmail: true,
}).required()

/**
 * Asks to change the user's email. Identity emails a link to the new address and keeps the current one until it is
 * opened, so the change shows as pending until then.
 */
export function EmailForm() {
  const { t } = useTranslation()
  const { data: user } = useGetApiAuthManageInfo()
  const { data: pending } = useGetPendingEmail()
  const form = useForm({
    resolver: zodResolver(EmailChangeBody, {
      error: fieldMessages({ newEmail: t('register.emailRules') }),
    }),
    defaultValues: { newEmail: '' },
  })
  const { errors, isSubmitting } = form.formState
  const { mutateAsync: changeEmail } = usePostApiAuthManageInfo({
    mutation: {
      meta: {
        handlesErrors: true,
        invalidates: [getGetPendingEmailQueryKey()],
      },
    },
  })

  async function submit({ newEmail }: { newEmail: string | null }) {
    try {
      await changeEmail({ data: { newEmail } })
      toast.success(t('account.emailLinkSent', { email: newEmail }))
      form.reset()
    } catch (error) {
      setServerErrors(form, error)
    }
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>{t('account.email')}</CardTitle>
        <CardDescription>
          {user && t('account.currentEmail', { email: user.email })}
        </CardDescription>
      </CardHeader>
      <CardContent className="flex flex-col gap-4">
        {pending?.email && <PendingEmailNotice email={pending.email} />}
        <form onSubmit={form.handleSubmit(submit)} noValidate>
          <FieldGroup>
            <Field data-invalid={!!errors.newEmail}>
              <FieldLabel htmlFor="new-email">
                {t('account.newEmail')}
              </FieldLabel>
              <Input
                id="new-email"
                type="email"
                autoComplete="email"
                aria-invalid={!!errors.newEmail}
                {...form.register('newEmail')}
              />
              <FieldError errors={[errors.newEmail]} />
            </Field>
            <FieldError errors={[errors.root]} />
            <Button
              type="submit"
              disabled={isSubmitting}
              className="self-start"
            >
              {t('account.changeEmail')}
            </Button>
          </FieldGroup>
        </form>
      </CardContent>
    </Card>
  )
}
