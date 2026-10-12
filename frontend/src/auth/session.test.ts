import { QueryClient } from '@tanstack/react-query'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { fetchSession } from './session'

function respondWith(status: number, body?: unknown) {
  const response =
    body === undefined
      ? new Response(null, { status })
      : Response.json(body, { status })
  vi.stubGlobal('fetch', vi.fn().mockResolvedValue(response))
}

function newQueryClient() {
  return new QueryClient({ defaultOptions: { queries: { retry: false } } })
}

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('fetchSession', () => {
  it('resolves the signed-in user', async () => {
    const user = { email: 'ana@example.com', isEmailConfirmed: false }
    respondWith(200, user)

    await expect(fetchSession(newQueryClient())).resolves.toEqual(user)
  })

  it('resolves null without a session cookie', async () => {
    respondWith(401)

    await expect(fetchSession(newQueryClient())).resolves.toBeNull()
  })

  it('resolves null when the cookie belongs to a user that no longer exists', async () => {
    respondWith(404)

    await expect(fetchSession(newQueryClient())).resolves.toBeNull()
  })

  it('rethrows any other failure', async () => {
    respondWith(500)

    await expect(fetchSession(newQueryClient())).rejects.toMatchObject({
      status: 500,
    })
  })
})
