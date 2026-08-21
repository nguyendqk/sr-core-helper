# Infrastructure - Auth/JWT/Swagger/Slugify

> Nguon:
> - FTELSRCore.Shared/Infrastructure/Extensions/Helpers/AuthorizationPolicyExtensions/AuthorizationPolicyExtensions.cs
> - FTELSRCore.Shared/Infrastructure/Extensions/Helpers/JWTAuthenticationExtensions/JWTBearerExtensions.cs
> - FTELSRCore.Shared/Infrastructure/Extensions/Helpers/SwaggerExtensions/SwaggerExtensions.cs
> - FTELSRCore.Shared/Infrastructure/Extensions/Helpers/SlugifyParameterTransformerExtensions/SlugifyParameterTransformerExtensions.cs
>
> Loai: static class (`AuthorizationPolicyExtensions`, `JWTBearerExtensions`) | class (`SwaggerExtensions` - implement `IConfigureOptions<SwaggerGenOptions>`) | partial class (`SlugifyParameterTransformerExtensions` - implement `IOutboundParameterTransformer`) | model phu tro (`JWTBearerModel` record, `JWTOptions` class)
> Cap nhat theo commit: 89c1ce9

## 1. Tong quan

Bon file nay nam trong tang Infrastructure cua thu vien dung chung `FTELSRCore.Shared`, cung cap cac "factory"/"configurator" duoc service tieu thu (nam ngoai repo `sr-core-helper` nay) goi trong `Program.cs`/`Startup` de:

1. Tao chinh sach uy quyen (authorization policy) dua tren claim `Permissions` (`AuthorizationPolicyExtensions`).
2. Cau hinh middleware xac thuc JWT Bearer cua ASP.NET Core (tham so validate token + 3 event handler tra ve JSON loi chuan hoa) (`JWTBearerExtensions`).
3. Cau hinh sinh tai lieu Swagger/OpenAPI theo tung API version, kem khai bao Bearer security scheme (`SwaggerExtensions`).
4. Chuyen doi ten tham so route (controller/action) sang dang kebab-case khi sinh URL outbound (`SlugifyParameterTransformerExtensions`).

Trong repo `sr-core-helper`, ca 4 kieu nay **chi duoc dinh nghia, khong co noi nao goi (invoke) chung** - da xac nhan bang grep toan repo (khong tim thay lenh goi `AddAuthorizationPolicy`, `AddJWTBearer`, khoi tao `SwaggerExtensions`, hay `SlugifyParameterTransformerExtensions` ben ngoai chinh file dinh nghia). Day la thu vien Shared duoc project khac tham chieu va goi; vi vay tai lieu nay chi mo ta hanh vi cua ham dua tren than ham, khong the kiem chung bang cach truy vet noi goi thuc te trong repo nay.

### 1.1 Pham vi chuc nang

