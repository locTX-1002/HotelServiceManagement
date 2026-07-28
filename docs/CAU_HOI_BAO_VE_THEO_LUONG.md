# Câu hỏi bảo vệ đồ án — theo từng luồng nghiệp vụ

> Tài liệu ôn tập, bám sát nội dung [`HUONG_DAN_LUONG_NGHIEP_VU.md`](HUONG_DAN_LUONG_NGHIEP_VU.md).
> Mỗi luồng có 2-3 câu hỏi kiểu giáo viên hay hỏi khi bảo vệ: 1 câu kiểm tra hiểu luồng,
> 1 câu tình huống biên/dễ nhầm (khai thác đúng phần "Lưu ý" trong tài liệu gốc), và một
> số luồng có thêm câu phản biện thiết kế ("vì sao làm vậy mà không làm khác"). Câu trả
> lời viết ngắn, đủ ý để trả lời miệng — không học thuộc lòng, hiểu ý rồi diễn đạt lại.

---

## Mục 2 — Đăng nhập & phân quyền

### Luồng 1 — Đăng nhập nhân viên

**Q1.** Tài khoản đúng mật khẩu nhưng vẫn không đăng nhập được — liệt kê hết các lý do có thể.
**Đáp:** `User.IsActive = false` (tài khoản bị khoá), hoặc `Role.IsActive = false` (vai trò của tài khoản đó bị vô hiệu hoá) — thiếu 1 trong 2 điều kiện là chặn, dù mật khẩu đúng.

**Q2.** Vì sao khi sai mật khẩu và khi tài khoản bị khoá lại hiện chung một thông báo lỗi?
**Đáp:** Chủ đích bảo mật — không tiết lộ lý do cụ thể để người ngoài dò được tài khoản nào tồn tại/đang bị khoá. Hệ quả phụ: nhân viên bị khoá sẽ không tự biết mình bị khoá, chỉ nghĩ mình gõ sai mật khẩu.

**Q3.** Hệ thống có tự khoá tài khoản sau nhiều lần đăng nhập sai không?
**Đáp:** Không. Không có cơ chế đếm số lần sai / tự khoá — đây là một hạn chế đã biết, không phải bug.

### Luồng 2 — Đăng nhập khách hàng

**Q1.** Khách đăng nhập bằng gì, và hệ thống phân biệt "đây là khách chứ không phải nhân viên" ở bước nào?
**Đáp:** Số điện thoại + mật khẩu. Phân biệt ngay từ ô nhập chung của `LoginWindow`: chuỗi toàn chữ số → thử là khách; có `@` → thử là nhân viên.

### Luồng 3 — Khách tự đăng ký

**Q1.** Một số điện thoại đã có hồ sơ khách (do lễ tân từng tạo khi khách đặt tại quầy) nhưng chưa có tài khoản online — khách đó có tự đăng ký được không?
**Đáp:** Không. Hệ thống từ chối tự đăng ký trong trường hợp này, bắt buộc phải nhờ lễ tân xác minh và "Cấp tài khoản" ở module Khách hàng — tránh người lạ tự nhận vơ hồ sơ khách đã có sẵn trong hệ thống.

**Q2.** Đăng ký xong khách có phải đăng nhập lại không? Có xác minh OTP không?
**Đáp:** Không phải đăng nhập lại — tự động đăng nhập luôn sau khi đăng ký thành công. Không có xác minh OTP qua điện thoại/email — đây là điểm yếu đã biết trước, không phải thiếu sót phát hiện lúc bảo vệ.

### Luồng 4 — Đăng xuất

**Q1.** Admin vừa đổi ma trận quyền của vai trò Lễ tân trong lúc có một lễ tân khác đang đăng nhập sẵn — người đó có bị ảnh hưởng ngay không?
**Đáp:** Không. Quyền chỉ nạp lại lúc đăng nhập; phiên đang mở giữ nguyên quyền cũ cho tới khi người dùng tự đăng xuất và đăng nhập lại.

---

## Mục 3 — Trang chủ

### Luồng 5 — Trang chủ theo vai trò

**Q1.** Một tài khoản vừa có quyền tạo đặt phòng (`reservation.create`) vừa có quyền quản lý tài khoản (`user.manage`) thì Trang chủ hiện khu nào?
**Đáp:** Khu "vận hành quầy" (thanh tra cứu phòng trống + nút đặt nhanh) được ưu tiên hiện, khu quản trị hệ thống bị ẩn — 3 kiểu hiển thị (vận hành quầy / quản trị hệ thống / Manager) loại trừ lẫn nhau theo đúng một thứ tự ưu tiên cố định.

