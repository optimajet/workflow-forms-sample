import {Form, FormKey, FormsViewer, type FormsViewerRequestContext} from '@optimajet/workflow-forms-viewer'
import {useCallback} from 'react'
import {useParams} from 'react-router-dom'
import {appendTenantId, AppProps, normalizeTenantId, useAppState, useShowError, useShowSuccess} from '../types.ts'

export function WeeklyReportPage({apiUrl, tenantId}: AppProps) {
  const {id} = useParams()
  const {selectedUser} = useAppState()
  const showError = useShowError()
  const showSuccess = useShowSuccess()

  const canDisplay = useCallback(() => {
    return !!id && !!selectedUser
  }, [id, selectedUser])

  const getForm = useCallback(async (formKey: FormKey, context?: FormsViewerRequestContext) => {
    if (selectedUser === null) throw new Error('User is not selected')

    const queryParams = new URLSearchParams({
      formName: formKey.formName,
      user: selectedUser,
    })
    if (typeof formKey.formVersion === 'number') {
      queryParams.set('formVersion', formKey.formVersion.toString())
    }
    appendTenantId(queryParams, context?.tenantId)

    const response = await fetch(`${apiUrl}/reports/forms/form?${queryParams}`)
    if (response.ok) return ((await response.json()) as Form).formCode
    const errorText = await response.text()
    throw new Error(errorText || `HTTP error ${response.status}`)
  }, [apiUrl, selectedUser])

  const getForms = useCallback(async (context?: FormsViewerRequestContext) => {
    if (!canDisplay() || id === undefined || selectedUser === null) return []
    const queryParams = new URLSearchParams({
      processId: id,
      user: selectedUser,
    })
    appendTenantId(queryParams, context?.tenantId)

    const response = await fetch(`${apiUrl}/reports/forms/get?${queryParams}`)
    if (response.ok) return await response.json() as Form[]
    const errorText = await response.text()
    throw new Error(errorText || `HTTP error ${response.status}`)
  }, [apiUrl, canDisplay, id, selectedUser])

  const saveForm = useCallback(async (
      processId: string,
      formKey: FormKey,
      data: Record<string, unknown>,
      context?: FormsViewerRequestContext
    ) => {
      const postData = {
        formKey: formKey,
        processId: processId,
        user: selectedUser,
        data: data,
        tenantId: normalizeTenantId(context?.tenantId),
      }
      const response = await fetch(`${apiUrl}/reports/forms/save`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify(postData)
      })
      if (response.ok) return {formData: await response.json()}
      if (response.status === 400) return {formErrors: await response.json()}
      const errorText = await response.text()
      throw new Error(errorText || `HTTP error ${response.status}`)
    }, [apiUrl, selectedUser]
  )

  const executeForm = useCallback(async (
    processId: string,
    formKey: FormKey,
    commandName: string,
    data: Record<string, unknown>,
    context?: FormsViewerRequestContext
  ) => {
    const postData = {
      formKey: formKey,
      processId: processId,
      commandName: commandName,
      user: selectedUser,
      data: data,
      tenantId: normalizeTenantId(context?.tenantId),
    }
    const response = await fetch(`${apiUrl}/reports/forms/execute`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(postData)
    })
    if (response.ok) return {wasExecuted: (await response.json()).wasExecuted}
    if (response.status === 400) return {formErrors: await response.json()}
    const errorText = await response.text()
    throw new Error(errorText || `HTTP error ${response.status}`)
  }, [apiUrl, selectedUser])

  return <>
    {canDisplay() &&
      <FormsViewer getForm={getForm} getForms={getForms} onError={showError} onSuccess={showSuccess} saveForm={saveForm}
                   executeForm={executeForm}
                   tenantId={normalizeTenantId(tenantId)}
      />}
  </>
}
