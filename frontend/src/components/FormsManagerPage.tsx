import {FormsManager} from '@optimajet/workflow-forms-manager'
import {AppProps, buildSampleDesignerApiUrl, normalizeTenantId, useAppState, useShowError} from '../types.ts'

export function FormsManagerPage({apiUrl, licenseKey, tenantId}: AppProps) {
  const {selectedUser} = useAppState()
  const showError = useShowError()
  if (selectedUser === null) return <>Select a user first</>

  return <FormsManager apiUrl={buildSampleDesignerApiUrl(apiUrl, selectedUser)} licenseKey={licenseKey} onError={showError}
                       tenantId={normalizeTenantId(tenantId)}>
  </FormsManager>
}
