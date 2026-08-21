# Wrappers - Result/IResult

> Nguon: `FTELSRCore.Shared/Wrappers/Result.cs`, `FTELSRCore.Shared/Wrappers/IResult.cs`
> Loai: class (`Result`, `Result<T>`) + interface (`IResult`, `IResult<T>`) + record (`ResultFTelCoreErrorModel`, `ResultFTelCoreMetadataModel`)
> Cap nhat theo commit: `89c1ce9`

## 1. Tong quan

`Result`/`IResult` la mot Result-pattern wrapper dung de tra ket qua xu ly (thanh cong hoac loi) cho toan bo he
thong FTELSRCore theo mot cau truc JSON co dinh, thay cho viec nem exception ra ngoai boundary API
(`IResult.cs:1-70`, `Result.cs:1-176`). Namespace khai bao la `FTELSRCore.Wrappers` (khong phai
`FTELSRCore.Shared.Wrappers` du file nam trong thu muc `FTELSRCore.Shared/Wrappers/`; day la quy uoc chung
cua project nay, cac lop lien quan nhu `ResponseWrapperByCodeMapper` cung dung goc `FTELSRCore.Wrappers.*`).
Module nam o tang cross-cutting/shared, duoc middleware xu ly loi va cac ham nghiep vu su dung de tra ve
response cho client (vi du `ExceptionHandlerMiddleWare.cs:47`, `JWTBearerExtensions.cs:78`).