**Q2.** Vì sao nút "Đặt phòng ngay" trên Trang chủ với vai trò Quản lý lại bị khoá dù nhìn thấy số liệu phòng trống?
**Đáp:** Số liệu là thông tin công khai cho mọi vai trò xem, nhưng thao tác đặt hộ khách cần quyền vận hành quầy lễ tân thật sự — UI phân biệt rõ "xem được" và "làm được".

---

## Mục 4 — Sơ đồ phòng & quản lý phòng

### Luồng 6 — Xem sơ đồ / danh sách phòng

**Q1.** Một phòng đã "Ngừng dùng" — tab Sơ đồ và tab Danh sách hiển thị khác nhau thế nào? Đây có phải lỗi không?
**Đáp:** Không phải lỗi. Tab Sơ đồ ẩn hẳn phòng ngừng dùng; tab Danh sách vẫn hiện nhưng làm mờ + gắn nhãn "Ngừng dùng" — hai cách hiển thị có chủ đích khác nhau cho hai mục đích khác nhau (Sơ đồ để vận hành nhanh, Danh sách để quản lý đầy đủ).

**Q2.** Panel "lịch sử đặt phòng gần đây" ở tab Danh sách — ai xem được, và vì sao lại giới hạn vậy?
**Đáp:** Chỉ tải được nếu người xem có thêm quyền xem đặt phòng (`reservation.view`). Đây là kiểm soát tách nhiệm vụ: Nhân viên dịch vụ tuy được xem module Phòng nhưng không được biết ai đang đặt phòng nào (thông tin khách hàng), nên phần này phải chặn riêng dù cùng nằm trên 1 màn hình.

### Luồng 7 — CRUD phòng và loại phòng

**Q1.** Xoá một phòng đã từng có khách ở (nhưng hiện đang trống) thì hệ thống làm gì?
**Đáp:** Không xoá cứng — chuyển sang "Ngừng dùng" để giữ nguyên số liệu báo cáo/lịch sử. Chỉ xoá cứng thật sự nếu phòng đó **chưa từng** được đặt lần nào.

**Q2.** Đang có khách ở phòng 101, quản lý có đổi giá loại phòng Standard được không?
**Đáp:** Không — chặn đổi đơn giá loại phòng khi đang có đặt phòng/lượt ở còn mở thuộc loại đó, để tránh thay đổi giá đã thoả thuận với khách giữa chừng.

### Luồng 8 — Bảo trì phòng

**Q1.** Lễ tân muốn đưa phòng 203 vào bảo trì — quy trình khác gì so với Quản lý làm việc đó?
**Đáp:** Quản lý đổi trực tiếp (Trống → Bảo trì ngay). Lễ tân chỉ có quyền **yêu cầu** (`room.maintenance.request`), phải qua hàng đợi phê duyệt, Quản lý duyệt xong mới thật sự đổi trạng thái. Yêu cầu chỉ gửi được khi phòng đang Trống.

**Q2.** Đưa phòng ra khỏi bảo trì (Bảo trì → Trống) thì lễ tân có tự làm được không?
**Đáp:** Không thể làm dưới bất kỳ hình thức nào (kể cả gửi yêu cầu) — chiều ra khỏi bảo trì không có luồng phê duyệt, chỉ Quản lý (hoặc Admin nếu được cấp `room.manage`) thao tác trực tiếp được. Đây là điểm bất đối xứng cố ý cần nêu rõ khi bị hỏi.

---

## Mục 5 — Đặt phòng

### Luồng 9 — Tạo/cập nhật đặt phòng tại quầy

**Q1.** Vì sao đơn tạo tại quầy mặc định là "Đã xác nhận" trong khi đơn khách tự đặt online mặc định "Chờ xác nhận"?
**Đáp:** Vì chính lễ tân là người xác nhận danh tính/thông tin khách ngay tại quầy, khách đến là có thể check-in luôn, không cần thêm một bước xác nhận thừa. Khách tự đặt online thì chưa ai xác minh trực tiếp nên phải qua bước lễ tân duyệt lại.

**Q2.** Sửa một đơn đặt phòng đã "Đã xác nhận" thì đổi được khách của đơn đó không?
**Đáp:** Không — sửa đơn chỉ đổi được phòng/ngày/số khách/ghi chú, không đổi được khách gắn với đơn đã tạo. Muốn đổi khách phải huỷ đơn cũ và tạo đơn mới.

**Q3.** Chặn trùng lịch hoạt động thế nào — 2 đơn ở trạng thái nào thì coi là xung đột?
**Đáp:** Một phòng không được có 2 đơn giao nhau về ngày nếu cả hai đều đang ở trạng thái Chờ xác nhận / Đã xác nhận / Đã check-in. Đơn Đã huỷ hoặc Không đến thì không tính là xung đột.

