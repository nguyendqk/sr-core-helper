# Logger Extensions (LoggerExtensions & LoggerErrorCategoriesHelper)

> Nguon: FTELSRCore.Shared/Extensions/Loggers/LoggerExtensions.cs; FTELSRCore.Shared/Extensions/Loggers/Helpers/LoggerErrorCategoriesHelper.cs
> Loai: static class (2 file: 1 static class chinh + cac kieu noi bo internal enum/static class; helper file la 1 static class chua 5 nested static class hang so + 1 method logic)
> Cap nhat theo commit: 89c1ce9

## 1. Tong quan

`LoggerExtensions` la lop **extension method duy nhat cho `ILogger`** dung xuyen suot repo (`FTELSRCore.Extensions.Loggers` namespace) de ghi log co cau truc theo mot dinh dang co dinh `{ClassName} - {MethodName} -- ...`. Lop nay la tang thap nhat cua toan bo he thong logging: 8 file Knowledge Base khac (CallApiWithHttp, CallApi, CoreMongoDB, CoreSQL, CoreSQL-TwoEntity, UnitOfWork/DbContexts, Resilience, va giu Dapper it hon) deu goi truc tiep vao cac method cua lop nay (`HttpErrorResult`, `HttpResultWithTracing`, `FailLogic`, `Warning`, `Info`, `ErrorException`...) nhung chua file nao tai lieu hoa chinh ban than cac method do — day la muc dich cua file nay.

`LoggerErrorCategoriesHelper` la lop khai bao **hang so chuoi phan loai loi** (`errorCategory`) dung lam tham so cho cac method Error/FailLogic/HttpErrorResult/ConnectionError\* ben tren, chia thanh 5 nhom (`BusinessCategory`, `SecurityCategory`, `SystemCategory`, `ApiCategory`, `InfrastructureCategory`) va 1 method logic duy nhat `ApiCategory.ResolveCategory`.

Ca hai file nam o tang `Shared/Extensions`, khong chua business logic nghiep vu, khong tu goi HTTP/DB/MQ nao — chung thuan tuy dinh nghia **cach ghi log** cho cac tang phia tren.

### 1.1 Pham vi chuc nang

