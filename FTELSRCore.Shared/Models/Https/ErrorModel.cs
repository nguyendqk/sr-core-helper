namespace FTELSRCore.Models.Https
{
    public record ErrorModel
    {
        public int Code { get; set; }

        public string Message { get; set; }

        public bool Succeeded { get; set; }

        public void ErrorDeConstruct(out string message, out int statusCode)
        {
            message = Message;
            statusCode = Code;
        }
    }
}