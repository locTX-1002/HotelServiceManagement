import { useEffect, useRef, useState } from 'react'

// Reveal khi cuộn tới bằng IntersectionObserver - không nghe window scroll (tốn CPU/reflow)
export function useInView(options = { threshold: 0.15 }) {
  const ref = useRef(null)
  const [inView, setInView] = useState(false)

  useEffect(() => {
    const el = ref.current
    if (!el) return
    const observer = new IntersectionObserver(([entry]) => {
      if (entry.isIntersecting) {
        setInView(true)
        observer.disconnect() // chỉ chạy 1 lần, tránh nhấp nháy khi cuộn qua lại
      }
    }, options)
    observer.observe(el)
    return () => observer.disconnect()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  return [ref, inView]
}

// Wrapper tiện dùng: <Reveal delay={100}>...</Reveal>
export function Reveal({ children, delay = 0, className = '' }) {
  const [ref, inView] = useInView()
  return (
    <div ref={ref} className={`reveal ${inView ? 'is-visible' : ''} ${className}`} style={{ transitionDelay: `${delay}ms` }}>
      {children}
    </div>
  )
}
