using BusinessObjects.Entities;
namespace Repositories; public interface IReportRepository { Task<List<Invoice>> GetInvoicesAsync(DateTime from, DateTime toExclusive); Task<List<Payment>> GetPaymentsAsync(DateTime from, DateTime toExclusive); Task<List<Room>> GetRoomsAsync(); }
