namespace FTELSRCore.Models.Https.ResponseModel
{
    public record SRBaseResponseModel
    {
        public int Code { get; set; }

        public string Status { get; set; }

        public bool Succeeded { get; set; }

        public string System { get; set; }

        public List<string> Messages { get; set; } = [];
    }

    public record SRBaseResponseModel<TModel> : SRBaseResponseModel where TModel : notnull
    {
        public TModel Data { get; set; }
    }
}
