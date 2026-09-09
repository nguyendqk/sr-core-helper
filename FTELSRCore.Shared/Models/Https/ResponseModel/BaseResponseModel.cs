namespace FTELSRCore.Models.Https.ResponseModel
{
    public class BaseResponseModel<TData> where TData : notnull
    {
        public TData Data { get; set; }
    }
}
