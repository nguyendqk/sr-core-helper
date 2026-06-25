// Ignore Spelling: Validator

using FluentValidation;
using System.Globalization;
using System.Text.RegularExpressions;
using static FTELSRCore.Helpers.ConvertHelpers;

namespace FTELSRCore.Extensions.Validators
{
    public static class ValidatorRequestExtensions
    {
        private const string FormatDate = "yyyy-MM-dd";
        private const string MatchsMatches = "^[^#$%^*<>]+$";
        private const string MatchsMatchesMessage = $"không chứa các kí tự đặc biệt: {MatchsMatches}";

        public static IRuleBuilderOptions<T, string> RegexString<T>(
            this IRuleBuilder<T, string> ruleBuilder, int maximumLength, int minimumLength = 0)
        {
            return ruleBuilder
                .Must(propertyName => !string.IsNullOrWhiteSpace(propertyName))
                .WithMessage("{PropertyName} không được rỗng.")
                .MaximumLength(maximumLength).WithMessage("{PropertyName} độ dài kí tự không lớn hơn " + maximumLength)
                .MinimumLength(minimumLength).WithMessage("{PropertyName} độ dài kí tự tối thiểu " + minimumLength)
                .Matches(MatchsMatches).WithMessage($"{{PropertyName}} {MatchsMatchesMessage}");
        }

        public static IRuleBuilderOptions<T, string> RegexString<T>(
            this IRuleBuilder<T, string> ruleBuilder, string message, int maximumLength, int minimumLength = 0)
        {
            return ruleBuilder
                .Must(propertyName => !string.IsNullOrWhiteSpace(propertyName))
                .WithMessage($"{message} không được rỗng.")
                .MaximumLength(maximumLength).WithMessage($"{message} độ dài kí tự không lớn hơn " + maximumLength)
                .MinimumLength(minimumLength).WithMessage($"{message} độ dài kí tự tối thiểu " + minimumLength)
                .Matches(MatchsMatches).WithMessage($"{message} {MatchsMatchesMessage}");
        }

        public static IRuleBuilderOptions<T, string> RegexString<T>
            (this IRuleBuilder<T, string> ruleBuilder, int maximumLength = 40)
        {
            return ruleBuilder
                .Must(propertyName => !string.IsNullOrWhiteSpace(propertyName))
                .WithMessage("{PropertyName} không được rỗng.")
                .MaximumLength(maximumLength).WithMessage("{PropertyName} độ dài kí tự không lớn hơn " + maximumLength)
                .Matches(MatchsMatches).WithMessage($"{{PropertyName}} {MatchsMatchesMessage}");
        }

        public static IRuleBuilderOptions<T, string> RegexString<T>
            (this IRuleBuilder<T, string> ruleBuilder, string message, int maximumLength = 40)
        {
            return ruleBuilder
                .Must(propertyName => !string.IsNullOrWhiteSpace(propertyName))
                .WithMessage($"{message} không được rỗng")
                .MaximumLength(maximumLength).WithMessage($"{message} độ dài kí tự không lớn hơn " + maximumLength)
                .Matches(MatchsMatches).WithMessage($"{message} {MatchsMatchesMessage}");
        }

        public static IRuleBuilderOptions<T, string> RegexString<T>
        (this IRuleBuilder<T, string> ruleBuilder, string message, string characterNotMatches,
            int maximumLength = 40)
        {
            return ruleBuilder
                .Must(propertyName => !string.IsNullOrWhiteSpace(propertyName))
                .WithMessage($"{message} không được rỗng")
                .MaximumLength(maximumLength).WithMessage($"{message} độ dài kí tự không lớn hơn " + maximumLength)
                .Matches(characterNotMatches).WithMessage($"{message} không chứa các kí tự {characterNotMatches}");
        }

        public static IRuleBuilderOptions<T, string> RegexStringDescription<T>
            (this IRuleBuilder<T, string> ruleBuilder, string message, int maximumLength = 40)
        {
            return ruleBuilder
                .Must(propertyName => !string.IsNullOrWhiteSpace(propertyName))
                .WithMessage($"{message} không được rỗng.")
                .MaximumLength(maximumLength).WithMessage($"{message} độ dài kí tự không lớn hơn " + maximumLength);
        }

        public static IRuleBuilderOptions<T, string> IsXSSPayload<T>
            (this IRuleBuilder<T, string> ruleBuilder, string message)
        {
            return ruleBuilder
                .Must(propertyName =>
                {
                    // Regular expression to check payload XSS
                    string xssPattern = @"<script\b[^>]*>.*?</script>|on\w+="".*?""|href=""javascript:.*?""|(<|>)";

                    if (string.IsNullOrWhiteSpace(propertyName))
                    {
                        return true;
                    }

                    return !Regex.IsMatch(propertyName, xssPattern, RegexOptions.IgnoreCase);
                }).WithMessage($"{message} không hợp lệ vì chứa những ký tự mã HTML/JavaScript độc hại.");
        }

