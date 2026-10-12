import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError, errorMessage, fetcher } from './fetcher'

function respondWith(response: Response) {
  vi.stubGlobal('fetch', vi.fn().mockResolvedValue(response))
}

function json(body: unknown, status: number, contentType = 'application/json') {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'content-type': contentType },
  })
}

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('fetcher', () => {
  it('resolves the JSON body of a successful response', async () => {
    respondWith(json([{ id: 1, title: 'Buy milk' }], 200))

    await expect(fetcher('/api/todos', {})).resolves.toEqual([
      { id: 1, title: 'Buy milk' },
    ])
  })

  it('resolves undefined when the response has no body', async () => {
    respondWith(new Response(null, { status: 204 }))

    await expect(
      fetcher('/api/todos/1', { method: 'DELETE' }),
    ).resolves.toBeUndefined()
  })

  it('throws an ApiError carrying validation errors', async () => {
    const problem = {
      title: 'One or more validation errors occurred.',
      errors: { Title: ['The Title field is required.'] },
    }
    respondWith(json(problem, 400, 'application/problem+json'))

    const error = await fetcher('/api/todos', { method: 'POST' }).catch(
      (e: unknown) => e,
    )

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({
      status: 400,
      message: problem.title,
      errors: problem.errors,
    })
  })

  it('throws an ApiError without a problem when the error has no body', async () => {
    respondWith(new Response(null, { status: 401 }))

    const error = await fetcher('/api/todos', {}).catch((e: unknown) => e)

    expect(error).toMatchObject({ status: 401, problem: undefined, errors: {} })
  })

  it('throws an ApiError with the seconds to wait when rate limited', async () => {
    respondWith(
      new Response(null, { status: 429, headers: { 'retry-after': '42' } }),
    )

    const error = await fetcher('/api/todos', {}).catch((e: unknown) => e)

    expect(error).toMatchObject({ status: 429, retryAfter: 42 })
    expect(errorMessage(error)).toContain('try again in 42 seconds')
  })

  it('leaves the wait unknown when a 429 has no Retry-After header', async () => {
    respondWith(new Response(null, { status: 429 }))

    const error = await fetcher('/api/todos', {}).catch((e: unknown) => e)

    expect(error).toMatchObject({ status: 429, retryAfter: undefined })
    expect(errorMessage(error)).toContain('try again in a moment')
  })
})