| Lam duoc | Khong lam duoc |
|---|---|
| Cung cap ~24 extension method ghi log voi template co dinh, gan `EventId` so va ten cho tung loai log (Request/Response/Info/Error/Http/Kafka/MediaR/Connection) | **Khong cho phep caller tuy chinh `LogLevel`** — moi method co `LogLevel` hardcode ngay trong dinh nghia (vi du `Warning` luon la `LogLevel.Warning`, khong co tham so nao de doi thanh `Error` hay nguoc lai) |
| Tu dong chuyen doi `message` (kieu `object`) sang chuoi hien thi: giu nguyen neu la `int`/`string`, serialize JSON (`System.Text.Json`, options mac dinh) neu la kieu khac, `string.Empty` neu `null` | **Khong bat exception cua `JsonSerializer.Serialize(message)`** o hau het method (tru 2 overload `Info` co `try/catch`) — object khong serialize duoc (vi du co tham chieu vong) se lam nem exception ngay tai dong log, co the che khuat exception nghiep vu goc |
| Tu tinh `LatencyRating` (Fast/Normal/Slow/TimeoutRisk) tu so ms de gan vao log tracing (`Response`, `MediaRResult`, `HttpResultWithTracing`) | **Khong ghi `Exception` object thuc su vao structured log** cho nhom method dung `logger.Log(...)` ad-hoc voi tham so `Exception e` (`HttpErrorResult` overload co `e`, `HttpResultWithTracing` (khong co `e`), `ErrorException`, `KafkaErrorException`, `ConnectionError*`) — cac method nay chi noi `e?.Message` va `e?.StackTrace` **da Trim() thanh string** vao template, KHONG truyen doi tuong `Exception` cho `ILogger.Log`. Chi nhom dung `LoggerMessage.Define` compiled delegate (`Warning`, `Debug`, `Request`, `Response`, `Info`, `FailLogic`, `HttpResult`, `MediaRResult`, `Kafka`, `KafkaErrorResult`, `KafkaErrorWithoutTopic`, `ErrorResult`, `Error`, `Connection`) moi thuc su truyen `Exception` vao `ILogger.Log<TState>` (xem muc 2 va muc 3, phat hien #1) |
| Phan loai loi theo HTTP status code qua `ApiCategory.ResolveCategory(int)` (dung 1 noi duy nhat trong repo: `HttpResultWithTracing`) | **Khong validate `className`/`methodName`/`message` co null hay khong** — moi tham so string deu duoc dua thang vao template, khong throw, khong warn |
| Cung cap 23 hang so chuoi phan loai loi co san, nhom theo Business/Security/System/API/Infrastructure | Nhieu hang so (toan bo `SystemCategory`, `BIZ_VALIDATION`, `BIZ_NOT_FOUND`, `BIZ_DUPLICATE`, `SEC_INJECTION`, `SEC_BRUTE_FORCE`, va 4/5 hang so cua `ApiCategory`) **khong duoc tham chieu o bat ky noi nao khac trong repo** — xem muc 2, bang hang so |

### 1.2 Dependency

| Thanh phan | Muc dich su dung |
|---|---|
| `Microsoft.Extensions.Logging.ILogger` / `LoggerMessage.Define<...>` | Nen tang cua toan bo extension method; `LoggerMessage.Define` tao delegate cache-cap-cao-hieu-nang thay cho goi `logger.Log` truc tiep moi lan |
| `System.Text.Json.JsonSerializer` | Serialize `message`/`parameters` kieu object phuc tap thanh JSON string truoc khi ghi log (khong co `JsonSerializerOptions` tuy chinh — dung mac dinh) |
| `MongoDB.Driver.FilterDefinition<T>`, `MongoDB.Bson.*` | Chi dung trong 2 overload `Info` danh rieng cho MongoDB de render `FilterDefinition`/`List<BsonDocument>` thanh JSON truoc khi log |
| `Confluent.Kafka` (using, khong dung truc tiep type nao trong file) | Import nhung khong thay type Kafka nao duoc su dung truc tiep trong `LoggerExtensions.cs` — co the la using thua |
| `SRKafkaLogFormatter.DirectionType` (`FTELSRCore.Infrastructure.Extensions.Helpers.SerilogProviderExtensions.Formatters`) | Enum `Inbound`/`Outbound` dung lam tham so `direction` cua `HttpResultWithTracing` |
| `LoggerErrorCategoriesHelper` (file thu 2 trong module nay) | Cung cap hang so `errorCategory` cho `FailLogic`, `ErrorResult`, `Error`, `ErrorException`, `HttpErrorResult`, `KafkaErrorException`, `ConnectionError*`, `HttpResultWithTracing` |

### 1.3 Danh muc API

| API | Nhom | Mo ta ngan |
|---|---|---|
| `Warning` | Chung | Log `Warning` don gian, khong category, khong tracing |
| `Debug` | Chung | Log `Debug` don gian — **khong co call site nao trong repo dung method nay** |
| `Request(className, methodName, e=null)` | Request | Log `Information` "request" khong tham so |
| `Request(className, methodName, requestName, parameters, e=null)` | Request | Log `Information` "request" kem ten request + tham so |
| `Response(className, methodName, e=null)` | Response | Log `Information` "response" khong tham so |
| `Response(className, methodName, message, e=null)` | Response | Log `Information` "response" kem message |
| `Response(className, methodName, latency, message, e=null)` | Response | Log `Information` "response" kem latency + `LatencyRating` |
| `Info(className, methodName, message, e=null)` | Information | Log `Information` chung |
| `Info<T>(className, methodName, FilterDefinition<T>, e=null)` | Information (Mongo) | Render filter Mongo thanh JSON roi log `Information`; loi render -> tu goi `ErrorException` |
| `Info(className, methodName, List<BsonDocument>, e=null)` | Information (Mongo) | Render list BsonDocument (explain plan) thanh JSON roi log `Information`; loi render -> tu goi `ErrorException` |
| `FailLogic` | Information (business) | Log `Information` (**khong phai Error**) khi guard clause chan mot luong nghiep vu; category luon cung `BIZ_LOGIC`, khong nhan tham so category |
| `HttpResult` | Http | Log `Information` ket qua HTTP thanh cong |
| `HttpErrorResult(className, methodName, message)` | Http | Log `Error` loi HTTP, **khong nhan Exception** |
| `HttpErrorResult(className, methodName, message, e)` | Http | Log `Error` loi HTTP kem message/stacktrace cua `e` (dang string, khong dang Exception object) |
| `HttpResultWithTracing` | Http | Log `Information` day du tracing (latency, status code, category tu status code, direction, system owner) |
| `MediaRResult` | MediaR | Log `Information` tracing cho MediatR — **khong chuan hoa `message` qua switch nhu cac method khac** |
| `KafkaErrorResult` | Kafka | Log `Error` loi Kafka co topic |
| `KafkaErrorWithoutTopic` | Kafka | Log `Error` loi Kafka khong topic — dung chung `EventId` Name voi `KafkaErrorResult` |
| `Kafka` | Kafka | Log `Information` cho hoat dong Kafka thong thuong |
| `KafkaErrorException` | Kafka | Log `Error` tu exception Kafka, co/khong topic; dung lai `EventId` cua `KafkaErrorResult` (khong co ten rieng) |
| `ErrorResult` | Error | Log `Error` chung, cho phep truyen `errorCategory` tuy chon |
| `Error` | Error | Log `Error` chung, cho phep truyen `errorCategory` tuy chon |
| `ErrorException` | Error | Log `Error` tu mot `Exception`, cho phep truyen `errorCategory` tuy chon |
| `Connection` | Connection | Log `Information` chung ve trang thai ket noi |
| `ConnectionErrorSQL` | Connection | Log `Error` ket noi, category co dinh `DB_SQLSERVER` |
| `ConnectionErrorMongoDB` | Connection | Log `Error` ket noi, category co dinh `DB_MONGODB` |
| `ConnectionErrorRedis` | Connection | Log `Error` ket noi, category co dinh `DB_REDIS` |
| `ConnectionErrorKafka` | Connection | Log `Error` ket noi Kafka, co/khong topic, category co dinh `MQ_KAFKA` — **khong tai su dung** helper `ConnectionError` private nhu 5 wrapper khac |
| `ConnectionErrorElasticSearch` | Connection | Log `Error` ket noi, category co dinh `DB_ELASTICSEARCH` |
| `ConnectionErrorRabbitMQ` | Connection | Log `Error` ket noi, category co dinh `MQ_RABBITMQ` |
| `LoggerErrorCategoriesHelper.BusinessCategory.*` | Hang so | 4 hang so phan loai loi nghiep vu |
| `LoggerErrorCategoriesHelper.SecurityCategory.*` | Hang so | 4 hang so phan loai loi bao mat |
| `LoggerErrorCategoriesHelper.SystemCategory.*` | Hang so | 4 hang so phan loai loi tai nguyen he thong |
| `LoggerErrorCategoriesHelper.ApiCategory.*` (hang so) | Hang so | 5 hang so phan loai loi HTTP/API |
| `LoggerErrorCategoriesHelper.ApiCategory.ResolveCategory(int)` | Method logic | Map HTTP status code -> 1 trong 5 hang so `ApiCategory`, hoac `string.Empty` |
| `LoggerErrorCategoriesHelper.InfrastructureCategory.*` | Hang so | 6 hang so phan loai loi ha tang (DB/MQ) |

## 2. Chi tiet API — `LoggerExtensions`

### 2.0 Kieu noi bo lien quan (khong public, nhung anh huong truc tiep cach doc log)

**`internal enum LatencyRating`** (`LoggerExtensions.cs:12-18`): `Fast = 1`, `Normal = 2`, `Slow = 3`, `TimeoutRisk = 4`. Chi dung ten (`nameof`) qua `LatencyRatingData(long latency)` (`:243-252`, `private static`):

| Dieu kien `latency` (ms) | Ket qua |
|---|---|
| `< 1_000` (**bao gom moi gia tri am** — switch danh gia tu tren xuong, nhanh dau tien khop la nhanh nay, khong co nhanh rieng cho so am) | `Fast` |
| `>= 1_000` va `< 3_000` | `Normal` |
| `>= 3_000` va `< 10_000` | `Slow` |
| `>= 10_000` | `TimeoutRisk` |

**`internal static class EventIds`** (`:20-61`): dinh nghia ma so `EventId` dung trong cac `LoggerMessage.Define`/`logger.Log`. Bang tham chieu (dung de tra cuu log theo `EventId` khi dieu tra su co):

| Ten hang so | Gia tri | Dung boi method |
|---|---|---|
| `Request` | 101 | `Request` (ca 2 overload) |
| `Response` | 102 | `Response` (ca 3 overload) |
| `Warning` | 103 | `Warning` |
| `Debug` | 104 | `Debug` |
| `Info` | 105 | `Info` (ca 3 overload), `Kafka` (dat ten `"Kafka"` nhung dung ma so 105) |
| `HttpResult` | 105001 | `HttpResult`, `HttpResultWithTracing` |
| `Connection` | 105002 | `Connection` |
| `MediaRResult` | 105003 | `MediaRResult` |
| `Error` | 106 | `Error` |
| `FailLogic` | 107 | `FailLogic` |
| `ErrorException` | 106001 | `ErrorException` |
| `ErrorResult` | 106002 | `ErrorResult` |
| `HttpErrorResult` | 106003 | `HttpErrorResult` (ca 2 overload) |
| `MediaRErrorResult` | 106004 | **Khong co method nao trong file dung hang so nay** — dinh nghia thua |
| `KafkaErrorResult` | 106005 | `KafkaErrorResult`, `KafkaErrorWithoutTopic`, va ca `KafkaErrorException` (dung lai cung Id+Name — xem muc 3) |
| `ConnectionError` | 106006 | `ConnectionErrorSQL/MongoDB/Redis/Kafka/ElasticSearch/RabbitMQ` (qua helper `ConnectionError` private) |

> Ghi chu: region comment trong source ghi la `ERORR` (`:107,113`, `:204,216`, `:595,644`) — loi chinh ta cua tu "ERROR" trong chinh source, giu nguyen de doi chieu, khong anh huong runtime.

---

### 2.1 Warning

**Signature**
```csharp
public static void Warning(this ILogger logger, string className, string methodName, object message, Exception e = null)
```
**Muc dich** — Ghi 1 dong log muc `Warning` chung, dinh dang `"{ClassName} - {MethodName} -- warning: {Message}"`.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `className` | `string` | Co (khong check null) | Khong validate | — |
| `methodName` | `string` | Co (khong check null) | Khong validate | — |
| `message` | `object` | Co | `switch`: `null`->`""`; `int`->giu nguyen; `string`->giu nguyen; kieu khac -> `JsonSerializer.Serialize(message)` | — |
| `e` | `Exception` | Khong | Khong check | `null` |

**Output** — `void`.

**Dieu kien xu ly** — Khong co nhanh re; luon goi delegate `_warning` (compiled boi `LoggerMessage.Define<string,string,object>(LogLevel.Warning, ...)`, `:134-137`).

**Side effect** — Ghi 1 dong log qua `ILogger`. Khong ghi DB, khong goi ngoai, khong mutate tham so.

**Error handling** — Khong co `try/catch`. `JsonSerializer.Serialize(message)` co the nem exception (serialize fail) neu `message` la object phuc tap khong serialize duoc — se nem thang ra caller, **truoc khi** dong log duoc ghi.

**Khi nao NEN dung** — Canh bao khong lam gian doan luong xu ly (vi du: cham hon nguong mong doi, retry thanh cong sau 1 lan loi).

**Khi nao KHONG dung** — Loi thuc su lam that bai request/nghiep vu (dung `Error`/`ErrorResult`/`ErrorException`).

**Gioi han** — `e` neu duoc truyen SE duoc dinh dang boi formatter cua LoggerMessage delegate (thuc su la doi tuong `Exception`, khong bi giam thanh string — khac voi nhom `ConnectionError*`/`ErrorException` dung `logger.Log` ad-hoc). Khong co gioi han do dai message.

---

### 2.2 Debug

**Signature**
```csharp
public static void Debug(this ILogger logger, string className, string methodName, object message, Exception e = null)
```
**Muc dich** — Ghi 1 dong log muc `Debug`, dinh dang `"{ClassName} - {MethodName} -- debug: {Message}"`.

**Input hop le / Output / Dieu kien xu ly / Side effect / Error handling** — Giong hoan toan `Warning` (muc 2.1), chi khac `LogLevel.Debug` va delegate `_debug` (`:129-132`, `EventId` = 104).

**Khi nao NEN dung** — Thong tin chi tiet phuc vu debug local/dev, khong can trong production.

**Khi nao KHONG dung** — Moi truong production voi minimum level >= `Information` (log se khong duoc ghi, vo nghia).

**Gioi han** — **Khong xac dinh duoc tu source code cua repo** ly do vi sao method nay ton tai nhung khong co call site nao (`grep "\.Debug("` toan repo = 0 ket qua ngoai file dinh nghia). Co the la du bi cho tuong lai hoac da bi thay the boi cach ghi log khac.

---

### 2.3 Request (khong tham so)

**Signature**
```csharp
public static void Request(this ILogger logger, string className, string methodName, Exception e = null)
```
**Muc dich** — Ghi 1 dong log `Information` bao dau mot request, khong kem noi dung.

**Output** — `void`. **Dieu kien xu ly** — Goi truc tiep `_requestWithoutParams` (`:146-149`, template `"-- request"`, `EventId` = 101 nhung Name = `"RequestWithoutParams"`). **Side effect** — Ghi 1 dong log. **Error handling** — Khong co.

**Khi nao NEN dung** — Diem vao method khi khong can/khong nen log tham so dau vao (du lieu nhay cam, hoac khong co tham so).

**Gioi han** — Khong log duoc thoi diem bat dau de tinh latency (khong co timestamp tra ve) — caller phai tu do latency rieng (xem `Response(...,latency,...)`).

### 2.4 Request (co tham so)

**Signature**
```csharp
public static void Request(this ILogger logger, string className, string methodName, string requestName, object parameters, Exception e = null)
```
**Muc dich** — Ghi 1 dong log `Information` bao dau request kem ten request va tham so.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc | Mac dinh |
|---|---|---|---|---|
| `requestName` | `string` | Co | Khong validate | — |
| `parameters` | `object` | Co | Cung switch null/int/string/JSON nhu muc 2.1 | — |

**Output** — `void`. **Dieu kien xu ly** — Goi `_request` (`:141-144`, `EventId` = 101, Name = `"Request"`, template kem `{RequestName}` va `{Parameters}`). **Side effect** — Ghi 1 dong log co the chua **toan bo tham so dau vao dang JSON** — rui ro log du lieu ca nhan/nhay cam neu `parameters` chua PII. **Error handling** — Khong co (loi serialize nem thang ra ngoai).

**Khi nao KHONG dung** — Khi `parameters` co the chua thong tin nhay cam (mat khau, token, PII) — khong co co che mask/redact trong code.

---

### 2.5 Response (khong tham so) / 2.6 Response (co message) / 2.7 Response (co latency)

**Signature**
```csharp
public static void Response(this ILogger logger, string className, string methodName, Exception e = null);
public static void Response(this ILogger logger, string className, string methodName, object message, Exception e = null);
public static void Response(this ILogger logger, string className, string methodName, long latency, object message, Exception e = null);
```
**Muc dich** — 3 overload doi xung voi `Request`, ghi log `Information` khi ket thuc mot xu ly: khong noi dung / kem message / kem latency+`LatencyRating`.

**Input hop le (overload co latency)**

| Tham so | Kieu | Bat buoc | Rang buoc | Mac dinh |
|---|---|---|---|---|
| `latency` | `long` | Co | Khong validate am/duong; dua vao `LatencyRatingData` (muc 2.0) | — |
| `message` | `object` | Co | Switch null/int/string/JSON | — |

**Output** — `void`.

**Dieu kien xu ly** — Overload latency goi `_responseWithTracing` (`:165-168`, `EventId` = 102 Name `"ResponseWithTracing"`, template co `{ResponseTimeMs}` va `{LatencyRating}`). 2 overload con lai goi `_responseWithoutParams`/`_response` (`:160-163`, `:155-158`; Name `"ResponseWithoutParams"`/`"Response"`, cung Id = 102).

**Side effect** — Ghi 1 dong log. **Error handling** — Khong co try/catch trong ca 3 overload.

**Khi nao NEN dung** — Overload latency danh cho diem ket thuc mot loi goi co do thoi gian (vi du sau khi goi DB/HTTP xong); 2 overload kia cho cac truong hop don gian hon.

**Gioi han** — `LatencyRatingData` khong phan biet duoc latency am (bug logic, `<1000` van dung cho ca so am) do do neu `latency` la gia tri sai (vi du do dong ho he thong lech), ket qua se bao `Fast` mot cach sai lech; **khong xac dinh duoc tu source code** liet caller co bao gio truyen `latency` am hay khong.

---

### 2.8 Info (message chung)

**Signature**
```csharp
public static void Info(this ILogger logger, string className, string methodName, object message, Exception e = null)
```
**Muc dich** — Ghi 1 dong log `Information` chung, dinh dang `"-- info: {Message}"`.

**Dieu kien xu ly / Side effect / Error handling** — Giong `Warning` (muc 2.1), dung delegate `_info` (`:174-177`, `EventId` = 105).

### 2.9 Info\<T\>(FilterDefinition\<T\>)

**Signature**
```csharp
public static void Info<T>(this ILogger logger, string className, string methodName, FilterDefinition<T> parameters, Exception e = null) where T : class
```
**Muc dich** — Render mot MongoDB `FilterDefinition<T>` thanh JSON (qua `BsonSerializer.SerializerRegistry`) roi ghi log `Information` — dung khi can log dieu kien truy van Mongo dang human-readable.

**Input hop le** — `parameters`: bat buoc; khong co check null truoc khi goi `.Render(...)` — neu `parameters == null`, `NullReferenceException` se bi bat boi `catch` ben duoi va chuyen thanh log `Error` (khong nem tiep).

**Output** — `void`.

**Dieu kien xu ly** — `try`: `parameters.Render(new RenderArgs<T>(BsonSerializer.SerializerRegistry.GetSerializer<T>(), BsonSerializer.SerializerRegistry))` -> `bsonFilterElements?.ToJSon()` -> ghi log qua `_info`. `catch (Exception exception)`: goi `ErrorException(logger, className, methodName, message: $"Error while rendering FilterDefinition of {typeof(T)?.Name}", e: exception)` (muc 2.20) — **khong nem lai**, method tra ve binh thuong nhu khong co loi.

**Side effect** — Ghi 1 dong log `Information` (thanh cong) HOAC 1 dong log `Error` qua `ErrorException` (that bai) — khong bao gio ca hai, khong bao gio khong co dong nao.

**Error handling** — Bat toan bo `Exception`; chuyen thanh log `Error` roi **nuot loi** (khong nem lai). Caller khong biet duoc viec log da that bai.

**Khi nao NEN dung** — Debug/trace cac truy van Mongo phuc tap (vi du truoc khi goi `Find`/`Update`).

**Gioi han** — Neu render loi, **thong tin filter goc bi mat hoan toan** khoi log (chi con thong bao loi chung `"Error while rendering FilterDefinition of {T}"`), khong co fallback ghi lai filter duoi dang khac.

### 2.10 Info(List\<BsonDocument\>)

**Signature**
```csharp
public static void Info(this ILogger logger, string className, string methodName, List<BsonDocument> parameters, Exception e = null)
```
**Muc dich** — Serialize danh sach `BsonDocument` (thuong la explain plan Mongo) thanh JSON (`RelaxedExtendedJson`, khong indent) roi ghi log `Information`.

**Dieu kien xu ly** — `try`: `parameters.ToJson(new JsonWriterSettings { Indent = false, OutputMode = JsonOutputMode.RelaxedExtendedJson })` -> ghi qua `_info`. `catch`: goi `ErrorException(..., message: "Error while rendering BsonDocument[]", e: exception)`, **khong nem lai**.

**Side effect/Error handling** — Giong muc 2.9.

**Gioi han** — `parameters == null` -> `NullReferenceException` ngay tai `.ToJson(...)`, duoc `catch` bat va chuyen thanh log loi (hanh vi giong muc 2.9, khong nem tiep).

---

### 2.11 FailLogic

**Signature**
```csharp
public static void FailLogic(this ILogger logger, string className, string methodName, object message, Exception e = null)
```
**Muc dich** — Ghi log khi mot **guard clause nghiep vu** (khong phai exception ha tang) chan luong xu ly — vi du input rong, entity null, script SQL rong (theo cach 8 file KB da co dang dung).

**Input hop le** — Giong muc 2.1; **khong co tham so `errorCategory`** — category luon co dinh.

**Output** — `void`.

**Dieu kien xu ly** — Goi `_failLogic(logger, className, methodName, LoggerErrorCategoriesHelper.BusinessCategory.BIZ_LOGIC, message-switch, e)` — **luon** gan category = `BIZ_LOGIC`, khong the doi thanh `BIZ_VALIDATION`/`BIZ_NOT_FOUND`/`BIZ_DUPLICATE` du cac hang so nay ton tai trong `LoggerErrorCategoriesHelper`.

**Side effect** — Ghi 1 dong log **muc `LogLevel.Information`** (`:179-182`) — **khong phai** `Warning` hay `Error`, du day la "loi nghiep vu".

**Error handling** — Khong co try/catch.

**Khi nao NEN dung** — Dung o guard clause dau method repository (nhu `CoreSQL`/`CoreMongoDB` dang lam) de ghi nhan "input khong hop le -> tra ve default", khong phai exception.

**Khi nao KHONG dung** — Khi can phan loai category khac `BIZ_LOGIC` — method nay khong ho tro; phai dung `ErrorResult`/`Error` thay the.

**Gioi han** — Muc log la `Information`, **rat de bi bo lot** neu he thong cau hinh minimum level tu `Warning` len (khop voi nhan dinh da co trong `Data-SQL-CoreSQL.md:1219` va `Data-SQL-CoreSQL-TwoEntity.md:1218` — xem xac nhan o muc 3).

---

### 2.12 HttpResult

**Signature**
```csharp
public static void HttpResult(this ILogger logger, string className, string methodName, object message, Exception e = null)
```
**Muc dich** — Ghi log `Information` ket qua HTTP thanh cong, dinh dang co tien to `"------------HTTP------------ "`.

**Dieu kien xu ly/Side effect/Error handling** — Giong muc 2.1, dung delegate `_httpResult` (`:188-191`, `EventId` = 105001).

---

### 2.13 HttpErrorResult (khong Exception)

**Signature**
```csharp
public static void HttpErrorResult(this ILogger logger, string className, string methodName, object message)
```
**Muc dich** — Ghi log `Error` mot ket qua HTTP loi, **khi khong co (hoac khong muon truyen) doi tuong exception**.

**Output** — `void`.

**Dieu kien xu ly** — Goi truc tiep `logger.Log(LogLevel.Error, new EventId(106003, "HttpErrorResult"), template, className, methodName, BusinessCategory.BIZ_LOGIC, message-switch)` (`:428-440`) — **category luon co dinh `BIZ_LOGIC`**, khong nhan tham so category.

**Side effect** — Ghi 1 dong log `Error`. Khong ghi Exception (khong co tham so nay).

**Error handling** — Khong co try/catch trong chinh method; day la method **danh cho nhanh catch khong can/khong co exception** (theo cach `Utilizes-CallApi.md` dang dung cho `OperationCanceledException`).

**Khi nao NEN dung** — Nhanh catch ma exception khong mang thong tin them (vi du timeout do chinh code tu huy, khong phai loi tu he thong khac).

**Khi nao KHONG dung** — Khi can dieu tra loi sau nay bang stack trace — method nay **khong co cach nao ghi stack trace** vi khong nhan `Exception`.

**Gioi han** — Category luon `BIZ_LOGIC` bat ke ban chat loi HTTP thuc su la gi (4xx/5xx/timeout) — khac voi `HttpResultWithTracing` co the tu suy category tu status code qua `ApiCategory.ResolveCategory`.

### 2.14 HttpErrorResult (co Exception)

**Signature**
```csharp
public static void HttpErrorResult(this ILogger logger, string className, string methodName, object message, Exception e)
```
**Muc dich** — Ghi log `Error` ket qua HTTP loi kem thong tin exception.

**Dieu kien xu ly** — `logger.Log(LogLevel.Error, new EventId(106003,"HttpErrorResult"), template + "\n-- {ErrorMessage}\n-- {StackTrace}", className, methodName, BIZ_LOGIC, message-switch, e?.Message?.Trim(), e?.StackTrace?.Trim())` (`:445-457`).

**Side effect** — Ghi 1 dong log `Error` **co chua noi dung message + stack trace cua exception duoi dang text** trong than log message.

**Error handling** — **QUAN TRONG**: `e` duoc nhan nhu mot tham so C#, nhung **KHONG duoc truyen tiep vao `ILogger.Log` nhu mot doi tuong `Exception`** — chi 2 thuoc tinh `e.Message` va `e.StackTrace` (da `.Trim()`) duoc noi vao chuoi template. Overload `Log(ILogger, LogLevel, EventId, string, object[])` cua BCL luon truyen `exception: null` cho `ILogger.Log<TState>` ben trong; vi vay **cac sink log co xu ly rieng field `Exception`** (Application Insights, Serilog exception enricher, ELK "exception" field...) se **khong thay duoc exception nao** o day, chi co text trong `{Message}`. `InnerException`, `Data`, cac thuoc tinh rieng cua exception con (vi du `SqlException.Number`) **deu bi mat hoan toan**, khong chi mat "stack trace" nhu 1 KB cu da ghi nhan mot phan (xem muc 3, phat hien #1).

**Khi nao NEN dung** — Nhanh catch co exception thuc su can ghi nhan text loi + stack (nhung KHONG can he thong logging phia sau nhan dien duoc field Exception).

**Gioi han** — Xem phat hien #1, muc 3.

---

### 2.15 HttpResultWithTracing

**Signature**
```csharp
public static void HttpResultWithTracing(
    this ILogger logger,
    string className, string methodName,
    string statusCode, string httpMethod,
    long responseTimeMs,
    string uri,
    object message,
    string systemOwner = "",
    string uriWithQuery = "",
    DirectionType direction = DirectionType.Inbound)
```
**Muc dich** — Ghi 1 dong log `Information` day du nhat trong toan lop: HTTP method, endpoint (+query), system owner, direction, status code, latency + latency rating, va **category loi tu status code**.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate | Mac dinh |
|---|---|---|---|---|
| `statusCode` | `string` | Co | `int.TryParse(statusCode, ...)`; parse loi -> dung `0` de tinh category | — |
| `httpMethod`, `uri` | `string` | Co | Khong validate | — |
| `responseTimeMs` | `long` | Co | Dua vao `LatencyRatingData` | — |
| `message` | `object` | Co | Switch null/int/string/JSON | — |
| `systemOwner` | `string` | Khong | Khong validate | `""` |
| `uriWithQuery` | `string` | Khong | Khong validate | `""` |
| `direction` | `DirectionType` (`Inbound`/`Outbound`) | Khong | Khong validate | `DirectionType.Inbound` |

**Output** — `void`.

**Dieu kien xu ly** — Goi truc tiep `logger.Log(LogLevel.Information, new EventId(105001,"HttpResultWithTracing"), template, className, methodName, httpMethod, uri, uriWithQuery, systemOwner, direction, statusCode, responseTimeMs, LatencyRatingData(responseTimeMs), LoggerErrorCategoriesHelper.ApiCategory.ResolveCategory(int.TryParse(statusCode,...) ? parsed : 0), message-switch)` (`:471-488`). **Day la noi duy nhat trong toan repo goi `ApiCategory.ResolveCategory`.**

**Side effect** — Ghi 1 dong log `Information`. **Khong ghi Exception** — method nay khong co tham so `Exception`.

**Error handling** — Khong co try/catch.

**Khi nao NEN dung** — Dong log tracing chinh trong `finally` cua moi ham goi HTTP ngoai (dung nhu `CallApiWithHttp`/`CallApi` dang lam).

**Gioi han** — Neu `uriWithQuery` khong duoc caller truyen (mac dinh `""`), truong `{EndpointWithQuery}` trong log se **luon rong**, sinh ra dang log `Endpoint:{uri}.` (co dau `.` du) — khop voi phat hien da co trong `Utilizes-CallApiWithHttp.md:818` (xem xac nhan muc 3). Category loi (`ApiCategory.ResolveCategory`) duoc tinh **ke ca voi status code 2xx/3xx thanh cong** — ket qua se la `string.Empty` (khong phai `null` — xem phat hien #2, muc 3), nen truong `{ErrorCategory}` trong log thanh cong se hien thi la chuoi rong, khong phai "khong ap dung".

---

### 2.16 MediaRResult

**Signature**
```csharp
public static void MediaRResult(this ILogger logger, string className, string methodName, long latency, object message, Exception e = null)
```
**Muc dich** — Ghi log `Information` tracing cho MediatR, tuong tu `Response(...,latency,...)` nhung tien to `"------------MEDIAR------------ "`.

**Input hop le** — Giong cac method khac ve `className`/`methodName`/`latency`; **`message` KHONG duoc chuan hoa qua switch null/int/string/JSON** nhu moi method khac trong file.

**Output** — `void`.

**Dieu kien xu ly** — Goi truc tiep `_mediaRResultWithTracing(logger, className, methodName, latency, LatencyRatingData(latency), message, e)` (`:495-498`) — **`message` duoc truyen thang, khong qua buoc serialize/normalize**.

**Side effect** — Ghi 1 dong log. Formatter cua `LoggerMessage.Define` se goi `message.ToString()` (hoac dua vao structured logging provider) de hien thi — neu `message` la mot object phuc tap (khong override `ToString()`), log se hien ra ten kieu (`Namespace.TypeName`) thay vi noi dung, khac voi hanh vi JSON-hoa cua tat ca method con lai.

**Error handling** — Khong co.

**Gioi han** — **Day la method duy nhat trong `LoggerExtensions.cs` khong ap dung buoc chuan hoa `message` switch** (xem phat hien #3, muc 3) — hanh vi khong nhat quan voi `Response`, `Info`, `HttpResult`, v.v. **Khong xac dinh duoc tu source code** day la co y (vi MediatR message da la string san) hay la thieu sot khi viet code, vi khong co comment giai thich.

---

### 2.17 KafkaErrorResult

**Signature**
```csharp
public static void KafkaErrorResult(this ILogger logger, string className, string methodName, string topic, object message, Exception e = null)
```
**Muc dich** — Ghi log `Error` khi xu ly Kafka loi, co ten topic.

**Dieu kien xu ly** — Goi `_kafkaErrorResult` (`:225-228`, compiled `LoggerMessage.Define`, `EventId` = 106005 Name `"KafkaErrorResult"`) — **truyen dung `Exception` object** (khac voi `KafkaErrorException` ben duoi).

**Side effect/Error handling** — Ghi 1 dong log; khong try/catch.

### 2.18 KafkaErrorWithoutTopic

**Signature**
```csharp
public static void KafkaErrorWithoutTopic(this ILogger logger, string className, string methodName, object message, Exception e = null)
```
**Muc dich** — Giong muc 2.17 nhung khong co truong topic trong template.

**Dieu kien xu ly** — Goi `_kafkaErrorWithoutTopic` (`:230-233`) — **cung `EventId` Id=106005 va Name=`"KafkaErrorResult"`** (khong co Name rieng `"KafkaErrorWithoutTopic"`) — xem phat hien #4, muc 3.

### 2.19 Kafka

**Signature**
```csharp
public static void Kafka(this ILogger logger, string className, string methodName, string topic, object message, Exception e = null)
```
**Muc dich** — Ghi log `Information` cho hoat dong Kafka thong thuong (khong phai loi), co topic.

**Dieu kien xu ly** — Goi `_kafka` (`:220-223`, `EventId` = 105 ("Info") nhung Name = `"Kafka"`).

### 2.20 KafkaErrorException

**Signature**
```csharp
public static void KafkaErrorException(this ILogger logger, string className, string methodName, Exception e, object message = null, string topic = "")
```
**Muc dich** — Ghi log `Error` tu mot exception xay ra trong luong Kafka, tu dong chon nhanh co/khong topic.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc | Mac dinh |
|---|---|---|---|---|
| `e` | `Exception` | Co (khong null-check) | Truy cap `e?.Message`, `e?.StackTrace` — an toan voi `e = null` (chi ra `""`) | — |
| `message` | `object` | Khong | Switch null/int/string/JSON | `null` |
| `topic` | `string` | Khong | `!string.IsNullOrWhiteSpace(topic)` chon nhanh | `""` |

**Output** — `void`.

**Dieu kien xu ly** — `switch (!string.IsNullOrWhiteSpace(topic))`: `true` -> log kem `{Topic}`; `false` -> log khong `{Topic}`. Ca hai nhanh deu goi `logger.Log(LogLevel.Error, new EventId(106005,"KafkaErrorResult"), ...)` **tai lap dung EventId/Name cua `KafkaErrorResult`** du day la method rieng (xem phat hien #4).

**Side effect** — Ghi 1 dong log `Error` chua `e?.Message?.Trim()` va `e?.StackTrace?.Trim()` **dang string**, category luon `InfrastructureCategory.MQ_KAFKA` (khong nhan tham so category).

**Error handling** — Khong co try/catch trong chinh method (day chinh la ham danh de log tu 1 exception da bat o noi khac). **Khong truyen `Exception` object thuc su cho `ILogger.Log`** (giong phat hien #1).

**Khi nao NEN dung** — Trong nhanh `catch` cua producer/consumer Kafka.

**Gioi han** — EventId Name gay nham lan khi loc log theo ten event (xem muc 3).

---

### 2.21 ErrorResult / 2.22 Error

**Signature**
```csharp
public static void ErrorResult(this ILogger logger, string className, string methodName, object message, string errorCategory = "", Exception e = null);
public static void Error(this ILogger logger, string className, string methodName, object message, string errorCategory = "", Exception e = null);
```
**Muc dich** — Hai method **gan giong nhau tuyet doi ve tham so va logic**, chi khac noi dung template hien thi (`"response error result"` vs `"error"`) va `EventId` (`ErrorResult`=106002 vs `Error`=106).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc | Mac dinh |
|---|---|---|---|---|
| `errorCategory` | `string` | Khong | `!string.IsNullOrWhiteSpace(errorCategory) ? errorCategory : BusinessCategory.BIZ_LOGIC` — chuoi rong/whitespace bi thay bang `BIZ_LOGIC`, **khong validate gia tri co nam trong danh sach hang so hop le hay khong** (co the truyen bat ky chuoi tuy y, khong bi chan) | `""` (-> `BIZ_LOGIC`) |

**Output** — `void`.

**Dieu kien xu ly** — Goi `_errorResult`/`_error` (compiled delegate, `:206-209`/`:211-214`, LogLevel.Error) — **truyen dung `Exception e` cho `ILogger.Log`** (khac nhom `HttpErrorResult`/`ErrorException`).

**Side effect** — Ghi 1 dong log `Error`.

**Error handling** — Khong co try/catch.

**Khi nao NEN dung** — Loi tong quat can category tuy chinh (khac `BIZ_LOGIC`).

**Gioi han** — Vi 2 method giong nhau ve logic, **khong xac dinh duoc tu source code** khi nao nen dung `ErrorResult` thay `Error` hay nguoc lai — su khac biet duy nhat la chu "response error result" vs "error" trong log text va `EventId`, khong co quy tac ro rang trong code hay comment. `errorCategory` khong duoc validate nen co the vo tinh truyen sai ten hang so (loi chinh ta) ma khong co canh bao nao.

### 2.23 ErrorException

**Signature**
```csharp
public static void ErrorException(this ILogger logger, string className, string methodName, Exception e, string errorCategory = "", object message = null)
```
**Muc dich** — Ghi log `Error` tu mot `Exception`, kem message tuy chon va category tuy chon; **day la method duoc goi nhieu nhat trong repo (14 file khac dung, theo grep)**.

**Input hop le** — `errorCategory` cung logic fallback `BIZ_LOGIC` nhu muc 2.21; `message` mac dinh `null` (khac `ErrorResult`/`Error` bat buoc `message`).

**Output** — `void`.

**Dieu kien xu ly** — `logger.Log(LogLevel.Error, new EventId(106001,"ErrorException"), template + "\n-- {ErrorMessage}\n-- {StackTrace}", className, methodName, errorCategory-hoac-BIZ_LOGIC, message-switch, e?.Message?.Trim(), e?.StackTrace?.Trim())` (`:627-641`).

**Side effect** — Ghi 1 dong log `Error` chua message/stacktrace cua `e` **dang string**.

**Error handling** — **Cung khiem khuyet nhu muc 2.14**: `e` khong duoc truyen thuc su cho `ILogger.Log` — chi 2 thuoc tinh string cua no duoc noi vao template (xem phat hien #1, muc 3). Day la method bi anh huong **nhieu nhat** trong toan module vi la method duoc goi nhieu nhat.

**Khi nao NEN dung** — Nhanh `catch (Exception ex)` chung, khi can ghi log loi kem exception.

**Khi nao KHONG dung** — Khi he thong log phia sau (Application Insights, Serilog sinks doc field Exception, alerting dua tren exception type) can nhan dien **doi tuong** exception (vi du de group loi theo exception type, hoac lay `InnerException`) — method nay khong dap ung duoc, chi co text.

**Gioi han** — Xem phat hien #1, muc 3.

---

### 2.24 Connection

**Signature**
```csharp
public static void Connection(this ILogger logger, string className, string methodName, object message, Exception e = null)
```
**Muc dich** — Ghi log `Information` chung ve trang thai ket noi, tien to `"------------CONNECTION------------ "`.

**Dieu kien xu ly** — Goi `_connection` (`:237-240`, compiled delegate, `EventId`=105002) — truyen dung `Exception`.

**Side effect/Error handling** — Ghi 1 dong log; khong try/catch.

### 2.25–2.29 ConnectionErrorSQL / ConnectionErrorMongoDB / ConnectionErrorRedis / ConnectionErrorElasticSearch / ConnectionErrorRabbitMQ

**Signature (5 method giong cau truc, khac ten + category co dinh)**
```csharp
public static void ConnectionErrorSQL(this ILogger logger, string className, string methodName, Exception e, object message = null);
public static void ConnectionErrorMongoDB(this ILogger logger, string className, string methodName, Exception e, object message = null);
public static void ConnectionErrorRedis(this ILogger logger, string className, string methodName, Exception e, object message = null);
public static void ConnectionErrorElasticSearch(this ILogger logger, string className, string methodName, Exception e, object message = null);
public static void ConnectionErrorRabbitMQ(this ILogger logger, string className, string methodName, Exception e, object message = null);
```
**Muc dich** — 5 wrapper mong, moi ham goi thang toi helper `private static void ConnectionError(logger, className, methodName, errorCategory, e, message)` (`:782-799`) voi `errorCategory` co dinh tuong ung: `InfrastructureCategory.DB_SQLSERVER` / `DB_MONGODB` / `DB_REDIS` / `DB_ELASTICSEARCH` / `MQ_RABBITMQ`.

**Dieu kien xu ly (trong helper `ConnectionError`)** — `logger.Log(LogLevel.Error, new EventId(106006,"ConnectionError"), template + "\n-- {ErrorMessage}\n-- {StackTrace}", className, methodName, errorCategory, message-switch, e?.Message?.Trim(), e?.StackTrace?.Trim())` — **cung khiem khuyet #1**: khong truyen `Exception` object thuc su.

**Output** — `void`. **Side effect** — Ghi 1 dong log `Error` voi tien to `"------------CONNECTION------------ "`. **Error handling** — Khong try/catch; khong nem lai (day la diem cuoi ghi log, khong xu ly loi).

**Khi nao NEN dung** — Bat loi ket noi ha tang (SQL Server, MongoDB, Redis, Elasticsearch, RabbitMQ) tai lop ha tang/repository.

**Gioi han** — Category luon co dinh theo ten method — khong the ghi "loi ket noi SQL Server" voi category khac `DB_SQLSERVER`.

### 2.30 ConnectionErrorKafka

**Signature**
```csharp
public static void ConnectionErrorKafka(this ILogger logger, string className, string methodName, Exception e, object message = null, string topic = "")
```
**Muc dich** — Giong 5 method tren nhung rieng cho Kafka, co them tham so `topic` va **khong tai su dung helper `ConnectionError` private** — tu viet logic switch co/khong topic ngay trong than method (`:709-751`), tuong tu cau truc `KafkaErrorException` (muc 2.20).

**Dieu kien xu ly** — `switch (!string.IsNullOrWhiteSpace(topic))`: `true` -> log kem `{Topic}`; `false` -> log khong topic. Ca hai deu dung `new EventId(EventIds.ConnectionError, nameof(ConnectionError))` = 106006/`"ConnectionError"` (**dung Id/Name nhat quan voi 5 wrapper khac**, khac voi truong hop lech cua Kafka o muc 2.18/2.20) va category co dinh `InfrastructureCategory.MQ_KAFKA`.

**Side effect/Error handling** — Giong muc 2.25-2.29; khong truyen `Exception` object thuc su.

**Gioi han** — Bat doi xung thiet ke: day la wrapper Kafka duy nhat trong nhom `ConnectionError*` khong goi qua helper chung, du logic cuoi cung tao ra cung `EventId` — tang trung lap code (2 khoi switch giong nhau ve cau truc voi `KafkaErrorException`) nhung khong anh huong hanh vi quan sat duoc.

---

## 2B. Chi tiet API — `LoggerErrorCategoriesHelper`

Bang hang so (5 nhom, 23 hang so) — cot cuoi ghi so file **su dung thuc su** trong repo (grep toan repo, tru file dinh nghia va 1 ban sao trung trong `.claude/worktrees/` la thu muc lam viec noi bo cua agent, khong tinh):

| Nhom | Hang so | Gia tri | Y nghia (theo XML doc) | Noi dung tham chieu thuc te |
|---|---|---|---|---|
| `BusinessCategory` | `BIZ_LOGIC` | `"BIZ_LOGIC"` | Loi logic nghiep vu khong xu ly duoc | Dung lam **gia tri mac dinh hardcode** trong `FailLogic`, `HttpErrorResult` (ca 2 overload), va fallback cua `ErrorResult`/`Error`/`ErrorException`. Khong tim thay noi nao trong repo (ngoai `LoggerExtensions.cs`) truyen hang so nay ro rang qua tham so `errorCategory` |
| `BusinessCategory` | `BIZ_VALIDATION` | `"BIZ_VALIDATION"` | Du lieu dau vao khong hop le | **0 noi su dung** ngoai file dinh nghia — khong method nao trong `LoggerExtensions.cs` hoac noi khac trong repo truyen hang so nay |
| `BusinessCategory` | `BIZ_NOT_FOUND` | `"BIZ_NOT_FOUND"` | Khong tim thay du lieu theo ID/key | **0 noi su dung** ngoai file dinh nghia |
| `BusinessCategory` | `BIZ_DUPLICATE` | `"BIZ_DUPLICATE"` | Trung lap du lieu khi insert | **0 noi su dung** ngoai file dinh nghia |
| `SecurityCategory` | `SEC_UNAUTHORIZED` | `"SEC_UNAUTHORIZED"` | Token khong hop le/het han/thieu | Dung tai `JWTBearerExtensions.cs:67,94` (truyen vao tham so `errorCategory` cua mot method log — **ngoai pham vi 2 file nguon cua tai lieu nay**, khong doc chi tiet method goi) |
| `SecurityCategory` | `SEC_FORBIDDEN` | `"SEC_FORBIDDEN"` | Co token nhung khong co quyen | Dung tai `JWTBearerExtensions.cs:139,169` |
| `SecurityCategory` | `SEC_INJECTION` | `"SEC_INJECTION"` | Phat hien SQL/NoSQL injection, XSS | **0 noi su dung** ngoai file dinh nghia |
| `SecurityCategory` | `SEC_BRUTE_FORCE` | `"SEC_BRUTE_FORCE"` | Dang nhap sai nhieu lan | **0 noi su dung** ngoai file dinh nghia |
| `SystemCategory` | `SYS_MEMORY` | `"SYS_MEMORY"` | OOM, memory leak, heap overflow | **0 noi su dung** ngoai file dinh nghia — toan bo `SystemCategory` khong duoc tham chieu o dau khac trong repo |
| `SystemCategory` | `SYS_CPU` | `"SYS_CPU"` | CPU spike, deadlock | **0 noi su dung** ngoai file dinh nghia |
| `SystemCategory` | `SYS_DISK` | `"SYS_DISK"` | Disk full, I/O error | **0 noi su dung** ngoai file dinh nghia |
| `SystemCategory` | `SYS_NETWORK` | `"SYS_NETWORK"` | Mat ket noi mang noi bo, DNS fail | **0 noi su dung** ngoai file dinh nghia |
| `ApiCategory` | `API_TIMEOUT` | `"API_TIMEOUT"` | Request timeout (connect/read) | Chi dung **noi bo** trong `ResolveCategory` (ben duoi); khong co call site nao truyen truc tiep hang so nay tu ben ngoai |
| `ApiCategory` | `API_4XX` | `"API_4XX"` | Loi phia client 4xx | Chi dung noi bo trong `ResolveCategory` |
| `ApiCategory` | `API_5XX` | `"API_5XX"` | Loi phia server 5xx | Chi dung noi bo trong `ResolveCategory` |
| `ApiCategory` | `API_CIRCUIT_BREAKER` | `"API_CIRCUIT_BREAKER"` | Circuit breaker mo | Chi dung noi bo trong `ResolveCategory` |
| `ApiCategory` | `API_RATE_LIMIT` | `"API_RATE_LIMIT"` | Vuot rate limit | Chi dung noi bo trong `ResolveCategory` |
| `InfrastructureCategory` | `DB_SQLSERVER` | `"DB_SQLSERVER"` | Loi ket noi/query/timeout SQL Server | Dung tai `LoggerExtensions.cs` (`ConnectionErrorSQL`, muc 2.25) — **hardcode co dinh**, khong co call site ngoai 2 file nguon truyen hang so nay ro rang |
| `InfrastructureCategory` | `DB_MONGODB` | `"DB_MONGODB"` | Loi ket noi/query MongoDB | Dung tai `LoggerExtensions.cs:681` (`ConnectionErrorMongoDB`) |
| `InfrastructureCategory` | `DB_REDIS` | `"DB_REDIS"` | Loi ket noi/cache miss/timeout Redis | Dung tai `LoggerExtensions.cs:695` (`ConnectionErrorRedis`) |
| `InfrastructureCategory` | `DB_ELASTICSEARCH` | `"DB_ELASTICSEARCH"` | Loi ket noi/query Elasticsearch | Dung tai `LoggerExtensions.cs:756` (`ConnectionErrorElasticSearch`) |
| `InfrastructureCategory` | `MQ_KAFKA` | `"MQ_KAFKA"` | Loi produce/consume/lag Kafka | Dung tai `LoggerExtensions.cs:557,577,718,738` (`KafkaErrorException`, `ConnectionErrorKafka`) |
| `InfrastructureCategory` | `MQ_RABBITMQ` | `"MQ_RABBITMQ"` | Loi ket noi/publish/subscribe RabbitMQ | Dung tai `LoggerExtensions.cs:770` (`ConnectionErrorRabbitMQ`) |

> Ghi chu quan trong: **tat ca cac call site "dung" cac hang so `InfrastructureCategory` va `BIZ_LOGIC` deu nam ben trong chinh `LoggerExtensions.cs`** (hardcode san trong tung wrapper) — khong co code nghiep vu nao ben ngoai 2 file nguon nay **tu chon** category qua tham so `errorCategory` cua `Error`/`ErrorResult`/`ErrorException` de truyen mot trong 23 hang so tren. Dieu nay **khong xac dinh duoc chac chan tu source code cua repo** (co the caller o repo khac/service khac dang lam, ngoai pham vi `sr-core-helper`), nhung trong pham vi repo hien tai, hau het hang so chi ton tai o dang khai bao.

### 2B.1 ApiCategory.ResolveCategory

**Signature**
```csharp
public static string ResolveCategory(int statusCode) => statusCode switch
{
    408 or 504 => API_TIMEOUT,
    429 => API_RATE_LIMIT,
    503 => API_CIRCUIT_BREAKER,
    >= 400 and <= 499 => API_4XX,
    >= 500 and <= 599 => API_5XX,
    _ => string.Empty
};
```
**Muc dich** — Map mot HTTP status code (dang `int`) sang 1 trong 5 hang so `ApiCategory`, phuc vu gan `errorCategory` tu dong khi log ket qua HTTP.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc | Mac dinh |
|---|---|---|---|---|
| `statusCode` | `int` | Co | Khong validate range truoc; switch tu xu ly moi gia tri int (ke ca am, ke ca > 599) | — |

**Output** — `string`. Y nghia tung truong hop:

| Status code | Ket qua | Ghi chu |
|---|---|---|
| `408`, `504` | `API_TIMEOUT` | Request/gateway timeout |
| `429` | `API_RATE_LIMIT` | Too many requests |
| `503` | `API_CIRCUIT_BREAKER` | Service unavailable |
| Con lai trong `[400,499]` | `API_4XX` | Bao gom `401`, `403`, `404`... |
| Con lai trong `[500,599]` | `API_5XX` | Bao gom moi 5xx tru `503`, `504` da xu ly rieng |
| Ngoai `[400,599]` (1xx, 2xx, 3xx, so am, so > 599, `0`) | **`string.Empty`** | **KHAC VOI XML DOC** — xem canh bao ben duoi |

**Dieu kien xu ly** — Switch expression, danh gia theo thu tu tren xuong, arm dau tien khop duoc chon (`408`/`504` va `429`/`503` duoc xu ly **truoc** khoang `>=500&&<=599` nen khong bi khoang rong nay "an" mat).

**Side effect** — Khong co (pure function, khong ghi log, khong mutate).

**Error handling** — Khong co; khong throw voi bat ky gia tri `int` nao (switch co arm `_` bao phu toan bo).

> [!WARNING]
> **Mau thuan XML doc vs code thuc te**: comment `/// <returns>` (`LoggerErrorCategoriesHelper.cs:108-111`) ghi *"Chuoi category chuan, hoac `null` neu status code khong thuoc nhom loi (1xx, 2xx, 3xx)"* — nhung than ham thuc te tra ve **`string.Empty`** cho nhanh mac dinh (`:131`), **khong bao gio tra ve `null`**. Theo nguyen tac "source code la nguon xac thuc cao nhat", ket qua thuc te la `string.Empty`. Anh huong truc tiep: code goi ham nay (`HttpResultWithTracing`, muc 2.15) kiem tra ket qua bang `?? "default"` hoac `== null` se **KHONG hoat dong** vi ket qua khong bao gio null; kiem tra dung phai la `string.IsNullOrEmpty(...)`.

**Khi nao NEN dung** — Suy category tu HTTP status code khi khong co category nghiep vu ro rang hon.

**Khi nao KHONG dung** — Khi `statusCode` khong phai HTTP status code chuan (vi du gia tri noi bo `0` dai dien "khong parse duoc" nhu `HttpResultWithTracing` dang dung) — ket qua se la `string.Empty`, de gay nham lan voi truong hop "thanh cong, khong co loi".

**Gioi han** — Day la method logic **duy nhat** trong ca hai file, va chi co **1 call site thuc su trong toan repo** (`HttpResultWithTracing`, `LoggerExtensions.cs:479`) — pham vi anh huong hep, nhung vi la diem duy nhat, moi thay doi mapping se anh huong toan bo log tracing HTTP.

## 3. Van de da biet

| # | Van de | Vi tri | Anh huong |
|---|---|---|---|
| 1 | **Exception object khong duoc truyen thuc su vao `ILogger.Log` cho toan bo method dung `logger.Log(...)` ad-hoc voi tham so `Exception e`** — chi `e?.Message?.Trim()` va `e?.StackTrace?.Trim()` (dang `string`, da cat khoang trang) duoc noi vao template; ban than doi tuong `Exception` (voi `InnerException`, `Data`, cac thuoc tinh rieng nhu `SqlException.Number`) khong bao gio den duoc sink logging duoi dang structured field | `HttpErrorResult(...,e)` (`LoggerExtensions.cs:443-458`), `ErrorException` (`:625-642`), `KafkaErrorException` (`:546-591`), `ConnectionError` private + 6 wrapper (`:663-799`) | **Nghiem trong.** Cac he thong log tap trung co truong "Exception" rieng (Application Insights, Serilog voi Exception enricher, ELK) se KHONG nhan duoc gi ngoai text; khong the group loi theo exception type, khong the truy `InnerException`. Nguoc lai, nhom dung `LoggerMessage.Define` compiled delegate va co tham so `Exception` (`Warning`, `Debug`, `Request`, `Response`, `Info`, `FailLogic`, `HttpResult`, `MediaRResult`, `Kafka`, `KafkaErrorResult`, `KafkaErrorWithoutTopic`, `ErrorResult`, `Error`, `Connection`) MOI truyen dung Exception object. Day la phat hien **chua duoc 8 file KB hien co ghi nhan** — cac KB do chi moi ghi "khong truyen exception -> mat stack trace" cho truong hop overload HOAN TOAN khong nhan `Exception` (`Utilizes-CallApi.md:90`), nhung KHONG ghi nhan rang ca overload/method CO nhan `e` (`HttpErrorResult(...,e)`, `ErrorException`, `ConnectionError*`) van chi giu duoc **text**, khong giu duoc **doi tuong** exception |
| 2 | **XML doc cua `ApiCategory.ResolveCategory` sai**: ghi tra ve `null` cho status ngoai 4xx/5xx, nhung code thuc te tra ve `string.Empty` | `LoggerErrorCategoriesHelper.cs:108-111` (doc) vs `:131` (code) | Code kiem tra ket qua bang `== null` se sai; phai dung `string.IsNullOrEmpty`. Ap dung nguyen tac "source code la nguon xac thuc" — tai lieu nay dung `string.Empty` la gia tri thuc |
| 3 | `MediaRResult` la method duy nhat KHONG chuan hoa `message` qua switch null/int/string/JSON nhu 12+ method con lai trong file | `LoggerExtensions.cs:495-498` | Neu `message` la object phuc tap khong override `ToString()`, log MediatR se hien ten kieu C# thay vi noi dung — khong nhat quan voi cach hien thi cua `Response`/`Info`/`HttpResult`/v.v. Khong ro la co y hay thieu sot (khong co comment) |
| 4 | `KafkaErrorWithoutTopic` va `KafkaErrorException` deu dung lai `EventId` Id=106005 **va** Name=`"KafkaErrorResult"` (tu `nameof(KafkaErrorResult)`) thay vi ten rieng cua chinh minh | `LoggerExtensions.cs:230-233` (`KafkaErrorWithoutTopic`), `:554`, `:574` (`KafkaErrorException`) | Loc/alert theo `EventId.Name = "KafkaErrorResult"` trong he thong log se lan ca 3 nguon log khac nhau (ket qua loi co topic, khong topic, va tu exception) vao 1 nhom, gay kho khi dieu tra rieng tung loai |
| 5 | `EventIds.MediaRErrorResult` (gia tri `106004`) duoc khai bao nhung **khong co method nao trong `LoggerExtensions.cs` su dung** | `LoggerExtensions.cs:42` | Hang so "chet", co the la du dinh cho mot ham `MediaRErrorResult` chua duoc viet — **khong xac dinh duoc tu source code** |
| 6 | `FailLogic` ghi log o muc **`LogLevel.Information`**, khong phai `Warning`/`Error`, du template ghi ro "fail logic" va gan `ErrorCategory` | `LoggerExtensions.cs:179-182` | Xac nhan dung mo ta da co trong `Data-SQL-CoreSQL.md:1219` va `Data-SQL-CoreSQL-TwoEntity.md:1218` ("Guard clause bi kich hoat rat de bi bo lot khi minimum level la Warning") — **2 KB cu nay mo ta DUNG voi source code**, khong phat hien sai lech |
| 7 | `LoggerExtensions.Warning` dung `LoggerMessage.Define` (truyen dung Exception object khi co) — xac nhan lai mo ta cua `Data-SQL-UnitOfWork-DbContexts.md:57,403` rang `Warning` co tham so `Exception e = null`; tuy nhien KB do chi neu **call site cua `UnitOfWork` khong truyen `e:`**, khong lien quan den kha nang cua `Warning` — **khong phat hien sai lech**, chi xac nhan | `LoggerExtensions.cs:254-266`; `UnitOfWork.cs:84-85,96-97` | Khong anh huong — ghi nhan de doi chieu, KB cu dung |
| 8 | Toan bo `SystemCategory` (`SYS_MEMORY/CPU/DISK/NETWORK`) va da so hang so `BusinessCategory`/`SecurityCategory`/`ApiCategory` **khong co call site nao trong repo** ngoai chinh file dinh nghia | `LoggerErrorCategoriesHelper.cs` (toan file); xem bang muc 2B | Hang so "du thua" hoac danh cho tuong lai/module khac ngoai `sr-core-helper` — **khong xac dinh duoc tu source code cua repo nay** ly do ton tai |
| 9 | `HttpResultWithTracing` khong co tham so `Exception` (khac `HttpErrorResult`) nhung van goi `ApiCategory.ResolveCategory` **ngay ca voi status code thanh cong** (2xx/3xx), tra ve `string.Empty` chu khong bo qua truong `ErrorCategory` | `LoggerExtensions.cs:460-489`, `LoggerErrorCategoriesHelper.cs:131` | Dong log thanh cong van co truong `[ErrorCategory: ]` rong trong noi dung — hoi kho doc, nhung khong gay loi runtime |
| 10 | Tham so `uriWithQuery` cua `HttpResultWithTracing` mac dinh rong va (theo `Utilizes-CallApiWithHttp.md:818,54,55`) khong noi bat ky call site nao trong `CallApiWithHttp.cs` truyen gia tri nay — xac nhan **dung** voi signature thuc te (`uriWithQuery = ""`, dong 468 trong file doc duoc) | `LoggerExtensions.cs:468` | Khong phat hien sai lech tu KB cu; xac nhan lai |

