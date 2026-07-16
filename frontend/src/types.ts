import {useCallback} from 'react'
import {toast} from 'sonner'
import {create} from 'zustand'

export interface AppProps {
  apiUrl: string,
  schemeCode: string,
  tenantId?: string | null,
  licenseKey?: string
}

export interface AppState {
  selectedUser: string | null,
  setSelectedUser: (user: string | null) => void,
}

export const useAppState = create<AppState>((set) => ({
  selectedUser: null,
  setSelectedUser: (user: string | null) => set({selectedUser: user}),
}))

export type Role = 'User' | 'Manager' | 'CTO';

export interface User {
  name: string;
  roles: Role[];
  division: string;
  tenantId?: string | null;
}

export type Users = User[];

export function useShowError() {
  return useCallback((error: string | Error) => {
    const message = typeof error === 'string'
      ? error
      : error?.message ?? 'Unknown error'

    console.error(error)

    const shortMessage = message.length > 500
      ? message.slice(0, 500) + '…'
      : message

    toast.error(shortMessage)
  }, [])
}

export function useShowSuccess() {
  return useCallback((message: string) => {
    toast.success(message)
  }, [])
}

export function camelCaseToWords(input: string) {
  const spaced = input.replace(/([\p{Ll}])([\p{Lu}])/gu, '$1 $2')
  return spaced
    .split(' ')
    .map(word => word.charAt(0).toLocaleUpperCase() + word.slice(1))
    .join(' ')
}

export function normalizeTenantId(tenantId: string | null | undefined) {
  return typeof tenantId === 'string' && tenantId.trim() !== ''
    ? tenantId
    : undefined
}

export function appendTenantId(searchParams: URLSearchParams, tenantId: string | null | undefined) {
  const normalizedTenantId = normalizeTenantId(tenantId)
  if (normalizedTenantId !== undefined) {
    searchParams.set('tenantId', normalizedTenantId)
  }
}

export function buildSampleDesignerApiUrl(apiUrl: string, user: string | null) {
  const searchParams = new URLSearchParams()
  if (user !== null) {
    searchParams.set('sampleUser', user)
  }

  const queryString = searchParams.toString()
  return queryString === ''
    ? `${apiUrl}/designer`
    : `${apiUrl}/designer?${queryString}`
}
