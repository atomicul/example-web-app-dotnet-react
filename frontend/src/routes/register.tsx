import { Link, createFileRoute } from '@tanstack/react-router'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import type { RegisterRequest } from '@/api/generated/model'
import { AuthCard } from '@/auth/auth-card'
import { publicSearchSchema, redirectWhenSignedIn } from '@/auth/public-route'
import { RegisterForm } from '@/auth/register-form'
import { useAuth } from '@/auth/use-auth'
import { buttonVariants } from '@/components/ui/button'

export const Route = createFileRoute('/register')({
  validateSearch: publicSearchSchema,
  beforeLoad: redirectWhenSignedIn,
  component: RegisterPage,
})

function RegisterPage() {
  const { t } = useTranslation()
  const search = Route.useSearch()
  const { register } = useAuth()
  const [registeredEmail, setRegisteredEmail] = useState<string>()

  async function submit(registration: RegisterRequest) {
    await register(registration)
    setRegisteredEmail(registration.email)
  }

  if (registeredEmail) {
    return (
      <AuthCard
        title={t('register.checkEmail')}
        description={t('register.linkSent', { email: registeredEmail })}
      >
        <Link
          to="/login"
          search={search}
          className={buttonVariants({ className: 'w-full' })}
        >
          {t('register.goToLogIn')}
        </Link>
      </AuthCard>
    )
  }

  return (
    <AuthCard
      title={t('register.title')}
      description={t('register.description')}
      footer={
        <p>
          {t('register.haveAccount')}{' '}
          <Link to="/login" search={search} className="underline">
            {t('register.logIn')}
          </Link>
        </p>
      }
    >
      <RegisterForm onSubmit={submit} />
    </AuthCard>
  )
}
