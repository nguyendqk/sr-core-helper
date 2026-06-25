using System.Net;

namespace FTELSRCore.Wrappers
{
    public class Result : IResult
    {
        public int Code { get; set; }

        public string Status { get; set; }

        public bool Succeeded { get; set; }

        public List<string> Messages { get; set; }

        public string System { get; set; } = CommonBaseConstant.System;

        protected Result()
        {
        }

        #region ::::::::::::: FAIL :::::::::::::

        public static IResult Fail(int statusCode = (int)HttpStatusCode.BadRequest, bool succeeded = true)
            => new Result { Succeeded = succeeded, Code = statusCode, Status = statusCode.ConvertHttpStatusCodeCodeByName() };

        public static IResult Fail(string message = "Thực hiện yêu cầu không thành công !", bool succeeded = true, int statusCode = (int)HttpStatusCode.OK)
            => new Result
            {
                Code = statusCode,
                Messages = [message],
                Succeeded = succeeded,
                Status = statusCode.ConvertHttpStatusCodeCodeByName()
            };

        public static IResult Fail(List<string> messages, bool succeeded = true, int statusCode = (int)HttpStatusCode.BadRequest)
            => new Result
            {
                Code = statusCode,
                Messages = messages,
                Succeeded = succeeded,
                Status = statusCode.ConvertHttpStatusCodeCodeByName()
            };

        public static Task<IResult> FailAsync(int statusCode = (int)HttpStatusCode.OK, bool succeeded = true)
            => Task.FromResult(Fail(statusCode, succeeded));

        public static Task<IResult> FailAsync(string message = "Thực hiện yêu cầu không thành công !", bool succeeded = true, int statusCode = (int)HttpStatusCode.BadRequest)
            => Task.FromResult(Fail(message, succeeded, statusCode));

        public static Task<IResult> FailAsync(List<string> messages, bool succeeded = true, int statusCode = (int)HttpStatusCode.BadRequest)
            => Task.FromResult(Fail(messages, succeeded, statusCode));

        #endregion ::::::::::::: FAIL :::::::::::::

        #region ::::::::::::: SUCCESS :::::::::::::

        public static IResult Success(int statusCode = (int)HttpStatusCode.OK)
            => new Result { Succeeded = true, Code = statusCode, Status = statusCode.ConvertHttpStatusCodeCodeByName() };

        public static IResult Success(string message = "Thực hiện yêu cầu thành công !", int statusCode = (int)HttpStatusCode.OK)
            => new Result
            {
                Succeeded = true,
                Code = statusCode,
                Messages = [message],
                Status = statusCode.ConvertHttpStatusCodeCodeByName()
            };

        public static IResult Success(List<string> messages, int statusCode = (int)HttpStatusCode.OK)
            => new Result
            {
                Succeeded = true,
                Code = statusCode,
                Messages = messages,
                Status = statusCode.ConvertHttpStatusCodeCodeByName()
            };

        public static Task<IResult> SuccessAsync(int statusCode = (int)HttpStatusCode.OK)
            => Task.FromResult(Success(statusCode));

        public static Task<IResult> SuccessAsync(string message = "Thực hiện yêu cầu thành công !", int statusCode = (int)HttpStatusCode.OK)
            => Task.FromResult(Success(message, statusCode));

        public static Task<IResult> SuccessAsync(List<string> messages, int statusCode = (int)HttpStatusCode.OK)
            => Task.FromResult(Success(messages, statusCode));

        #endregion ::::::::::::: SUCCESS :::::::::::::

        /// <summary>
        /// Don't recommend use in function
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="statusCode"></param>
        /// <returns></returns>
        public static Result FailSystem(List<string> messages, int statusCode = (int)HttpStatusCode.BadRequest)
            => new()
            {
                Succeeded = false,
                Code = statusCode,
                Messages = messages,
                Status = statusCode.ConvertHttpStatusCodeCodeByName()
            };

        /// <summary>
        /// Don't recommend use in function
        /// </summary>
        /// <param name="message"></param>
        /// <param name="statusCode"></param>
        /// <returns></returns>
        public static Result FailSystem(string message, int statusCode = (int)HttpStatusCode.BadRequest)
             => new()
             {
                 Code = statusCode,
                 Succeeded = false,
                 Messages = [message],
                 Status = statusCode.ConvertHttpStatusCodeCodeByName()
             };
    }

    public class Result<T> : Result, IResult<T>
    {
        public T Data { get; set; }

        public Result()
        {
        }

        #region ::::::::::::: FAIL :::::::::::::

        public new static Result<T> Fail(int statusCode = (int)HttpStatusCode.BadRequest, bool succeeded = true)
            => new()
            { Succeeded = succeeded, Code = statusCode, Status = statusCode.ConvertHttpStatusCodeCodeByName() };

        public new static Result<T> Fail(string message = "Thực hiện yêu cầu không thành công !", bool succeeded = true, int statusCode = (int)HttpStatusCode.BadRequest)
            => new()
            {
                Code = statusCode,
                Messages = [message],
                Succeeded = succeeded,
                Status = statusCode.ConvertHttpStatusCodeCodeByName()
            };

