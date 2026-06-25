namespace FTELSRCore.Models.Https
{
    public class TokenResultModel
    {
        public string Type { get; set; }

        public string Token { get; set; }

        public long ExpiresAt { get; set; }
    }
}