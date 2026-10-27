import {FormsViewer} from '@optimajet/workflow-forms-viewer'
import {useMemo} from 'react'
import {useParams} from 'react-router-dom'
import {WorkflowApiClient} from '../api.ts'
import {AppProps, normalizeTenantId, useAppState, useShowError, useShowSuccess} from '../types.ts'

export function WeeklyReportPage({apiUrl, tenantId}: AppProps) {
  const {id} = useParams()
  const {selectedUser} = useAppState()
  const showError = useShowError()
  const showSuccess = useShowSuccess()
  const client = useMemo(
    () => new WorkflowApiClient({baseUrl: apiUrl, tenantId: normalizeTenantId(tenantId)}),
    [apiUrl, tenantId]
  )

  if (id === undefined || selectedUser === null) {
    return <>Select a user and a report first.</>
  }

  return <FormsViewer
    getForm={formKey => client.getFormCode(formKey)}
    getForms={() => client.getExecutableForms(id, selectedUser)}
    saveForm={(processId, formKey, formData) => client.saveForm(
      processId,
      formKey,
      formData,
      selectedUser
    )}
    executeForm={(processId, formKey, commandName, formData) => client.executeForm(
      processId,
      formKey,
      commandName,
      formData,
      selectedUser
    )}
    onError={showError}
    onSuccess={showSuccess}
    tenantId={normalizeTenantId(tenantId)}
  />
}