### Luồng 10 — Huỷ đặt phòng / Không đến

**Q1.** Lễ tân bấm nút "Huỷ" ngay trên màn Đặt phòng — đơn có bị huỷ ngay không?
**Đáp:** Không bao giờ huỷ ngay. Bấm Huỷ chỉ tạo ra một **yêu cầu** gửi vào hàng đợi Phê duyệt, chờ Quản lý duyệt mới thật sự chuyển đơn sang Đã huỷ — áp dụng cho mọi đơn, bất kể ai tạo ra nó.

**Q2.** Trên màn Nhận/Trả phòng cũng có nút "Huỷ" cho đơn quá hạn — nút này có giống nút Huỷ ở màn Đặt phòng không? Đây là câu hỏi bẫy hay gặp khi demo.
**Đáp:** Không giống — nút này gọi huỷ **trực tiếp**, không qua phê duyệt, nên đòi quyền `reservation.cancel.approve` (quyền duyệt) chứ không phải quyền yêu cầu. Với cấu hình mặc định, Lễ tân **nhìn thấy nút này nhưng bấm vào luôn báo lỗi thiếu quyền** vì Lễ tân chỉ có quyền yêu cầu, không có quyền duyệt — đây là hành vi đúng thiết kế, không phải bug UI.

**Q3.** No-show (Không đến) có cần qua phê duyệt như huỷ không? Áp dụng được cho đơn đang ở trạng thái nào?
**Đáp:** Không cần qua phê duyệt, làm trực tiếp. Áp dụng được cho cả đơn Chờ xác nhận lẫn Đã xác nhận, miễn đơn đó **chưa từng có lượt nhận phòng nào**.

### Luồng 11 — No-show sweep tự động

**Q1.** Cơ chế tự động chuyển đơn quá hạn sang Không đến chạy khi nào, và tính mốc theo gì?
**Đáp:** Chạy mỗi lần mở ứng dụng (không phải chạy nền định kỳ theo giờ). Chỉ cần qua nửa đêm của ngày nhận phòng ghi trên đơn là đủ điều kiện — không có ngưỡng "trễ bao nhiêu ngày mới sweep", và chỉ sweep đơn Chờ xác nhận/Đã xác nhận chưa từng check-in.

---

## Mục 6 — Nhận / Trả phòng

### Luồng 12 — Nhận phòng (check-in)

**Q1.** Khách đến nhận phòng sớm hơn ngày ghi trên đơn 2 ngày — hệ thống tính tiền thế nào?
**Đáp:** Hệ thống tự kéo lùi ngày nhận trên đơn về đúng ngày vào thực tế — tức tính tiền theo ngày vào thật, không giữ nguyên ngày đặt ban đầu.

**Q2.** Còn nhận phòng muộn hơn ngày trên đơn thì sao — có đối xứng với trường hợp trên không?
**Đáp:** Không đối xứng — nhận muộn thì **giữ nguyên** ngày đặt ban đầu (không lùi ngày tính tiền theo hướng có lợi cho khách), áp nguyên tắc "vào trễ không được lùi ngày tính tiền".

**Q3.** Điều kiện bắt buộc nào khiến một đơn Đã xác nhận vẫn không nhận phòng được?
**Đáp:** Khách chưa có CCCD/hộ chiếu trong hồ sơ, hoặc đã qua ngày trả phòng ghi trên đơn (quá hạn hoàn toàn) — 2 điều kiện chặn dù đơn đang đúng trạng thái Đã xác nhận.

### Luồng 13 — Gia hạn lưu trú

**Q1.** Gia hạn lưu trú có tạo ra một bản ghi Stay mới không, hay sửa trên bản ghi cũ?
**Đáp:** Sửa trực tiếp ngày trả trên đơn đặt phòng hiện có, không tạo bản ghi mới — giữ nguyên lịch sử, không làm sai doanh thu nếu tính lại sau này.

**Q2.** Muốn gia hạn thêm 3 đêm cho phòng đang có khách ở — hệ thống có kiểm tra gì trước khi cho phép?
**Đáp:** Kiểm tra phòng đó có bị đơn khác giữ chỗ trong đúng khoảng 3 đêm định gia hạn thêm không — nếu có xung đột thì chặn, dù khách đang ở thực tế trong phòng.

### Luồng 14 — Thêm phụ thu

