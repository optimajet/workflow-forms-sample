import type {
  ExecuteFormResponse,
  Form,
  FormKey,
  SaveFormResponse
} from '@optimajet/workflow-forms-viewer'
import type {User} from './types.ts'

export type WorkflowApiClientOptions = {
  baseUrl: string
  tenantId?: string
  fetch?: typeof globalThis.fetch
}

type ApiErrorBody = {
  code?: string
  message?: string
  errors?: Record<string, unknown>
}

class WorkflowApiError extends Error {
  constructor(message: string, readonly status: number, readonly body: unknown) {
    super(message)
  }
}

export class WorkflowApiClient {
  constructor(options: WorkflowApiClientOptions) {
    this.baseUrl = options.baseUrl.replace(/\/$/, '')
    this.tenantId = normalizeTenantId(options.tenantId)
    this.transport = options.fetch ?? globalThis.fetch.bind(globalThis)
  }

  readonly baseUrl: string

  get designerUrl() {
    return `${this.baseUrl}/workflow-api/designer`
  }

  readonly customFetch = (input: RequestInfo | URL, init?: RequestInit) => {
    const headers = new Headers(init?.headers)
    this.addTenantHeader(headers)
    return this.transport(input, {...init, headers})
  }

  async createInstance(schemeCode: string, processId: string, identityId: string) {
    await this.post('/workflow-api/rpc/create-instance', {schemeCode, processId, identityId})
  }

  async getFormCode(formKey: FormKey) {
    const response = await this.post<{form: {formCode: string}}>(
      '/workflow-api/forms/get-form',
      {formKey}
    )
    return response.form.formCode
  }

  async getExecutableForms(
    processId: string,
    identityId: string
  ): Promise<Form[]> {
    const response = await this.post<{forms: Form[]}>(
      '/workflow-api/forms/get-executable-forms',
      {processId, identityId, conditionCheck: true}
    )
    return response.forms
  }

  async saveForm(
    processId: string,
    formKey: FormKey,
    formData: Record<string, unknown>,
    identityId: string
  ): Promise<SaveFormResponse> {
    try {
      const response = await this.post<{data: Record<string, unknown>}>(
        '/workflow-api/forms/save-form',
        {processId, formKey, formData, identityId, conditionCheck: true}
      )
      return {formData: response.data}
    } catch (error) {
      const validationErrors = getValidationErrors(error)
      if (validationErrors !== undefined) return {formErrors: validationErrors}
      throw error
    }
  }

  async executeForm(
    processId: string,
    formKey: FormKey,
    commandName: string,
    formData: Record<string, unknown>,
    identityId: string
  ): Promise<ExecuteFormResponse> {
    try {
      const response = await this.post<{wasExecuted: boolean}>(
        '/workflow-api/forms/execute-form',
        {processId, formKey, commandName, formData, identityId}
      )
      return {wasExecuted: response.wasExecuted}
    } catch (error) {
      const validationErrors = getValidationErrors(error)
      if (validationErrors !== undefined) return {formErrors: validationErrors}
      throw error
    }
  }

  getUsers(): Promise<User[]> {
    return this.get('/api/users/all')
  }

  getReports(user: string, skip: number, take: number): Promise<Record<string, unknown>[]> {
    const query = new URLSearchParams({user, skip: skip.toString(), take: take.toString()})
    return this.get(`/api/reports/data/query?${query}`)
  }

  getReportsCount(user: string): Promise<number> {
    const query = new URLSearchParams({user})
    return this.get(`/api/reports/data/count?${query}`)
  }

  private get<T>(path: string): Promise<T> {
    return this.request(path)
  }

  private post<T = unknown>(path: string, body: unknown): Promise<T> {
    return this.request(path, {
      method: 'POST',
      headers: {'Content-Type': 'application/json'},
      body: JSON.stringify(body)
    })
  }

  private async request<T>(path: string, init?: RequestInit): Promise<T> {
    const headers = new Headers(init?.headers)
    this.addTenantHeader(headers)
    const response = await this.transport(`${this.baseUrl}${path}`, {...init, headers})
    const text = await response.text()
    const body = parseBody(text)

    if (!response.ok) {
      const message = isRecord(body) && typeof body.message === 'string'
        ? (body as ApiErrorBody).message!
        : text
          ? `HTTP ${response.status} ${response.statusText}: ${text}`
          : `HTTP ${response.status} ${response.statusText}`
      throw new WorkflowApiError(message, response.status, body)
    }

    return body as T
  }

  private addTenantHeader(headers: Headers) {
    if (this.tenantId !== undefined) {
      headers.set('Workflow-Api-Tenant-ID', this.tenantId)
    }
  }

  private readonly tenantId?: string
  private readonly transport: typeof globalThis.fetch
}

function getValidationErrors(error: unknown) {
  if (error instanceof WorkflowApiError && error.status === 400 && isRecord(error.body)) {
    const body = error.body as ApiErrorBody
    if (body.code !== 'ValidationError') return undefined

    const errors = body.errors
    if (isRecord(errors)) {
      const entries = Object.entries(errors).flatMap(([key, value]) => {
        if (typeof value === 'string') return [[key, value]]
        if (Array.isArray(value)) {
          const messages = value.filter((message): message is string => typeof message === 'string')
          if (messages.length > 0) return [[key, messages.join(' ')]]
        }
        return []
      })
      return entries.length > 0 ? Object.fromEntries(entries) : undefined
    }
  }
  return undefined
}

function parseBody(text: string): unknown {
  if (text === '') return undefined
  try {
    return JSON.parse(text)
  } catch {
    return text
  }
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value)
}

function normalizeTenantId(tenantId: string | null | undefined) {
  const normalized = tenantId?.trim()
  return normalized ? normalized : undefined
}
