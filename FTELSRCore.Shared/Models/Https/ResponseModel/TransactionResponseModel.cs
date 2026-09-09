namespace FTELSRCore.Models.Https.ResponseModel
{
    public class TransactionResponseModel<TData> where TData : notnull
    {
        public TData Data { get; set; }

        public int StatusCode { get; set; }

        public string Title { get; set; }
    }
}
