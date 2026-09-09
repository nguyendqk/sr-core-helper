using FTELSRCore.Models.Https;
using FTELSRCore.Models.Https.ResponseModel;
using System.Net;

namespace FTELSRCore.Utilizes
{
    public static class HandlerResponseHttpClientUtilizes
    {
        /// <summary>
        /// Thông tin response cần cho hệ thống SR mới.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <typeparam name="TError"></typeparam>
        /// <param name="value"></param>
        /// <returns>
        /// <see cref="Result{TResult}.Fail(string, bool, int, ResultFTelCoreErrorModel)"/> khi <c>errorModel.Succeeded == false</c> (status &gt; 400)
        /// hoặc <c>result</c> null (message/statusCode lấy từ <c>errorModel</c>);
        /// <see cref="Result{TResult}.Fail(List{string}, bool, int, ResultFTelCoreErrorModel)"/> khi <c>result.Data == null</c>
        /// (messages/statusCode lấy nguyên trạng từ <c>result.Messages</c>/<c>result.Code</c>);
        /// ngược lại <see cref="Result{TResult}.Succeed(TResult, List{string}, bool, int)"/> với statusCode lấy từ <c>result.Code</c>
        /// (không hardcode <c>200</c> như các method khác trong lớp này) và messages fallback về
        /// "Thực hiện yêu cầu thành công" khi <c>result.Messages</c> rỗng/null.
        /// </returns>
        public static Result<TResult> SRBaseResultHttpClient<TResult, TError>(
            this (SRBaseResponseModel<TResult> result, TError errorModel) value)
            where TError : ErrorModel
            where TResult : notnull
        {
            if (value.errorModel is { Succeeded: false, Code: > (int)HttpStatusCode.BadRequest } || value.result is null)
            {
                return Result<TResult>.Fail(message: value.errorModel.Message, statusCode: value.errorModel.Code, succeeded: false);
            }

            if (value.result is { Data: null })
            {
                return Result<TResult>.Fail(statusCode: value.result.Code, messages: value.result.Messages, succeeded: false);
            }

            return Result<TResult>.Succeed(data: value.result.Data,
                                           messages: value.result?.Messages is not null
                                                    && !value.result.Messages.IsNullOrEmpty() ? value.result.Messages : ["Thực hiện yêu cầu thành công"],
                                           statusCode: value.result.Code);
        }

        /// <summary>
        /// Thông tin response cần cho hệ thống SR cũ.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <typeparam name="TError"></typeparam>
        /// <param name="value"></param>
        /// <returns>
        /// <see cref="Result{TResult}.Fail(string, bool, int, ResultFTelCoreErrorModel)"/> khi <c>errorModel.Succeeded == false</c> (status &gt; 400)
        /// hoặc <c>result</c> null (message/statusCode lấy từ <c>errorModel</c>);
        /// <see cref="Result{TResult}.Fail(List{string}, bool, int, ResultFTelCoreErrorModel)"/> khi <c>result.Data == null</c>
        /// (statusCode lấy từ <c>result.Code</c>, message duy nhất lấy từ <c>result.Message</c>);
        /// ngược lại <see cref="Result{TResult}.Succeed(TResult, string, bool, int)"/> với statusCode lấy từ <c>result.Code</c>
        /// và message fallback về "Thực hiện thành công" khi <c>result.Message</c> rỗng/khoảng trắng.
        /// </returns>
        public static Result<TResult> SRBaseV1ResultHttpClient<TResult, TError>(
           this (SRBaseV1ResponseModel<TResult> result, TError errorModel) value)
            where TError : ErrorModel where TResult : notnull
        {
            if (value.errorModel is { Succeeded: false, Code: > (int)HttpStatusCode.BadRequest } || value.result is null)
            {
                return Result<TResult>.Fail(message: value.errorModel.Message, succeeded: false, statusCode: value.errorModel.Code);
            }

            if (value.result is { Data: null })
            {
                return Result<TResult>.Fail(statusCode: value.result.Code, messages: [value.result.Message], succeeded: false);
            }

            return Result<TResult>.Succeed(data: value.result.Data, message: !string.IsNullOrWhiteSpace(value.result.Message) ? value.result.Message : "Thực hiện thành công", statusCode: value.result.Code);
        }

