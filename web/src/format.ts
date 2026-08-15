import type { DecisionOutcome } from './api'

const currency = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' })
const dateOnly = new Intl.DateTimeFormat('en-US', { dateStyle: 'medium', timeZone: 'UTC' })
const dateAndTime = new Intl.DateTimeFormat('en-US', { dateStyle: 'medium', timeStyle: 'short' })

export const formatMoney = (value: number) => currency.format(value)

/**
 * SQL Server's datetime2 does not persist DateTimeKind, so values read back
 * through EF Core are Unspecified and serialize with no timezone designator
 * ("2026-08-15T20:02:20.466"). JavaScript parses a bare string like that as
 * *local* time. Every date field the API exposes is UTC by convention - the
 * property names all end in Utc - so attach the designator before parsing,
 * otherwise every timestamp silently shifts by the viewer's offset.
 */
function parseUtc(iso: string): Date {
  const hasDesignator = /(?:Z|[+-]\d{2}:?\d{2})$/.test(iso)
  return new Date(hasDesignator ? iso : `${iso}Z`)
}

/**
 * For calendar dates such as MemberSince, which are a day rather than an
 * instant. Formatted in UTC so the rendered day matches the stored day instead
 * of rolling backwards for viewers west of UTC.
 */
export const formatDate = (iso: string) => dateOnly.format(parseUtc(iso))

/** For true instants (submitted, evaluated, funded), rendered in local time. */
export const formatDateTime = (iso: string) => dateAndTime.format(parseUtc(iso))

/**
 * Maps a decision outcome to its display label and badge class. A null outcome
 * means the application row exists with no decision recorded against it, which
 * the UI shows as "Pending" rather than silently rendering an empty cell.
 */
export function describeOutcome(outcome: DecisionOutcome | null): {
  label: string
  tone: 'approved' | 'denied' | 'review' | 'pending'
} {
  switch (outcome) {
    case 'Approved':
      return { label: 'Approved', tone: 'approved' }
    case 'Denied':
      return { label: 'Denied', tone: 'denied' }
    case 'ManualReviewRequired':
      return { label: 'Manual review', tone: 'review' }
    default:
      return { label: 'Pending', tone: 'pending' }
  }
}
