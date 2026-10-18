import { describe, expect, it } from 'vitest'
import type { z } from 'zod'
import {
  PostApiAuthLoginBody,
  PostApiAuthRegisterBody,
} from '@/api/generated/index.zod'
import { fieldMessages } from './field-messages'

const messages = { email: 'Bad email.', password: 'Weak password.' }

function errorsFor(schema: z.ZodType, values: object) {
  const result = schema.safeParse(values, { error: fieldMessages(messages) })
  return result.error?.issues.map(({ path, message }) => ({ path, message }))
}

describe('fieldMessages', () => {
  it('accepts a registration Identity accepts', () => {
    expect(
      errorsFor(PostApiAuthRegisterBody, {
        email: 'ada@localhost',
        password: 'Abcdef1!',
      }),
    ).toBeUndefined()
  })

  it.each(['abc', 'abcdef1!', 'Abcdefg!'])(
    "reports the weak password %s with the password's message",
    (password) => {
      expect(
        errorsFor(PostApiAuthRegisterBody, {
          email: 'ada@example.com',
          password,
        }),
      ).toContainEqual({ path: ['password'], message: messages.password })
    },
  )

  it("reports an email with characters Identity refuses with the email's message", () => {
    expect(
      errorsFor(PostApiAuthRegisterBody, {
        email: 'a b@example.com',
        password: 'Abcdef1!',
      }),
    ).toEqual([{ path: ['email'], message: messages.email }])
  })

  it("keeps zod's message for a field without one", () => {
    const result = PostApiAuthLoginBody.safeParse(
      { email: 'ada@example.com' },
      { error: fieldMessages({ email: messages.email }) },
    )
    const [issue] = result.error?.issues ?? []
    expect(issue.path).toEqual(['password'])
    expect(issue.message).toMatch(/expected string/i)
  })
})