        /// <summary>
        /// Thông tin response cần cho hệ thống thanh toán.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <typeparam name="TError"></typeparam>
        /// <param name="value"></param>
        /// <returns>
        /// <see cref="Result{TResult}.Fail(string, bool, int, ResultFTelCoreErrorModel)"/> khi <c>errorModel.Succeeded == false</c> (status &gt; 400)
        /// hoặc <c>result</c> null (message/statusCode lấy từ <c>errorModel</c>);
        /// <see cref="Result{TResult}.Fail(List{string}, bool, int, ResultFTelCoreErrorModel)"/> khi <c>result.Data == null</c>
        /// (statusCode lấy từ <c>result.Code</c>, message duy nhất lấy từ <c>result.Message</c>);
        /// ngược lại <see cref="Result{TResult}.Succeed(TResult, string, bool, int)"/> với statusCode lấy từ <c>result.Code</c>
        /// và message fallback về "Thực hiện thành công" khi <c>result.Message</c> rỗng/khoảng trắng.
        /// Cấu trúc hoàn toàn giống <see cref="SRBaseV1ResultHttpClient{TResult, TError}"/>, chỉ khác model response nguồn.
        /// </returns>
        public static Result<TResult> PaymentResultHttpClient<TResult, TError>(
           this (PaymentResponseModel<TResult> result, TError errorModel) value) where TError : ErrorModel
                                                                                  where TResult : notnull
        {
            if (value.errorModel is { Succeeded: false, Code: > (int)HttpStatusCode.BadRequest } || value.result is null)
            {
                return Result<TResult>.Fail(message: value.errorModel.Message, succeeded: false, statusCode: value.errorModel.Code);
            }

            if (value.result is { Data: null })
            {
                return Result<TResult>.Fail(statusCode: value.result.Code, messages: [value.result.Message], succeeded: false);
            }

            return Result<TResult>.Succeed(data: value.result.Data, message: !string.IsNullOrWhiteSpace(value.result.Message) ? value.result.Message : "Thực hiện thành công", statusCode: value.result.Code);
        }

        /// <summary>
        /// Thông tin response cần cho hệ thống Reinvent.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <typeparam name="TError"></typeparam>
        /// <param name="value"></param>
        /// <returns>
        /// <see cref="Result{TResult}.Fail(string, bool, int, ResultFTelCoreErrorModel)"/> khi <c>errorModel.Succeeded == false</c> (status &gt; 400)
        /// hoặc <c>result</c> null/<c>Data</c> null (message lấy từ <c>result.Message</c> nếu <c>result</c> khác null,
        /// ngược lại "Không tìm thấy thông tin"; statusCode cố định <c>400</c>);
        /// ngược lại <see cref="Result{TResult}.Succeed(TResult, string, bool, int)"/> với statusCode cố định <c>200</c>.
        /// Lưu ý: các field <c>ErrorData</c>, <c>ExceptionMessage</c>, <c>ClientRequestId</c>, <c>Description</c> của
        /// <see cref="CoreReinventResponseModel{TData}"/> không được đọc ở bất kỳ nhánh nào của method này.
        /// </returns>
        public static Result<TResult> CoreReinventResultHttpClient<TResult, TError>(
            this (CoreReinventResponseModel<TResult> result, TError errorModel) value) where TError : ErrorModel
                                                                                    where TResult : notnull
        {
            if (value.errorModel is { Succeeded: false, Code: > (int)HttpStatusCode.BadRequest })
            {
                return Result<TResult>.Fail(message: value.errorModel.Message, succeeded: false, statusCode: value.errorModel.Code);
            }

            if (value.result is null || value.result is { Data: null })
            {
                return Result<TResult>.Fail(message: value.result?.Message ?? "Không tìm thấy thông tin", succeeded: false, statusCode: (int)HttpStatusCode.BadRequest);
            }

            return Result<TResult>.Succeed(data: value.result.Data, message: "Thực hiện thành công", statusCode: (int)HttpStatusCode.OK);
        }

