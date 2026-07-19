import { useEffect, useRef } from 'react'
import { EASE } from '../utils/ui'

const SCRIPT_SRC = 'https://accounts.google.com/gsi/client'
const CLIENT_ID = import.meta.env.VITE_GOOGLE_CLIENT_ID

// Nut "Đăng nhập bằng Google" dung Google Identity Services (GIS). Nut CHINH THUC cua Google
// (google.accounts.id.renderButton) chi dung duoc font Roboto rieng cua Google, khong tuy bien theo
// font trang duoc (gioi han co chu dinh cua Google, khong phai bug FE) - nen render nut that o dang
// AN (opacity-0, khong phai display:none vi GIS can layout thuc de khoi tao dung), roi dung 1 nut
// TU BIEN dung font/mau cua trang, bam vao thi trigger .click() len nut that qua so do. Day la cach
// lam chuan cho tinh huong nay, khong phai hack rui ro - click dong bo tu trong 1 user-gesture that
// van giu duoc "user activation" nen khong bi trinh duyet chan popup.
export default function GoogleSignInButton({ onCredential }) {
  const hiddenContainerRef = useRef(null)
  const proxyRef = useRef(null)

  useEffect(() => {
    if (!CLIENT_ID) return

    let cancelled = false

    const render = () => {
      if (cancelled || !hiddenContainerRef.current || !window.google?.accounts?.id) return
      window.google.accounts.id.initialize({
        client_id: CLIENT_ID,
        callback: (response) => onCredential(response.credential),
      })
      hiddenContainerRef.current.innerHTML = ''
      window.google.accounts.id.renderButton(hiddenContainerRef.current, {
        theme: 'outline',
        size: 'large',
        width: 320,
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

  const triggerRealButton = () => {
    // Nut that cua Google la 1 <div role="button"> ben trong iframe cross-origin - khong bam thang
    // vao iframe duoc, phai bam vao phan tu co role="button" ma GIS render ra trong container.
    const realButton = hiddenContainerRef.current?.querySelector('div[role="button"]')
    realButton?.click()
  }

  return (
    <>
      <button
        type="button"
        ref={proxyRef}
        onClick={triggerRealButton}
        className={`flex w-full items-center justify-center gap-3 rounded-lg border border-black/15 bg-white px-4 py-3 text-sm font-semibold text-ink-900 ${EASE} hover:bg-cream-50`}
      >
        <svg width="18" height="18" viewBox="0 0 18 18" aria-hidden>
          <path fill="#4285F4" d="M17.64 9.2c0-.64-.06-1.25-.16-1.84H9v3.48h4.84a4.14 4.14 0 01-1.8 2.72v2.26h2.92c1.7-1.57 2.68-3.88 2.68-6.62z" />
          <path fill="#34A853" d="M9 18c2.43 0 4.47-.8 5.96-2.18l-2.92-2.26c-.81.54-1.84.86-3.04.86-2.34 0-4.32-1.58-5.03-3.7H.96v2.33A9 9 0 009 18z" />
          <path fill="#FBBC05" d="M3.97 10.72A5.4 5.4 0 013.68 9c0-.6.1-1.18.29-1.72V4.95H.96A9 9 0 000 9c0 1.45.35 2.83.96 4.05l3.01-2.33z" />
          <path fill="#EA4335" d="M9 3.58c1.32 0 2.51.46 3.44 1.35l2.58-2.58C13.46.89 11.43 0 9 0A9 9 0 00.96 4.95l3.01 2.33C4.68 5.16 6.66 3.58 9 3.58z" />
        </svg>
        Đăng nhập bằng Google
      </button>
      {/* Nut that cua Google - dua ra ngoai man hinh (khong phai display:none/w-0 vi GIS can kich
          thuoc thuc de tinh layout ben trong dung) nen van khoi tao va bam duoc, chi la khong ai thay. */}
      <div
        ref={hiddenContainerRef}
        className="pointer-events-none absolute -left-[9999px] top-0 h-10 w-[320px] opacity-0"
        aria-hidden
      />
    </>
  )
}
