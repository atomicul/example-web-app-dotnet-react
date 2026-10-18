import { afterEach, describe, expect, it } from 'vitest'
import { z } from 'zod'
import en from './en.json'
import ro from './ro.json'
import { i18n } from '.'

/** Every key of a catalog, without the plural suffixes languages differ in. */
function keysOf(catalog: object, prefix = ''): string[] {
  return Object.entries(catalog).flatMap(([key, value]) =>
    typeof value === 'object'
      ? keysOf(value, `${prefix}${key}.`)
      : [`${prefix}${key.replace(/_(zero|one|two|few|many|other)$/, '')}`],
  )
}

afterEach(async () => {
  await i18n.changeLanguage('en')
})

describe('i18n', () => {
  it('has a Romanian translation for every English text', () => {
    const texts = keysOf(ro).filter(
      (key) => !key.startsWith('validationErrors.'),
    )
    expect(new Set(texts)).toEqual(new Set(keysOf(en)))
  })

  it('words plurals by the rules of the language', async () => {
    await i18n.changeLanguage('ro')

    expect(i18n.t('errors.tooManyRequests', { count: 1 })).toContain(
      '1 secundă',
    )
    expect(i18n.t('errors.tooManyRequests', { count: 5 })).toContain(
      '5 secunde',
    )
    expect(i18n.t('errors.tooManyRequests', { count: 42 })).toContain(
      '42 de secunde',
    )
  })

  it("words zod's messages in the SPA's language", async () => {
    const english = z.string().safeParse(1).error?.issues[0].message

    await i18n.changeLanguage('ro')

    expect(z.string().safeParse(1).error?.issues[0].message).not.toBe(english)
  })
})
