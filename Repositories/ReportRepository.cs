using BusinessObjects.Entities;
using DataAccessObjects;
namespace Repositories; public sealed class ReportRepository : IReportRepository { public Task<List<Invoice>> GetInvoicesAsync(DateTime f, DateTime t) => ReportDAO.Instance.GetInvoicesAsync(f, t); public Task<List<Payment>> GetPaymentsAsync(DateTime f, DateTime t) => ReportDAO.Instance.GetPaymentsAsync(f, t); public Task<List<Room>> GetRoomsAsync() => ReportDAO.Instance.GetRoomsAsync(); }
