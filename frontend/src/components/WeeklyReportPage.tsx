import {Form, FormKey, FormsViewer} from '@optimajet/workflow-forms-viewer'
import {useCallback} from 'react'
import {useParams} from 'react-router-dom'
import {AppProps, useAppState, useShowError, useShowSuccess} from '../types.ts'

export function WeeklyReportPage({apiUrl}: AppProps) {
  const {id} = useParams()
  const {selectedUser} = useAppState()
  const showError = useShowError()
  const showSuccess = useShowSuccess()

  const canDisplay = useCallback(() => {
    return !!id && !!selectedUser
  }, [id, selectedUser])

  const getForm = useCallback(async (formKey: FormKey) => {
    const formVersion = typeof formKey.formVersion === 'number' ? `&formVersion=${formKey.formVersion}` : ''
    const response = await fetch(`${apiUrl}/reports/forms/form?formName=${formKey.formName}${formVersion}`)
    if (response.ok) return ((await response.json()) as Form).formCode
    const errorText = await response.text()
    throw new Error(errorText || `HTTP error ${response.status}`)
  }, [apiUrl])

  const getForms = useCallback(async () => {
    if (!canDisplay()) return []
    const response = await fetch(`${apiUrl}/reports/forms/get?processId=${id}&user=${selectedUser}`)
    if (response.ok) return await response.json() as Form[]
    const errorText = await response.text()
    throw new Error(errorText || `HTTP error ${response.status}`)
  }, [apiUrl, canDisplay, id, selectedUser])

  const saveForm = useCallback(async (processId: string, formKey: FormKey, data: Record<string, unknown>) => {
      const postData = {
        formKey: formKey,
        processId: processId,
        user: selectedUser,
        data: data
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

  const executeForm = useCallback(async (processId: string, formKey: FormKey, commandName: string, data: Record<string, unknown>) => {
    const postData = {
      formKey: formKey,
      processId: processId,
      commandName: commandName,
      user: selectedUser,
      data: data
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
      />}
  </>
}
