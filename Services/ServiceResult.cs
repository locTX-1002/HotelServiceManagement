namespace Services
{
    /// <summary>
    /// Ket qua nghiep vu dung chung cho ca nhom: Ok = thanh cong, Message = thong bao
    /// tieng Viet hien thang len UI (Notify/banner). KHONG dung exception cho loi
    /// nghiep vu du doan truoc duoc - exception chi danh cho loi ky thuat (mat DB...).
    /// </summary>
    public record ServiceResult(bool Ok, string Message)
    {
        public static ServiceResult Success(string message = "") => new(true, message);
        public static ServiceResult Failure(string message) => new(false, message);
    }

    public record ServiceResult<T>(bool Ok, string Message, T? Data)
    {
        public static ServiceResult<T> Success(T data, string message = "")
            => new(true, message, data);

        public static ServiceResult<T> Failure(string message)
            => new(false, message, default);
    }
}
