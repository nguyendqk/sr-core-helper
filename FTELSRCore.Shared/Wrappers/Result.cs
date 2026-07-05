using FTELSRCore.Wrappers.ErrorCodes;
using FTELSRCore.Wrappers.ErrorCodes.Catalogs;
using System.Net;
using System.Text.Json.Serialization;

namespace FTELSRCore.Wrappers
{
    public class Result : IResult
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("dispatched")]
        public bool Dispatched { get; set; }

        //[JsonPropertyName("succeeded")]
        //public bool Succeeded { get; set; } = true;

        [JsonPropertyName("messages")]
        public List<string> Messages { get; set; }

        [JsonPropertyName("system")]
        public string System { get; set; } = CommonBaseConstant.System;

        [JsonPropertyName("error")]
        public ResultFTELCoreErrorModel Error { get; set; } = null;

        [JsonPropertyName("meta")]
        public ResultFTELCoreMetadataModel Meta { get; set; } = null;

        protected Result()
        {
        }

        private static readonly CatalogsErorrCodeModel _catalogsBadRequest =
            ResponseWrapperByCodeMapper.FromStatusCode(
                statusCode: HttpStatusCode.BadRequest, sourceType: ErrorSourceType.General);

        private static readonly ResultFTELCoreErrorModel _failLogicDefault =
            new()
            {
                Code = _catalogsBadRequest.Code,
                Retryable = _catalogsBadRequest.Retryable
            };

        private static readonly CatalogsErorrCodeModel _catalogsInternalServerError =
            ResponseWrapperByCodeMapper.FromStatusCode(
                statusCode: HttpStatusCode.InternalServerError, sourceType: ErrorSourceType.General);

        private static readonly ResultFTELCoreErrorModel _errorSystemDefault = new()
        {
            Retryable = _catalogsInternalServerError.Retryable,
            Code = _catalogsInternalServerError.Code
        };

        #region ::::::::::::: FAIL :::::::::::::

        public static Result Fail(
            string message = "Thực hiện yêu cầu không thành công", bool succeeded = false, int statusCode = (int)HttpStatusCode.BadRequest, ResultFTELCoreErrorModel error = null)
            => new Result
            {
                Code = statusCode,
                Messages = [message],
                Success = succeeded,
                Dispatched = true,
                Status = statusCode.ConvertHttpStatusCodeCodeByName(),
                Error = error ?? _failLogicDefault
            };

        public static Result Fail(
            List<string> messages, bool succeeded = true, int statusCode = (int)HttpStatusCode.BadRequest, ResultFTELCoreErrorModel error = null)
            => new Result
            {
                Code = statusCode,
                Messages = messages,
                Success = succeeded,
                Dispatched = true,
                Status = statusCode.ConvertHttpStatusCodeCodeByName(),
                Error = error ?? _failLogicDefault
            };

        public static Task<Result> FailAsync(
            string message = "Thực hiện yêu cầu không thành công", bool succeeded = false, int statusCode = (int)HttpStatusCode.BadRequest, ResultFTELCoreErrorModel error = null)
            => Task.FromResult(Fail(message: message, succeeded: succeeded, statusCode: statusCode, error: error));

        public static Task<Result> FailAsync(
            List<string> messages, bool succeeded = false, int statusCode = (int)HttpStatusCode.BadRequest, ResultFTELCoreErrorModel error = null)
            => Task.FromResult(Fail(messages: messages, succeeded: succeeded, statusCode: statusCode, error: error));

        #endregion ::::::::::::: FAIL :::::::::::::

        #region ::::::::::::: SUCCESS :::::::::::::

        public static Result Succeed(
            string message = "Thực hiện yêu cầu thành công", int statusCode = (int)HttpStatusCode.OK)
            => new Result
            {
                Success = true,
                Dispatched = true,
                Code = statusCode,
                Messages = [message],
                Status = statusCode.ConvertHttpStatusCodeCodeByName()
            };

        public static Result Succeed(List<string> messages, int statusCode = (int)HttpStatusCode.OK)
            => new Result
            {
                Success = true,
                Dispatched = true,
                Code = statusCode,
                Messages = messages,
                Status = statusCode.ConvertHttpStatusCodeCodeByName()
            };

        public static Task<Result> SucceedAsync(
            string message = "Thực hiện yêu cầu thành công", int statusCode = (int)HttpStatusCode.OK)
            => Task.FromResult(Succeed(message, statusCode));

        public static Task<Result> SucceedAsync(
            List<string> messages, int statusCode = (int)HttpStatusCode.OK)
            => Task.FromResult(Succeed(messages, statusCode));

        #endregion ::::::::::::: SUCCESS :::::::::::::

        /// <summary>
        /// Don't recommend use in function
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="metadata"></param>
        /// <param name="serviceName"></param>
        /// <param name="statusCode"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        ///
        public static Result FailSystem(
            List<string> messages, ResultFTELCoreMetadataModel metadata, string serviceName, int statusCode = (int)HttpStatusCode.InternalServerError, ResultFTELCoreErrorModel error = null)
            => new Result
            {
                Success = false,
                Dispatched = false,
                Code = statusCode,
                Messages = messages,
                System = serviceName,
                Status = statusCode.ConvertHttpStatusCodeCodeByName(),
                Meta = metadata,
                Error = error ?? _errorSystemDefault
            };

        /// <summary>
        /// Don't recommend use in function
        /// </summary>
        /// <param name="message"></param>
        /// <param name="metadata"></param>
        /// <param name="serviceName"></param>
        /// <param name="statusCode"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        ///
        public static Result FailSystem(
            string message, ResultFTELCoreMetadataModel metadata, string serviceName, int statusCode = (int)HttpStatusCode.InternalServerError, ResultFTELCoreErrorModel error = null)
            => new Result
            {
                Code = statusCode,
                Success = false,
                Dispatched = false,
                Messages = [message],
                System = serviceName,
                Status = statusCode.ConvertHttpStatusCodeCodeByName(),
                Meta = metadata,
                Error = error ?? _errorSystemDefault
            };
    }

    public class Result<T> : Result, IResult<T>
    {
        [JsonPropertyName("data")]
        public T Data { get; set; }

        public Result()
        {
        }

        private static readonly CatalogsErorrCodeModel _catalogsBadRequest =
            ResponseWrapperByCodeMapper.FromStatusCode(
                statusCode: HttpStatusCode.BadRequest, sourceType: ErrorSourceType.General);

        private static readonly ResultFTELCoreErrorModel _failLogicDefault =
            new()
            {
                Code = _catalogsBadRequest.Code,
                Retryable = _catalogsBadRequest.Retryable
            };

        #region ::::::::::::: FAIL :::::::::::::

        public new static Result<T> Fail(
            string message = "Thực hiện yêu cầu không thành công", bool succeeded = false, int statusCode = (int)HttpStatusCode.BadRequest, ResultFTELCoreErrorModel error = null)
            => new Result<T>()
            {
                Code = statusCode,
                Messages = [message],
                Success = succeeded,
                Dispatched = true,
                Status = statusCode.ConvertHttpStatusCodeCodeByName(),
                Error = error ?? _failLogicDefault
            };

        public new static Result<T> Fail(List<string> messages, bool succeeded = false, int statusCode = (int)HttpStatusCode.BadRequest, ResultFTELCoreErrorModel error = null)
            => new Result<T>
            {
                Code = statusCode,
                Messages = messages,
                Success = succeeded,
                Dispatched = true,
                Status = statusCode.ConvertHttpStatusCodeCodeByName(),
                Error = error ?? _failLogicDefault
            };

        public static Result<T> Fail(
            T data, string message = "Thực hiện yêu cầu không thành công", bool succeeded = false, int statusCode = (int)HttpStatusCode.BadRequest, ResultFTELCoreErrorModel error = null)
            => new Result<T>
            {
                Data = data,
                Code = statusCode,
                Messages = [message],
                Success = succeeded,
                Dispatched = true,
                Status = statusCode.ConvertHttpStatusCodeCodeByName(),
                Error = error ?? _failLogicDefault
            };

        public static Result<T> Fail(
            T data, List<string> messages, bool succeeded = false, int statusCode = (int)HttpStatusCode.BadRequest, ResultFTELCoreErrorModel error = null)
            => new Result<T>
            {
                Data = data,
                Code = statusCode,
                Messages = messages,
                Success = succeeded,
                Dispatched = true,
                Status = statusCode.ConvertHttpStatusCodeCodeByName(),
                Error = error ?? _failLogicDefault
            };

        public new static Task<Result<T>> FailAsync(
            string message = "Thực hiện yêu cầu không thành công", bool succeeded = false, int statusCode = (int)HttpStatusCode.BadRequest, ResultFTELCoreErrorModel error = null)
            => Task.FromResult(Fail(message: message, succeeded: succeeded, statusCode: statusCode, error: error));

        public new static Task<Result<T>> FailAsync(
            List<string> messages, bool succeeded = false, int statusCode = (int)HttpStatusCode.BadRequest, ResultFTELCoreErrorModel error = null)
            => Task.FromResult(Fail(messages: messages, succeeded: succeeded, statusCode: statusCode, error: error));

        public static Task<Result<T>> FailAsync(
            T data, string message = "Thực hiện yêu cầu không thành công", bool succeeded = false, int statusCode = (int)HttpStatusCode.BadRequest, ResultFTELCoreErrorModel error = null)
            => Task.FromResult(Fail(data: data, message: message, succeeded: succeeded, statusCode: statusCode, error: error));

        public static Task<Result<T>> FailAsync(
            T data, List<string> messages, bool succeeded = false, int statusCode = (int)HttpStatusCode.BadRequest, ResultFTELCoreErrorModel error = null)
            => Task.FromResult(Fail(data: data, messages: messages, succeeded: succeeded, statusCode: statusCode, error: error));

        #endregion ::::::::::::: FAIL :::::::::::::

        #region ::::::::::::: SUCCESS :::::::::::::

        public static Result<T> Succeed(
            string message = "Thực hiện yêu cầu thành công", bool succeeded = true, int statusCode = (int)HttpStatusCode.OK)
            => new Result<T>
            {
                Success = succeeded,
                Dispatched = true,

                Code = statusCode,
                Messages = [message],
                Status = statusCode.ConvertHttpStatusCodeCodeByName()
            };

        public static Result<T> Succeed(
            List<string> messages, bool succeeded = true, int statusCode = (int)HttpStatusCode.OK)
            => new Result<T>
            {
                Success = succeeded,
                Dispatched = true,

                Code = statusCode,
                Messages = messages,
                Status = statusCode.ConvertHttpStatusCodeCodeByName()
            };

        public static Result<T> Succeed(
            T data, string message = "Thực hiện yêu cầu thành công", bool succeeded = true, int statusCode = (int)HttpStatusCode.OK)
          => new Result<T>
          {
              Data = data,
              Success = succeeded,
              Dispatched = true,
              Code = statusCode,
              Messages = [message],
              Status = statusCode.ConvertHttpStatusCodeCodeByName()
          };

        public static Result<T> Succeed(
           T data, List<string> messages, bool succeeded = true, int statusCode = (int)HttpStatusCode.OK)
            => new Result<T>
            {
                Data = data,
                Success = succeeded,
                Dispatched = true,
                Code = statusCode,
                Messages = messages,
                Status = statusCode.ConvertHttpStatusCodeCodeByName()
            };

        public static Task<Result<T>> SucceedAsync(
            string message = "Thực hiện yêu cầu thành công", bool succeeded = true, int statusCode = (int)HttpStatusCode.OK)
            => Task.FromResult(Succeed(message, succeeded, statusCode));

        public static Task<Result<T>> SucceedAsync(
            List<string> messages, bool succeeded = true, int statusCode = (int)HttpStatusCode.OK)
            => Task.FromResult(Succeed(messages, succeeded, statusCode));

        public static Task<Result<T>> SucceedAsync(
            T data, string message = "Thực hiện yêu cầu thành công", bool succeeded = true, int statusCode = (int)HttpStatusCode.OK)
            => Task.FromResult(Succeed(data, message, succeeded, statusCode));

        public static Task<Result<T>> SucceedAsync(
            T data, List<string> messages, bool succeeded = true, int statusCode = (int)HttpStatusCode.OK)
            => Task.FromResult(Succeed(data, messages, succeeded, statusCode));

        #endregion ::::::::::::: SUCCESS :::::::::::::
    }
}