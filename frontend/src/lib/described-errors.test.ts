import { describe, expect, it } from 'vitest'
import type { z } from 'zod'
import {
  PostApiAuthLoginBody,
  PostApiAuthRegisterBody,
} from '@/api/generated/index.zod'
import { describedErrors } from './described-errors'

function errorsFor(schema: z.ZodType, values: object) {
  const result = schema.safeParse(values, { error: describedErrors(schema) })
  return result.error?.issues.map(({ path, message }) => ({ path, message }))
}

const passwordRules = PostApiAuthRegisterBody.shape.password.description
const emailRules = PostApiAuthRegisterBody.shape.email.description

describe('describedErrors', () => {
  it('accepts a registration Identity accepts', () => {
    expect(
      errorsFor(PostApiAuthRegisterBody, {
        email: 'ada@localhost',
        password: 'Abcdef1!',
      }),
    ).toBeUndefined()
  })

  it.each(['abc', 'abcdef1!', 'Abcdefg!'])(
    'reports the weak password %s with the password rules',
    (password) => {
      expect(
        errorsFor(PostApiAuthRegisterBody, {
          email: 'ada@example.com',
          password,
        }),
      ).toContainEqual({ path: ['password'], message: passwordRules })
    },
  )

  it('reports an email with characters Identity refuses with the email rules', () => {
    expect(
      errorsFor(PostApiAuthRegisterBody, {
        email: 'a b@example.com',
        password: 'Abcdef1!',
      }),
    ).toEqual([{ path: ['email'], message: emailRules }])
  })

  it("keeps zod's message for a field without a description", () => {
    const [issue] =
      errorsFor(PostApiAuthLoginBody, { email: 'ada@example.com' }) ?? []
    expect(issue.path).toEqual(['password'])
    expect(issue.message).not.toBe(passwordRules)
  })
})