**Q1.** Thêm phụ thu "Giặt ủi" hai lần liên tiếp cho cùng một lượt ở — hệ thống tạo ra 2 dòng hay gộp lại?
**Đáp:** Gộp — nếu mục đó đã có dòng cho lượt ở này rồi thì cộng dồn số lượng vào dòng cũ, không tạo dòng trùng.

**Q2.** Đổi giá phụ thu trong danh mục sau khi đã có dòng phụ thu ghi cho khách — dòng cũ có bị đổi giá theo không?
**Đáp:** Không — đơn giá được chụp lại (snapshot) tại đúng thời điểm ghi, đổi giá danh mục sau đó không ảnh hưởng dòng đã ghi.

### Luồng 15 — Trả phòng (check-out)

**Q1.** Liệt kê đủ 3 điều kiện bắt buộc để trả phòng thành công.
**Đáp:** (1) lượt ở đang Đang lưu trú; (2) không còn đơn dịch vụ nào đang Chờ làm (phải hoàn tất/huỷ hết); (3) đã có hoá đơn và hoá đơn đó Đã thanh toán đủ.

**Q2.** Khách phát sinh thêm 1 đơn dịch vụ mới ngay sau khi hoá đơn đã lập xong — lễ tân có trả phòng ngay được không?
**Đáp:** Không — hệ thống vẫn chặn trả phòng vì hoá đơn hiện tại không còn khớp thực tế, buộc lễ tân tính lại hoá đơn và thu nốt phần chênh lệch trước khi trả phòng.

**Q3.** Đã nhận phòng nhầm (check-in sai) — có cách nào "huỷ" lượt lưu trú đó không?
**Đáp:** Không có. `StayStatus.Cancelled` có định nghĩa trong enum nhưng chưa có bất kỳ thao tác nào trong hệ thống gán được giá trị này — đường đi duy nhất từ Đang lưu trú là qua Trả phòng để thành Hoàn tất. Đây là hạn chế đã biết.

---

## Mục 7 — Khách hàng

### Luồng 16 — Quản lý hồ sơ khách hàng

**Q1.** Hệ thống chặn trùng theo trường nào khi tạo hồ sơ khách mới? Có chặn trùng số điện thoại không?
**Đáp:** Chỉ chặn trùng **CCCD**. Số điện thoại và email chỉ kiểm tra đúng định dạng, không kiểm tra trùng — có thể có 2 hồ sơ khách khác CCCD nhưng cùng số điện thoại.

**Q2.** Gắn nhãn "Cảnh báo" (Blacklist) cho một khách — khách đó còn đặt được phòng bằng cách nào không?
**Đáp:** Còn — Blacklist chỉ chặn kênh khách **tự đặt online** qua Cổng khách hàng; lễ tân tại quầy vẫn đặt hộ được bình thường, hệ thống chỉ hiện dòng cảnh báo để lễ tân tự cân nhắc, không khoá gì cả.

### Luồng 17 — Cấp tài khoản Cổng khách hàng

**Q1.** "Cấp tài khoản" cho một khách đã có hồ sơ — về bản chất là tạo mới hay mở khoá tài khoản cũ?
**Đáp:** Tạo mới hoàn toàn một tài khoản đăng nhập gắn với hồ sơ khách đã có sẵn (không phải mở khoá) — và mỗi khách chỉ cấp được đúng một lần.

---

## Mục 8 — Dịch vụ

### Luồng 18 — Quản lý danh mục dịch vụ

**Q1.** Ngừng bán một món dịch vụ đã có người gọi trước đó — các đơn cũ có bị ảnh hưởng gì không?
**Đáp:** Không — các đơn đã gọi trước vẫn giữ nguyên và vẫn tính tiền bình thường, món chỉ đơn giản là không xuất hiện để gọi mới nữa. Vì lý do này món dịch vụ không có nút xoá, chỉ có ngừng bán/bán lại.

**Q2.** Sửa/tắt được một nhóm danh mục dịch vụ đã tạo không?
**Đáp:** Chưa — nhóm dịch vụ hiện chỉ thêm mới được, chưa có màn hình sửa/tắt nhóm (khác với món dịch vụ bên trong nhóm, có đủ nút ngừng bán/bán lại). Đây là khoảng trống chức năng cần nêu nếu bị hỏi.

### Luồng 19 — Gọi dịch vụ cho phòng đang ở

**Q1.** Phòng đang hiện "Đã đặt" (có khách sắp tới nhưng chưa check-in) — lễ tân có gọi dịch vụ trước cho phòng đó được không?
**Đáp:** Không — chỉ gọi được dịch vụ cho lượt ở đang thật sự **Đang lưu trú**; hệ thống kiểm tra lại điều kiện này ngay tại thời điểm tạo đơn (không chỉ dựa vào danh sách phòng đang hiển thị trên UI), nên dù UI có sơ hở gì thì service vẫn chặn đúng.

