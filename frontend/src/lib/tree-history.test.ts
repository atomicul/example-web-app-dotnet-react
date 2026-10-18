import { createMemoryHistory } from '@tanstack/react-router'
import { describe, expect, it } from 'vitest'
import { withTreeNavigation } from './tree-history'

function historyAt(href: string) {
  return withTreeNavigation(createMemoryHistory({ initialEntries: [href] }))
}

function visit(href: string, ...hrefs: string[]) {
  const history = historyAt(href)
  hrefs.forEach((next) => history.push(next))
  return history
}

/** The hrefs of the entries from the first to the current one. */
function entries(history: ReturnType<typeof historyAt>) {
  const hrefs = [history.location.href]
  while (history.canGoBack()) {
    history.back()
    hrefs.unshift(history.location.href)
  }
  return hrefs
}

describe('withTreeNavigation', () => {
  it('adds an entry for a page nested under the current one', () => {
    const history = visit('/', '/account', '/account/email')

    expect(entries(history)).toEqual(['/', '/account', '/account/email'])
  })

  it('goes back to a page behind instead of adding it again', () => {
    const history = visit('/', '/account', '/account/email', '/')

    expect(entries(history)).toEqual(['/'])
  })

  it('returns to the page as it was left when the link names only its path', () => {
    const history = visit('/?filter=done', '/account', '/')

    expect(history.location.href).toBe('/?filter=done')
  })

  it('replaces the current entry for a page that is neither nested nor behind', () => {
    const history = visit('/', '/account', '/settings')

    expect(entries(history)).toEqual(['/', '/settings'])
  })

  it('replaces a page opened directly when going up from it', () => {
    const history = visit('/account', '/')

    expect(entries(history)).toEqual(['/'])
  })

  it('keeps the entries behind when the current one is replaced', () => {
    const history = visit('/', '/account')
    history.replace('/account?tab=password')
    history.push('/')

    expect(entries(history)).toEqual(['/'])
  })
})
