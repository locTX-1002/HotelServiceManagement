// Dai anh tieu de dung chung cho cac trang noi bo cua nhan vien - dua khong khi khach san
// (anh + lop phu toi + serif) vao thay header chu tren nen kem, dong bo voi Tong quan va cong
// khach theo cac template da chot. Nut hanh dong dat trong hero phai dung mau sang (bg-brand-500
// hoac ring-white/40) vi nen phia sau la anh toi.
export default function PageHero({ image, kicker, title, subtitle, children }) {
  return (
    <div className="card-rise relative overflow-hidden rounded-3xl">
      <img src={image} alt="" className="h-44 w-full object-cover sm:h-48" />
      <div className="absolute inset-0 bg-gradient-to-t from-ink-900/85 via-ink-900/40 to-ink-900/10" />
      <div className="absolute inset-x-0 bottom-0 flex flex-wrap items-end justify-between gap-3 p-5 sm:px-7 sm:pb-6">
        <div>
          <p className="font-display text-[14px] italic capitalize text-cream-100/90">{kicker}</p>
          <h1 className="mt-0.5 font-display text-3xl font-semibold tracking-tight text-white sm:text-4xl">{title}</h1>
          {subtitle && <p className="mt-1 max-w-xl text-[13px] text-cream-100/85">{subtitle}</p>}
        </div>
        {children && <div className="flex flex-wrap items-center gap-2.5">{children}</div>}
      </div>
    </div>
  )
}