**Q2.** Đơn giá món dịch vụ hiển thị trên đơn đã gọi có tự cập nhật theo giá mới trong danh mục không?
**Đáp:** Không — đơn giá được chụp (snapshot) ngay lúc gọi vào chi tiết đơn; đổi giá danh mục sau đó không ảnh hưởng đơn cũ.

### Luồng 20 — Xử lý đơn dịch vụ

**Q1.** Một đơn dịch vụ đang "Chờ làm" có chuyển thẳng sang "Đã huỷ" được không, hay bắt buộc phải qua "Đang làm" trước?
**Đáp:** Chuyển thẳng được — giao diện hiện tại không có bước "Đang làm" bắt buộc, chỉ có 2 lựa chọn cuối cùng là Hoàn tất hoặc Huỷ ngay từ Chờ làm.

**Q2.** Vì sao module này không cho một vai trò vừa tạo đơn vừa xử lý đơn của module đó?
**Đáp:** Tách nhiệm vụ triệt để — `service.order.create` (Lễ tân) và `service.order.process` (Quản lý + Nhân viên dịch vụ) là hai quyền khác nhau, không vai trò mặc định nào có cả hai, tránh một người vừa gọi vừa tự "hoàn tất" đơn của chính mình mà không ai kiểm tra chéo.

---

## Mục 9 — Hoá đơn & Thanh toán

### Luồng 21 — Lập / tính lại hoá đơn

**Q1.** Khách ở thực tế 3 đêm nhưng đơn đặt 5 đêm (trả sớm) — hoá đơn tính tiền phòng theo 3 hay 5 đêm?
**Đáp:** Theo 3 đêm thực tế — số đêm tính tiền luôn dựa trên giờ nhận/trả phòng thực tế, tối thiểu 1 đêm, không thu tối thiểu theo số đêm đã đặt ban đầu.

**Q2.** Liệt kê đúng 3 nguồn giảm giá và cách chúng kết hợp với nhau.
**Đáp:** (1) Mã khuyến mãi gõ tay, (2) ưu đãi VIP tự động 10% nếu khách gắn nhãn VIP, (3) giảm giá thủ công (phải qua yêu cầu — Quản lý duyệt). Ba nguồn này **cộng dồn với nhau**, không phải chọn 1 trong 3, sau đó mới giới hạn về khoảng hợp lệ (không âm, không vượt tổng tiền trước giảm).

**Q3.** Hoá đơn đã có 1 khoản thanh toán, sau đó khách gọi thêm dịch vụ, lễ tân tính lại hoá đơn — mức giảm giá cũ có bị tính lại theo khuyến mãi hiện hành không?
**Đáp:** Không — giữ nguyên mức giảm giá cũ (không tính lại theo mã khuyến mãi/VIP hiện tại), để tránh tổng hoá đơn tụt xuống dưới số tiền đã thu thật.

### Luồng 22 — Ghi nhận thanh toán

**Q1.** Vấn đề "thu tiền trùng nhiều phiếu do bấm liên tục" đã từng xảy ra thế nào, và hệ thống hiện xử lý bằng cơ chế gì?
**Đáp:** Đây là lỗi nghiêm trọng nhất từng gặp ở phiên bản trước (double-submit). Hiện xử lý bằng giao dịch cơ sở dữ liệu có khoá (transaction + lock) kết hợp nút thu tiền tự khoá khi đang xử lý, đảm bảo 2 lần bấm liên tiếp không tạo ra 2 bản ghi thanh toán.

**Q2.** Đặt phòng có tiền cọc — khi lập hoá đơn, tiền cọc đó xử lý ra sao?
**Đáp:** Tự động trở thành một khoản thanh toán **đã hoàn tất** ngay trên hoá đơn, không cần lễ tân thu lại thủ công lần nữa.

**Q3.** Chuyển khoản có cần nhập mã giao dịch không? Có kiểm tra trùng không?
**Đáp:** Bắt buộc nhập mã giao dịch, và mã đó không được trùng với mã đã dùng trước đó — chặn ngay ở tầng service.

### Luồng 23 — Huỷ hoá đơn

**Q1.** Một hoá đơn đã thu được một phần tiền (PartiallyPaid) — lễ tân có gửi được yêu cầu huỷ hoá đơn đó không?
**Đáp:** Không — chỉ gửi được yêu cầu huỷ khi hoá đơn **chưa có khoản thanh toán nào đã hoàn tất**. Nếu còn thanh toán, hệ thống chặn ngay từ giao diện, bắt phải huỷ hết các giao dịch thanh toán (luồng 24) trước.

