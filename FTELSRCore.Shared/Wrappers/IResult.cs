namespace FTELSRCore.Wrappers
{
    public interface IResult<out T> : IResult
    {
        T Data { get; }
    }

    public interface IResult
    {
        /// <summary>
        /// HttpStatusCode dạng số.
        /// </summary>
        ///
        int Code { get; set; }

        /// <summary>
        /// HttpStatusCode dạng tên.
        /// </summary>
        ///
        string Status { get; set; }

        /// <summary>
        /// [QUY ĐỊNH] - { true: nếu đã vào được hàm xử lý } || { false: Khi không vào được hàm xử lý }
        /// </summary>
        ///
        bool Succeeded { get; set; }

        string System { get; set; }

        /// <summary>
        /// Danh sách thông báo
        /// </summary>
        ///
        List<string> Messages { get; set; }
    }
}