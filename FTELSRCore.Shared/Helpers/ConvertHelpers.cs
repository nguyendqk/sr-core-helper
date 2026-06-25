using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;

namespace FTELSRCore.Helpers
{
    public static class ConvertHelpers
    {
        /// <summary>
        /// Chập nhật mọi giá trị User-Agent từ Header của HttpContext
        /// </summary>
        /// <param name="httpContext"></param>
        /// <returns></returns>
        ///
        public static string GetUserAgent(HttpContext httpContext)
        {
            try
            {
                StringValues userAgent = string.Empty;

                if (httpContext.Request.Headers.TryGetValue("User-Agent", out userAgent))
                {
                    return userAgent.ToString();
                }

                if (httpContext.Request.Headers.TryGetValue("UserAgent", out userAgent))
                {
                    return userAgent.ToString();
                }

                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Hàm xử lý che số điện thạoi
        /// </summary>
        /// <param name="phoneNumber"></param>
        /// <returns></returns>
        /// 
        public static string MaskPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber)
                || VietnamesePhoneValidator.Validate(phoneNumber) is { Valid: false })
            {
                return phoneNumber;
            }

            string lastThree = phoneNumber[^3..];

            string masked = new string('*', phoneNumber.Length - 3);

            return masked + lastThree;
        }

        /// <summary>
        /// Lấy thông tin mô tả (Description) của một giá trị Enum
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        ///
        public static string GetDescriptionInEnum<TEnum>(this TEnum value) where TEnum : struct, System.Enum
        {
            try
            {
                var field = value.GetType().GetField(value.ToString());

                var attr = field?.GetCustomAttribute<DescriptionAttribute>();

                return attr?.Description ?? value.ToString();
            }
            catch
            {
                return value.ToString();
            }
        }

        /// <summary>
        /// Chuyển đổi chuỗi thành giá trị enum nullable.
        /// </summary>
        /// <typeparam name="TEnum">Kiểu enum cần chuyển.</typeparam>
        /// <param name="value">Chuỗi đầu vào.</param>
        /// <param name="ignoreCase">Có bỏ qua phân biệt chữ hoa/chữ thường không (mặc định: true).</param>
        /// <returns>Giá trị enum nếu chuyển đổi thành công, ngược lại là null.</returns>
        ///
        public static TEnum? ConvertEnum<TEnum>(this string value, bool ignoreCase = true) where TEnum : struct, System.Enum
        {
            return System.Enum.TryParse(value, ignoreCase, out TEnum result) ? result : null;
        }

        /// <summary>
        /// Lấy giá trị Enum nhỏ nhất trong danh sách.
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="items"></param>
        /// <returns></returns>
        ///
        public static TEnum GetMinEnumValue<TEnum>(params TEnum[] items)
        {
            return items.OrderBy((TEnum e) => Convert.ToInt32(e)).First();
        }

        /// <summary>
        /// Chuyển đổi mã số HTTP status code thành tên tương ứng trong enum <see cref="HttpStatusCode"/>.
        /// </summary>
        /// <param name="httpStatusCode">Giá trị mã HTTP status code (ví dụ: 200, 404).</param>
        /// <returns>
        /// Tên tương ứng trong enum <see cref="HttpStatusCode"/> (ví dụ: "OK", "NotFound");
        /// nếu không tìm thấy thì trả về chuỗi rỗng.
        /// </returns>
        ///
        public static string ConvertHttpStatusCodeCodeByName(this int httpStatusCode)
        {
            return System.Enum.GetName(typeof(HttpStatusCode), httpStatusCode) ?? string.Empty;
        }

        /// <summary>
        /// Loại bỏ dấu tiếng Việt ra khỏi chuỗi.
        /// </summary>
        /// <param name="text">Chuỗi tiếng Việt có dấu.</param>
        /// <returns>Chuỗi không dấu.</returns>
        ///
        public static string UnsignViet(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            return new Regex(@"\p{M}")
                 .Replace(text.Normalize(NormalizationForm.FormD), string.Empty)
                 .Normalize(NormalizationForm.FormC);
        }

