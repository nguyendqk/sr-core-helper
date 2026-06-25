using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.Net;
using System.Text;

namespace FTELSRCore.Infrastructure.Extensions.Helpers.JWTAuthenticationExtensions
{
    public static class JWTBearerExtensions
    {
        public static Action<JwtBearerOptions> AddJWTBearer(ILogger logger, JWTOptions jwtOptions)
        {
            if (EnvironmentExtensions.GetEnvironment() is not EnvironmentExtensions.EProd)
            {
                IdentityModelEventSource.ShowPII = true;
            }

            byte[] secretKey = Encoding.UTF8.GetBytes(jwtOptions!.SecretKey);

            return (bearer) =>
            {
                bearer.SaveToken = true;
                bearer.RequireHttpsMetadata = false;
                bearer.TokenValidationParameters =
                    new TokenValidationParameters()
                    {
                        // Kiểm tra xem token có phát hành từ đúng issuer không
                        ValidateIssuer = true,

                        // Kiểm tra xem token có được phát hành cho đúng audience không
                        ValidateAudience = true,

                        // Kiểm tra hạn sử dụng của token
                        ValidateLifetime = true,

                        // Kiểm tra khóa ký có đúng không
                        ValidateIssuerSigningKey = true,

                        // Issuer hợp lệ (phải khớp với giá trị trong token)
                        ValidIssuer = jwtOptions!.Issuer,

                        // Cho phép lệch thời gian tối đa là 5 phút để tránh lỗi do đồng hồ hệ thống không đồng bộ
                        ClockSkew = TimeSpan.FromMinutes(5),

                        // Audience hợp lệ (phải khớp với giá trị trong token)
                        ValidAudience = jwtOptions!.Audience,

                        // Khóa để xác thực token (phải khớp với khóa đã dùng để ký token)
                        IssuerSigningKey = new SymmetricSecurityKey(secretKey)
                    };
                bearer.Events =
                    new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            if (context.Exception is SecurityTokenExpiredException)
                            {
                                logger.ErrorException(nameof(JWTBearerExtensions), nameof(AddJWTBearer),
                                    message: $"SecurityTokenExpiredException - Path: {context?.Request?.Path} " +
                                    $"- Headers: {JsonConvert.SerializeObject(context?.Request?.Headers?.ToDictionary(h => h.Key, h => h.Value.ToString()))}", e: context.Exception);

                                context.Response.ContentType = "application/json";
                                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;

                                return context.Response.WriteAsJsonAsync(
                                    Result.FailSystem("Thông tin Token được cấp đã hết hạn.", (int)HttpStatusCode.Unauthorized));
                            }
                            else
                            {
                                logger.ErrorException(nameof(JWTBearerExtensions), nameof(AddJWTBearer),
                                    message: $"OnAuthenticationFailed - Path: {context?.Request?.Path} " +
                                    $"- Headers: {JsonConvert.SerializeObject(context?.Request?.Headers?.ToDictionary(h => h.Key, h => h.Value.ToString()))}", e: context.Exception);

                                context.NoResult();
                                context.Response.ContentType = "application/json";
                                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;

                                switch (EnvironmentExtensions.GetEnvironment())
                                {
                                    case EnvironmentExtensions.EProd or EnvironmentExtensions.EStag:
                                        {
                                            return context.Response.WriteAsJsonAsync(
                                                Result.FailSystem("Xử lý đăng nhập để cấp quyền không thành công.", (int)HttpStatusCode.Unauthorized));
                                        }
                                    default:
                                        {
                                            return context.Response.WriteAsJsonAsync(
                                                Result.FailSystem(JsonConvert.SerializeObject(context.Exception), (int)HttpStatusCode.Unauthorized));
                                        }
                                }
                            }
                        },
                        OnChallenge = context =>
                        {
                            context.HandleResponse();

                            if (!context.Response.HasStarted)
                            {
                                logger.Error(nameof(JWTBearerExtensions), nameof(AddJWTBearer),
                                    message: $"OnChallenge - Path: {context?.Request?.Path} " +
                                             $"- Headers: {JsonConvert.SerializeObject(context?.Request?.Headers?.ToDictionary(h => h.Key, h => h.Value.ToString()))}");

                                context.Response.ContentType = "application/json";
                                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;

                                return context.Response.WriteAsJsonAsync(
                                    Result.FailSystem("Yêu cầu chưa được cấp quyền.", (int)HttpStatusCode.Unauthorized));
                            }

                            return Task.CompletedTask;
                        },
                        OnForbidden = context =>
                        {
                            logger.Error(nameof(JWTBearerExtensions), nameof(AddJWTBearer),
                                message: $"OnForbidden - Path: {context?.Request?.Path} - Headers: {JsonConvert.SerializeObject(context?.Request?.Headers)}");

                            context.Response.ContentType = "application/json";
                            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;

                            return context.Response.WriteAsJsonAsync(
                                Result.FailSystem(message: "Yêu cầu không được phép truy cập tài nguyên này.", statusCode: (int)HttpStatusCode.Forbidden));
                        }
                    };
            };
        }
    }

    /// <summary>
    /// Các tuỳ chọn cấu hình cho JWT (JSON Web Token).
    /// </summary>
    ///
    public class JWTOptions
    {
        /// <summary>
        /// Nhà phát hành token (Issuer), thường là tên hệ thống hoặc ứng dụng phát hành token.
        /// </summary>
        ///
        public string Issuer { get; set; }

        /// <summary>
        /// Đối tượng được token phát hành cho (Audience), xác định người dùng hoặc hệ thống nhận token.
        /// </summary>
        ///
        public string Audience { get; set; }

        /// <summary>
        /// Khóa bí mật dùng để ký và xác thực token.
        /// </summary>
        ///
        public string SecretKey { get; set; }

        /// <summary>
        /// Thời gian sống của token (tính bằng phút) trước khi hết hạn.
        /// </summary>
        ///
        public int ExpireMin { get; set; }
    }
}