### Luồng 24 — Huỷ giao dịch thanh toán

**Q1.** Huỷ một giao dịch thanh toán có xoá bản ghi thanh toán đó khỏi cơ sở dữ liệu không?
**Đáp:** Không xoá — chuyển trạng thái giao dịch sang Đã huỷ, giữ nguyên bản ghi để giữ lịch sử; tổng đã thu và trạng thái hoá đơn được tính lại ngay theo đúng ngưỡng (Chưa thanh toán/Một phần/Đã thanh toán).

---

## Mục 10 — Khuyến mãi

### Luồng 25 — Quản lý khuyến mãi

**Q1.** Muốn ngừng dùng một mã khuyến mãi thì thao tác thế nào — có nút Xoá không?
**Đáp:** Không có nút xoá — sửa lại chương trình đó và bỏ tick "Đang áp dụng".

**Q2.** Một mã khuyến mãi có áp dụng riêng cho loại phòng Suite hay yêu cầu ở tối thiểu 3 đêm được không?
**Đáp:** Không — khuyến mãi không có điều kiện áp dụng theo loại phòng hay số đêm tối thiểu, áp dụng chung cho mọi hoá đơn miễn nhập đúng mã còn hiệu lực.

**Q3.** Khách đặt phòng trước, khuyến mãi hết hạn giữa kỳ ở, đến ngày trả mới lập hoá đơn — mã đó có dùng được không?
**Đáp:** Không — ngày hiệu lực khớp theo **ngày lập hoá đơn** (hoặc ngày trả thực tế), không phải ngày check-in hay ngày đặt phòng ban đầu, nên mã hết hạn giữa kỳ sẽ không áp dụng được nữa dù lúc đặt phòng nó còn hiệu lực.

---

## Mục 11 — Báo cáo

### Luồng 26 — Xem/xuất báo cáo doanh thu

**Q1.** Bảng doanh thu theo ngày sắp xếp theo tiêu chí gì — theo thứ tự ngày hay theo số tiền? Vì sao?
**Đáp:** Sắp xếp **giảm dần theo tổng doanh thu trong ngày**, không phải theo thứ tự thời gian — đây là yêu cầu bắt buộc ghi rõ trong đề bài gốc.

**Q2.** Đổi khoảng ngày Từ/Đến trên màn Báo cáo — khối "Công suất phòng hiện tại" có đổi theo không?
**Đáp:** Không — khối công suất phòng luôn là số chụp tại **thời điểm đang xem báo cáo**, không phụ thuộc khoảng ngày đã chọn. Chỉ bảng doanh thu theo ngày mới đổi theo khoảng ngày.

**Q3.** Một hoá đơn đã bị huỷ có được tính vào doanh thu không?
**Đáp:** Không — chỉ tính hoá đơn chưa bị huỷ; thực thu chỉ tính các khoản thanh toán đã Hoàn tất.

---

## Mục 12 — Người dùng

### Luồng 27 — Quản lý tài khoản nhân viên

**Q1.** Admin có xoá được một tài khoản nhân viên không?
**Đáp:** Không có khái niệm xoá tài khoản — chỉ có Khoá/Mở khoá, dữ liệu (kể cả lịch sử thao tác) được giữ nguyên vĩnh viễn.

**Q2.** Admin A có sửa/khoá được tài khoản của Admin B không?
**Đáp:** Không — vai trò Quản trị viên hoàn toàn không thể bị tạo/sửa/khoá/đặt lại mật khẩu qua giao diện dưới bất kỳ hình thức nào, kể cả bởi một Admin khác. Tài khoản Admin chỉ khởi tạo sẵn từ cấu hình triển khai hệ thống. Đây là chủ đích thiết kế để tránh một Admin tự nhân bản quyền cao nhất hoặc tự khoá mình ra khỏi hệ thống.

---

## Mục 13 — Phân quyền

### Luồng 28 — Cấu hình ma trận phân quyền

**Q1.** Vừa lưu lại quyền của vai trò Lễ tân — người đang đăng nhập với vai trò Lễ tân có bị đổi quyền ngay lập tức không?
**Đáp:** Không — như luồng 4 đã nêu, phải đăng xuất/đăng nhập lại mới áp dụng quyền mới.

**Q2.** Hệ thống có cho lưu một vai trò với 0 quyền không? Có cho phép không còn vai trò nào giữ quyền `permission.manage` không?
**Đáp:** Cả hai đều bị chặn — không cho lưu vai trò hệ thống với 0 quyền, và luôn phải giữ ít nhất một vai trò còn quyền cấu hình phân quyền (tránh tự khoá đường vào lại chính màn Phân quyền).