| Lam duoc | Khong lam duoc |
|---|---|
| `AddAuthorizationPolicy` tra ve delegate `Action<AuthorizationPolicyBuilder>` yeu cau user da authenticated (`RequireAuthenticatedUser`) roi kiem tra claim `Permissions` bang `RequireAssertion` | Khong tu dang ky policy vao `AuthorizationOptions` - noi goi phai tu goi `options.AddPolicy(name, AddAuthorizationPolicy(policy))` |
| `AddJWTBearer` tra ve delegate `Action<JwtBearerOptions>` thiet lap day du 5 co validate (`ValidateIssuer/Audience/Lifetime/IssuerSigningKey` + `ClockSkew`) va 3 event handler (`OnAuthenticationFailed`, `OnChallenge`, `OnForbidden`) | Khong tu goi `services.AddAuthentication().AddJwtBearer(...)` - noi goi phai truyen delegate tra ve vao ham do cua ASP.NET Core |
| `SwaggerExtensions.Configure` sinh 1 `SwaggerDoc` cho tung `ApiVersionDescription`, gan tieu de/license/canh bao deprecated, khai bao Bearer security scheme + security requirement toan cuc, bat `EnableAnnotations()` | Viec nap file XML comment cua project thuc te **khong hoat dong dung nhu dong comment mo ta** trong code (xem muc 3, phat hien #2) |
| `TransformOutbound` chuyen 1 gia tri route dang `PascalCase`/`camelCase` sang `kebab-case` roi ve chu thuong | Khong xu ly inbound (khong chuyen kebab-case nguoc lai ten controller/action), khong tach chuoi chu hoa lien tiep (acronym) |

### 1.2 Dependency

| Thanh phan | Muc dich su dung |
|---|---|
| `Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder` | API ASP.NET Core de build policy (`RequireAuthenticatedUser`, `RequireAssertion`) |
| `FTELSRCore.Constants.ClaimTypesConstant.Permissions` (= chuoi `"Permissions"`) | Ten loai claim dung de kiem tra quyen trong `AddAuthorizationPolicy` |
| `Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions` / `JwtBearerEvents` | Cau hinh middleware xac thuc JWT cua ASP.NET Core |
| `Microsoft.IdentityModel.Tokens.TokenValidationParameters` / `SymmetricSecurityKey` | Khai bao tham so validate token va khoa ky doi xung |
| `Microsoft.IdentityModel.Logging.IdentityModelEventSource` | Bat/tat log chua thong tin ca nhan (PII) cua thu vien `Microsoft.IdentityModel` |
| `FTELSRCore.Extensions.EnvironmentExtensions.GetEnvironment()` va cac hang so `ELocal/EDev/EStag/EProd` | Doc bien moi truong `ASPNETCORE_ENVIRONMENT` de re nhanh xu ly theo moi truong |
| `FTELSRCore.Extensions.Loggers.Helpers.LoggerErrorCategoriesHelper.SecurityCategory` | Ma danh muc loi bao mat dung khi ghi log (`SEC_UNAUTHORIZED`, `SEC_FORBIDDEN`) |
| `FTELSRCore.Wrappers.Result` / `ResultFTelCoreErrorModel` | Kieu response JSON chuan hoa tra ve khi xac thuc/uy quyen that bai |
| `FTELSRCore.Wrappers.ErrorCodes.ResponseWrapperByCodeMapper.FromStatusCode` + `CatalogsErrorCodeModel`, enum `ErrorSourceType` | Map `HttpStatusCode` + `ErrorSourceType.Authentication` sang ma loi chuan hoa (`Code`, `Retryable`) |
| `FTELSRCore.Infrastructure.MiddleWares.Helpers.BuildMetaHelper.Build` | Sinh metadata (`Request_Id`, `Trace_Id`, `Timestamp`) dinh kem response loi |
| `Newtonsoft.Json.JsonConvert` | Serialize header cua request vao noi dung message log |
| `Asp.Versioning.ApiExplorer.IApiVersionDescriptionProvider` | Cung cap danh sach `ApiVersionDescription` da khai bao trong app (dung de tao nhieu `SwaggerDoc`) |
| `Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions`, `Microsoft.OpenApi.*` (`OpenApiSecurityScheme`, `OpenApiSecuritySchemeReference`, `OpenApiInfo`, `OpenApiLicense`, `ParameterLocation`, `SecuritySchemeType`) | Cau hinh sinh tai lieu OpenAPI va dinh nghia security scheme Bearer |
| `Microsoft.AspNetCore.Routing.IOutboundParameterTransformer` | Interface chuan cua ASP.NET Core routing de transform tham so route khi sinh URL |
| `System.Text.RegularExpressions` voi `[GeneratedRegex]` | Regex duoc sinh ma tai thoi diem compile (source generator, khong tao `Regex` runtime moi lan goi) |

### 1.3 Danh muc API

| API | Nhom | Mo ta ngan |
|---|---|---|
| `AuthorizationPolicyExtensions.AddAuthorizationPolicy(string authorizationPolicy)` | Authorization | Factory tra ve delegate cau hinh policy dua tren claim `Permissions` |
| `JWTBearerExtensions.AddJWTBearer(ILogger logger, JWTOptions jwtOptions, JWTBearerModel model)` | Authentication | Factory tra ve delegate cau hinh `JwtBearerOptions` + 3 event handler loi |
| `JWTBearerModel` (record) | Model | Model phu, chi co `ServiceName` |
| `JWTOptions` (class) | Model | Options binding tu config: `Issuer`, `Audience`, `SecretKey`, `ExpireMin` |
| `SwaggerExtensions(IApiVersionDescriptionProvider provider, string userAgent)` | Swagger | Constructor (primary constructor C#), implement `IConfigureOptions<SwaggerGenOptions>` |
| `SwaggerExtensions.Configure(SwaggerGenOptions options)` | Swagger | Method chinh, duoc DI container cua ASP.NET Core options framework goi |
| `SwaggerExtensions.CreateInfoForApiVersion(ApiVersionDescription description)` (private) | Swagger | Ham noi bo, dung boi `Configure` de tao `OpenApiInfo` cho tung version |
| `SlugifyParameterTransformerExtensions.TransformOutbound(object value)` | Routing | Chuyen 1 gia tri route sang kebab-case |
| `SlugifyParameterTransformerExtensions.RegexController()` (private static partial) | Routing | Regex sinh ma tai compile-time, pattern `([a-z])([A-Z])` |

## 2. Chi tiet API

### 2.1 AuthorizationPolicyExtensions.AddAuthorizationPolicy

**Signature**
```csharp
public static Action<AuthorizationPolicyBuilder> AddAuthorizationPolicy(string authorizationPolicy)
```

**Muc dich** - Tra ve mot delegate cau hinh `AuthorizationPolicyBuilder`: yeu cau nguoi dung da xac thuc (authenticated), va gan them mot dieu kien (assertion) yeu cau nguoi dung phai co claim ten `Permissions` khop voi tham so `authorizationPolicy` truyen vao. Ham nay khong tu goi `AddPolicy` - no chi tao ra "cau hinh" de noi khac su dung, thuong theo mau `options.AddPolicy("ten-policy", AddAuthorizationPolicy("gia-tri-permission"))`.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `authorizationPolicy` | `string` | Co | Khong co validate null/empty trong ham; duoc dua vao closure va so sanh trong assertion | Khong co |

**Output** - `Action<AuthorizationPolicyBuilder>`: luon tra ve mot delegate khac null (khong bao gio tra ve `null`), bat ke `authorizationPolicy` la gi (ke ca `null`/rong).

**Dieu kien xu ly** (thu tu trong delegate tra ve, khi ASP.NET Core thuc thi policy tren 1 request):
1. `builder.RequireAuthenticatedUser()` - yeu cau `HttpContext.User` phai la authenticated; neu khong, policy fail (Forbidden/Challenge do middleware Authorization xu ly).
2. `builder.RequireAssertion(context => context.User.HasClaim(c => c.Type.Equals(ClaimTypesConstant.Permissions) && c.Value.Any(value => value.Equals(authorizationPolicy))))` - kiem tra co claim nao thoa dieu kien.

**Side effect** - Khong co (ham chi tao va tra ve delegate, khong ghi log/goi ngoai/mutate tham so).

**Error handling** - Khong co try/catch. Neu `context.User` la `null` khi ASP.NET Core goi assertion, se throw `NullReferenceException` (khong duoc ham nay bat).

**Khi nao NEN dung** - Khi service tieu thu can mot policy don gian "nguoi dung phai co 1 permission cu the trong claim `Permissions`", va chap nhan bug o muc 3 (xem ben duoi) truoc khi dua vao production.

**Khi nao KHONG dung** - Hien tai KHONG NEN dung o production cho toi khi bug o phan "Van de da biet" #1 duoc sua, vi assertion nay khong bao gio tra ve true (xem chi tiet).

**Gioi han** - **(Nghiem trong - xem muc 3 #1)** Bieu thuc `c.Value.Any(value => value.Equals(authorizationPolicy))` tai `AuthorizationPolicyExtensions.cs:15` duyet qua **tung ky tu** cua chuoi `c.Value` (vi `string` la `IEnumerable<char>`) va so sanh moi ky tu (`char`) voi ca chuoi `authorizationPolicy` (`string`) bang `char.Equals(object)`. Phep so sanh `char` voi `string` luon tra ve `false` (khac kieu du lieu) — ket qua la `Any(...)` luon `false`, khien `HasClaim` luon `false`, khien `RequireAssertion` luon that bai. Nhu vay **moi request di qua policy do `AddAuthorizationPolicy` tao ra deu bi tu choi (Forbidden), bat ke user co claim `Permissions` dung hay khong**. Khong tim thay noi goi ham nay trong repo de doi chieu hanh vi thuc te tai runtime.

### 2.2 JWTBearerExtensions.AddJWTBearer

**Signature**
```csharp
public static Action<JwtBearerOptions> AddJWTBearer(ILogger logger, JWTOptions jwtOptions, JWTBearerModel model)
```

**Muc dich** - Tra ve mot delegate cau hinh `JwtBearerOptions` cho middleware xac thuc JWT Bearer cua ASP.NET Core: thiet lap tham so validate token, va gan 3 event handler (`OnAuthenticationFailed`, `OnChallenge`, `OnForbidden`) de tra ve JSON response chuan hoa (kieu `Result`) khi xac thuc/uy quyen that bai.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `logger` | `ILogger` | Co | Khong co null-check; dung truc tiep de goi `logger.ErrorException(...)`/`logger.Error(...)` trong event handler | Khong co |
| `jwtOptions` | `JWTOptions` | Co | Truy cap `jwtOptions!.SecretKey`, `jwtOptions!.Issuer`, `jwtOptions!.Audience` bang null-forgiving operator (`!`) - **khong co null-check runtime thuc su**, `!` chi tat canh bao compiler | Khong co |
| `model` | `JWTBearerModel` | Co | Dung `model.ServiceName ?? CommonBaseConstant.System` trong tung event handler | Khong co |

**Output** - `Action<JwtBearerOptions>`: delegate dung de gan cho `AddJwtBearer(...)`. Ham co the throw truoc khi tra ve delegate (xem Error handling).

**Dieu kien xu ly**:
1. Neu `EnvironmentExtensions.GetEnvironment()` tra ve `"Local"` hoac `"Development"` (`JWTBearerExtensions.cs:20-24`) thi bat `IdentityModelEventSource.ShowPII = true` (log se chua chi tiet PII cua token/claim tu thu vien `Microsoft.IdentityModel`).
2. Tinh `secretKey` tu `jwtOptions.SecretKey` (UTF8 bytes) - thuc hien **ngay khi goi `AddJWTBearer`**, khong doi den luc delegate duoc thuc thi.
3. Delegate tra ve gan: `SaveToken = true`, `RequireHttpsMetadata = true`, va `TokenValidationParameters` voi ca 4 co validate (`ValidateIssuer`, `ValidateAudience`, `ValidateLifetime`, `ValidateIssuerSigningKey`) deu duoc **dat cung la `true`** (khong co co nao bi tat hoac de mac dinh cua thu vien), `ClockSkew = TimeSpan.FromMinutes(5)`, `ValidIssuer`/`ValidAudience` lay tu `jwtOptions`, `IssuerSigningKey` la `SymmetricSecurityKey` tu `secretKey`.
4. `OnAuthenticationFailed`: neu `context.Exception is SecurityTokenExpiredException` → log loi (category `SEC_UNAUTHORIZED`) roi tra JSON 401 voi message co dinh `"Thông tin Token được cấp đã hết hạn"`. Neu la exception khac → `context.NoResult()` (chan xu ly loi mac dinh cua middleware), log loi, tra JSON 401 voi message co dinh `"Xử lý đăng nhập để cấp quyền không thành công"`; **rieng nhanh nay** re them theo moi truong: neu `EProd`/`EStag` → giu message co dinh; nguoc lai (Local/Dev/khac) → gan them `result.Messages = [context.Exception.Message]` (lo chi tiet exception).
5. `OnChallenge`: goi `context.HandleResponse()`; neu response chua bat dau (`!context.Response.HasStarted`) → log loi (category `SEC_FORBIDDEN`) va tra JSON 401 voi message `"Yêu cầu chưa được cấp quyền"`; neu response da bat dau → tra `Task.CompletedTask` (khong lam gi them).
6. `OnForbidden`: luon log loi (category `SEC_FORBIDDEN`) va tra JSON 403 voi message `"Yêu cầu không được phép truy cập tài nguyên này"`.

**Side effect**:
- Mutate static/global state: `IdentityModelEventSource.ShowPII` (anh huong toan bo process, khong chi request hien tai), thuc hien tai thoi diem goi `AddJWTBearer` (thuong la luc app khoi dong).
- Ghi log qua `logger.ErrorException`/`logger.Error` trong ca 3 event handler khi co loi.
- Ghi truc tiep vao `HttpResponse` (`ContentType`, `StatusCode`, body JSON) trong ca 3 event handler.

**Error handling** - Ham chinh khong co try/catch: neu `jwtOptions` la `null` hoac `jwtOptions.SecretKey` la `null`, `Encoding.UTF8.GetBytes(null)` se throw ngay (truoc khi tra ve delegate). Trong cac event handler, exception tu chinh middleware JWT duoc phan loai thanh `SecurityTokenExpiredException` (401, message rieng) hoac loai khac (401, message chung, kem chi tiet exception neu khong phai Prod/Staging) - khong co re nhanh rieng cho cac loai `SecurityTokenException` khac (VD sai signature, sai issuer) ngoai 2 nhanh da neu.

**Khi nao NEN dung** - Khi service tieu thu can mot cau hinh JWT Bearer day du validate (issuer/audience/lifetime/signing key) kem response loi JSON dong bo voi chuan cua he thong (`Result`).

**Khi nao KHONG dung** - Khi can validate them dieu kien khac (VD kiem tra claim tuy chinh ngay tai buoc validate token) - ham nay khong ho tro `TokenValidationParameters.ValidateActor` hay custom validator ngoai 4 co da liet ke.

**Gioi han**:
- Khong co null-check cho `jwtOptions`/`logger`/`model` truoc khi su dung.
- `IdentityModelEventSource.ShowPII = true` la thay doi **toan cuc/tinh** (khong scope theo request) - neu app chay o Local/Dev, moi log lien quan IdentityModel trong toan bo tien trinh se chua PII, khong chi log cua request hien tai.
- Khong xac dinh duoc tu source code gia tri that su cua `jwtOptions.Issuer`/`Audience`/`SecretKey`/`ExpireMin` (do binding tu config ben ngoai, khong nam trong 4 file nay).
- `ExpireMin` (trong `JWTOptions`) duoc khai bao nhung **khong duoc doc/su dung** o dau trong `AddJWTBearer` — validate lifetime dua vao claim `exp` cua token (do thu vien IdentityModel tu xu ly), khong dua vao `ExpireMin`.
- **(Bo sung)** Cach serialize header vao log **khong nhat quan giua 3 event handler**: `OnAuthenticationFailed` (`JWTBearerExtensions.cs:69,96`) va `OnChallenge` (`:141`) deu goi `JsonConvert.SerializeObject(context?.Request?.Headers?.ToDictionary(h => h.Key, h => h.Value.ToString()))` (chuyen ve `Dictionary<string, string>` truoc khi serialize), rieng `OnForbidden` (`:170`) goi truc tiep `JsonConvert.SerializeObject(context?.Request?.Headers)` (serialize nguyen doi tuong `IHeaderDictionary`, khong qua `ToDictionary`). Khong phai loi runtime (ca hai cach deu serialize duoc, khong throw), nhung la mot su khong dong nhat trong cach ghi log giua cac handler cua cung 1 ham.

### 2.3 JWTBearerModel (record)

**Signature**
```csharp
public record JWTBearerModel
{
    public string ServiceName { get; set; }
}
```
**Muc dich** - Model don gian mang ten service, dung de dien vao truong `System`/`serviceName` cua `Result` khi tra loi trong `AddJWTBearer`.
**Input hop le** - `ServiceName`: `string`, khong bat buoc (co the null - khi null se fallback ve `CommonBaseConstant.System` tai noi su dung).
**Output** - Khong ap dung (property record).
**Dieu kien xu ly** - Khong co logic.
**Side effect** - Khong co.
**Error handling** - Khong co.
**Khi nao NEN/KHONG dung** - Dung khi khoi tao tham so cho `AddJWTBearer`.
**Gioi han** - Khong co XML doc (`/// <summary>`) trong source.

### 2.4 JWTOptions (class)

**Signature**
```csharp
public class JWTOptions
{
    public string Issuer { get; set; }
    public string Audience { get; set; }
    public string SecretKey { get; set; }
    public int ExpireMin { get; set; }
}
```
**Muc dich** - Theo dung XML doc trong code: "Các tuỳ chọn cấu hình cho JWT (JSON Web Token)" - `Issuer` (JWTBearerExtensions.cs:209), `Audience` (:215), `SecretKey` (:221), `ExpireMin` - "Thời gian sống của token (tính bằng phút) trước khi hết hạn" (:227).
**Input hop le** - 4 property nhu tren, khong co validate (data annotation) nao trong class nay.
**Output** - Khong ap dung.
**Dieu kien xu ly** - Khong co logic.
**Side effect** - Khong co.
**Error handling** - Khong co.
**Khi nao NEN/KHONG dung** - Binding tu configuration (`appsettings`/secret) roi truyen vao `AddJWTBearer`.
**Gioi han** - Nhu neu o muc 2.2, `ExpireMin` **khong duoc `AddJWTBearer` doc/su dung**; comment XML doc mo ta dung y nghia nhung khong co bang chung ham nao trong 4 file dang tai lieu hoa thuc su ap dung gia tri nay vao logic validate/sinh token (viec sinh token JWT co the nam o file khac ngoai pham vi tai lieu nay).

### 2.5 SwaggerExtensions (class) va SwaggerExtensions.Configure

**Signature**
```csharp
public class SwaggerExtensions(IApiVersionDescriptionProvider provider, string userAgent) : IConfigureOptions<SwaggerGenOptions>
```
```csharp
public void Configure(SwaggerGenOptions options)
```

**Muc dich** - Trien khai `IConfigureOptions<SwaggerGenOptions>` de ASP.NET Core Options framework tu goi `Configure` khi resolve `SwaggerGenOptions`, qua do: (1) dang ky 1 `SwaggerDoc` cho moi `ApiVersionDescription` do `provider` cung cap, (2) nap XML comment file (neu tim thay), (3) khai bao security scheme + security requirement ten `"Bearer"`, (4) bat `EnableAnnotations()`.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `provider` (constructor) | `IApiVersionDescriptionProvider` | Co | Khong co null-check; duoc dung truc tiep trong `Configure` qua `provider.ApiVersionDescriptions` | Khong co |
| `userAgent` (constructor) | `string` | Co | Khong co null-check; dung trong `CreateInfoForApiVersion` de dat `Title` | Khong co |
| `options` (`Configure`) | `SwaggerGenOptions` | Co (do framework truyen vao) | Khong co null-check trong method | Khong co |

**Output** - `void`. Ham lam viec bang cach **mutate truc tiep** doi tuong `options` (`SwaggerGenOptions`) duoc truyen vao (goi `SwaggerDoc`, `IncludeXmlComments`, `AddSecurityRequirement`, `AddSecurityDefinition`, `EnableAnnotations`).

**Dieu kien xu ly**:
1. Voi moi `description` trong `provider.ApiVersionDescriptions`: goi `options.SwaggerDoc($"v{description.ApiVersion}_{description.GroupName}", CreateInfoForApiVersion(description))`.
2. Voi moi `assembly` trong `AppDomain.CurrentDomain.GetAssemblies()`: **neu `!assembly.IsDynamic` (assembly KHONG phai dynamic) thi `continue` (bo qua)** — nghia la chi cac assembly co `IsDynamic == true` moi duoc tiep tuc kiem tra file XML (`$"{assembly.GetName().Name}.xml"` trong `AppDomain.CurrentDomain.BaseDirectory`) va goi `options.IncludeXmlComments(xmlPath)` neu file ton tai.
3. `options.AddSecurityRequirement(document => new() { [new OpenApiSecuritySchemeReference(RoutePrefixToken, document)] = [] })` - khai bao yeu cau security scheme "Bearer" o muc document (toan cuc), khong gan scope cu the (mang rong `[]`).
4. `options.AddSecurityDefinition("Bearer", _securityScheme)` voi `_securityScheme` la `OpenApiSecurityScheme` kieu `ApiKey`, dat trong Header, ten `Authorization`.
5. `options.EnableAnnotations()`.

**CreateInfoForApiVersion** (private, duoc `Configure` goi):
- Tao `OpenApiInfo` voi `Version = description.ApiVersion.ToString()`, `Title = $"{_userAgent} - {description.GroupName.ToUpper()}"`, `License` co dinh ten `"MIT License"` va URL `https://opensource.org/licenses/MIT`.
- Neu `description.IsDeprecated` thi `info.Description += " This API version has been deprecated."` (cong noi vao gia tri `Description` mac dinh cua `OpenApiInfo`, khong xac dinh duoc gia tri mac dinh do tu thu vien `Microsoft.OpenApi`); neu khong deprecated, `Description` khong duoc gan gi trong ham nay.

**Side effect** - Mutate doi tuong `options` truyen vao (khong co gia tri tra ve rieng, tac dung chinh la side effect nay). Doc file tren dia (`File.Exists`, doc XML) neu dieu kien o buoc 2 duoc thoa.

**Error handling** - Khong co try/catch trong `Configure`/`CreateInfoForApiVersion`. Neu `provider` la `null`, `provider.ApiVersionDescriptions` se throw `NullReferenceException`.

**Khi nao NEN dung** - Khi app da tich hop `Asp.Versioning` (API versioning) va `Swashbuckle.AspNetCore`, muon 1 SwaggerDoc/version kem Bearer scheme.

**Khi nao KHONG dung** - Khi can nap XML comment cua chinh project (xem bug #2 o muc 3) — hien tai co che nay khong hoat dong dung ky vong.

**Gioi han** - **(Xem muc 3 #2)** Dieu kien `if (!assembly.IsDynamic) { continue; }` tai `SwaggerExtensions.cs:36-39` mau thuan voi comment `// Include all project's xml comments` (dong 33): assembly dong (compiled, khong dong = khong dynamic) — vi du assembly cua chinh project dang chay — se **luon bi bo qua** vi `IsDynamic` cua no la `false`. Chi assembly dynamic (sinh bang `Reflection.Emit`, proxy runtime, v.v.) moi duoc xet tiep, nhung loai assembly nay thong thuong khong co file `.xml` doc kem theo tren dia. Ket qua thuc te: `IncludeXmlComments` hau nhu khong bao gio duoc goi voi file XML doc thuc su cua project.

### 2.6 SlugifyParameterTransformerExtensions.TransformOutbound

**Signature**
```csharp
public string TransformOutbound(object value)
```
(class khai bao `public partial class SlugifyParameterTransformerExtensions : IOutboundParameterTransformer`)

**Muc dich** - Trien khai `IOutboundParameterTransformer.TransformOutbound` cua ASP.NET Core routing: chuyen 1 gia tri tham so route (thuong la ten controller/action dang PascalCase) thanh dang kebab-case chu thuong khi sinh URL.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `value` | `object` | Co (theo interface) | Kiem tra `value == null` truoc khi xu ly | Khong co (do interface quy dinh) |

**Output** - `string`: tra ve `null` neu `value == null`; nguoc lai tra ve chuoi da qua regex thay the roi `ToLower(CultureInfo.CurrentCulture)`. Khong bao gio throw voi `value` khong null (do co `?? string.Empty` cho ket qua `ToString()`).

**Dieu kien xu ly**:
1. `value == null` → tra ve `null` ngay.
2. Nguoc lai: `value.ToString() ?? string.Empty` → ap dung `RegexController().Replace(..., "$1-$2")` (chen `-` giua 1 ky tu thuong va 1 ky tu hoa lien tiep ngay sau) → `.ToLower(CultureInfo.CurrentCulture)`.

**Side effect** - Khong co (ham thuan, khong mutate tham so, khong I/O).

**Error handling** - Khong co try/catch; khong can vi khong co duong dan nao trong ham co the throw voi input hop le (`value` bat ky object, `ToString()` co fallback `?? string.Empty`).

**Khi nao NEN dung** - Dang ky qua convention routing (VD `RouteTokenTransformerConvention`) de URL API co dang kebab-case (VD `/service-request/get-by-id`). Khong tim thay noi dang ky cu the trong repo nay.

**Khi nao KHONG dung** - Khi can xu ly nguoc (tu kebab-case ve lai ten class/action) - interface `IOutboundParameterTransformer` va ham nay chi ho tro 1 chieu (outbound).

**Gioi han**:
- Regex `([a-z])([A-Z])` (`SlugifyParameterTransformerExtensions.cs:15`) chi chen dau `-` tai ranh gioi **1 ky tu thuong ngay truoc 1 ky tu hoa**; khong xu ly chuoi nhieu ky tu hoa lien tiep (acronym). **Da kiem chung truc tiep hanh vi regex** (khong chi suy dien): voi input `"ABCController"`, khong co bat ky cap ky tu "thuong-ngay-truoc-hoa" nao trong toan chuoi (`A`,`B`,`C`,`C` deu hoa lien tiep, sau do `ontroller` toan chu thuong, khong con chu hoa nao dung sau de tao ranh gioi) → regex **khong thay the gi ca**, ket qua sau `ToLower` la `"abccontroller"` (dinh lien toan bo, khong co dau `-` nao). Vi du dung de minh hoa acronym: `"ABCTestController"` → chi co 1 ranh gioi thuong-hoa hop le la `t` (cua "Test") ngay truoc `C` (cua "Controller") → ket qua `"abctest-controller"` (phan `ABC` dau van dinh lien, khong tach). Nhu vay tien to acronym (nhieu chu hoa lien tiep o dau chuoi) luon dinh lien voi tu ke tiep neu tu do khong co chu thuong dung truoc chu hoa dau tien cua no.
- Dung `CultureInfo.CurrentCulture` (khong dung `InvariantCulture`) cho `ToLower` - ket qua co the khac nhau tuy locale/culture cua thread/server luc thuc thi (rui ro known .NET, VD locale `tr-TR` xu ly ky tu `I`/`i` khac invariant).

## 3. Van de da biet

| # | Van de | Vi tri | Anh huong |
|---|---|---|---|
| 1 | `RequireAssertion` trong `AddAuthorizationPolicy` so sanh tung ky tu (`char`) cua `c.Value` voi ca chuoi `authorizationPolicy` (`string`) qua `value.Equals(authorizationPolicy)`. `char.Equals(object)` luon `false` khi so sanh voi mot `string`, nen `Any(...)` luon `false`, `HasClaim` luon `false`, `RequireAssertion` luon fail | `AuthorizationPolicyExtensions.cs:13-15` | **Nghiem trong**: bat ky policy nao tao boi `AddAuthorizationPolicy` deu **khong bao gio thanh cong**, bat ke user co claim `Permissions` khop hay khong - chuc nang phan quyen theo permission coi nhu khong hoat dong. **Sua lai cho chinh xac** (KB cu ghi "luon tra ve 403 Forbidden cho MOI user" la khai quat hoa qua muc, tu mau thuan voi muc 2.1 diem 1 dang ghi dung la "Forbidden/Challenge"): 4 file dang xet khong chua logic quyet dinh Challenge (401) hay Forbid (403) - do la hanh vi mac dinh cua ASP.NET Core Authorization middleware (`PolicyEvaluator`, nam ngoai 4 file nay), theo do request **chua xac thuc duoc (vd khong co token/token khong hop le)** nhan **401 Challenge** (roi do `JWTBearerExtensions.OnChallenge` xu ly), con request **da xac thuc thanh cong** (token hop le) nhung fail assertion nay moi nhan **403 Forbidden** (do `JWTBearerExtensions.OnForbidden` xu ly). Vay chi user da xac thuc hop le moi chac chan bi tu choi bang 403; user chua xac thuc bi tu choi bang 401, khong phai 403 |
| 2 | Dieu kien nap XML comment bi dao nguoc: `if (!assembly.IsDynamic) { continue; }` chi cho phep assembly **dynamic** di tiep, trong khi comment ngay tren no ghi "Include all project's xml comments" (nguc voi hanh vi thuc te - assembly cua project la assembly da compile, khong dynamic) | `SwaggerExtensions.cs:33-39` | XML comment (`/// <summary>`) cua controller/action trong project tieu thu **hau nhu khong bao gio** duoc `IncludeXmlComments` nap vao Swagger UI, du file `.xml` co ton tai tren dia, vi vong lap bo qua assembly khong-dynamic truoc khi kiem tra file |
| 3 | `Encoding.UTF8.GetBytes(jwtOptions!.SecretKey)` dung null-forgiving operator (`!`) nhung khong co null-check runtime thuc su cho `jwtOptions` hay `jwtOptions.SecretKey` | `JWTBearerExtensions.cs:26` | Neu cau hinh (`JWTOptions`) bi thieu/binding sai dan den `jwtOptions == null` hoac `SecretKey == null`, `AddJWTBearer` throw exception **ngay luc khoi dong app** (truoc khi delegate duoc tra ve), khong co message loi nghiep vu ro rang |
| 4 | `IdentityModelEventSource.ShowPII = true` la thuoc tinh static toan cuc cua thu vien `Microsoft.IdentityModel`, duoc bat khi moi truong la Local/Development | `JWTBearerExtensions.cs:20-24` | Anh huong toan bo tien trinh (khong chi request/hoi hien tai): moi log lien quan IdentityModel trong qua trinh chay se lo thong tin PII (VD noi dung claim/token) khi chay o Local/Dev |
| 5 | `TransformOutbound` dung `CultureInfo.CurrentCulture` thay vi `CultureInfo.InvariantCulture` cho `ToLower` | `SlugifyParameterTransformerExtensions.cs:12` | Ket qua slug route co the khac nhau giua cac moi truong/server co culture khac nhau - rui ro khong on dinh URL da known trong .NET (VD locale Turkish voi ky tu I/i) |
| 6 | `JWTOptions.ExpireMin` co XML doc mo ta la "thoi gian song cua token" nhung khong duoc doc/su dung boi bat ky logic nao trong `AddJWTBearer` | `JWTBearerExtensions.cs:227-230` (XML doc) doi chieu voi toan bo than `AddJWTBearer` (khong co tham chieu `ExpireMin`) | Sai lech giua tai lieu (XML doc) va hanh vi runtime cua 4 file dang xet - viec het han token (neu co) phai do noi khac (VD noi sinh JWT, nam ngoai pham vi 4 file nay) dam nhan, khong xac dinh duoc tu source code trong pham vi tai lieu nay |
| 7 | Ca 4 API/model trong tai lieu nay khong co noi goi (call site) nao trong repo `sr-core-helper` | Xac nhan bang grep toan repo (khong tinh thu muc `.claude/worktrees/...` la ban sao lam viec cua agent, khong phai ma nguon chinh thuc) | Khong the doi chieu hanh vi runtime thuc te (ten policy cu the, gia tri `JWTOptions` thuc te, danh sach version API thuc te) - tai lieu chi dua tren doc than ham |
| 8 | Doi chieu voi 8 file Knowledge Base cu (theo yeu cau doi chieu nguoc) | Khong ap dung | Khong co type/file nao trong danh sach can doi chieu (`AuditModel`, `HttpOptionModel`, `ErrorModel`, `CustomException`, `ProjectToExtensions`, `PrecateBuilderExtensions`, `MeasureExecutionTimeExtensions.InvokeForHTTP`, `MongoResiliencePolicyFactory`, `BaseEntityMongoDB`/`BaseEntitySQL`) duoc 4 file nguon cua module nay su dung - da kiem tra bang doc toan bo 4 file, khong co tham chieu |
| 9 | (Bo sung) `OnForbidden` serialize header request truc tiep (`JsonConvert.SerializeObject(context?.Request?.Headers)`), khac voi `OnAuthenticationFailed`/`OnChallenge` (deu `ToDictionary(...)` truoc khi serialize) | `JWTBearerExtensions.cs:69, 96, 141` (2 handler dung `ToDictionary`) doi chieu voi `:170` (`OnForbidden` khong dung) | Khong gay loi runtime, nhung noi dung/dinh dang message log giua 3 event handler cua cung 1 ham khong dong nhat - anh huong den viec parse/tim kiem log tap trung neu he thong log ky vong dinh dang giong nhau |
