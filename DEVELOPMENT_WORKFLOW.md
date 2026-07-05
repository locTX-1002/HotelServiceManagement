# Quy trình Phát triển Tính năng (Step-by-Step Development Workflow)

Hướng dẫn quy trình chuẩn để thêm một tính năng vào HSMS, từ entity đến màn hình frontend. Ví dụ xuyên suốt: tính năng **Service Order** (Role 2 làm API, Role 4 làm UI).

---

## Bước 0: Tạo nhánh từ develop

```bash
git checkout develop && git pull
git checkout -b feature/backend-room-reservation-service   # nhánh theo TeamAssignment §5
```

## Bước 1: Entity (Domain) - nếu cần bảng mới

13 entity đã có sẵn đủ cho MVP. Nếu thật sự cần đổi: sửa entity trong `Domain/Entities/`, rồi **PR cho Khoa** để Khoa tạo migration — không tự tạo.

## Bước 2: DTO + Validator (Application)

```csharp
// DTOs/ServiceOrders/CreateServiceOrderRequest.cs
public class CreateServiceOrderRequest
{
    public int StayId { get; set; }
    public List<OrderItemRequest> Items { get; set; } = [];
}

// Validators/CreateServiceOrderRequestValidator.cs
public class CreateServiceOrderRequestValidator : AbstractValidator<CreateServiceOrderRequest>
{
    public CreateServiceOrderRequestValidator()
    {
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(i => i.RuleFor(x => x.Quantity).GreaterThan(0));
    }
}
```

## Bước 3: Interface + Service (Application)

```csharp
// Interfaces/IServiceOrderService.cs
public interface IServiceOrderService
{
    Task<ServiceOrderResponse> CreateAsync(CreateServiceOrderRequest request, CancellationToken ct);
}

// Services/ServiceOrderService.cs - LOGIC NGHIỆP VỤ Ở ĐÂY
// BR06: chỉ stay đang Active mới được thêm order -> kiểm tra trước khi insert
```

Đăng ký DI trong `Program.cs`: `builder.Services.AddScoped<IServiceOrderService, ServiceOrderService>();`

## Bước 4: Controller (Api) - mỏng nhất có thể

```csharp
[ApiController]
[Route("api/service-orders")]
public class ServiceOrdersController(IServiceOrderService service) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "ServiceStaff,Receptionist")]
    public async Task<IActionResult> Create(CreateServiceOrderRequest request, CancellationToken ct)
        => Ok(await service.CreateAsync(request, ct));
}
```

## Bước 5: Tự test trên Swagger TRƯỚC khi mở PR

Happy path + ít nhất 1 ca lỗi (ví dụ: stay đã Completed → kỳ vọng 400/409, quantity = 0 → 400). Đây là một phần của Definition of Done, không phải việc của QA.

## Bước 6: Frontend nối API

- Contract nằm ở `frontend/API_DOCS.md` — làm mock đúng contract trước khi API xong, nối thật sau.
- Gọi API qua `src/api/client.js` (đã gắn JWT + redirect 401 sẵn). Không tự tạo axios instance mới.
- Màu trạng thái, format tiền: dùng `src/utils/roomStatus.js`. Ảnh phòng: `src/utils/roomImages.js`.

## Bước 7: PR vào develop

- PR nhỏ hơn 300 dòng, mô tả rõ đã tự test những ca nào.
- Checklist trong CONTRIBUTING.md phải pass. 1 người khác review (cross-area càng tốt).
- CI xanh (backend: build + 11 unit test; frontend: build).

## Bước 8: Cập nhật tiến độ

Đổi trạng thái task trên Notion board trước standup 21:00.
