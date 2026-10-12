import type { QueryClient } from '@tanstack/react-query'
import { redirect } from '@tanstack/react-router'
import { z } from 'zod'
import { fetchSession } from './session'

/** Search params of the public pages: where to return after signing in, limited to this site. */
export const publicSearchSchema = z.object({
  redirect: z
    .string()
    .regex(/^\/(?!\/)/)
    .optional()
    .catch(undefined),
})

type PublicRouteGuardOptions = {
  context: { queryClient: QueryClient }
  search: z.infer<typeof publicSearchSchema>
}

/** Sends signed-in users on to where they were heading instead of showing them a sign-in page. */
export async function redirectWhenSignedIn({
  context,
  search,
}: PublicRouteGuardOptions) {
  if (await fetchSession(context.queryClient)) {
    throw redirect({ href: search.redirect ?? '/' })
  }
}