        /// <summary>
        /// Thông tin response cần cho hệ thống nguồn Internet.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <typeparam name="TError"></typeparam>
        /// <param name="value"></param>
        /// <returns>
        /// <see cref="Result{TResult}.Fail(string, bool, int, ResultFTelCoreErrorModel)"/> khi <c>errorModel.Succeeded == false</c> (status &gt; 400)
        /// hoặc <c>result</c> null/<c>Data</c> null (message lấy từ <c>result.Message</c> nếu <c>result</c> khác null,
        /// ngược lại "Không tìm thấy thông tin"; statusCode cố định <c>400</c>);
        /// ngược lại <see cref="Result{TResult}.Succeed(TResult, string, bool, int)"/> với statusCode cố định <c>200</c>.
        /// Cấu trúc giống hệt <see cref="CoreReinventResultHttpClient{TResult, TError}"/>; field <c>Error</c> (mã lỗi số)
        /// của <see cref="ProductInternetResponseModel{TData}"/> không được đọc ở bất kỳ nhánh nào.
        /// </returns>
        public static Result<TResult> ProductInternetResultHttpClient<TResult, TError>(
           this (ProductInternetResponseModel<TResult> result, TError errorModel) value) where TError : ErrorModel
                                                                                         where TResult : notnull
        {
            if (value.errorModel is { Succeeded: false, Code: > (int)HttpStatusCode.BadRequest })
            {
                return Result<TResult>.Fail(message: value.errorModel.Message, succeeded: false, statusCode: value.errorModel.Code);
            }

            if (value.result is null || value.result is { Data: null })
            {
                return Result<TResult>.Fail(message: value.result?.Message ?? "Không tìm thấy thông tin", succeeded: false, statusCode: (int)HttpStatusCode.BadRequest);
            }

            return Result<TResult>.Succeed(data: value.result.Data, message: "Thực hiện thành công", statusCode: (int)HttpStatusCode.OK);
        }

        /// <summary>
        /// Thông tin response cần cho hệ thống Ticket Support.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <typeparam name="TError"></typeparam>
        /// <param name="value"></param>
        /// <returns>
        /// <see cref="Result{TResult}.Fail(string, bool, int, ResultFTelCoreErrorModel)"/> khi <c>errorModel.Succeeded == false</c> (status &gt; 400)
        /// hoặc <c>result</c> null/<c>Data</c> null (message lấy từ <c>result.ErrorDescription</c>, statusCode cố định <c>400</c>);
        /// ngược lại <see cref="Result{TResult}.Succeed(TResult, string, bool, int)"/> với statusCode cố định <c>200</c>.
        /// </returns>
        public static Result<TResult> TicketSupportResultHttpClient<TResult, TError>(
           this (TicketSupportResponseModel<TResult> result, TError errorModel) value) where TError : ErrorModel
                                                                                       where TResult : notnull
        {
            if (value.errorModel is { Succeeded: false, Code: > (int)HttpStatusCode.BadRequest })
            {
                return Result<TResult>.Fail(message: value.errorModel.Message, succeeded: false, statusCode: value.errorModel.Code);
            }

            if (value.result is null || value.result is { Data: null })
            {
                return Result<TResult>.Fail(message: value.result?.ErrorDescription ?? "Không tìm thấy thông tin", succeeded: false, statusCode: (int)HttpStatusCode.BadRequest);
            }

            return Result<TResult>.Succeed(data: value.result.Data, message: "Thực hiện thành công", statusCode: (int)HttpStatusCode.OK);
        }

