namespace FTELSRCore.Models.Https.ResponseModel
{
    public record SRBaseV1ResponseModel
    {
        public int Code { get; set; }

        public string Status { get; set; }

        public bool Succeeded { get; set; }

        public string System { get; set; }

        public string Message { get; set; }
    }

    public record SRBaseV1ResponseModel<TModel> : SRBaseV1ResponseModel where TModel : notnull
    {
        public TModel Data { get; set; }
    }
}
