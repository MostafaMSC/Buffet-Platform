import axios from 'axios'

export const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? '/api',
})

api.interceptors.request.use((config) => {
  const token = localStorage.getItem('buffet_token')
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

api.interceptors.response.use(
  (res) => res,
  (error) => {
    if (error.response?.status === 401) {
      localStorage.removeItem('buffet_token')
      localStorage.removeItem('buffet_role')
      localStorage.removeItem('buffet_restaurant_id')
    }
    return Promise.reject(error)
  },
)
