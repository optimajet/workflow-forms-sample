import {describe, expect, it, vi} from 'vitest'
import {WorkflowApiClient} from './api.ts'

describe('WorkflowApiClient', () => {
  it('unwraps Forms API envelopes and adds the tenant header', async () => {
    const transport = vi.fn<typeof fetch>().mockResolvedValue(jsonResponse({
      forms: [{
        processId: 'process-id',
        formKey: {formName: 'Created', formVersion: 1},
        formCode: '{}',
        formData: {whatWasDone: 'API'},
        commands: [],
        allowSave: true
      }]
    }))
    const client = new WorkflowApiClient({baseUrl: 'http://localhost/', tenantId: 'tenant-1', fetch: transport})

    const forms = await client.getExecutableForms('process-id', 'Paula')

    expect(forms[0].formData).toEqual({whatWasDone: 'API'})
    const request = transport.mock.calls[0]
    expect(request[0]).toBe('http://localhost/workflow-api/forms/get-executable-forms')
    expect(new Headers(request[1]?.headers).get('Workflow-Api-Tenant-ID')).toBe('tenant-1')
  })

  it('returns only validation errors from the standard Error envelope to FormsViewer', async () => {
    const transport = vi.fn<typeof fetch>().mockResolvedValue(jsonResponse({
      code: 'ValidationError',
      message: 'One or more validation errors occurred.',
      errors: {whatWasDone: ["'What was done' is required."]}
    }, 400))
    const client = new WorkflowApiClient({baseUrl: 'http://localhost', fetch: transport})

    const result = await client.saveForm(
      'process-id',
      {formName: 'Created'},
      {whatWasDone: ''},
      'Paula'
    )

    expect(result).toEqual({formErrors: {whatWasDone: "'What was done' is required."}})
  })

  it('does not treat other 400 errors as form validation errors', async () => {
    const transport = vi.fn<typeof fetch>().mockResolvedValue(jsonResponse({
      code: 'BadRequest',
      message: 'The request is invalid.',
      errors: {whatWasDone: ['This field cannot be used here.']}
    }, 400))
    const client = new WorkflowApiClient({baseUrl: 'http://localhost', fetch: transport})

    await expect(client.saveForm(
      'process-id',
      {formName: 'Created'},
      {whatWasDone: ''},
      'Paula'
    )).rejects.toThrow('The request is invalid.')
  })

  it('adds the tenant header to Forms Manager requests without changing their body', async () => {
    const transport = vi.fn<typeof fetch>().mockResolvedValue(jsonResponse({isError: false}))
    const client = new WorkflowApiClient({baseUrl: 'http://localhost', tenantId: 'tenant-1', fetch: transport})
    const body = 'operation=formsplugin_getformnames'

    await client.customFetch(client.designerUrl, {
      method: 'POST',
      headers: {'Content-Type': 'application/x-www-form-urlencoded'},
      body
    })

    const request = transport.mock.calls[0]
    expect(new Headers(request[1]?.headers).get('Workflow-Api-Tenant-ID')).toBe('tenant-1')
    expect(request[1]?.body).toBe(body)
  })

  it('returns the backend read-only form with a single request', async () => {
    const form = {
      processId: 'process-id',
      formKey: {formName: 'View', formVersion: 2},
      formCode: '{}',
      formData: {stateName: 'Accepted'},
      commands: [],
      allowSave: false
    }
    const transport = vi.fn<typeof fetch>().mockResolvedValue(jsonResponse({forms: [form]}))
    const client = new WorkflowApiClient({baseUrl: 'http://localhost', fetch: transport})

    expect(await client.getExecutableForms('process-id', 'Paula')).toEqual([form])
    expect(transport).toHaveBeenCalledTimes(1)
    expect(JSON.parse(transport.mock.calls[0][1]?.body as string)).toEqual({
      processId: 'process-id', identityId: 'Paula', conditionCheck: true
    })
  })

  it('returns an empty list without making another request', async () => {
    const transport = vi.fn<typeof fetch>().mockResolvedValue(jsonResponse({forms: []}))
    const client = new WorkflowApiClient({baseUrl: 'http://localhost', fetch: transport})

    expect(await client.getExecutableForms('process-id', 'Paula')).toEqual([])
    expect(transport).toHaveBeenCalledTimes(1)
  })

  it.each([400, 401, 403, 404, 500])('preserves HTTP %s without requesting a fallback', async status => {
    const transport = vi.fn<typeof fetch>().mockResolvedValue(jsonResponse({message: 'Request failed'}, status))
    const client = new WorkflowApiClient({baseUrl: 'http://localhost', fetch: transport})

    await expect(client.getExecutableForms('process-id', 'Paula')).rejects.toThrow('Request failed')
    expect(transport).toHaveBeenCalledTimes(1)
  })

  it('uses the standard message field for unsuccessful responses', async () => {
    const transport = vi.fn<typeof fetch>().mockResolvedValue(jsonResponse({message: 'Process not found'}, 500))
    const client = new WorkflowApiClient({baseUrl: 'http://localhost', fetch: transport})

    await expect(client.getReportsCount('Paula')).rejects.toThrow('Process not found')
  })
})

function jsonResponse(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), {
    status,
    headers: {'Content-Type': 'application/json'}
  })
}