        public new static Result<T> Fail(List<string> messages, bool succeeded = true, int statusCode = (int)HttpStatusCode.BadRequest)
            => new()
            {
                Code = statusCode,
                Messages = messages,
                Succeeded = succeeded,
                Status = statusCode.ConvertHttpStatusCodeCodeByName()
            };

        public static Result<T> Fail(T data, string message = "Thực hiện yêu cầu không thành công !", bool succeeded = true, int statusCode = (int)HttpStatusCode.BadRequest)
            => new()
            {
                Data = data,
                Code = statusCode,
                Messages = [message],
                Succeeded = succeeded,
                Status = statusCode.ConvertHttpStatusCodeCodeByName()
            };

        public static Result<T> Fail(T data, List<string> messages, bool succeeded = true, int statusCode = (int)HttpStatusCode.BadRequest)
            => new()
            {
                Data = data,
                Code = statusCode,
                Messages = messages,
                Succeeded = succeeded,
                Status = statusCode.ConvertHttpStatusCodeCodeByName()
            };

        public new static Task<Result<T>> FailAsync(int statusCode = (int)HttpStatusCode.OK, bool succeeded = true)
            => Task.FromResult(Fail(statusCode, succeeded));

        public new static Task<Result<T>> FailAsync(string message = "Thực hiện yêu cầu không thành công !", bool succeeded = true, int statusCode = (int)HttpStatusCode.BadRequest)
            => Task.FromResult(Fail(message, succeeded, statusCode));

        public new static Task<Result<T>> FailAsync(List<string> messages, bool succeeded = true, int statusCode = (int)HttpStatusCode.BadRequest)
            => Task.FromResult(Fail(messages, succeeded, statusCode));

        public static Task<Result<T>> FailAsync(T data, string message = "Thực hiện yêu cầu không thành công !", bool succeeded = true, int statusCode = (int)HttpStatusCode.BadRequest)
            => Task.FromResult(Fail(data, message, succeeded, statusCode));

        public static Task<Result<T>> FailAsync(T data, List<string> messages, bool succeeded = true, int statusCode = (int)HttpStatusCode.BadRequest)
            => Task.FromResult(Fail(data, messages, succeeded, statusCode));

        #endregion ::::::::::::: FAIL :::::::::::::

        #region ::::::::::::: SUCCESS :::::::::::::

        public new static Result<T> Success(int statusCode = (int)HttpStatusCode.OK)
            => new()
            { Succeeded = true, Code = statusCode, Status = statusCode.ConvertHttpStatusCodeCodeByName() };

        public new static Result<T> Success(string message = "Thực hiện yêu cầu thành công !", int statusCode = (int)HttpStatusCode.OK)
            => new()
            {
                Succeeded = true,
                Code = statusCode,
                Messages = [message],
                Status = statusCode.ConvertHttpStatusCodeCodeByName()
            };

        public new static Result<T> Success(List<string> messages, int statusCode = (int)HttpStatusCode.OK)
            => new()
            {
                Succeeded = true,
                Code = statusCode,
                Messages = messages,
                Status = statusCode.ConvertHttpStatusCodeCodeByName()
            };

        public static Result<T> Success(T data, string message = "Thực hiện yêu cầu thành công !", int statusCode = (int)HttpStatusCode.OK)
          => new()
          {
              Data = data,
              Succeeded = true,
              Code = statusCode,
              Messages = [message],
              Status = statusCode.ConvertHttpStatusCodeCodeByName()
          };

        public static Result<T> Success(T data, List<string> messages, int statusCode = (int)HttpStatusCode.OK)
            => new()
            {
                Data = data,
                Succeeded = true,
                Code = statusCode,
                Messages = messages,
                Status = statusCode.ConvertHttpStatusCodeCodeByName()
            };

        public new static Task<Result<T>> SuccessAsync(int statusCode = (int)HttpStatusCode.OK)
            => Task.FromResult(Success(statusCode));

        public new static Task<Result<T>> SuccessAsync(string message = "Thực hiện yêu cầu thành công !", int statusCode = (int)HttpStatusCode.OK)
            => Task.FromResult(Success(message, statusCode));

        public new static Task<Result<T>> SuccessAsync(List<string> messages, int statusCode = (int)HttpStatusCode.OK)
            => Task.FromResult(Success(messages, statusCode));

        public static Task<Result<T>> SuccessAsync(T data, string message = "Thực hiện yêu cầu thành công !", int statusCode = (int)HttpStatusCode.OK)
            => Task.FromResult(Success(data, message, statusCode));

        public static Task<Result<T>> SuccessAsync(T data, List<string> messages, int statusCode = (int)HttpStatusCode.OK)
            => Task.FromResult(Success(data, messages, statusCode));

        #endregion ::::::::::::: SUCCESS :::::::::::::
    }
}