Co hai nhom API duoc thiet ke de goi tu ben ngoai: `Result` (khong co du lieu payload, dung cho cac hanh
dong khong can tra data) va `Result<T>` (ke thua `Result`, co them property `Data` de tra payload kieu `T`).
Ca hai deu chi cung cap **static factory method** (`Fail`, `Succeed`, `FailSystem`) - constructor cua `Result`
la `protected` (`Result.cs:34-36`) nen khong the `new Result()` truc tiep tu ngoai lop; rieng `Result<T>` lai
co constructor `public` khong tham so (`Result.cs:182-184`), tuc `new Result<T>()` van goi duoc tu ben ngoai
du day khong phai cach dung khuyen nghi (xem muc 3, van de #1).

### 1.1 Pham vi chuc nang

| Lam duoc | Khong lam duoc |
|---|---|
| Tao object ket qua thanh cong qua `Result.Succeed(...)` / `Result<T>.Succeed(...)` voi message hoac list message, co hoac khong co `Data` (`Result.cs:97-124`, `268-330`) | Khong co implicit operator nao chuyen doi giua `T` va `Result<T>`, hoac giua `Result` va `Result<T>` - moi truong hop chuyen sang `Result<T>` phai goi factory method ro rang (khong tim thay `operator` nao trong `Result.cs` va `IResult.cs`) |
| Tao object ket qua loi qua `Result.Fail(...)` / `Result<T>.Fail(...)`, tuy chinh `statusCode`, `succeeded`, `error` (`Result.cs:61-91`, `199-263`) | Khong tu dong nem/bat exception - toan bo logic la tao va tra ve object thuan (POCO), khong co try/catch ben trong cac factory method |
| Tao ket qua loi cap he thong (`FailSystem`) kem `ResultFTelCoreMetadataModel` va ten service (`Result.cs:138-174`) | `Result<T>` **khong co** overload `FailSystem` rieng - goi `Result<T>.FailSystem(...)` se tra ve kieu `Result` (khong phai `Result<T>`) vi day la static method ke thua, khong bi "new" hide (xem muc 3, van de #4) |
| Cung cap ban async cho moi factory (`FailAsync`, `SucceedAsync`) - chi la wrapper `Task.FromResult(...)` (`Result.cs:85-91`, `118-124`, `248-262`, `316-330`) | Khong co `FailSystemAsync` - `FailSystem` chi co ban dong bo |
| Tu dong gan `Status` (ten enum `HttpStatusCode`) tu `statusCode` (int) qua extension `ConvertHttpStatusCodeCodeByName()` (`ConvertHelpers.cs:175-178`) | Khong tu dong xac thuc `statusCode` co hop le voi `HttpStatusCode` enum hay khong - neu khong khop, `Status` tra ve chuoi rong (`string.Empty`) |
| Tu dong gan `Error` mac dinh (catalog loi chung) neu caller khong truyen `error` (`Result.cs:38-57`, `186-195`) | `Error` mac dinh **khong dong bo voi `statusCode` thuc te** duoc truyen vao `Fail`/`FailSystem` - no luon la catalog cua `BadRequest`/`InternalServerError` bat ke `statusCode` param la gi (xem muc 3, van de #2) |

### 1.2 Dependency

| Thanh phan | Muc dich su dung |
|---|---|
| `FTELSRCore.Wrappers.ErrorCodes.ResponseWrapperByCodeMapper.FromStatusCode` (`ResponseWrapperByCodeMapper.cs:8-14`) | Tra ve `CatalogsErrorCodeModel` (Code, Retryable...) tuong ung mot `HttpStatusCode` + `ErrorSourceType`, dung de khoi tao cac `static readonly` default error o `Result.cs:38-57` va `186-195` |
| `FTELSRCore.Wrappers.ErrorCodes.Catalogs.CatalogsErrorCode.StatusMap` (`CatalogsErorrCode.cs:7-46`) | Bang tra cuu (statusCode, sourceType) -> `CatalogsErrorCodeModel`, duoc `ResponseWrapperByCodeMapper` doc |
| `FTELSRCore.Wrappers.ErrorCodes.Catalogs.Systems.CatalogsErrorCodes.BadRequest` / `.SystemError` (`CatalogsErorrCodeForSystem.cs:9-14`, `32-37`) | Nguon gia tri thuc te cho `_failLogicDefault` (`Code = "BUSINESS_RULE_400"`) va `_errorSystemDefault` (`Code = "SYS_500"`) |
| `System.Net.HttpStatusCode` | Kieu enum dung lam gia tri mac dinh cho `statusCode` param (`BadRequest`, `OK`, `InternalServerError`) |
| `ConvertHelpers.ConvertHttpStatusCodeCodeByName(this int)` (`ConvertHelpers.cs:175-178`) | Chuyen `int` statusCode sang ten enum `HttpStatusCode` (chuoi) de gan vao `Status` |
| `FTELSRCore.Constants.CommonBaseConstant.System` (`CommonBaseConstant.cs:35`) | Gia tri mac dinh (`"FTEL-SERVICEREQUEST-API"`) cho property `System` cua `Result` khi khong bi `FailSystem` ghi de |
| `System.Text.Json.Serialization.JsonPropertyName` | Gan ten field JSON (`code`, `status`, `dispatched`, `succeeded`, `system`, `messages`, `meta`, `error`, `data`) cho toan bo property |

### 1.3 Danh muc API

| API | Nhom | Mo ta ngan |
|---|---|---|
| `IResult.Code`, `Status`, `Dispatched`, `Succeeded`, `System`, `Messages` | Property (interface) | Cac truong chuan cua response FTELCore |
| `IResult.Meta`, `IResult.Error` | Property (interface) | Metadata request/trace va thong tin loi chi tiet |
| `IResult<T>.Data` | Property (interface, covariant `out T`, chi co getter) | Payload tra ve khi thanh cong (hoac du lieu kem theo khi loi, tuy factory) |
| `ResultFTelCoreErrorModel` | Record | `Code` (string), `Retryable` (bool, default `false`) |
| `ResultFTelCoreMetadataModel` | Record | `Request_Id`, `Trace_Id`, `Timestamp` (deu string) |
| `Result.Fail(string, bool, int, ResultFTelCoreErrorModel)` | Static factory | Tao `Result` loi tu 1 message |
| `Result.Fail(List<string>, bool, int, ResultFTelCoreErrorModel)` | Static factory | Tao `Result` loi tu nhieu message |
| `Result.FailAsync(...)` (2 overload) | Static factory (async) | Ban `Task<Result>` cua 2 `Fail` tren |
| `Result.Succeed(string, int)` | Static factory | Tao `Result` thanh cong tu 1 message |
| `Result.Succeed(List<string>, int)` | Static factory | Tao `Result` thanh cong tu nhieu message |
| `Result.SucceedAsync(...)` (2 overload) | Static factory (async) | Ban `Task<Result>` cua 2 `Succeed` tren |
| `Result.FailSystem(List<string>, ResultFTelCoreMetadataModel, string, int, ResultFTelCoreErrorModel)` | Static factory | Loi cap he thong (Dispatched=false), kem metadata + ten service |
| `Result.FailSystem(string, ResultFTelCoreMetadataModel, string, int, ResultFTelCoreErrorModel)` | Static factory | Giong tren, nhan 1 message thay vi list |
| `Result<T>.Data` | Property | Payload kieu `T` |
| `Result<T>.Fail(string, bool, int, ResultFTelCoreErrorModel)` (new) | Static factory | Nhu `Result.Fail` nhung tra `Result<T>`, `Data` mac dinh `default(T)` |
| `Result<T>.Fail(List<string>, bool, int, ResultFTelCoreErrorModel)` (new) | Static factory | Tuong tu, nhan list message |
| `Result<T>.Fail(T, string, bool, int, ResultFTelCoreErrorModel)` | Static factory | Nhu tren nhung gan them `Data = data` |
| `Result<T>.Fail(T, List<string>, bool, int, ResultFTelCoreErrorModel)` | Static factory | Nhu tren, list message + `Data` |
| `Result<T>.FailAsync(...)` (4 overload) | Static factory (async) | Ban `Task<Result<T>>` cua 4 `Fail` tren |
| `Result<T>.Succeed(string, bool, int)` | Static factory | Thanh cong, khong `Data`, co them tham so `succeeded` |
| `Result<T>.Succeed(List<string>, bool, int)` | Static factory | Tuong tu, list message |
| `Result<T>.Succeed(T, string, bool, int)` | Static factory | Thanh cong kem `Data` |
| `Result<T>.Succeed(T, List<string>, bool, int)` | Static factory | Thanh cong kem `Data`, list message |
| `Result<T>.SucceedAsync(...)` (4 overload) | Static factory (async) | Ban `Task<Result<T>>` cua 4 `Succeed` tren |

## 2. Chi tiet API

### 2.1 IResult / IResult\<T\> - cac property chuan

**Signature**
```csharp
public interface IResult<out T> : IResult
{
    [JsonPropertyName("data")]
    T Data { get; }
}

public partial interface IResult
{
    [JsonPropertyName("code")] int Code { get; set; }
    [JsonPropertyName("status")] string Status { get; set; }
    [JsonPropertyName("dispatched")] bool Dispatched { get; set; }
    [JsonPropertyName("succeeded")] bool Succeeded { get; set; }
    [JsonPropertyName("system")] string System { get; set; }
    [JsonPropertyName("messages")] List<string> Messages { get; set; }
}

public partial interface IResult
{
    [JsonPropertyName("meta")] public ResultFTelCoreMetadataModel Meta { get; set; }
    [JsonPropertyName("error")] public ResultFTelCoreErrorModel Error { get; set; }
}
```
(`IResult.cs:1-70`)

**Muc dich** - Dinh nghia cau truc JSON chuan cho moi response cua he thong (`Result.cs:8`, `177` implement
truc tiep hai interface nay). `IResult` duoc khai bao thanh 2 `partial interface` (`IResult.cs:11`, `63`) -
ve mat compile day chi la mot interface duy nhat, viec tach lam 2 khoi chi de nhom XML doc, khong tao ra
khac biet hanh vi.

**Muc dich tung field** (theo XML doc trong source, `IResult.cs:13-56`, `89-111`):
- `Code` (`int`) - HttpStatusCode dang so.
- `Status` (`string`) - HttpStatusCode dang ten (vi du `"BadRequest"`).
- `Dispatched` (`bool`) - "Tinh trang xu ly tai he thong". Nguyen van XML doc (`IResult.cs:27-32`):
  "[QUY DINH]: true: He thong chap nhan cac Rule he thong cho phep den ham xu ly yeu cau. false: He thong tu
  choi yeu cau ngay tai Rule he thong, khong den ham xu ly." (ban dich/dien giai o day KHONG phai trich dan
  nguyen van, chi la dien giai lai y nghia).
- `Succeeded` (`bool`) - Yeu cau xu ly co thanh cong hay khong.
- `System` (`string`) - Ten he thong xu ly yeu cau.
- `Messages` (`List<string>`) - Danh sach thong bao.
- `Meta` (`ResultFTelCoreMetadataModel`) - Thong tin request_id/trace_id/timestamp.
- `Error` (`ResultFTelCoreErrorModel`) - Thong tin loi (code noi bo + co retry duoc khong).
- `IResult<T>.Data` - Payload; `out T` cho phep covariance (vi du gan `IResult<Derived>` cho
  `IResult<Base>`), chi co getter (khong `set`) tren interface.

**Input hop le / Output** - Day la interface, khong co logic thuc thi; xem muc tuong ung o `Result`/`Result<T>`
ben duoi.

**Side effect** - Khong co (interface).

**Error handling** - Khong ap dung.

**Khi nao NEN dung** - Khi can khai bao tham so/kieu tra ve chap nhan ca `Result` va `Result<T>` (polymorphism
qua interface), hoac can mot kieu du lieu tra ve co covariance tren `Data`.

**Khi nao KHONG dung** - Khong the khoi tao truc tiep (interface); phai dung `Result`/`Result<T>`.

**Gioi han** - `IResult<T>.Data` chi co getter tren interface trong khi `Result<T>.Data` (class) co ca getter
va setter (`Result.cs:179-180`) - nghia la mutate `Data` chi thuc hien duoc khi lam viec voi kieu cu the
`Result<T>`, khong the qua bien kieu `IResult<T>`.

### 2.2 ResultFTelCoreErrorModel

**Signature**
```csharp
public sealed record ResultFTelCoreErrorModel
{
    [JsonPropertyName("code")]
    public string Code { get; set; }

    [JsonPropertyName("retryable")]
    public bool Retryable { get; set; } = false;
}
```
(`IResult.cs:72-87`)

**Muc dich** - Theo XML doc: `Code` la "Ma loi noi bo dang string (vi du: SR_500, INVALID_OTP)"
(`IResult.cs:74-77`); `Retryable` "Cho biet client co the retry khong (true voi 408/429/5xx)"
(`IResult.cs:81-84`).

**Input hop le** - Day la record voi property init/set thong thuong, khong co validate nao trong code
(khong co logic rang buoc gia tri `Code` phai theo mot format cu the).

**Output** - Khong ap dung (data model thuan).

**Dieu kien xu ly / Side effect / Error handling** - Khong co (record POCO, khong logic).

**Khi nao NEN dung** - Khi can custom `Error` khac voi mac dinh khi goi `Fail`/`FailSystem` (truyen qua param
`error`), hoac khi doc field `Error` tu mot `Result` da nhan duoc.

**Gioi han** - `Retryable` mac dinh `false` neu khong set; khong co annotation nao rang buoc `Code` non-null.

### 2.3 ResultFTelCoreMetadataModel

**Signature**
```csharp
public sealed record ResultFTelCoreMetadataModel
{
    [JsonPropertyName("request_id")] public string Request_Id { get; set; }
    [JsonPropertyName("trace_id")] public string Trace_Id { get; set; }
    [JsonPropertyName("timestamp")] public string Timestamp { get; set; }
}
```
(`IResult.cs:89-111`)

**Muc dich** - Theo XML doc: `Request_Id` la "ID dinh danh request (per-request, sinh boi gateway / ASP.NET
TraceIdentifier)" (`IResult.cs:91-93`); `Trace_Id` la "ID trace cross-system (W3C activity?.TraceId hoac
fallback x-correlation-id)" (`IResult.cs:98-100`); `Timestamp` la "ISO 8601 UTC timestamp luc tao response"
(`IResult.cs:105-107`).

**Input hop le / Output / Side effect / Error handling** - Khong co (record POCO, khong logic; khong co ham
nao trong `Result.cs`/`IResult.cs` tu dong khoi tao `Request_Id`/`Trace_Id`/`Timestamp` - viec build object
nay la trach nhiem cua caller, vi du `BuildMetaHelper.Build(...)` duoc thay dung trong
`ExceptionHandlerMiddleWare.cs:50` va `JWTBearerExtensions.cs:82` - **file `BuildMetaHelper` khong thuoc pham
vi tai lieu nay, khong xac dinh chi tiet tu source cua module dang xet**).

**Khi nao NEN dung** - Truyen vao `metadata` cua `Result.FailSystem(...)` de dinh danh request/trace khi bao
loi he thong.

**Gioi han** - Ca 3 field deu `string` (khong phai `Guid`/`DateTime`), khong co validate format.

### 2.4 Result - constructor va default error field

**Signature**
```csharp
public class Result : IResult
{
    protected Result() { }

    private static readonly CatalogsErrorCodeModel _catalogsBadRequest = ...;
    private static readonly ResultFTelCoreErrorModel _failLogicDefault = new()
    {
        Code = _catalogsBadRequest.Code,
        Retryable = _catalogsBadRequest.Retryable
    };

    private static readonly CatalogsErrorCodeModel _catalogsInternalServerError = ...;
    private static readonly ResultFTelCoreErrorModel _errorSystemDefault = new()
    {
        Retryable = _catalogsInternalServerError.Retryable,
        Code = _catalogsInternalServerError.Code
    };
}
```
(`Result.cs:8-57`)

**Muc dich** - Khoi tao 2 gia tri `ResultFTelCoreErrorModel` mac dinh dung lam fallback khi caller khong
truyen `error` cho cac factory method: `_failLogicDefault` (dung cho `Fail`) va `_errorSystemDefault` (dung
cho `FailSystem`).

**Input hop le** - Khong ap dung (khong nhan tham so tu ngoai; day la static field khoi tao 1 lan luc load
type).

**Output** - `_failLogicDefault.Code = "BUSINESS_RULE_400"`, `Retryable = false` (lay tu
`CatalogsErrorCodes.BadRequest`, `CatalogsErorrCodeForSystem.cs:9-14`).
`_errorSystemDefault.Code = "SYS_500"`, `Retryable = false` (lay tu `CatalogsErrorCodes.SystemError`,
`CatalogsErorrCodeForSystem.cs:32-37`).

**Dieu kien xu ly** - `ResponseWrapperByCodeMapper.FromStatusCode` duoc goi voi `sourceType: ErrorSourceType.General`
co dinh (`Result.cs:39-40`, `50-51`) - **khong bao gio thay doi theo tham so runtime**, vi day la
`static readonly` field khoi tao mot lan.

**Side effect** - Khong co (chi doc du lieu tu dictionary tinh `CatalogsErrorCode.StatusMap`).

**Error handling** - `ResponseWrapperByCodeMapper.FromStatusCode` co fallback noi bo (`FromStatusCodeDefault`,
`ResponseWrapperByCodeMapper.cs:16-28`) neu key khong ton tai trong map, nhung vi `(400, General)` va
`(500, General)` deu co trong `CatalogsErrorCode.StatusMap` (`CatalogsErorrCode.cs:14-15`, `32-33`), nhanh
fallback nay khong duoc kich hoat trong thuc te doi voi `Result.cs`.

**Khi nao NEN dung / KHONG dung** - Day la field noi bo (`private`), khong duoc goi truc tiep tu ngoai lop.

**Gioi han** - Constructor `protected Result()` (`Result.cs:34-36`) ngan `new Result()` tu code ngoai
assembly/lop con truc tiep, buoc phai di qua factory method - tuy nhien **khong ngan duoc** `Result<T>` co
constructor `public` rieng (xem muc 2.10).

### 2.5 Result.Fail

**Signature**
```csharp
public static Result Fail(
    string message = "Thực hiện yêu cầu không thành công", bool succeeded = false,
    int statusCode = (int)HttpStatusCode.BadRequest, ResultFTelCoreErrorModel error = null)

public static Result Fail(
    List<string> messages, bool succeeded = false,
    int statusCode = (int)HttpStatusCode.BadRequest, ResultFTelCoreErrorModel error = null)
```
(`Result.cs:61-83`)

**Muc dich** - Tao mot `Result` bieu dien ket qua xu ly khong thanh cong ("loi nghiep vu", van con
`Dispatched = true` vi request van den duoc ham xu ly).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `message` | `string` | Khong | Khong validate (co the null/empty, se duoc wrap vao list `[message]`) | `"Thực hiện yêu cầu không thành công"` |
| `messages` | `List<string>` | Co (overload 2, khong co default) | Khong validate null | - |
| `succeeded` | `bool` | Khong | Khong validate | `false` |
| `statusCode` | `int` | Khong | Khong validate co phai HttpStatusCode hop le | `(int)HttpStatusCode.BadRequest` (400) |
| `error` | `ResultFTelCoreErrorModel` | Khong | Khong validate | `null` -> fallback `_failLogicDefault` |

**Output** - `Result` moi voi: `Code = statusCode`; `Messages = [message]` hoac `messages`; `Succeeded =
succeeded`; `Dispatched = true` (luon `true`, khong phu thuoc tham so nao); `Status =
statusCode.ConvertHttpStatusCodeCodeByName()`; `Error = error ?? _failLogicDefault`; `System` giu gia tri
mac dinh cua property (`CommonBaseConstant.System`, khong bi ham nay ghi de); `Meta` giu `null` (khong duoc
ham nay gan).

**Dieu kien xu ly** - Khong co nhanh re/guard clause; ham la mot bieu thuc object-initializer don, chay
thang tu dau den cuoi.

**Side effect** - Khong co (khong log, khong goi ngoai, khong mutate tham so dau vao).

**Error handling** - Khong bat exception nao; ham khong the throw tru khi `statusCode.ConvertHttpStatusCodeCodeByName()`
throw (kiem tra `ConvertHelpers.cs:175-178` cho thay ham nay dung `Enum.GetName` va tra `string.Empty` neu
khong khop, khong throw).

**Khi nao NEN dung** - Khi logic nghiep vu (business rule) xac dinh yeu cau khong thanh cong nhung request
van duoc dispatch den ham xu ly (vi du validate input sai, entity not found).

**Khi nao KHONG dung** - Khi loi xay ra truoc khi den duoc ham xu ly (bi chan boi middleware/rule he thong) -
truong hop do nen dung `Result.FailSystem` (xem 2.9) de `Dispatched = false` dung ngu nghia XML doc cua
`IResult.Dispatched`.

**Gioi han** - `Error` mac dinh (`_failLogicDefault`, `Code = "BUSINESS_RULE_400"`) **khong lien quan** den
`statusCode` thuc te duoc truyen (vi du goi `Fail(statusCode: 404)` van tra `Error.Code = "BUSINESS_RULE_400"`
neu khong tu truyen `error`) - xem muc 3, van de #2.

### 2.6 Result.FailAsync

**Signature**
```csharp
public static Task<Result> FailAsync(
    string message = "...", bool succeeded = false, int statusCode = (int)HttpStatusCode.BadRequest,
    ResultFTelCoreErrorModel error = null)
    => Task.FromResult(Fail(message: message, succeeded: succeeded, statusCode: statusCode, error: error));

public static Task<Result> FailAsync(
    List<string> messages, bool succeeded = false, int statusCode = (int)HttpStatusCode.BadRequest,
    ResultFTelCoreErrorModel error = null)
    => Task.FromResult(Fail(messages: messages, succeeded: succeeded, statusCode: statusCode, error: error));
```
(`Result.cs:85-91`)

**Muc dich** - Ban "async" cua `Fail`, chi de tien goi trong pipeline `async`/`await` sans phai `await
Task.FromResult(...)` tay.

**Input hop le / Output** - Giong hoan toan `Fail` (muc 2.5), boc trong `Task<Result>` da hoan thanh
(`Task.FromResult`).

**Dieu kien xu ly** - Khong co logic bat dong bo thuc su; day chi la wrapper dong bo tra ve Task da complete.

**Side effect / Error handling** - Giong `Fail`.

**Khi nao NEN dung** - Khi signature ham goi yeu cau `Task<Result>` (vi du interface async), nhung ban than
logic tao `Result` khong can I/O bat dong bo thuc.

**Khi nao KHONG dung** - Khong mang lai loi ich hieu nang thuc su (khong co `await` I/O ben trong) - chi la
adapter ve kieu.

**Gioi han** - Khong co ban async cho `FailSystem` (xem muc 1.1).

### 2.7 Result.Succeed

**Signature**
```csharp
public static Result Succeed(
    string message = "Thực hiện yêu cầu thành công", int statusCode = (int)HttpStatusCode.OK)

public static Result Succeed(List<string> messages, int statusCode = (int)HttpStatusCode.OK)
```
(`Result.cs:97-116`)

**Muc dich** - Tao `Result` bieu dien ket qua xu ly thanh cong.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate | Gia tri mac dinh |
|---|---|---|---|---|
| `message` | `string` | Khong | Khong validate | `"Thực hiện yêu cầu thành công"` |
| `messages` | `List<string>` | Co (overload 2) | Khong validate | - |
| `statusCode` | `int` | Khong | Khong validate | `(int)HttpStatusCode.OK` (200) |

**Output** - `Result` voi `Succeeded = true` (hardcode, **khong co tham so de override** - khac voi
`Result<T>.Succeed` co them param `succeeded`, xem muc 3 van de #3); `Dispatched = true`; `Code = statusCode`;
`Messages = [message]` hoac `messages`; `Status = statusCode.ConvertHttpStatusCodeCodeByName()`; `Error`
giu `null` (mac dinh cua property, `Result.cs:29`); `Meta` giu `null`.

**Dieu kien xu ly** - Khong co nhanh re.

**Side effect / Error handling** - Khong co.

**Khi nao NEN dung** - Tra ket qua thanh cong khong can payload (vi du cac hanh dong "xoa", "cap nhat" chi
can bao thanh cong).

**Khi nao KHONG dung** - Khi can tra kem du lieu (`Data`) - phai dung `Result<T>.Succeed` (muc 2.13).

**Gioi han** - `Succeeded` luon `true`, khong the tao mot "ket qua thanh cong nhung Succeeded=false" bang ham
nay (ham `Result<T>.Succeed` tuong ung thi lam duoc, xem van de #3).

### 2.8 Result.SucceedAsync

**Signature**
```csharp
public static Task<Result> SucceedAsync(
    string message = "Thực hiện yêu cầu thành công", int statusCode = (int)HttpStatusCode.OK)
    => Task.FromResult(Succeed(message, statusCode));

public static Task<Result> SucceedAsync(
    List<string> messages, int statusCode = (int)HttpStatusCode.OK)
    => Task.FromResult(Succeed(messages, statusCode));
```
(`Result.cs:118-124`)

**Muc dich/Input/Output/Side effect/Error handling** - Giong `Succeed` (muc 2.7), boc trong
`Task.FromResult(...)`.

**Khi nao NEN/KHONG dung** - Giong ly do o muc 2.6 (`Result.FailAsync`).

### 2.9 Result.FailSystem

**Signature**
```csharp
/// <summary>
/// Don't recommend use in function
/// </summary>
public static Result FailSystem(
    List<string> messages, ResultFTelCoreMetadataModel metadata, string serviceName,
    int statusCode = (int)HttpStatusCode.InternalServerError, ResultFTelCoreErrorModel error = null)

/// <summary>
/// Don't recommend use in function
/// </summary>
public static Result FailSystem(
    string message, ResultFTelCoreMetadataModel metadata, string serviceName,
    int statusCode = (int)HttpStatusCode.InternalServerError, ResultFTelCoreErrorModel error = null)
```
(`Result.cs:138-174`)

**Muc dich** - Tao `Result` bieu dien loi cap he thong: request bi tu choi/loi **truoc khi** den duoc ham
xu ly nghiep vu (`Dispatched = false`), kem theo metadata trace va ten service phat sinh loi.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate | Gia tri mac dinh |
|---|---|---|---|---|
| `messages` / `message` | `List<string>` / `string` | Co | Khong validate null | - |
| `metadata` | `ResultFTelCoreMetadataModel` | Co | Khong validate null | - |
| `serviceName` | `string` | Co | Khong validate null/empty | - |
| `statusCode` | `int` | Khong | Khong validate | `(int)HttpStatusCode.InternalServerError` (500) |
| `error` | `ResultFTelCoreErrorModel` | Khong | Khong validate | `null` -> fallback `_errorSystemDefault` |

**Output** - `Result` voi `Succeeded = false`; `Dispatched = false`; `Code = statusCode`; `Messages` = list
hoac `[message]`; `System = serviceName` (**ghi de** gia tri mac dinh cua property); `Status =
statusCode.ConvertHttpStatusCodeCodeByName()`; `Meta = metadata`; `Error = error ?? _errorSystemDefault`.

**Dieu kien xu ly** - Khong co nhanh re; hai overload chi khac o kieu `messages`/`message`.

**Side effect** - Khong co ben trong ham (khong tu log) - caller thuong tu log truoc/sau khi goi (vi du
`ExceptionHandlerMiddleWare` log exception rieng, khong nam trong pham vi file nay).

**Error handling** - Khong bat exception nao.

**Khi nao NEN dung** - Theo XML doc, day duoc ghi "Don't recommend use in function" (`Result.cs:128-137`,
`152-161`) - **tuy nhien thuc te trong repo, day chinh la cach duoc dung** cho cac tinh huong loi he thong
truoc khi vao handler: middleware xu ly exception toan cuc (`ExceptionHandlerMiddleWare.cs:47-51`) va JWT
event handler khi token het han/khong hop le/khong du quyen (`JWTBearerExtensions.cs:78-83`, `106-111`,
`150-155`, `179-184`) - deu goi `Result.FailSystem(...)` truc tiep. Day la **mau thuan giua XML doc va cach
dung thuc te trong code** - ghi vao muc 3, van de #5.

**Khi nao KHONG dung** - Trong logic nghiep vu thong thuong (business rule fail) - nen dung `Fail` (muc 2.5)
vi request van den duoc handler.

**Gioi han** - Khong co ban generic (`Result<T>.FailSystem` khong ton tai, xem van de #4); khong co ban async.

### 2.10 IResult\<T\>.Data / Result\<T\> - property va constructor

**Signature**
```csharp
public class Result<T> : Result, IResult<T>
{
    [JsonPropertyName("data")]
    public T Data { get; set; }

    public Result() { }

    private static readonly CatalogsErrorCodeModel _catalogsBadRequest = ...;
    private static readonly ResultFTelCoreErrorModel _failLogicDefault = new() { ... };
}
```
(`Result.cs:177-195`)

**Muc dich** - `Result<T>` ke thua toan bo property cua `Result` va bo sung `Data` (co the doc/ghi, khac
`IResult<T>.Data` tren interface chi co getter). Property `Data` khong co gia tri mac dinh khac
`default(T)` khi khong duoc factory method nao gan (vi du cac overload `Fail(string, ...)`/`Fail(List<string>,
...)` khong gan `Data`).

**Input hop le / Output** - Khong ap dung (property + constructor rong).

**Dieu kien xu ly** - Constructor `public Result() { }` (`Result.cs:182-184`) **khong co logic gi**, khac
voi constructor `protected Result()` cua lop cha (`Result.cs:34-36`).

**Side effect / Error handling** - Khong co.

**Khi nao NEN dung** - Nen luon tao `Result<T>` qua cac factory method (`Fail`/`Succeed`), khong nen tu goi
`new Result<T>()` truc tiep (mac du compiler cho phep - xem van de #1).

**Gioi han** - `Result<T>` tu khai bao lai `_catalogsBadRequest` va `_failLogicDefault` cua rieng no
(`Result.cs:186-195`), **khong tai su dung** field cung ten cua lop cha (`Result.cs:38-47`) vi field cha la
`private` nen khong ke thua duoc - day la trung lap logic/du lieu giua 2 lop (xem van de #6). `Result<T>`
**khong tu khai bao lai** `_catalogsInternalServerError`/`_errorSystemDefault` vi khong can (khong co
`FailSystem` rieng cho `Result<T>`).

### 2.11 Result\<T\>.Fail (khong Data)

**Signature**
```csharp
public new static Result<T> Fail(
    string message = "Thực hiện yêu cầu không thành công", bool succeeded = false,
    int statusCode = (int)HttpStatusCode.BadRequest, ResultFTelCoreErrorModel error = null)

public new static Result<T> Fail(
    List<string> messages, bool succeeded = false, int statusCode = (int)HttpStatusCode.BadRequest,
    ResultFTelCoreErrorModel error = null)
```
(`Result.cs:199-220`)

**Muc dich** - Tuong duong `Result.Fail` (muc 2.5) nhung tra kieu `Result<T>` (voi `Data = default(T)`, vi
khong overload nao gan `Data`). Modifier `new` chi de **an (hide)** method cung ten/cung tham so cua lop
cha khi truy cap qua ten kieu `Result<T>` - vi day la static method, `new` khong lien quan gi den virtual
dispatch/polymorphism runtime (static method khong the override).

**Input hop le/Output/Dieu kien xu ly/Side effect/Error handling** - Giong hoan toan muc 2.5, chi khac kieu
tra ve la `Result<T>` va `Data` luon la `default(T)`.

**Khi nao NEN dung** - Khi bien/tham so duoc khai bao voi kieu `Result<T>` cu the (khong phai `Result`) va
can tao truong hop loi khong co payload cu the (`Data` se la `default(T)`, vi du `null` voi reference type).

**Khi nao KHONG dung** - Neu bien duoc khai bao kieu `Result` (lop cha) hoac `IResult`, goi `Fail(...)` se
phan giai (resolve) ve method cua `Result`, khong phai ban `new` nay, du gia tri runtime la instance
`Result<T>` - can luu y de tranh nham lan ve overload duoc goi (day la hanh vi chuan cua C# method hiding,
khong phai bug).

**Gioi han** - `Data` luon `default(T)`, khong co cach nao truyen `Data` qua 2 overload nay (phai dung
overload co tham so `data`, muc 2.12).

### 2.12 Result\<T\>.Fail (co Data)

**Signature**
```csharp
public static Result<T> Fail(
    T data, string message = "Thực hiện yêu cầu không thành công", bool succeeded = false,
    int statusCode = (int)HttpStatusCode.BadRequest, ResultFTelCoreErrorModel error = null)

public static Result<T> Fail(
    T data, List<string> messages, bool succeeded = false, int statusCode = (int)HttpStatusCode.BadRequest,
    ResultFTelCoreErrorModel error = null)
```
(`Result.cs:222-246`)

**Muc dich** - Nhu muc 2.11 nhung cho phep gan `Data = data` du la truong hop loi - huu ich khi can tra lai
du lieu da nhap/partial data kem thong bao loi cho client.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate | Gia tri mac dinh |
|---|---|---|---|---|
| `data` | `T` | Co | Khong validate null | - |
| `message`/`messages` | `string`/`List<string>` | Overload 1 co default, overload 2 bat buoc | Khong validate | `"Thực hiện yêu cầu không thành công"` |
| `succeeded` | `bool` | Khong | Khong validate | `false` |
| `statusCode` | `int` | Khong | Khong validate | `400` |
| `error` | `ResultFTelCoreErrorModel` | Khong | Khong validate | `null` -> `_failLogicDefault` (ban rieng cua `Result<T>`) |

**Output** - `Result<T>` voi `Data = data`, cac field khac giong muc 2.5.

**Dieu kien xu ly/Side effect/Error handling** - Khong nhanh re, khong side effect, khong bat exception.

**Khi nao NEN dung** - Tra ve loi validate nhung van muon client nhan lai du lieu goc (echo) de hien thi lai
form, hoac tra ve du lieu partial da xu ly duoc truoc khi gap loi.

**Khi nao KHONG dung** - Khi khong co du lieu nao can echo lai - dung overload muc 2.11 cho gon.

**Gioi han** - Khong validate `data` co "hop ly" voi `error`/`statusCode` hay khong (hoan toan do caller
quyet dinh).

### 2.13 Result\<T\>.FailAsync (4 overload)

**Signature**
```csharp
public new static Task<Result<T>> FailAsync(string message = "...", bool succeeded = false, int statusCode = (int)HttpStatusCode.BadRequest, ResultFTelCoreErrorModel error = null)
public new static Task<Result<T>> FailAsync(List<string> messages, bool succeeded = false, int statusCode = (int)HttpStatusCode.BadRequest, ResultFTelCoreErrorModel error = null)
public static Task<Result<T>> FailAsync(T data, string message = "...", bool succeeded = false, int statusCode = (int)HttpStatusCode.BadRequest, ResultFTelCoreErrorModel error = null)
public static Task<Result<T>> FailAsync(T data, List<string> messages, bool succeeded = false, int statusCode = (int)HttpStatusCode.BadRequest, ResultFTelCoreErrorModel error = null)
```
(`Result.cs:248-262`)

**Muc dich/Input/Output/Dieu kien xu ly/Side effect/Error handling** - Wrapper `Task.FromResult(...)` cho 4
overload `Fail` tuong ung (muc 2.11, 2.12), khong them logic bat dong bo.

**Khi nao NEN/KHONG dung** - Giong ly do muc 2.6.

**Gioi han** - Giong muc 2.11/2.12 (theo tung overload duoc goi).

### 2.14 Result\<T\>.Succeed (4 overload)

**Signature**
```csharp
public static Result<T> Succeed(
    string message = "Thực hiện yêu cầu thành công", bool succeeded = true, int statusCode = (int)HttpStatusCode.OK)

public static Result<T> Succeed(
    List<string> messages, bool succeeded = true, int statusCode = (int)HttpStatusCode.OK)

public static Result<T> Succeed(
    T data, string message = "Thực hiện yêu cầu thành công", bool succeeded = true, int statusCode = (int)HttpStatusCode.OK)

public static Result<T> Succeed(
    T data, List<string> messages, bool succeeded = true, int statusCode = (int)HttpStatusCode.OK)
```
(`Result.cs:268-314`)

**Muc dich** - Tao `Result<T>` bieu dien ket qua thanh cong, co hoac khong co `Data`.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate | Gia tri mac dinh |
|---|---|---|---|---|
| `data` | `T` | Chi bat buoc o overload 3, 4 | Khong validate null | - |
| `message`/`messages` | `string`/`List<string>` | Tuy overload | Khong validate | `"Thực hiện yêu cầu thành công"` (khi la `string`) |
| `succeeded` | `bool` | Khong | Khong validate - **khac `Result.Succeed` (muc 2.7), o day co the truyen `false`** | `true` |
| `statusCode` | `int` | Khong | Khong validate | `(int)HttpStatusCode.OK` (200) |

**Output** - `Result<T>` voi `Succeeded = succeeded` (tham so, khong hardcode `true`); `Dispatched = true`;
`Code = statusCode`; `Messages` tuong ung; `Status = statusCode.ConvertHttpStatusCodeCodeByName()`; `Data =
data` (voi overload 3, 4) hoac giu `default(T)` (overload 1, 2).

**Dieu kien xu ly** - Khong nhanh re.

**Side effect / Error handling** - Khong co.

**Khi nao NEN dung** - Tra ket qua thanh cong kem/khong kem payload; dung tham so `succeeded` khi can bieu
dien mot trang thai "thanh cong ve mat HTTP nhung nghiep vu chua hoan tat" (vi du) - **nhung ten ham van la
`Succeed`**, xem canh bao o van de #3.

**Khi nao KHONG dung** - Neu muon dam bao `Succeeded` luon `true` mot cach cung, can tu kiem tra logic goi
(ham khong tu rang buoc).

**Gioi han** - Khong validate quan he giua `succeeded` va `statusCode` (co the goi `Succeed(succeeded: false,
statusCode: 200)` tao ra ket qua "HTTP 200 nhung Succeeded=false" - de gay nham lan cho consumer neu khong
doc ky JSON body).

### 2.15 Result\<T\>.SucceedAsync (4 overload)

**Signature**
```csharp
public static Task<Result<T>> SucceedAsync(string message = "...", bool succeeded = true, int statusCode = (int)HttpStatusCode.OK)
public static Task<Result<T>> SucceedAsync(List<string> messages, bool succeeded = true, int statusCode = (int)HttpStatusCode.OK)
public static Task<Result<T>> SucceedAsync(T data, string message = "...", bool succeeded = true, int statusCode = (int)HttpStatusCode.OK)
public static Task<Result<T>> SucceedAsync(T data, List<string> messages, bool succeeded = true, int statusCode = (int)HttpStatusCode.OK)
```
(`Result.cs:316-330`)

**Muc dich/Input/Output/Dieu kien xu ly/Side effect/Error handling** - Wrapper `Task.FromResult(...)` cho 4
overload `Succeed` tuong ung (muc 2.14).

**Khi nao NEN/KHONG dung** - Giong ly do muc 2.6.

**Gioi han** - Giong muc 2.14.

## 3. Van de da biet

| # | Van de | Vi tri | Anh huong |
|---|---|---|---|
| 1 | `Result` co constructor `protected` (khong the `new Result()` tu ngoai) nhung `Result<T>` (ke thua `Result`) lai tu khai bao constructor `public` khong tham so | `Result.cs:34-36` (protected), `Result.cs:182-184` (public) | Code ngoai co the goi `new Result<T>()` tao ra object voi toan bo field o gia tri mac dinh (`Code=0`, `Status=null`, `Messages=null`, `Succeeded=true` do gia tri khoi tao property...), bo qua hoan toan cac factory method va logic gan `Error`/`Status` chuan - du day khong phai cach dung khuyen nghi |
| 2 | `Error` mac dinh cua `Fail`/`FailSystem` (`_failLogicDefault`, `_errorSystemDefault`) duoc tinh 1 lan luc load type, luon gan voi `HttpStatusCode.BadRequest`/`InternalServerError` + `ErrorSourceType.General` co dinh, **khong lien he** voi tham so `statusCode` thuc te ma caller truyen vao method | `Result.cs:38-51` (khoi tao static field), `Result.cs:65,69-70` (`Fail` dung `_failLogicDefault` khi `error=null`, khong quan tam `statusCode` param) | Goi `Result.Fail(message: "...", statusCode: 404)` ma khong tu truyen `error` se tra ve `Code=404` nhung `Error.Code="BUSINESS_RULE_400"` (catalog cua 400) - client doc `Error.Code` co the hieu sai loai loi thuc te |
| 3 | `Result.Succeed` (non-generic) hardcode `Succeeded = true`, khong co tham so `succeeded`; trong khi `Result<T>.Succeed` co them tham so `succeeded` (default `true` nhung cho phep truyen `false`) | `Result.cs:97-116` (khong co param `succeeded`) vs `Result.cs:268-314` (co param `succeeded`) | Hai ham cung ten "Succeed" nhung API bat doi xung: voi `Result<T>`, goi `Succeed(..., succeeded: false)` tao ra object co ten factory la "Succeed" nhung `Succeeded=false` trong payload - de gay nham lan neu doc code khong ky |
| 4 | `Result<T>` khong dinh nghia lai (`new`) method `FailSystem` | Khong tim thay `FailSystem` nao trong pham vi `Result.cs:177-333` | Goi `Result<T>.FailSystem(...)` van compile duoc (vi static method duoc ke thua) nhung **tra ve kieu `Result`** (mat `Data`/generic), khong tra `Result<T>` nhu ky vong khi doc ten kieu goi |
| 5 | XML doc cua ca 2 overload `FailSystem` ghi "Don't recommend use in function" nhung code thuc te trong repo dung chinh `FailSystem` cho cac tinh huong loi he thong (middleware xu ly exception, JWT auth event) | XML doc: `Result.cs:128-137`, `152-161`. Usage thuc te: `ExceptionHandlerMiddleWare.cs:47-51`, `JWTBearerExtensions.cs:78-83,106-111,150-155,179-184` | Theo nguyen tac "source code la nguon xac thuc cao nhat" cua tai lieu nay: khuyen nghi trong XML doc **mau thuan** voi pattern dang duoc ap dung thuc te trong repo; developer moi doc XML doc co the tranh dung `FailSystem` mot cach khong can thiet du day dung la cong cu danh cho dung truong hop cua ho (loi truoc handler) |
| 6 | `Result<T>` tu khai bao lai (duplicate) `_catalogsBadRequest` va `_failLogicDefault` giong 100% logic cua lop cha `Result`, do field cha la `private` nen khong ke thua duoc | `Result.cs:38-47` (ban cua `Result`) vs `Result.cs:186-195` (ban rieng cua `Result<T>`) | Trung lap code; ca 2 static field deu doc tu cung nguon (`CatalogsErrorCodes.BadRequest`) nen gia tri runtime giong nhau, nhung day la 2 lan goi `ResponseWrapperByCodeMapper.FromStatusCode(...)` doc lap (chi phi khoi tao gap doi, va rui ro "sua mot noi quen noi kia" neu sau nay thay doi logic mapping) |
| 7 | Khong tim thay implicit/explicit `operator` nao trong `Result.cs` va `IResult.cs` (khong co conversion tu `T` sang `Result<T>`, hay tu `Result` sang `Result<T>` hoac nguoc lai) | Toan bo noi dung 2 file, khong co tu khoa `operator` | Moi lan can `Result<T>` PHAI goi factory method (`Succeed`/`Fail`) mot cach ro rang; day KHONG phai loai Result-pattern co implicit conversion nhu mot so thu vien khac (vi du FluentResults) - can nhan manh voi developer moi de tranh gia dinh sai |
