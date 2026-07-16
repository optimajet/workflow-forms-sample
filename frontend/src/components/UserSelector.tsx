import {useEffect, useMemo, useState} from 'react'
import {SelectPicker} from 'rsuite'
import {appendTenantId, AppProps, useAppState, Users, useShowError} from '../types.ts'

export function UserSelector({apiUrl, tenantId}: AppProps) {
  const {selectedUser, setSelectedUser} = useAppState()
  const [users, setUsers] = useState<Users>([])
  const showError = useShowError()

  useEffect(() => {
    const queryParams = new URLSearchParams()
    appendTenantId(queryParams, tenantId)
    const queryString = queryParams.toString()
    const url = queryString === ''
      ? `${apiUrl}/users/all`
      : `${apiUrl}/users/all?${queryString}`

    fetch(url)
      .then(response => response.json())
      .then(data => {
        const users = data as Users
        setUsers(users)
        setSelectedUser(users.length > 0 ? users[0].name : null)
      })
      .catch(error => showError('Failed to load users: ' + error.message))
  }, [setSelectedUser, showError, apiUrl, tenantId])

  const data = useMemo(() => {
    return users.map(u => {
      const roles = u.roles.join(', ')
      return ({label: `${u.name} (${roles}) (${u.division})`, value: u.name})
    })
  }, [users])

  return <SelectPicker data={data} style={{width: 256}} value={selectedUser} onChange={setSelectedUser}/>
}
