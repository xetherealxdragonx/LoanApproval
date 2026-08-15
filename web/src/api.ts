// Wire contracts mirroring the DTOs in LoanApproval.Application.DTOs.
// ASP.NET Core serializes property names as camelCase by default, and
// DecisionType as a string because Program.cs registers JsonStringEnumConverter.

export type DecisionOutcome = 'Approved' | 'Denied' | 'ManualReviewRequired'

export interface ApplicantSummary {
  id: number
  memberNumber: string
  fullName: string
  monthlyIncome: number
  openLoanCount: number
  hasRecentDelinquency: boolean
  memberSince: string
  applicationCount: number
}

export interface LoanApplicationSummary {
  loanApplicationId: number
  requestedAmount: number
  submittedAtUtc: string
  fundedAtUtc: string | null
  /** Null when an application was persisted but no decision was recorded against it. */
  outcome: DecisionOutcome | null
  reasoning: string | null
  evaluatedAtUtc: string | null
  evaluationDurationMs: number | null
}

export interface ApplicantDetail {
  id: number
  memberNumber: string
  fullName: string
  monthlyIncome: number
  openLoanCount: number
  hasRecentDelinquency: boolean
  memberSince: string
  applications: LoanApplicationSummary[]
}

/** Thrown for any non-2xx response, carrying the status so callers can treat 404 specially. */
export class ApiError extends Error {
  // Declared and assigned explicitly rather than as a constructor parameter
  // property, which the tsconfig's erasableSyntaxOnly setting disallows.
  readonly status: number

  constructor(message: string, status: number) {
    super(message)
    this.status = status
  }
}

async function getJson<T>(path: string): Promise<T> {
  const response = await fetch(path, { headers: { Accept: 'application/json' } })

  if (!response.ok) {
    // The API returns { error: "..." } for 404s; fall back to the status text
    // for anything that isn't shaped that way (a 500 HTML page, for instance).
    const detail = await response
      .json()
      .then((body: { error?: string }) => body?.error)
      .catch(() => undefined)

    throw new ApiError(detail ?? `Request failed (${response.status})`, response.status)
  }

  return (await response.json()) as T
}

export const getApplicants = () => getJson<ApplicantSummary[]>('/api/applicants')

export const getApplicant = (memberNumber: string) =>
  getJson<ApplicantDetail>(`/api/applicants/${encodeURIComponent(memberNumber)}`)
