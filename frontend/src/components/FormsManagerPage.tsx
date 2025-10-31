import {FormsManager} from '@optimajet/workflow-forms-manager'
import {AppProps, useShowError} from '../types.ts'

export function FormsManagerPage({apiUrl, licenseKey}: AppProps) {
  const showError = useShowError()
  return <FormsManager apiUrl={`${apiUrl}/designer`} licenseKey={licenseKey} onError={showError}>
  </FormsManager>
}
