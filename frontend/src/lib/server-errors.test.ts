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
  it('puts validation errors under the fields they point to', () => {
    const error = new ApiError(400, {
      issues: [
        { pointer: '#/title', detail: 'Too long.', code: 'StringLength' },
        { pointer: '#/title', detail: 'Too shouty.', code: 'Shouty' },
        { pointer: '#/dueDate', detail: 'In the past.', code: 'Past' },
      ],
    })
    expect(errorsFor(error)).toEqual({
      title: 'Too long. Too shouty.',
      dueDate: 'In the past.',
    })
  })

  it('joins validation errors about other fields or none into the root error', () => {
    const error = new ApiError(400, {
      issues: [
        { pointer: '#/title', detail: 'Too long.', code: 'StringLength' },
        { pointer: '#/owner', detail: 'Missing.', code: 'Required' },
        { detail: 'Bad body.', code: 'Validate' },
      ],
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
