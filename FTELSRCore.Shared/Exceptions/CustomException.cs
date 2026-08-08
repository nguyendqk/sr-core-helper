using System.Net;

namespace FTELSRCore.Exceptions
{
    public class CustomException(string message, int statusCode = (int)HttpStatusCode.InternalServerError) : Exception(message)
    {
        public int Code { get; set; } = statusCode;

        public IEnumerable<string> Messages { get; set; } = [message];

        public CustomException(int statusCode, Exception inner) : this(inner?.Message?.ToString(), statusCode)
        {
        }
    }
}