**Q3.** Có gán được cả `payment.record` (ghi nhận thanh toán) và `payment.void.approve` (duyệt huỷ giao dịch) cho cùng một vai trò không? Vì sao?
**Đáp:** Không — đây là một trong các cặp quyền bị chặn tách nhiệm vụ: người ghi nhận thanh toán không được đồng thời có quyền duyệt huỷ giao dịch của chính khoản thanh toán đó, tránh một người vừa thu tiền vừa tự huỷ giao dịch mà không ai kiểm tra chéo.

---

## Mục 14 — Nhật ký hệ thống

### Luồng 29 — Tra cứu nhật ký

**Q1.** Một nhân viên vừa đổi trạng thái phòng, sau đó tài khoản người đó bị khoá — dòng nhật ký cũ hiển thị người thực hiện là gì?
**Đáp:** Vẫn còn dòng log nhưng hiển thị người thực hiện là "Không rõ" — log không bị xoá theo tài khoản.

**Q2.** Nếu chính lúc ghi nhật ký gặp lỗi kỹ thuật (ví dụ mất kết nối tạm thời), nghiệp vụ chính (ví dụ khoá tài khoản) có bị huỷ theo không?
**Đáp:** Không — nghiệp vụ chính vẫn thực hiện bình thường, lỗi ghi log không được phép làm hỏng nghiệp vụ. Hệ quả là về lý thuyết có thể có một số ít thao tác không để lại dấu vết nếu đúng lúc gặp sự cố ghi log.

---

## Mục 15 — Phê duyệt

### Luồng 30 — Xử lý hàng đợi phê duyệt

**Q1.** Người tạo yêu cầu huỷ đặt phòng có tự duyệt được yêu cầu của chính mình nếu họ cũng có quyền duyệt không?
**Đáp:** Không bao giờ — hệ thống so sánh trực tiếp người tạo và người duyệt, trùng nhau thì chặn, bất kể người đó có đủ quyền duyệt hay không.

**Q2.** Hai quản lý cùng lúc bấm Duyệt một yêu cầu — chuyện gì xảy ra?
**Đáp:** Chỉ một người xử lý thành công; người còn lại nhận thông báo "yêu cầu này đã được xử lý", không có chuyện thực thi 2 lần (xử lý bằng khoá ở tầng dữ liệu, tương tự cơ chế chống thu tiền trùng ở luồng 22).

**Q3.** Yêu cầu huỷ đặt phòng do khách hàng tự gửi qua Cổng khách hàng có được ưu tiên xử lý khác với yêu cầu do lễ tân gửi không?
**Đáp:** Không — cả hai nguồn đi chung một hàng đợi, một quy trình duyệt như nhau, không phân biệt đối xử.

**Q4.** Một vai trò tuỳ chỉnh chỉ có đúng quyền `room.maintenance.approve` (không có 4 quyền duyệt còn lại) — vai trò đó có thấy menu "Phê duyệt" không? Có xử lý được yêu cầu bảo trì không?
**Đáp:** Không thấy menu (điều kiện hiện menu chỉ xét 4 quyền duyệt kia), nhưng nếu vào được bằng đường khác thì vẫn đủ quyền xử lý yêu cầu bảo trì ở tầng service — đây là khoảng lệch đã biết giữa điều kiện hiện UI và quyền thực thi thật.

---

## Mục 16 — Cổng khách hàng (Guest Portal)

### Luồng 31 — Khách tự đặt phòng

**Q1.** Khách tự đặt phòng có chọn được trạng thái ban đầu như lễ tân không (Chờ xác nhận hay Đã xác nhận)?
**Đáp:** Không — đơn tự đặt luôn khởi tạo Chờ xác nhận, khách không có lựa chọn nào khác; chỉ lễ tân xác nhận đơn mới chuyển sang Đã xác nhận.

**Q2.** Một khách bị gắn nhãn Blacklist có tự đặt phòng online được không? So sánh với việc lễ tân đặt hộ.
**Đáp:** Bị chặn cứng, không tự đặt được — khác hẳn với lễ tân đặt hộ tại quầy (luồng 9), nơi Blacklist chỉ hiện cảnh báo mềm chứ không chặn.

