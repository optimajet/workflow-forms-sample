import {FormsManager} from '@optimajet/workflow-forms-manager'
import {useMemo} from 'react'
import {WorkflowApiClient} from '../api.ts'
import {AppProps, normalizeTenantId, useAppState, useShowError} from '../types.ts'

export function FormsManagerPage({apiUrl, licenseKey, tenantId}: AppProps) {
  const {selectedUser} = useAppState()
  const showError = useShowError()
  const client = useMemo(
    () => new WorkflowApiClient({baseUrl: apiUrl, tenantId: normalizeTenantId(tenantId)}),
    [apiUrl, tenantId]
  )
  if (selectedUser === null) return <>Select a user first</>

  return <FormsManager apiUrl={client.designerUrl} licenseKey={licenseKey} onError={showError}
                       customFetch={client.customFetch}>
  </FormsManager>
}
