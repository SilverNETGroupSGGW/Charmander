import { initializeApp } from 'firebase/app'
import { getMessaging, getToken } from 'firebase/messaging'

const firebaseConfig = {
  apiKey: 'AIzaSyDHWIpuUVGSQc-Wivt-NzN27qZ8PjcGfvs',
  authDomain: 'sub-to-me-137ad.firebaseapp.com',
  projectId: 'sub-to-me-137ad',
  storageBucket: 'sub-to-me-137ad.firebasestorage.app',
  messagingSenderId: '200862685502',
  appId: '1:200862685502:web:3dfa4a5635701b328f346f',
  measurementId: 'G-JE5T5GXEXY',
}

const app = initializeApp(firebaseConfig)
const messaging = getMessaging(app)

export async function requestNotificationToken(): Promise<string> {
  const permission = await Notification.requestPermission()
  if (permission !== 'granted') {
    throw new Error('Notification permission denied')
  }
  const swReg = await navigator.serviceWorker.register('/firebase-messaging-sw.js')
  const token = await getToken(messaging, {
    vapidKey: import.meta.env.VITE_VAPID_KEY as string,
    serviceWorkerRegistration: swReg,
  })
  if (!token) {
    throw new Error('Failed to get FCM token')
  }
  return token
}