**Q3.** Phòng đang có một đơn "Chờ xác nhận" của khách A (lễ tân chưa duyệt) — khách B vào Cổng khách hàng tìm phòng cùng ngày có thấy phòng đó không?
**Đáp:** Không thấy — phòng đã bị đơn Chờ xác nhận của A "giữ chỗ" dù lễ tân chưa xác nhận, tránh tình huống 2 khách cùng giữ 1 phòng. Đây là hành vi đúng, không phải lỗi hiển thị thiếu phòng.

### Luồng 32 — Khách tự yêu cầu huỷ đặt phòng

**Q1.** Đây là câu hỏi hay bị hỏi nhất của cả hệ thống — khách bấm "Yêu cầu huỷ" thì đơn có bị huỷ ngay không?
**Đáp:** Không. Yêu cầu của khách đi vào **đúng cùng một hàng đợi phê duyệt** với yêu cầu huỷ do lễ tân gửi (luồng 10, luồng 30) — đơn giữ nguyên trạng thái cho tới khi Quản lý duyệt. Không hề có việc "khách tự huỷ ngay, chỉ nhân viên mới cần duyệt".

**Q2.** Có giới hạn thời gian (ví dụ trước giờ nhận phòng 24h) khi khách gửi yêu cầu huỷ không?
**Đáp:** Không có giới hạn mốc thời gian nào — khách gửi yêu cầu bất cứ lúc nào trước khi đơn kết thúc vòng đời của nó.

### Luồng 33 — Khách tự gọi dịch vụ

**Q1.** Khách đã đặt phòng (Đã xác nhận) nhưng chưa tới ngày nhận phòng — có gọi dịch vụ trước được không?
**Đáp:** Không — chỉ gọi được khi đang thật sự ở trạng thái Đang lưu trú (đã check-in, chưa check-out); nếu chưa/không còn ở thì toàn bộ menu và giỏ hàng bị khoá kèm thông báo lý do rõ ràng.

**Q2.** Khách tự đổi được trạng thái đơn dịch vụ mình vừa gửi (ví dụ tự đánh dấu Hoàn tất) không?
**Đáp:** Không — đơn luôn khởi tạo Chờ xử lý, việc chuyển Đang làm/Hoàn tất là của nhân viên xử lý ở màn hình riêng (mục 8), khách chỉ tạo và xem.

### Luồng 34 — Khách xem hoá đơn của mình

**Q1.** Khách có thanh toán online được ngay trên Cổng khách hàng không?
**Đáp:** Không — đây là màn hình chỉ xem, chưa có tính năng thanh toán online; muốn thanh toán khách vẫn phải qua lễ tân tại quầy.

### Luồng 35 — Khách xem/sửa hồ sơ cá nhân

**Q1.** Khách phát hiện số điện thoại đăng ký bị sai, có tự sửa trên Cổng khách hàng được không?
**Đáp:** Không — khách chỉ tự đổi được mật khẩu; muốn sửa họ tên/số điện thoại/email/CCCD phải nhờ lễ tân sửa hộ ở module Khách hàng.

---

## Câu hỏi tổng hợp xuyên luồng (hay được hỏi cuối buổi bảo vệ)

**Q1.** Kể tên tất cả những "cặp yêu cầu — phê duyệt" trong toàn hệ thống và giải thích vì sao cần tách thành 2 bước thay vì làm trực tiếp.
**Đáp:** 5 cặp: Huỷ đặt phòng, Giảm giá hoá đơn, Huỷ hoá đơn, Huỷ giao dịch thanh toán, Bảo trì phòng (chiều vào). Tất cả đều là thao tác có thể gây thất thoát tiền hoặc ảnh hưởng vận hành nếu làm sai/làm bừa — cần người thứ hai xác nhận độc lập, và người tạo yêu cầu không bao giờ được tự duyệt của chính mình.

**Q2.** Tính năng nào đã có đủ logic ở tầng Service nhưng chưa có màn hình sử dụng?
**Đáp:** "Yêu cầu buồng phòng" (dọn phòng/thêm khăn/thêm nước) — có đủ entity, service, luật chuyển trạng thái (Chờ xử lý → Đã tiếp nhận → Hoàn tất/Đã huỷ) và kiểm tra quyền, nhưng chưa có ViewModel/View nào trong WPF gọi tới.

**Q3.** Nêu 3 quyết định thiết kế thể hiện rõ nhất nguyên tắc "tách nhiệm vụ" (Segregation of Duties) trong hệ thống.
**Đáp:** (1) Admin không thấy module nghiệp vụ nào, chỉ quản trị định danh; (2) người ghi nhận thanh toán không được đồng thời có quyền duyệt huỷ giao dịch, người lập hoá đơn không được đồng thời duyệt giảm giá; (3) không vai trò nào vừa tạo vừa xử lý được đơn dịch vụ của chính module đó.
