import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError, fetcher } from './fetcher'

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

    await expect(fetcher('/api/v1/todos', {})).resolves.toEqual([
      { id: 1, title: 'Buy milk' },
    ])
  })

  it('resolves undefined when the response has no body', async () => {
    respondWith(new Response(null, { status: 204 }))

    await expect(
      fetcher('/api/v1/todos/1', { method: 'DELETE' }),
    ).resolves.toBeUndefined()
  })

  it('throws an ApiError carrying validation errors', async () => {
    const problem = {
      title: 'One or more validation errors occurred.',
      errors: { Title: ['The Title field is required.'] },
    }
    respondWith(json(problem, 400, 'application/problem+json'))

    const error = await fetcher('/api/v1/todos', { method: 'POST' }).catch(
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

    const error = await fetcher('/api/v1/todos', {}).catch((e: unknown) => e)

    expect(error).toMatchObject({ status: 401, problem: undefined, errors: {} })
  })
})
