import {useCallback, useState} from 'react'
import {useNavigate} from 'react-router-dom'
import {Button} from 'rsuite'
import {WorkflowApiClient} from '../api.ts'
import {AppProps, useAppState, useShowError} from '../types.ts'

export function CreateReportButton({apiUrl, schemeCode, tenantId}: AppProps) {
  const {selectedUser} = useAppState()
  const navigate = useNavigate()
  const showError = useShowError()
  const [loading, setLoading] = useState(false)

  const handleClick = useCallback(async () => {
    setLoading(true)
    try {
      if (selectedUser === null) throw new Error('User is not selected')
      const processId = crypto.randomUUID()
      const client = new WorkflowApiClient({baseUrl: apiUrl, tenantId: tenantId ?? undefined})
      await client.createInstance(schemeCode, processId, selectedUser)
      navigate(`/weeklyReport/${processId}`)
    } catch (error) {
      showError(error as Error)
    } finally {
      setLoading(false)
    }
  }, [apiUrl, navigate, schemeCode, selectedUser, showError, tenantId])

  return (
    <Button appearance="primary" color="red" loading={loading} onClick={handleClick}>
      Create new report
    </Button>
  )
}
