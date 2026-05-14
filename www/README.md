# sub-to-me — Web Push Subscription Page

A single-page React app that asks the user to subscribe to push notifications via Firebase Cloud Messaging (FCM) and registers their device token with a backend API.

## Prerequisites

- Node.js 18+
- A [Firebase](https://console.firebase.google.com/) project with **Cloud Messaging** enabled
- A backend API that accepts `POST /register-device` with body `{ "token": "<fcm-token>" }`

## Setup

### 1. Install dependencies

```bash
npm install
```

### 2. Configure environment variables

Create a `.env` file in the project root (or copy from `.env.example`):

```env
VITE_BASE_API=https://your-api-base-url.com
VITE_VAPID_KEY=your-web-push-vapid-key
```

| Variable | Description |
|---|---|
| `VITE_BASE_API` | Base URL of your backend API (no trailing slash) |
| `VITE_VAPID_KEY` | Web Push VAPID key from Firebase Console |

**Getting the VAPID key:**
1. Go to Firebase Console → your project → **Project Settings** → **Cloud Messaging** tab
2. Scroll to **Web Push certificates**
3. Click **Generate key pair** if none exists
4. Copy the **Key pair** value

### 3. Run the dev server

```bash
npm run dev
```

### 4. Build for production

```bash
npm run build
```

## How it works

1. User clicks **"Yes, subscribe me"**
2. The browser requests notification permission
3. The FCM service worker (`/firebase-messaging-sw.js`) is registered
4. A device token is obtained using the VAPID key
5. The token is sent to `POST $VITE_BASE_API/register-device`
6. A success message is shown on completion

## React Compiler

The React Compiler is not enabled on this template because of its impact on dev & build performances. To add it, see [this documentation](https://react.dev/learn/react-compiler/installation).

## Expanding the ESLint configuration

If you are developing a production application, we recommend updating the configuration to enable type-aware lint rules:

```js
export default defineConfig([
  globalIgnores(['dist']),
  {
    files: ['**/*.{ts,tsx}'],
    extends: [
      // Other configs...

      // Remove tseslint.configs.recommended and replace with this
      tseslint.configs.recommendedTypeChecked,
      // Alternatively, use this for stricter rules
      tseslint.configs.strictTypeChecked,
      // Optionally, add this for stylistic rules
      tseslint.configs.stylisticTypeChecked,

      // Other configs...
    ],
    languageOptions: {
      parserOptions: {
        project: ['./tsconfig.node.json', './tsconfig.app.json'],
        tsconfigRootDir: import.meta.dirname,
      },
      // other options...
    },
  },
])
```

You can also install [eslint-plugin-react-x](https://github.com/Rel1cx/eslint-react/tree/main/packages/plugins/eslint-plugin-react-x) and [eslint-plugin-react-dom](https://github.com/Rel1cx/eslint-react/tree/main/packages/plugins/eslint-plugin-react-dom) for React-specific lint rules:

```js
// eslint.config.js
import reactX from 'eslint-plugin-react-x'
import reactDom from 'eslint-plugin-react-dom'

export default defineConfig([
  globalIgnores(['dist']),
  {
    files: ['**/*.{ts,tsx}'],
    extends: [
      // Other configs...
      // Enable lint rules for React
      reactX.configs['recommended-typescript'],
      // Enable lint rules for React DOM
      reactDom.configs.recommended,
    ],
    languageOptions: {
      parserOptions: {
        project: ['./tsconfig.node.json', './tsconfig.app.json'],
        tsconfigRootDir: import.meta.dirname,
      },
      // other options...
    },
  },
])
```