        public static IRuleBuilderOptions<T, DateTime?> IsDateTime<T>
            (this IRuleBuilder<T, DateTime?> ruleBuilder)
        {
            return ruleBuilder
                .Must(propertyName =>
                {
                    if (propertyName is null) return false;

                    if (DateTime.TryParse(propertyName.ToString(), CultureInfo.InvariantCulture, out DateTime dateTime))
                    {
                        // Ensure the date is valid for SQL DateTime range
                        return dateTime >= new DateTime(1753, 1, 1, 0, 0, 0, DateTimeKind.Utc) &&
                               dateTime <= new DateTime(9999, 12, 31, 23, 59, 59, DateTimeKind.Utc);
                    }

                    return false;
                })
                .WithMessage($"{{PropertyName}} định dạng thời gian không hợp lệ.")
                .NotNull().WithMessage($"{{PropertyName}} không được rỗng.");
        }

        public static IRuleBuilderOptions<T, DateTime?> IsDateTime<T>
            (this IRuleBuilder<T, DateTime?> ruleBuilder, string message)
        {
            return ruleBuilder
                .Must(propertyName =>
                {
                    if (propertyName is null) return false;

                    if (DateTime.TryParse(propertyName.ToString(), CultureInfo.InvariantCulture, out DateTime dateTime))
                    {
                        // Ensure the date is valid for SQL DateTime range
                        return dateTime >= new DateTime(1753, 1, 1, 0, 0, 0, DateTimeKind.Utc) &&
                               dateTime <= new DateTime(9999, 12, 31, 23, 59, 59, DateTimeKind.Utc);
                    }

                    return false;
                })
                .WithMessage($"{message} định dạng thời gian không hợp lệ.")
                .NotNull().WithMessage($"{message} không được rỗng.");
        }

        public static IRuleBuilderOptions<T, DateTime> DateTimeGreaterThanCurrentDate<T>
            (this IRuleBuilder<T, DateTime> ruleBuilder, string message)
        {
            return ruleBuilder
                .Must(propertyName => propertyName <= CommonBaseConstant.DateTimeUtc())
                .WithMessage($"{message} lớn hơn hiện tại.")
                .NotNull().WithMessage($"{message} không được rỗng.");
        }

        public static IRuleBuilderOptions<T, string> IsStringToDate<T>
            (this IRuleBuilder<T, string> ruleBuilder, string message, string format = FormatDate)
        {
            return ruleBuilder
                .Must(propertyName => DateTime.TryParseExact(propertyName, format, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out DateTime _))
                .WithMessage($"{message} định dạng thời gian không hợp lệ.")
                .Must(x => !string.IsNullOrWhiteSpace(x)).WithMessage($"{message} không được rỗng.");
        }

        public static IRuleBuilderOptions<T, string> IsStringToDate<T>
            (this IRuleBuilder<T, string> ruleBuilder, string format = FormatDate)
        {
            return ruleBuilder
                .Must(propertyName => DateTime.TryParseExact(propertyName, format, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out DateTime _))
                .WithMessage($"{{PropertyName}} định dạng thời gian không hợp lệ.")
                .Must(x => !string.IsNullOrWhiteSpace(x)).WithMessage($"{{PropertyName}} không được rỗng.");
        }

        public static IRuleBuilderOptions<T, TType> IsCustomer<T, TType>
            (this IRuleBuilder<T, TType> ruleBuilder, Func<TType, bool> predicate, string message)
        {
            return ruleBuilder
                .Must(predicate).WithMessage(message);
        }

        public static IRuleBuilderOptions<T, string> IsNumberPhone<T>(this IRuleBuilder<T, string> ruleBuilder,
            string message, int maximumLength = 20, int minimumLength = 10)
        {
            return ruleBuilder
                 .Must(x => !string.IsNullOrWhiteSpace(x)).WithMessage($"{message} không được rỗng.")
                 .Must(propertyName =>
                 {
                     if (propertyName is null) return false;

                     var result = VietnamesePhoneValidator.Validate(number: propertyName);

                     return result.Valid;
                 }).WithMessage($"{message} không hợp lệ. Vui lòng nhập đúng định dạng số điện thoại.")
                .MinimumLength(minimumLength).WithMessage($"{message} phải có ít nhất {minimumLength} chữ số.")
                .MaximumLength(maximumLength).WithMessage($"{message} không được vượt quá {maximumLength} chữ số.");
        }

        public static IRuleBuilderOptions<T, string> IsNumber<T>(this IRuleBuilder<T, string> ruleBuilder,
            string message)
        {
            return ruleBuilder
                .Must(x => !string.IsNullOrWhiteSpace(x)).WithMessage($"{message} không được rỗng.")
                .Matches(@"^\d+(\.\d+)?$")
                .WithMessage($"{message} phải là ký tự số.");
        }
    }
}