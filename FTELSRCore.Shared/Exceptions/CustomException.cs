using System.Net;

namespace FTELSRCore.Exceptions
{
    public class CustomException(string message, int statusCode = (int)HttpStatusCode.InternalServerError) : Exception(message)
    {
        public int Code { get; set; } = statusCode;

        public string Status { get; set; } = statusCode.ConvertHttpStatusCodeCodeByName();

        public bool Succeeded { get; set; } = false;

        public IEnumerable<string> Messages { get; set; } = [message];

        public string System { get; set; } = CommonBaseConstant.System;

        public CustomException(int statusCode, Exception inner) : this(inner?.Message?.ToString(), statusCode)
        {
        }
    }
}