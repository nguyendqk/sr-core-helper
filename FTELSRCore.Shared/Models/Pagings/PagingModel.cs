namespace FTELSRCore.Models.Pagings
{
    public abstract record PagingModel : Paging
    {
        public string FromDate { get; set; }
        public string ToDate { get; set; }

        public string Search { get; set; } = string.Empty;
    }

    public abstract record PagingMongoDBModel : Paging
    {
        public string StartDateTime { get; set; }
        public string EndDateTime { get; set; }

        public string Search { get; set; } = string.Empty;
    }

    public abstract record Paging
    {
        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 10;
    }

    public record PagingCursorModel
    {
        public string Cursor { get; set; }

        public int PageSize { get; set; } = 20;
    }

    public record PageInfoCursorResponseModel
    {
        /// <summary>
        /// Cho biết liệu có còn kết quả ở trang trước trang hiện tại hay không
        /// Giúp xác định có thể lùi về trang trước được không
        /// </summary>
        ///
        public bool HasPreviousPage { get; set; }

        /// <summary>
        /// Cho biết liệu có còn kết quả ở trang sau trang hiện tại hay không
        /// Giúp xác định có thể chuyển sang trang tiếp theo được không
        /// </summary>
        ///
        public bool HasNextPage { get; set; }

        /// <summary>
        /// Con trỏ (cursor) của node đầu tiên trong danh sách nodes
        /// Dùng để truy vấn các trang trước đó
        /// </summary>
        ///
        public string StartCursor { get; set; }

        /// <summary>
        /// Con trỏ (cursor) của node cuối cùng trong danh sách nodes
        /// Dùng để truy vấn các trang tiếp theo
        /// </summary>
        ///
        public string EndCursor { get; set; }
    }
}