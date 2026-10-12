import { describe, expect, it, vi } from 'vitest'
import { ApiError, NetworkError, errorMessage } from '@/api/fetcher'
import { FormError, setServerErrors } from './server-errors'

function errorsFor(error: unknown) {
  const setError = vi.fn()
  setServerErrors(
    { getValues: () => ({ title: '', dueDate: null }), setError },
    error,
  )
  return Object.fromEntries(
    setError.mock.calls.map(([field, { message }]) => [field, message]),
  )
}

describe('setServerErrors', () => {
  it('puts validation errors under the fields they name', () => {
    const error = new ApiError(400, {
      errors: {
        Title: ['Too long.', 'Too shouty.'],
        DueDate: ['In the past.'],
      },
    })
    expect(errorsFor(error)).toEqual({
      title: 'Too long. Too shouty.',
      dueDate: 'In the past.',
    })
  })

  it('joins validation errors for unknown keys into the root error', () => {
    const error = new ApiError(400, {
      errors: { Title: ['Too long.'], Owner: ['Missing.'], '': ['Bad body.'] },
    })
    expect(errorsFor(error)).toEqual({
      title: 'Too long.',
      root: 'Missing. Bad body.',
    })
  })

  it('reports an error without validation errors on the root', () => {
    expect(errorsFor(new ApiError(500, { title: 'Server broke' }))).toEqual({
      root: 'Server broke',
    })
    expect(errorsFor(new NetworkError())).toEqual({
      root: errorMessage(new NetworkError()),
    })
  })

  it("reports a FormError's message on the root", () => {
    expect(errorsFor(new FormError('Invalid email or password.'))).toEqual({
      root: 'Invalid email or password.',
    })
  })

  it('rethrows an error that did not come from a request', () => {
    const bug = new TypeError('todo is undefined')
    expect(() => errorsFor(bug)).toThrow(bug)
  })
})
