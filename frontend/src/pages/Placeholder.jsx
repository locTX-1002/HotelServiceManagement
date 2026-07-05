import { useEffect, useState } from 'react'
import client from '../api/client'

// Trang giữ chỗ Day 0 - mỗi màn hình sẽ được thay bằng UI thật theo Task Sheet
export default function Placeholder({ title, owner, day }) {
  const [health, setHealth] = useState(null)

  useEffect(() => {
    client.get('/health').then((res) => setHealth(res.data)).catch(() => setHealth(null))
  }, [])

  return (
    <div className="rounded-lg border border-dashed border-gray-300 bg-white p-8 text-center">
      <h2 className="text-lg font-semibold text-gray-800">{title}</h2>
      <p className="mt-2 text-sm text-gray-500">
        Trang này sẽ được xây trong task của <b>{owner}</b> ({day}).
      </p>
      <p className="mt-3 text-xs">
        Backend:{' '}
        {health ? (
          <span className="text-green-600">✔ kết nối OK ({health.status})</span>
        ) : (
          <span className="text-red-500">✖ chưa kết nối được /health</span>
        )}
      </p>
    </div>
  )
}
