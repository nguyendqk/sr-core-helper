using System.Net;

namespace FTELSRCore.Models.Https
{
    public record BaseApiModel
    {
        public string Status { get; set; } = "OK";
        public int Code { get; set; } = (int)HttpStatusCode.OK;
        public string Message { get; set; } = HttpStatusCode.OK.ToString();
    }
}