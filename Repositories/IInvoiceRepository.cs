using BusinessObjects.Entities;
namespace Repositories;

public interface IInvoiceRepository { Task<Invoice?> GetByIdAsync(int id); Task<Invoice?> GetByStayAsync(int id); Task<Stay?> GetStayForBillingAsync(int id); Task<bool> SaveAsync(Invoice invoice, bool add); Task<bool> CancelAsync(int id); }
