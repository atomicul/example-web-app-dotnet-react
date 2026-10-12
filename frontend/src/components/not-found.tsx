import { Link } from '@tanstack/react-router'
import { buttonVariants } from '@/components/ui/button'

/** Shown for any path that no route matches. */
export function NotFound() {
  return (
    <main className="grid min-h-svh place-items-center p-4">
      <div className="flex max-w-sm flex-col items-center gap-3 text-center">
        <p className="text-muted-foreground font-mono text-sm">404</p>
        <h1 className="font-heading text-2xl font-semibold">Page not found</h1>
        <p className="text-muted-foreground text-sm">
          The page you are looking for does not exist or has moved.
        </p>
        <Link to="/" className={buttonVariants({ className: 'mt-2' })}>
          Back to your todos
        </Link>
      </div>
    </main>
  )
}