        /// <summary>
        /// Thông tin response tổng quát dùng khi hệ thống nguồn không có wrapper riêng
        /// (chỉ có field <c>Data</c>, không có code/message/status kèm theo).
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <typeparam name="TError"></typeparam>
        /// <param name="value"></param>
        /// <returns>
        /// <see cref="Result{TResult}.Fail(string, bool, int, ResultFTelCoreErrorModel)"/> khi <c>errorModel.Succeeded == false</c> (status &gt; 400)
        /// hoặc <c>result</c> null/<c>Data</c> null (message cố định "Không tìm thấy thông tin", statusCode cố định <c>400</c>);
        /// ngược lại <see cref="Result{TResult}.Succeed(TResult, string, bool, int)"/> với statusCode cố định <c>200</c>.
        /// </returns>
        public static Result<TResult> BaseResultHttpClient<TResult, TError>(
            this (BaseResponseModel<TResult> result, TError errorModel) value) where TError : ErrorModel
                                                                               where TResult : notnull
        {
            if (value.errorModel is { Succeeded: false, Code: > (int)HttpStatusCode.BadRequest })
            {
                return Result<TResult>.Fail(message: value.errorModel.Message, succeeded: false, statusCode: value.errorModel.Code);
            }

            if (value.result is null || value.result is { Data: null })
            {
                return Result<TResult>.Fail(message: "Không tìm thấy thông tin", succeeded: false, statusCode: (int)HttpStatusCode.BadRequest);
            }

            return Result<TResult>.Succeed(data: value.result.Data, message: "Thực hiện thành công", statusCode: (int)HttpStatusCode.OK);
        }

        /// <summary>
        /// Thông tin response cần cho hệ thống ACS.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <typeparam name="TError"></typeparam>
        /// <param name="value"></param>
        /// <returns>
        /// <see cref="Result{TResult}.Fail(string, bool, int, ResultFTelCoreErrorModel)"/> khi <c>errorModel.Succeeded == false</c> (status &gt; 400)
        /// hoặc <c>result</c> null/<c>Data</c> null/<c>result.Code != 200</c> (message lấy từ <c>result.Message</c>, statusCode cố định <c>400</c>);
        /// ngược lại <see cref="Result{TResult}.Succeed(TResult, string, bool, int)"/> với statusCode cố định <c>200</c>.
        /// Đây là hệ thống nguồn duy nhất trong lớp này bắt buộc kiểm tra thêm <c>result.Code == 200</c> ngoài <c>Data != null</c>.
        /// </returns>
        public static Result<TResult> ACSResultHttpClient<TResult, TError>(
            this (ACSResponseModel<TResult> result, TError errorModel) value)
            where TError : ErrorModel
            where TResult : notnull
        {
            if (value.errorModel is { Succeeded: false, Code: > (int)HttpStatusCode.BadRequest })
            {
                return Result<TResult>.Fail(message: value.errorModel.Message, succeeded: false, statusCode: value.errorModel.Code);
            }

            if (value.result is null || value.result is { Data: null } or { Code: not (int)HttpStatusCode.OK })
            {
                return Result<TResult>.Fail(message: value.result?.Message ?? "Không tìm thấy thông tin.", succeeded: false, statusCode: (int)HttpStatusCode.BadRequest);
            }

            return Result<TResult>.Succeed(data: value.result.Data, message: value.result?.Message ?? "Thực hiện thành công", statusCode: (int)HttpStatusCode.OK);
        }

        /// <summary>
        /// Thông tin response cần cho hệ thống Inside.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <typeparam name="TError"></typeparam>
        /// <param name="value"></param>
        /// <returns>
        /// <see cref="Result{TResult}.Fail(string, bool, int, ResultFTelCoreErrorModel)"/> khi <c>errorModel.Succeeded == false</c> (status &gt; 400)
        /// hoặc <c>result</c> null/<c>Data</c> null (message cố định "Không tìm thấy thông tin.", statusCode cố định <c>400</c>);
        /// ngược lại <see cref="Result{TResult}.Succeed(TResult, string, bool, int)"/> với statusCode cố định <c>200</c>.
        /// Lưu ý: <see cref="InsideResponseModel{TData}"/> có cả field <c>Data</c> và <c>Result</c>, nhưng ở đây chỉ
        /// <c>Data</c> được kiểm tra/sử dụng — field <c>Result</c> không được đọc bởi extension method này.
        /// </returns>
        public static Result<TResult> InsideResultHttpClient<TResult, TError>(
            this (InsideResponseModel<TResult> result, TError errorModel) value)
            where TError : ErrorModel
            where TResult : notnull
        {
            if (value.errorModel is { Succeeded: false, Code: > (int)HttpStatusCode.BadRequest })
            {
                return Result<TResult>.Fail(message: value.errorModel.Message, succeeded: false, statusCode: value.errorModel.Code);
            }

            if (value.result is null || value.result is { Data: null })
            {
                return Result<TResult>.Fail(message: "Không tìm thấy thông tin.", succeeded: false, statusCode: (int)HttpStatusCode.BadRequest);
            }

            return Result<TResult>.Succeed(data: value.result.Data, message: "Thực hiện thành công", statusCode: (int)HttpStatusCode.OK);
        }

