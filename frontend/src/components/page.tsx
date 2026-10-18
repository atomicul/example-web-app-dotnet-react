import { Link } from '@tanstack/react-router'
import { ArrowLeftIcon } from 'lucide-react'
import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/button'

type PageProps = {
  title: string
  children: ReactNode
}

/**
 * The layout of every page nested under another: a button back to the page its path is nested under and the
 * page's title, then its content.
 */
export function Page({ title, children }: PageProps) {
  const { t } = useTranslation()

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center gap-2">
        <Button
          variant="ghost"
          size="icon"
          aria-label={t('page.back')}
          nativeButton={false}
          render={<Link to=".." />}
        >
          <ArrowLeftIcon />
        </Button>
        <h1 className="font-heading text-lg font-semibold">{title}</h1>
      </div>
      {children}
    </div>
  )
}
