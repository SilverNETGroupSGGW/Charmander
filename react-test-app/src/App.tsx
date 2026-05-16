import { useState } from 'react'
import { requestNotificationToken } from './firebase'
import './App.css'

const BASE_API = import.meta.env.VITE_BASE_API as string

type Status = 'idle' | 'loading' | 'success' | 'error'

async function subscribeDevice(): Promise<void> {
  const token = await requestNotificationToken()
  const res = await fetch(`${BASE_API}/register-device`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ token }),
  })
  if (!res.ok) {
    throw new Error(`Registration failed (${res.status})`)
  }
}

function App() {
  const [status, setStatus] = useState<Status>('idle')
  const [errorMsg, setErrorMsg] = useState('')

  async function handleSubscribe() {
    setStatus('loading')
    setErrorMsg('')
    try {
      await subscribeDevice()
      setStatus('success')
    } catch (err) {
      setErrorMsg(err instanceof Error ? err.message : 'Something went wrong')
      setStatus('error')
    }
  }

  return (
    <>
      {status !== 'success' && (
        <button
          type="button"
          className="subscribe-btn"
          onClick={handleSubscribe}
          disabled={status === 'loading'}
        >
          {status === 'loading' ? 'Subscribing…' : 'Yes, subscribe me'}
        </button>
      )}
      {status === 'success' && (
        <p className="status-message success">
          You're subscribed! You'll receive notifications.
        </p>
      )}
      {status === 'error' && (
        <p className="status-message error">{errorMsg}</p>
      )}
    </>
  )
}

export default App
