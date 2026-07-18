import { useEffect, useRef } from 'react'

const SCRIPT_SRC = 'https://accounts.google.com/gsi/client'
const CLIENT_ID = import.meta.env.VITE_GOOGLE_CLIENT_ID

// Nut "Đăng nhập bằng Google" dung Google Identity Services (GIS) - tu load script CDN 1 lan (dung
// chung neu nhieu instance cung mount), khong can them thu vien npm nao. An het neu chua cau hinh
// Client ID (VITE_GOOGLE_CLIENT_ID rong) thay vi hien nut hong.
export default function GoogleSignInButton({ onCredential }) {
  const containerRef = useRef(null)

  useEffect(() => {
    if (!CLIENT_ID) return

    let cancelled = false

    const render = () => {
      if (cancelled || !containerRef.current || !window.google?.accounts?.id) return
      window.google.accounts.id.initialize({
        client_id: CLIENT_ID,
        callback: (response) => onCredential(response.credential),
      })
      containerRef.current.innerHTML = ''
      window.google.accounts.id.renderButton(containerRef.current, {
        theme: 'outline',
        size: 'large',
        width: 320,
        text: 'signin_with',
      })
    }

    if (window.google?.accounts?.id) {
      render()
      return
    }

    let script = document.querySelector(`script[src="${SCRIPT_SRC}"]`)
    if (!script) {
      script = document.createElement('script')
      script.src = SCRIPT_SRC
      script.async = true
      script.defer = true
      document.head.appendChild(script)
    }
    script.addEventListener('load', render)
    return () => {
      cancelled = true
      script.removeEventListener('load', render)
    }
  }, [onCredential])

  if (!CLIENT_ID) return null

  return <div ref={containerRef} className="flex justify-center" />
}
