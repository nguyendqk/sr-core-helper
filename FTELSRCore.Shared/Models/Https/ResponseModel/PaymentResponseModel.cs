namespace FTELSRCore.Models.Https.ResponseModel
{
    public record PaymentResponseModel
    {
        public int Code { get; set; }

        public dynamic Status { get; set; }

        public bool Succeeded { get; set; }

        public string System { get; set; }

        public string Message { get; set; }
    }

    public record PaymentResponseModel<TModel> : PaymentResponseModel where TModel : notnull
    {
        public TModel Data { get; set; }
    }
}
