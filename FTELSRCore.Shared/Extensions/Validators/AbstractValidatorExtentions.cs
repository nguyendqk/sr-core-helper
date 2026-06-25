using FluentValidation;
using FluentValidation.Results;

namespace FTELSRCore.Extensions.Validators
{
    public static class AbstractValidatorExtensions
    {
        /// <summary>
        /// Xác thực mô hình dữ liệu sử dụng Fluent Validation.
        /// Phương thức này sử dụng trình xác thực (validator) để kiểm tra các ràng buộc của mô hình.
        /// </summary>
        /// <typeparam name="TValidator">Loại của trình xác thực, phải kế thừa từ <see cref="AbstractValidator{T}"/>.</typeparam>
        /// <typeparam name="T">Loại mô hình dữ liệu cần xác thực, phải là lớp (class).</typeparam>
        /// <param name="model">Mô hình dữ liệu cần được xác thực.</param>
        /// <returns>
        /// Trả về một tuple gồm hai phần tử:
        /// - <c>IsSuccess</c>: Kết quả xác thực (true nếu mô hình hợp lệ, false nếu không hợp lệ).
        /// - <c>Messages</c>: Danh sách các thông báo lỗi nếu có, hoặc danh sách trống nếu mô hình hợp lệ.
        /// </returns>
        /// <remarks>
        /// Phương thức này thực hiện việc xác thực mô hình sử dụng Fluent Validation. Nếu xác thực thành công,
        /// nó sẽ trả về <c>IsSuccess = true</c> cùng với một danh sách thông báo trống. Ngược lại, nếu có lỗi xác thực,
        /// danh sách các thông báo lỗi sẽ được trả về.
        /// </remarks>
        ///
        public static (bool IsSuccess, List<string> Messages) Validate<TValidator, T>(T model)
            where TValidator : AbstractValidator<T>, new()
            where T : class
        {
            TValidator validator = new();

            ValidationResult result = validator.Validate(model);

            if (result.IsValid is true)
            {
                return (IsSuccess: true, Messages: []);
            }

            IEnumerable<string> messages = [];

            if (result.Errors is not null
                && result.Errors.Count > 0)
            {
                messages = result.Errors.Where(x => !string.IsNullOrWhiteSpace(x.ErrorMessage)).Select(x => x.ErrorMessage);
            }

            return (IsSuccess: false, Messages: [.. messages]);
        }
    }
}