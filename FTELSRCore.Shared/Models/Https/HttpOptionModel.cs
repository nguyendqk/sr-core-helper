namespace FTELSRCore.Models.Https
{
    public record HttpOptionModel
    {
        public HttpClient Client { get; set; }

        public string BaseAddress { get; set; }

        public string Token { get; set; }

        public string AuthType { get; set; } = "Bearer";

        public string Uri { get; set; }

        public string SystemOwner { get; set; } = "Service Request";

        public HttpCompletionOption CompletionOption { get; set; } = HttpCompletionOption.ResponseContentRead;
    }

    public record HttpOptionModel<T> : HttpOptionModel where T : notnull
    {
        public T Value { get; init; }
    }
}