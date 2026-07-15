import ExcelJS from 'exceljs'
import { jsPDF } from 'jspdf'
import autoTable from 'jspdf-autotable'
import { formatVnd } from './roomStatus'

// Tải file xuống máy - dùng chung cho cả Excel lẫn PDF
const downloadBlob = (blob, filename) => {
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = filename
  document.body.appendChild(a)
  a.click()
  a.remove()
  URL.revokeObjectURL(url)
}

// jsPDF font mặc định không có dấu tiếng Việt -> bỏ dấu riêng cho bản PDF (Excel là UTF-8 gốc, không cần)
const stripDiacritics = (s) => String(s ?? '').normalize('NFD').replace(/[̀-ͯ]/g, '').replace(/đ/g, 'd').replace(/Đ/g, 'D')

export async function exportReportToExcel({ from, to, revenue, occupancy }) {
  const wb = new ExcelJS.Workbook()

  const overview = wb.addWorksheet('Tổng quan')
  overview.addRows([
    ['Từ ngày', from],
    ['Đến ngày', to],
    ['Tiền phòng', revenue?.roomRevenue ?? 0],
    ['Tiền dịch vụ', revenue?.serviceRevenue ?? 0],
    ['Đã thu', revenue?.paymentRevenue ?? 0],
    ['Tổng doanh thu', revenue?.totalRevenue ?? 0],
    ['Công suất hiện tại (%)', occupancy?.totalRooms ? Math.round(((occupancy.occupiedRooms + (occupancy.reservedRooms ?? 0)) / occupancy.totalRooms) * 100) : 0],
    ['Tổng phòng', occupancy?.totalRooms ?? 0],
    ['Đang ở', occupancy?.occupiedRooms ?? 0],
    ['Đã đặt', occupancy?.reservedRooms ?? 0],
  ])

  const byDaySheet = wb.addWorksheet('Doanh thu theo ngày')
  byDaySheet.addRow(['Ngày', 'Tiền phòng', 'Tiền dịch vụ', 'Tổng'])
  ;(revenue?.byDay ?? []).forEach((d) => {
    byDaySheet.addRow([String(d.date).slice(0, 10), d.roomRevenue, d.serviceRevenue, d.totalRevenue])
  })

  const floorSheet = wb.addWorksheet('Công suất theo tầng')
  floorSheet.addRow(['Tầng', 'Tổng phòng', 'Đang ở', 'Đã đặt', 'Công suất (%)'])
  ;(occupancy?.byFloor ?? []).forEach((f) => {
    const rate = f.totalRooms ? Math.round(((f.occupiedRooms + (f.reservedRooms ?? 0)) / f.totalRooms) * 100) : 0
    floorSheet.addRow([f.floor, f.totalRooms, f.occupiedRooms, f.reservedRooms ?? 0, rate])
  })

  const buffer = await wb.xlsx.writeBuffer()
  downloadBlob(new Blob([buffer], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' }), `bao-cao-doanh-thu_${from}_${to}.xlsx`)
}

export function exportReportToPdf({ from, to, revenue, occupancy }) {
  const doc = new jsPDF()

  doc.setFontSize(14)
  doc.text(stripDiacritics(`Bao cao doanh thu ${from} - ${to}`), 14, 16)
  doc.setFontSize(10)
  doc.text(stripDiacritics(`Tien phong: ${formatVnd(revenue?.roomRevenue ?? 0)}`), 14, 24)
  doc.text(stripDiacritics(`Tien dich vu: ${formatVnd(revenue?.serviceRevenue ?? 0)}`), 14, 30)
  doc.text(stripDiacritics(`Tong doanh thu: ${formatVnd(revenue?.totalRevenue ?? 0)}`), 14, 36)

  autoTable(doc, {
    startY: 44,
    head: [['Ngay', 'Tien phong', 'Tien dich vu', 'Tong'].map(stripDiacritics)],
    body: (revenue?.byDay ?? []).map((d) => [
      String(d.date).slice(0, 10),
      formatVnd(d.roomRevenue),
      formatVnd(d.serviceRevenue),
      formatVnd(d.totalRevenue),
    ]),
  })

  const nextY = (doc.lastAutoTable?.finalY ?? 44) + 10
  doc.text(stripDiacritics('Cong suat theo tang'), 14, nextY)
  autoTable(doc, {
    startY: nextY + 4,
    head: [['Tang', 'Tong phong', 'Dang o', 'Da dat', 'Cong suat'].map(stripDiacritics)],
    body: (occupancy?.byFloor ?? []).map((f) => {
      const rate = f.totalRooms ? Math.round(((f.occupiedRooms + (f.reservedRooms ?? 0)) / f.totalRooms) * 100) : 0
      return [f.floor, f.totalRooms, f.occupiedRooms, f.reservedRooms ?? 0, `${rate}%`]
    }),
  })

  doc.save(`bao-cao-doanh-thu_${from}_${to}.pdf`)
}
