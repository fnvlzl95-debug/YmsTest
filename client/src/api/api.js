import axios from 'axios'

const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5000/api',
  headers: {
    'Content-Type': 'application/json',
  },
})

const toParamValue = (value) => {
  if (Array.isArray(value)) {
    return value.join(',')
  }

  return value
}

const buildParams = (filters = {}) => {
  const params = {}

  Object.entries(filters).forEach(([key, value]) => {
    if (value === undefined || value === null) {
      return
    }

    if (Array.isArray(value) && value.length === 0) {
      return
    }

    if (typeof value === 'string' && value.trim() === '') {
      return
    }

    params[key] = toParamValue(value)
  })

  return params
}

export const getEquipments = async (filters = {}) => {
  const response = await apiClient.get('/equipments', { params: buildParams(filters) })
  return response.data
}

export const getLines = async () => {
  const response = await apiClient.get('/equipments/lines')
  return response.data
}

export const getClasses = async (lineIds = []) => {
  const response = await apiClient.get('/equipments/classes', {
    params: buildParams({ lineId: lineIds }),
  })

  return response.data
}

export const getEmployees = async (filters = {}) => {
  const response = await apiClient.get('/employees', { params: buildParams(filters) })
  return response.data
}

export const getAdminCandidates = async (filters = {}) => {
  const response = await apiClient.get('/employees/admins', { params: buildParams(filters) })
  return response.data
}

export const getReservations = async (filters = {}) => {
  const response = await apiClient.get('/reservations', { params: buildParams(filters) })
  return response.data
}

export const getReservationById = async (id) => {
  const response = await apiClient.get(`/reservations/${id}`)
  return response.data
}

export const createReservation = async (payload) => {
  const response = await apiClient.post('/reservations', payload)
  return response.data
}

export const updateReservation = async (id, payload) => {
  const response = await apiClient.put(`/reservations/${id}`, payload)
  return response.data
}

export const deleteReservation = async (id) => {
  await apiClient.delete(`/reservations/${id}`)
}

export const checkReceptionAuth = async (payload) => {
  const response = await apiClient.post('/auth/check-reception', payload)
  return response.data
}

export const getNotificationReceivers = async (issueNo, approvalSeq = '0') => {
  const response = await apiClient.get('/notifications/receivers', {
    params: buildParams({ issueNo, approvalSeq }),
  })

  return response.data
}

export const setRequestNotification = async (payload) => {
  const response = await apiClient.post('/notifications/request', payload)
  return response.data
}

export const saveSearchHistory = async (payload) => {
  await apiClient.post('/ui-audit/search-history', payload)
}

export default apiClient