        /// <summary>
        /// Thông tin response cần cho hệ thống đối tác sản phẩm (Product Partner).
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <typeparam name="TError"></typeparam>
        /// <param name="value"></param>
        /// <returns>
        /// <see cref="Result{TResult}.Fail(string, bool, int, ResultFTelCoreErrorModel)"/> khi <c>errorModel.Succeeded == false</c> (status &gt; 400)
        /// hoặc <c>result</c> null (message lấy từ <c>errorModel.Message</c>, statusCode lấy từ <c>errorModel.Code</c>);
        /// khi <c>result.Data == null</c> trả <c>Fail</c> với statusCode cố định <c>502 (BadGateway)</c> và <c>succeeded</c> lấy nguyên trạng từ <c>result.Success</c>
        /// (khác các method còn lại — nơi <c>succeeded</c> luôn cố định <c>false</c> ở nhánh lỗi);
        /// ngược lại <see cref="Result{TResult}.Succeed(TResult, string, bool, int)"/> với statusCode cố định <c>200</c>.
        /// </returns>
        public static Result<TResult> ProductPartnerResultHttpClient<TResult, TError>(
            this (ProductPartnerResponseModel<TResult> result, TError errorModel) value)
            where TError : ErrorModel
            where TResult : notnull
        {
            if (value.errorModel is { Succeeded: false, Code: > (int)HttpStatusCode.BadRequest } || value.result is null)
            {
                return Result<TResult>.Fail(message: value.errorModel.Message, succeeded: false, statusCode: value.errorModel.Code);
            }

            if (value.result is { Data: null })
            {
                return Result<TResult>.Fail(statusCode: (int)HttpStatusCode.BadGateway, messages: [value.result.Message], succeeded: value.result.Success);
            }

            return Result<TResult>.Succeed(data: value.result.Data, message: "Thực hiện thành công", statusCode: (int)HttpStatusCode.OK);
        }

        /// <summary>
        /// Thông tin response cần cho hệ thống Loyalty (khách hàng thân thiết).
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <typeparam name="TError"></typeparam>
        /// <param name="value"></param>
        /// <returns>
        /// <see cref="Result{TResult}.Fail(string, bool, int, ResultFTelCoreErrorModel)"/> khi <c>errorModel.Succeeded == false</c> (status &gt; 400)
        /// hoặc <c>result</c> null/<c>Data</c> null/<c>result.Success == false</c> (message cố định "Không tìm thấy thông tin.", statusCode cố định <c>400</c>);
        /// ngược lại <see cref="Result{TResult}.Succeed(TResult, string, bool, int)"/> với statusCode cố định <c>200</c>.
        /// Đây là 1 trong 2 method (cùng <see cref="ACSResultHttpClient{TResult, TError}"/>) kiểm tra thêm cờ trạng thái
        /// nội tại của response (<c>Success</c>) chứ không chỉ dựa vào <c>Data != null</c>.
        /// </returns>
        public static Result<TResult> LoyaltyResultHttpClient<TResult, TError>(
            this (LoyaltyResponseModel<TResult> result, TError errorModel) value)
            where TError : ErrorModel
            where TResult : notnull
        {
            if (value.errorModel is { Succeeded: false, Code: > (int)HttpStatusCode.BadRequest })
            {
                return Result<TResult>.Fail(message: value.errorModel.Message, succeeded: false, statusCode: value.errorModel.Code);
            }

            if (value.result is null || value.result is { Data: null } || value.result is { Success: false })
            {
                return Result<TResult>.Fail(message: "Không tìm thấy thông tin.", succeeded: false, statusCode: (int)HttpStatusCode.BadRequest);
            }

            return Result<TResult>.Succeed(data: value.result.Data, message: "Thực hiện thành công", statusCode: (int)HttpStatusCode.OK);
        }