        /// <summary>
        /// Loại bỏ dấu (diacritics) khỏi chuỗi, dùng cho Unicode nói chung và hỗ trợ tiếng Việt.
        /// </summary>
        /// <param name="text">Chuỗi đầu vào.</param>
        /// <returns>Chuỗi không dấu.</returns>
        ///
        public static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return text;
            }

            // Xử lý Đ/đ - ký tự đặc biệt duy nhất của tiếng Việt
            text = text.Replace("Đ", "D").Replace("đ", "d");

            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }

        /// <summary>
        ///  Viết hoa chữ cái đầu và giữ nguyên phần còn lại
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        ///
        public static string CapitalizeFirstLetter(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            return char.ToUpper(input[default]) + input[1..];
        }

        /// <summary>
        /// Lấy dữ liệu từ ClaimsPrincipal dựa trên loại claim.
        /// </summary>
        /// <param name="claimsPrincipal">Đối tượng chứa claims.</param>
        /// <param name="claimType">Loại claim cần lấy.</param>
        /// <param name="setDataDefault"></param>
        /// <returns>Giá trị claim nếu tìm thấy, ngược lại là <paramref name="setDataDefault"/>.</returns>
        ///
        public static string ConvertClaimsPrincipalToData(
            ClaimsPrincipal claimsPrincipal, string claimType, string setDataDefault = "")
        {
            try
            {
                if (claimsPrincipal is null && string.IsNullOrWhiteSpace(claimType))
                {
                    return setDataDefault;
                }

                return claimsPrincipal?.FindFirst(claimType)?.Value?.ToString().Trim() ?? setDataDefault;
            }
            catch
            {
                return setDataDefault;
            }
        }

        /// <summary>
        /// Lấy giá trị Description của giá trị const
        /// | EX: [Description("Chưa xử lý")] public const int UnProcessed = 102;
        /// </summary>
        /// <param name="containingClass">EX: typeof(ConsCodeDetail.Source)</param>
        /// <param name="member">EX: nameof(ConsCodeDetail.Source.SR)</param>
        /// <returns>string</returns>
        ///
        public static string DescriptionForProperty(Type containingClass, object member)
        {
            try
            {
                if (containingClass is null || member is null)
                {
                    return string.Empty;
                }

                string nameProperty = HaveValueInClassConstants(containingClass, member);

                if (nameProperty is null)
                {
                    return string.Empty;
                }

                PropertyInfo propertyInfo = containingClass.GetProperty(nameProperty);

                if (string.IsNullOrWhiteSpace(nameProperty))
                {
                    return propertyInfo?.Name ?? string.Empty;
                }

                FieldInfo fieldInfo = containingClass.GetField(nameProperty);

                if (fieldInfo is not null)
                {
                    DescriptionAttribute descriptionAttribute =
                        (DescriptionAttribute)Attribute.GetCustomAttribute(fieldInfo, typeof(DescriptionAttribute));

                    if (descriptionAttribute is not null)
                    {
                        return descriptionAttribute.Description;
                    }
                }

                return propertyInfo?.Name ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }

            #region --------------- HaveValueInClassConstants ---------------

            static string HaveValueInClassConstants(Type classType, object value)
            {
                try
                {
                    FieldInfo[] fields = classType.GetFields(BindingFlags.Public | BindingFlags.Static);

                    foreach (FieldInfo field in fields)
                    {
                        if (field.GetValue(null).ToString() == value.ToString())
                        {
                            return field.Name;
                        }
                    }

                    return null;
                }
                catch
                {
                    return null;
                }
            }

            #endregion --------------- HaveValueInClassConstants ---------------
        }

        private static readonly string[] AllDateFormats =
        [
            // ISO 8601 / round-trip (K xử lý cả 'Z' và offset)
            "yyyy-MM-ddTHH:mm:ss.FFFFFFFK",
            "yyyy-MM-ddTHH:mm:ssK",
            "yyyy-MM-ddTHH:mm:ss.FFFFFFF",
            "yyyy-MM-ddTHH:mm:ss",

            // SQL / datetime thường gặp
            "yyyy-MM-dd HH:mm:ss.FFFFFFF",
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-dd HH:mm",

            // chỉ ngày
            "yyyy-MM-dd",
            "yyyy/MM/dd",
            "yyyyMMdd",

            // ngày-trước (VN) — 'd'/'M' nhận cả 1 và 2 chữ số
            "d/M/yyyy",
            "d-M-yyyy",

            // tháng-trước (US) — đặt sau
            "M/d/yyyy",
        ];

        /// <summary>
        /// Chuyển chuỗi sang DateTime, thử lần lượt theo format chỉ định,
        /// danh sách format chuẩn, rồi fallback parse tổng quát.
        /// </summary>
        /// <param name="dateInput">Chuỗi ngày cần parse.</param>
        /// <param name="format">Format cụ thể; null thì dùng AllDateFormats.</param>
        /// <returns>DateTime nếu parse được, ngược lại null.</returns>
        /// 
        public static DateTime? ConvertStringToDateTime(this string dateInput,
            string format = null,
            IFormatProvider provider = null,
            DateTimeStyles style = DateTimeStyles.RoundtripKind,
            bool fallbackToGeneralParse = true)
        {
            if (string.IsNullOrWhiteSpace(dateInput))
            {
                return null;
            }

            provider ??= CultureInfo.InvariantCulture;

            if (!string.IsNullOrWhiteSpace(format))
            {
                if (DateTime.TryParseExact(dateInput, format, provider, style, out var byFormat))
                {
                    return byFormat;
                }
            }
            else if (DateTime.TryParseExact(dateInput, AllDateFormats, provider, style, out var byList))
            {
                return byList;
            }

            if (fallbackToGeneralParse &&
                DateTime.TryParse(dateInput, provider, style, out var byGeneral))
            {
                return byGeneral;
            }

            return null;
        }

        /// <summary>
        /// Hàm xử lý chuyển đổi số thành chữ tiếng việt.
        /// </summary>
        /// <param name="number"></param>
        /// <param name="useThousand">Dùng từ 'nghìn' thay vì dùng từ 'ngàn'</param>
        /// <returns></returns>
        ///
        public static string NumberToVietnameseWords(decimal number, bool useThousand = true)
        {
            if (number == default) return "Không đồng";

            string[] units = [string.Empty, (useThousand ? "nghìn" : "ngàn"), "triệu", "tỷ"];

            string[] digits = [string.Empty, "một", "hai", "ba", "bốn", "năm", "sáu", "bảy", "tám", "chín"];

            int unitIndex = default;

            string result = string.Empty;

            while (number > 0)
            {
                int groupOfThree = (int)(number % 1000);

                number /= 1000;

                if (groupOfThree > 0)
                {
                    string groupInWords = ConvertGroupToWords(groupOfThree, digits);

                    result = $"{groupInWords} {units[unitIndex]} {result}".Trim();
                }

                unitIndex++;
            }

            return CapitalizeFirstLetter(result.Trim() + " đồng");

            #region --------------- ConvertGroupToWords ---------------

            static string ConvertGroupToWords(int number, string[] digits)
            {
                int hundreds = number / 100;
                int tens = (number % 100) / 10;
                int units = number % 10;

                StringBuilder words = new();

                // Xử lý hàng trăm
                if (hundreds > 0)
                {
                    words.Append(digits[hundreds] + " trăm ");
                }

                // Xử lý hàng chục
                if (tens > 1)
                {
                    words.Append(digits[tens] + " mươi ");
                }
                else if (tens == 1)
                {
                    words.Append("mười ");
                }
                else if (units > 0 && hundreds > 0)
                {
                    words.Append("lẻ ");
                }

                // Xử lý hàng đơn vị
                if (units > 0)
                {
                    if (tens > 1 && units == 1)
                    {
                        words.Append("mốt ");
                    }
                    else if (units == 5 && tens > 0)
                    {
                        words.Append("lăm ");
                    }
                    else
                    {
                        words.Append(digits[units] + " ");
                    }
                }

                return words.ToString().Trim();
            }

            #endregion --------------- ConvertGroupToWords ---------------
        }

        /// <summary>
        /// Vietnam Phone Numbers: Complete Format & Validation Guide: www.sent.dm/resources/vn
        /// </summary>
        ///
        public static class VietnamesePhoneValidator
        {
            // Toll-free numbers (1800 prefix)
            private static readonly Regex TollFreePattern = new(@"^1800\d{4,6}$", RegexOptions.Compiled);

            // Premium rate numbers (1900 prefix)
            private static readonly Regex PremiumPattern = new(@"^1900\d{4,6}$", RegexOptions.Compiled);

            // Geographic landline numbers (2–3 digit area codes + 7–8 digit subscriber numbers)
            private static readonly Regex GeoPattern = new(@"^0([2-9]\d{1,2})\d{7,8}$", RegexOptions.Compiled);

            // Mobile numbers (updated for 2025 operator prefixes including 089 and 081-088 ranges)
            private static readonly Regex MobilePattern = new(@"^0(3[2-9]|5[2689]|7[06-9]|8[1-9]|9[0-4689])\d{7}$", RegexOptions.Compiled);

            /// <summary>
            /// Kết quả xác thực số điện thoại Việt Nam
            /// </summary>
            /// <param name="Valid">Số điện thoại có hợp lệ hay không</param>
            /// <param name="Type">Loại số: "mobile" (di động), "geographic" (cố định), "toll-free" (miễn phí), "premium" (cước cao)</param>
            /// <param name="Carrier">Nhà mạng: "Viettel", "MobiFone", "Vinaphone", "Vietnamobile", "Gmobile" hoặc null nếu không phải số di động</param>
            /// <param name="Error">Thông báo lỗi nếu số không hợp lệ</param>
            ///
            public record ValidationResult(bool Valid, string Type, string Carrier, string Error = null);

            public static ValidationResult Validate(string number)
            {
                //[ Dòng này loại bỏ tất cả ký tự không phải số khỏi chuỗi đầu vào ]
                // Input: "098-765-4321"  → Output: "0987654321"
                // Input: "098 765 4321"  → Output: "0987654321"
                // Input: "(098) 765-4321" → Output: "0987654321"
                // Input: "+84 98 765 4321" → Output: "84987654321"
                string cleaned = Regex.Replace(number, @"\D", "");

                if (GeoPattern.IsMatch(cleaned))
                {
                    return new(true, "geographic", null);
                }

                if (MobilePattern.IsMatch(cleaned))
                {
                    return new(true, "mobile", IdentifyCarrier(cleaned));
                }

                if (TollFreePattern.IsMatch(cleaned))
                {
                    return new(true, "toll-free", null);
                }

                if (PremiumPattern.IsMatch(cleaned))
                {
                    return new(true, "premium", null);
                }

                return new(false, null, null, "Số điện thoại không tồn tại");
            }

            private static string IdentifyCarrier(string number)
            {
                var prefix = number[..3];

                return prefix switch
                {
                    var p when Regex.IsMatch(p, @"^(03[2-9]|09[6-8])") => "Viettel",

                    var p when Regex.IsMatch(p, @"^(070|07[6-9]|089|090|093)") => "MobiFone",

                    var p when Regex.IsMatch(p, @"^(08[1-8]|091|094)") => "Vinaphone",

                    var p when Regex.IsMatch(p, @"^(052|056|058|092)") => "Vietnamobile",

                    var p when Regex.IsMatch(p, @"^(059|099)") => "Gmobile",

                    _ => "Unknown"
                };
            }
        }
    }
}