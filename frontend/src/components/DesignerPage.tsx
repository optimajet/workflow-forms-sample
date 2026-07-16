import {AppProps, buildSampleDesignerApiUrl, normalizeTenantId, useAppState} from '../types.ts'
import WorkflowDesigner from '@optimajet/workflow-designer-react'

export function DesignerPage({schemeCode, apiUrl, tenantId}: AppProps) {
  const {selectedUser} = useAppState()
  if (selectedUser === null) return <>Select a user first</>

  const normalizedTenantId = normalizeTenantId(tenantId)
  const designerConfig = {
    renderTo: 'wfdesigner',
    apiurl: buildSampleDesignerApiUrl(apiUrl, selectedUser),
    showSaveButton: true,
    name: 'wfe',
    language: 'en',
    ...(normalizedTenantId === undefined ? {} : {tenantId: normalizedTenantId}),
  }
  return <WorkflowDesigner
    schemeCode={schemeCode}
    designerConfig={designerConfig}
    readOnly={false}
  />
}
