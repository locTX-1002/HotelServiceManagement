using BusinessObjects.Entities;
using DataAccessObjects;
namespace Repositories;

public sealed class InvoiceRepository : IInvoiceRepository { public Task<Invoice?> GetByIdAsync(int id) => InvoiceDAO.Instance.GetByIdAsync(id); public Task<Invoice?> GetByStayAsync(int id) => InvoiceDAO.Instance.GetByStayAsync(id); public Task<Stay?> GetStayForBillingAsync(int id) => InvoiceDAO.Instance.GetStayForBillingAsync(id); public Task<bool> SaveAsync(Invoice x, bool add) => InvoiceDAO.Instance.SaveAsync(x, add); public Task<bool> CancelAsync(int id) => InvoiceDAO.Instance.CancelAsync(id); }