        /// <summary>
        /// Thông tin response cần cho hệ thống Transaction.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <typeparam name="TError"></typeparam>
        /// <param name="value"></param>
        /// <returns>
        /// <see cref="Result{TResult}.Fail(string, bool, int, ResultFTelCoreErrorModel)"/> khi <c>errorModel.Succeeded == false</c> (status &gt; 400)
        /// hoặc <c>result</c> null/<c>Data</c> null (message cố định "Không tìm thấy thông tin.", statusCode cố định <c>400</c>);
        /// ngược lại <see cref="Result{TResult}.Succeed(TResult, string, bool, int)"/> với statusCode cố định <c>200</c>.
        /// Lưu ý: <see cref="TransactionResponseModel{TData}.Title"/> không được đọc ở bất kỳ nhánh nào của method này.
        /// </returns>
        public static Result<TResult> TransactionResultHttpClient<TResult, TError>(
            this (TransactionResponseModel<TResult> result, TError errorModel) value)
            where TError : ErrorModel
            where TResult : notnull
        {
            if (value.errorModel is { Succeeded: false, Code: > (int)HttpStatusCode.BadRequest })
            {
                return Result<TResult>.Fail(message: value.errorModel.Message, succeeded: false, statusCode: value.errorModel.Code);
            }

            if (value.result is null || value.result is { Data: null })
            {
                return Result<TResult>.Fail(message: "Không tìm thấy thông tin.", succeeded: false, statusCode: (int)HttpStatusCode.BadRequest);
            }

            return Result<TResult>.Succeed(data: value.result.Data, message: "Thực hiện thành công", statusCode: (int)HttpStatusCode.OK);
        }

        /// <summary>
        /// Thông tin response cần cho hệ thống CPE (thiết bị đầu cuối khách hàng).
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <typeparam name="TError"></typeparam>
        /// <param name="value"></param>
        /// <returns>
        /// <see cref="Result{TResult}.Fail(string, bool, int, ResultFTelCoreErrorModel)"/> khi <c>errorModel.Succeeded == false</c> (status &gt; 400)
        /// hoặc <c>result</c> null/<c>Data</c> null (message lấy từ <c>result.Message</c> nếu khác rỗng, ngược lại "Không tìm thấy thông tin", statusCode cố định <c>400</c>);
        /// ngược lại <see cref="Result{TResult}.Succeed(TResult, string, bool, int)"/> với statusCode cố định <c>200</c>.
        /// </returns>
        /// <remarks>
        /// Khác với các method còn lại, nhánh thành công truyền thẳng <c>value.result.Message</c> làm message mà
        /// KHÔNG fallback sang chuỗi mặc định khi rỗng/null — nếu hệ thống CPE trả <c>message</c> rỗng ở response 2xx,
        /// <see cref="Wrappers.Result{T}.Messages"/> của kết quả thành công sẽ chứa message rỗng/null thay vì
        /// "Thực hiện thành công".
        /// </remarks>
        public static Result<TResult> CPEResultHttpClient<TResult, TError>(
            this (CPEResponseModel<TResult> result, TError errorModel) value)
            where TError : ErrorModel
            where TResult : notnull
        {
            if (value.errorModel is { Succeeded: false, Code: > (int)HttpStatusCode.BadRequest })
            {
                return Result<TResult>.Fail(message: value.errorModel.Message, succeeded: false, statusCode: value.errorModel.Code);
            }

            if (value.result is null || value.result is { Data: null })
            {
                return Result<TResult>.Fail(
                    message: !string.IsNullOrWhiteSpace(value.result.Message) ? value.result.Message : "Không tìm thấy thông tin",
                    succeeded: false, statusCode: (int)HttpStatusCode.BadRequest);
            }

            return Result<TResult>.Succeed(data: value.result.Data, message: value.result.Message, statusCode: (int)HttpStatusCode.OK);
        }
    }
}
