# Constants & Permission/Role Catalogs

> Nguon: `FTELSRCore.Shared/Constants/CachedBaseConstant.cs`, `FTELSRCore.Shared/Constants/ClaimTypesConstant.cs`, `FTELSRCore.Shared/Constants/CommonBaseConstant.cs`, `FTELSRCore.Shared/Constants/DelimiterConstant.cs`, `FTELSRCore.Shared/Constants/HeaderConstant.cs`, `FTELSRCore.Shared/Constants/OpenTelemetryConstant.cs`, `FTELSRCore.Shared/Constants/SerilogConstant.cs`, `FTELSRCore.Shared/Constants/Permissions/SRTypeActions.cs`, `FTELSRCore.Shared/Constants/Permissions/SRTypePermissions.cs`, `FTELSRCore.Shared/Constants/RoleData/RoleDataConstant.cs`, `FTELSRCore.Shared/Constants/RoleData/RoleSRConstant.cs`, `FTELSRCore.Shared/Enum/RoleSR.cs`
> Loai: static class (da so) + 1 class thuong khong static (`SRTypeActions`) + 1 enum (`RoleSR`)
> Cap nhat theo commit: `89c1ce9`

## 1. Tong quan

Module nay tap hop toan bo hang so (constant) va 2 catalog phan quyen/vai tro dung xuyen suot `FTELSRCore.Shared`: hang so cache (`CachedBaseConstant`), ten claim JWT (`ClaimTypesConstant`), hang so dung chung cua he thong SR (`CommonBaseConstant`), ky tu phan tach (`DelimiterConstant`), ten HTTP header (`HeaderConstant`), ten `ActivitySource` cho OpenTelemetry (`OpenTelemetryConstant`), ten property dung trong log Serilog (`SerilogConstant`), catalog hanh dong (`SRTypeActions`) va catalog ma quyen (`SRTypePermissions`) dung de dung ma permission-code cho he thong SR, cung catalog role code (`RoleSRConstant`), enum do phuc tap vai tro (`RoleSR`) va logic quy doi tu role-code sang do phuc tap vai tro (`RoleDataConstant`).

Ve mat kien truc, day la tang thap nhat (cross-cutting) cua `FTELSRCore.Shared`: khong phu thuoc tang nao khac trong repo, nhung hau het cac tang tren (Audits, Infrastructure/MiddleWares, Infrastructure/Extensions, Utilizes, Caches, Data/SQL, Data/MongoDB, CQRS) deu tham chieu nguoc lai cac hang so nay.

Phan lon file trong module CHI chua hang so (khong co logic dang ke): `ClaimTypesConstant`, `DelimiterConstant`, `HeaderConstant`, `OpenTelemetryConstant`, `SerilogConstant`, `SRTypeActions`, `RoleSRConstant`, va cac nested class hang so cua `SRTypePermissions`. Tai lieu nay dung TEMPLATE RIENG cho phan do. Rieng `CachedBaseConstant`, `CommonBaseConstant`, `RoleDataConstant` va ban than `SRTypePermissions` co them method/logic thuc su (khong chi la gia tri) nen duoc mo ta chi tiet o muc 3 theo cau truc day du (Input/Output/Dieu kien xu ly/Side effect/Error handling).

### 1.1 Pham vi chuc nang

| Lam duoc | Khong lam duoc |
|---|---|
| Cung cap hang so tap trung (prefix, mo ta claim, ky tu phan tach, ten header, ten property log, ten `ActivitySource`) de tranh hardcode rai rac. | Khong tu dong validate gia tri (VD: khong co co che bao "constant nay da bi doi gia tri gay vo hop dong voi consumer ben ngoai"). |
| Sinh chuoi ma quyen (permission code) theo quy uoc `FTEL.SR.<MODULE>[.<SUBMODULE>].<ACTION>` thong qua `SRTypePermissions` ket hop `SRTypeActions`. | Khong tu dong dong bo permission code nay voi du lieu quyen luu trong DB/IAM — day chi la hang so string, viec seed/gan quyen thuc te nam ngoai pham vi cac file duoc doc. |
| Cung cap ham `SRTypePermissions.GetRegisteredPermissions()` de liet ke toan bo permission code da khai bao bang reflection. | Khong loc/validate trung lap permission code giua cac nested class (neu 2 nested class vo tinh sinh ra cung 1 chuoi, ham van tra ve ca 2 phan tu giong nhau ma khong bao loi). |
| Cung cap `RoleDataConstant.GetRoleData()` de quy doi danh sach role-code cua user thanh 1 gia tri `RoleSR` duy nhat (do phuc tap/pham vi truy cap). | Khong tu xu ly truong hop role-code khong ton tai trong `RoleSRConstant` (chi don gian la khong khop, roi ve nhanh mac dinh). |
| Cung cap `CachedBaseConstant.RandomTimeCache()` de tinh thoi gian cache co jitter, tranh cache-stampede. | 3 hang so thoi gian cache (`ShortTime`, `MediumTime`, `LongTime`) duoc khai bao nhung **khong duoc bat ky noi nao trong repo nay dung** (xem muc 3). |

### 1.2 Dependency

| Thanh phan | Muc dich su dung |
|---|---|
| `System.ComponentModel` (`DisplayNameAttribute`, `DescriptionAttribute`) | Gan nhan hien thi + mo ta cho tung nhom quyen trong `SRTypePermissions` (`SRTypePermissions.cs:8-9` v.v.). |
| `System.Reflection` (`FieldInfo`, `BindingFlags`) | `SRTypePermissions.GetRegisteredPermissions()` dung de quet toan bo field public static cua cac nested type (`SRTypePermissions.cs:1-2, 295`). |
| `TimeProvider.System` (.NET) | `CommonBaseConstant.DateTimeUtc()` lay thoi gian UTC he thong (`CommonBaseConstant.cs:49`). |
| `Random.Shared` (.NET) | `CachedBaseConstant.RandomTimeCache()` sinh so ngau nhien de jitter thoi gian cache (`CachedBaseConstant.cs:39`). |
| `FTELSRCore.Enum.RoleSR` | `RoleDataConstant` va `RoleSRConstant` (giu vai tro Order/RoleSR) phu thuoc enum nay de anh xa role-code -> muc do phuc tap (`RoleData/RoleDataConstant.cs:1, 9`). |
| `FTELSRCore.Constants.CommonBaseConstant` | `RoleSRConstant` va `SRTypePermissions` dung `CommonBaseConstant.APPCode` (= `"FTEL.SR"`) lam tien to cho moi role-code/permission-code (`RoleSRConstant.cs:11` v.v.; `SRTypePermissions.cs:16` v.v.). |
| `FTELSRCore.Constants.Permissions.SRTypeActions` | `SRTypePermissions` dung cac hang so hanh dong (`VIEW`, `CREATE`, ...) lam hau to cho tung permission-code (`SRTypePermissions.cs:16` v.v.). |

### 1.3 Danh muc file/thanh phan

| File | Loai | Public member | Vai tro chinh |
|---|---|---|---|
| `CachedBaseConstant.cs` | static class | 3 const + 1 static method | Hang so thoi gian cache + ham jitter. |
| `ClaimTypesConstant.cs` | static class | 9 const | Ten claim dung trong JWT/`ClaimsPrincipal`. |
| `CommonBaseConstant.cs` | static class | 10 const + 3 static method | Hang so dung chung toan he thong SR + helper log console + helper thoi gian. |
| `DelimiterConstant.cs` | static class | 10 const | Ky tu/chuoi phan tach dung chung. |
| `HeaderConstant.cs` | static class | 3 const | Ten HTTP header dung chung. |
| `OpenTelemetryConstant.cs` | static class | 6 const | Ten `ActivitySource` cho tracing/metrics. |
| `SerilogConstant.cs` | static class | 32 const | Ten property gan vao Serilog `LogEvent`. |
| `SRTypeActions.cs` | **class thuong (khong static)** | 12 const | Danh muc hanh dong (action) dung lam hau to permission-code. |
| `SRTypePermissions.cs` | static class + 5 nested static class | 57 const (chia theo 5 nhom) + 1 static method | Danh muc permission-code toan he thong theo module nghiep vu. |
| `RoleSRConstant.cs` | static class | 8 const | Danh muc role-code toan he thong. |
| `RoleDataConstant.cs` | static class | 1 static method (+ 1 truong private chua bang anh xa) | Anh xa danh sach role-code cua user -> `RoleSR` (muc do phuc tap truy cap). |
| `RoleSR.cs` | enum | 7 gia tri | Muc do phuc tap/pham vi truy cap du lieu cua user. |

## 2. Chi tiet hang so va enum (theo file)

### 2.1 CachedBaseConstant.cs

| Ten hang so | Gia tri thuc | Y nghia (theo comment/context) | Noi dung tham chieu |
|---|---|---|---|
| `ShortTime` | `15` (double, don vi: phut theo comment) | "Thoi gian cache ngan (15 phut)" (`CachedBaseConstant.cs:9-12`). | **0 file** dung hang so nay trong repo (grep toan repo, ngoai file khai bao) — hang so hien khong duoc tieu thu. |
| `MediumTime` | `60` | "Thoi gian cache trung binh (60 phut)" (`CachedBaseConstant.cs:14-17`). | **0 file** dung. |
| `LongTime` | `480` | "Thoi gian cache dai (480 phut)" (`CachedBaseConstant.cs:19-22`). | **0 file** dung. |

Method `RandomTimeCache` (co logic) duoc mo ta chi tiet o muc 3.1.

### 2.2 ClaimTypesConstant.cs

| Ten hang so | Gia tri thuc | Y nghia (theo comment/context) | Noi dung tham chieu |
|---|---|---|---|
| `SRRoles` | `"SR.SRRoles"` | Khong co XML doc; theo ten va cach dung, la loai claim chua danh sach role-code cua SR. | 3 file dung: `UserAudit.cs:88`, `SRLogEventEnricherExtensions.cs:101`, `SerilogHandlerMiddleWare.cs:57`. |
| `FTelRoles` | `"SR.FTelRoles"` | Khong co XML doc; loai claim chua role-code pham vi FTEL (ngoai SR). | 1 file dung: `UserAudit.cs:93`. |
| `BranchId` | `"BranchId"` | Khong co XML doc. | **0 file** dung ngoai khai bao — hien khong duoc tieu thu trong repo nay. |
| `TitleCode` | `"TitleCode"` | Khong co XML doc. | **0 file** dung. |
| `LocationId` | `"LocationId"` | Khong co XML doc. | **0 file** dung. |
| `Permissions` | `"Permissions"` | Khong co XML doc; loai claim chua danh sach quyen. | 2 file dung: `UserAudit.cs:98`, `AuthorizationPolicyExtensions.cs:14`. |
| `SRPermissions` | `"SR.Permissions"` | Khong co XML doc. | Chi xuat hien trong **code da bi comment (// TODO)** tai `SRLogEventEnricherExtensions.cs:91` va `SerilogHandlerMiddleWare.cs:46` — khong co lenh dang active nao dung gia tri nay. |
| `ConcurrentArea` | `"SR.ConcurrentArea"` | Khong co XML doc; loai claim chua dia ban kiem nhiem. | 1 file dung: `UserAudit.cs:74`. |
| `Organization` | `"Organization"` | Khong co XML doc. | **0 file** dung ngoai khai bao. |

### 2.3 CommonBaseConstant.cs

| Ten hang so | Gia tri thuc | Y nghia (theo comment/context) | Noi dung tham chieu |
|---|---|---|---|
| `Prefix` | `"SR"` | "Dinh danh he thong SR" (`CommonBaseConstant.cs:6-9`). | Dung truc tiep de tao `APPCode` (`CommonBaseConstant.cs:27`); khong thay noi khac dung truc tiep `Prefix`. |
| `TypePermissions` | `"FTEL.SR"` | "Dinh danh nhom quyen cua he thong SR" (`CommonBaseConstant.cs:11-15`). | Khong xac dinh duoc noi dung tham chieu tu source code (grep khong thay noi nao trong repo dung `CommonBaseConstant.TypePermissions`, gia tri trung voi `APPCode`). |
| `None` | `"N/A"` | "Gia tri mac dinh" (`CommonBaseConstant.cs:17-21`). | Khong xac dinh duoc tu source code (khong thay noi dung `CommonBaseConstant.None` trong pham vi cac file duoc doc). |
| `APPCode` | `"FTEL.SR"` (chuoi noi `$"FTEL.{Prefix}"`) | "Gia tri mac dinh cua APPCode" (`CommonBaseConstant.cs:23-27`). | Dung lam tien to cho moi permission-code trong `SRTypePermissions.cs` (VD dong 16, 26...) va cho moi role-code trong `RoleSRConstant.cs` (VD dong 11...). |
| `AnonymousCode` | `"0"` | Ma nguoi dung an danh (fallback). | Dung trong `UserAudit.cs:23,179`. |
| `OrganizationForISC` | `"FTEL"` | To chuc mac dinh cho ISC. | Dung trong `UserAudit.cs:47,181`. |
| `Anonymous` | `"Anonymous"` | Ten nguoi dung an danh (fallback). | Dung trong `UserAudit.cs:29,180,289`, `SRLogEventEnricherExtensions.cs:32,83,98`. |
| `System` | `"FTEL-SERVICEREQUEST-API"` | Dinh danh he thong dung cho log/audit. | Dung trong `HttpClientUtilizes.cs:25`, `SRLogEventEnricherExtensions.cs:14`. |
| `UserAgentCore` | `"FTELSRCore"` | Khong co XML doc; theo ten, la gia tri User-Agent mac dinh cua thu vien core khi goi HTTP. | Khong xac dinh duoc noi dung tham chieu tu cac file da doc (khong thay noi nao dung `CommonBaseConstant.UserAgentCore` trong nhom file duoc doc cho module nay). |
| `CORSDefault` | `"CorsPolicy"` | Khong co XML doc; theo ten, la ten policy CORS mac dinh. | Khong xac dinh duoc tu cac file da doc cho module nay. |

3 method (`DateTimeUtc`, `ConfigLoggerExceptionByConsole`, `ConfigLoggerInformationByConsole`) duoc mo ta chi tiet o muc 3.2–3.4.

### 2.4 DelimiterConstant.cs

| Ten hang so | Gia tri thuc | Y nghia (theo comment/context) | Noi dung tham chieu |
|---|---|---|---|
| `CHAR_COMMA` | `','` | Ky tu phay. | 3 vi tri dung dang active: `CoreCacheExtension.cs:530`, `SRLogEventEnricherExtensions.cs:105` (noi nhieu role thanh 1 chuoi cach nhau boi phay), `SerilogHandlerMiddleWare.cs:62`; ngoai ra con 2 vi tri nam trong code da bi comment (`SRLogEventEnricherExtensions.cs:94`, `SerilogHandlerMiddleWare.cs:49`) khong tinh la dang su dung. |
| `CHAR_DOT` | `'.'` | Ky tu cham. | 1 vi tri dung: `HttpClientUtilizes.cs:177` (`jwt.Split(DelimiterConstant.CHAR_DOT)` — tach 3 phan cua JWT). |
| `CHAR_SEMICOLON` | `';'` | Ky tu cham phay. | **0 vi tri** dung ngoai khai bao. |
| `CHAR_APOSTROPHE` | `'/'` | Ten hang so la "APOSTROPHE" nhung gia tri thuc la `/` (slash), khong phai `'` — xem canh bao muc 4. | 2 vi tri dung, ca hai deu tai `HttpClientUtilizes.cs:65,95` de noi `SubDirectory` voi `requestUri` (dung nhu mot dau `/` phan tach duong dan). |
| `CHAR_DASH` | `'-'` | Ky tu gach ngang. | **0 vi tri** dung ngoai khai bao. |
| `STRING_COMMA` | `","` | Ban chuoi cua `CHAR_COMMA`. | **0 vi tri** dung — cac noi can dau phay dang dung ban `char` (`CHAR_COMMA`), khong dung ban `string` nay. |
| `STRING_DOT` | `"."` | Ban chuoi cua `CHAR_DOT`. | **0 vi tri** dung. |
| `STRING_SEMICOLON` | `";"` | Ban chuoi cua `CHAR_SEMICOLON`. | **0 vi tri** dung. |
| `STRING_APOSTROPHE` | `"/"` | Ban chuoi cua `CHAR_APOSTROPHE`; cung mang ten sai nhu ban `char`. | **0 vi tri** dung. |
| `STRING_DASH` | `"-"` | Ban chuoi cua `CHAR_DASH`. | **0 vi tri** dung. |

### 2.5 HeaderConstant.cs

| Ten hang so | Gia tri thuc | Y nghia (theo comment/context) | Noi dung tham chieu |
|---|---|---|---|
| `CorrelationIdHeaderKey` | `"X-Correlation-Id"` | Khong co XML doc; ten HTTP header dung de truyen/nhan Correlation Id giua cac service. | 6 vi tri dung: `SRLogEventEnricherExtensions.cs:57`, `CorrelationIdMiddleWare.cs:15,28,35,37`, `BuildMetaHelper.cs:27`. |
| `UserAgentHeaderKey` | `"User-Agent"` | Ten HTTP header User-Agent chuan. | 3 vi tri dung: `UserAgentForwardExtensions.cs:50,52`, `SerilogHandlerMiddleWare.cs:23`. |
| `ForwardedHeaderKey` | `"X-Forwarded-For"` | Ten HTTP header chuan de truyen IP goc qua proxy. | 3 vi tri dung: `IPAddressForwardExtensions.cs:41,43`, `SerilogHandlerMiddleWare.cs:40`. |

### 2.6 OpenTelemetryConstant.cs

| Ten hang so | Gia tri thuc | Y nghia (theo comment/context) | Noi dung tham chieu |
|---|---|---|---|
| `CoreCacheActivitySource` | `"FTELSRCore.Caches.CoreCacheExtension"` | Ten `ActivitySource` cho module cache. | Dung de tao `ActivitySource` tai `CoreCacheExtension.cs:11`, va duoc **dang ky** vao tracer/meter provider tai `OpenTelemetryExtensions.cs:15,53`. |
| `MongoDBActivitySource` | `"MongoDB.Driver.Core.Extensions.DiagnosticSources"` | Ten source noi bo cua MongoDB driver. | **0 vi tri dung/dang ky** trong cac file `.cs` cua repo nay (khong thay `AddSource`/`AddMeter` nao truyen hang so nay) — xem canh bao muc 4. |
| `LoggingBehaviorActivitySource` | `"FTELSRCore.CQRS.Behaviors.LoggingBehavior"` | Ten `ActivitySource` cho CQRS logging behavior. | Dung de tao `ActivitySource` tai `LoggingBehavior.cs:11`, va duoc **dang ky** tai `OpenTelemetryExtensions.cs:16,54`. |
| `SqlResilienceActivitySource` | `"FTELSRCore.Data.SQL.Helpers.Policies.SqlResiliencePolicyFactory"` | Ten `ActivitySource` cho policy resilience SQL. | Dung de tao `ActivitySource` tai `SqlResiliencePolicyFactory.cs:18`. **Khong thay** duoc dang ky qua `AddSource`/`AddMeter` trong `OpenTelemetryExtensions.cs` — xem canh bao muc 4. |
| `MongoResilienceActivitySource` | `"FTELSRCore.Data.MongoDB.Helpers.Policies.MongoResiliencePolicyFactory"` | Ten `ActivitySource` cho policy resilience MongoDB. | Dung de tao `ActivitySource` tai `MongoResiliencePolicyFactory.cs:12`. **Khong thay** duoc dang ky qua `AddSource`/`AddMeter` — xem canh bao muc 4. |
| `HttpResilienceActivitySource` | `"FTELSRCore.Utilizes.Policies.HttpResiliencePolicyFactory"` | Ten `ActivitySource` cho policy resilience HTTP. | **0 vi tri dung**: khong tim thay file/class `HttpResiliencePolicyFactory` nao trong repo nay dung hang so nay de tao `ActivitySource`, cung khong duoc dang ky trong `OpenTelemetryExtensions.cs` — xem canh bao muc 4. |

### 2.7 SerilogConstant.cs

Toan bo la ten property (string) duoc gan vao `LogEvent` cua Serilog thong qua `.Enrich.WithProperty(...)` hoac tuong tu (dua tren cach dung tai cac file enrich/middleware). Bang duoi liet ke gia tri va so vi tri dung thuc te (grep, khong tinh dong khai bao):

| Ten hang so | Gia tri thuc | So vi tri dung trong repo |
|---|---|---|
| `DynamicRule` | `"Rule"` | 1 |
| `ActionIdPropertyName` | `"ActionId"` | 1 |
| `ActionNamePropertyName` | `"ActionName"` | 1 |
| `ClassNamePropertyName` | `"ClassName"` | 1 |
| `ClientIpPropertyName` | `"ClientIp"` | 2 |
| `CorrelationIdPropertyName` | `"CorrelationId"` | 3 |
| `EnvironmentNamePropertyName` | `"EnvironmentName"` | 1 |
| `EventIdNamePropertyName` | `"EventId"` | 1 |
| `MachineNamePropertyName` | `"MachineName"` | 1 |
| `MethodNamePropertyName` | `"MethodName"` | 1 |
| `ParametersPropertyName` | `"Parameters"` | 1 |
| `RequestIdPropertyName` | `"RequestId"` | 1 |
| `RequestNamePropertyName` | `"RequestName"` | 1 |
| `RequestPathPropertyName` | `"RequestPath"` | 2 |
| `ServiceNamePropertyName` | `"ServiceName"` | 2 |
| `UserPropertyName` | `"User"` | 2 |
| `ForwardedPropertyName` | `"Forwarded"` | 2 |
| `UserAgentPropertyName` | `"UserAgent"` | 2 |
| `SourceContextPropertyName` | `"SourceContext"` | 1 |
| `UserInfoPropertyName` | `"UserInfo"` | 5 |
| `EndpointPropertyName` | `"Endpoint"` | 1 |
| `HttpMethodPropertyName` | `"HttpMethod"` | 1 |
| `ResponseTimeMsPropertyName` | `"ResponseTimeMs"` | 1 |
| `HttpStatusCodePropertyName` | `"HttpStatusCode"` | 1 |
| `LatencyRatingPropertyName` | `"LatencyRating"` | 1 |
| `DirectionPropertyName` | `"Direction"` | 1 |
| `SystemOwnerPropertyName` | `"SystemOwner"` | 1 |
| `StackTracePropertyName` | `"StackTrace"` | 1 |
| `ErrorMessagePropertyName` | `"ErrorMessage"` | 1 |
| `ErrorCategoryPropertyName` | `"ErrorCategory"` | 1 |
| `ErrorCodePropertyName` | `"ErrorCode"` | **0** — khong tim thay noi nao dung, xem canh bao muc 4. |
| `TopicPropertyName` | `"Topic"` | 1 |

Ghi chu: khong co XML doc `/// <summary>` cho bat ky hang so nao trong file nay; Y nghia duoc suy ra truc tiep tu ten hang so va gia tri chuoi (deu la ten property don gian, khong co logic dac biet).

### 2.8 SRTypeActions.cs

Class nay khai bao 12 hang so hanh dong (action), dung lam hau to cho permission-code trong `SRTypePermissions`.

| Ten hang so | Gia tri thuc | Y nghia (theo XML doc) |
|---|---|---|
| `VIEW` | `"VIEW"` | "Xem" (`SRTypeActions.cs:7-10`). |
| `CREATE` | `"CREATE"` | "Tao" (`:12-15`). |
| `UPDATE` | `"UPDATE"` | "Cap nhat" (`:17-20`). |
| `DELETE` | `"DELETE"` | "Xoa" (`:22-25`). |
| `CANCEL` | `"CANCEL"` | "Huy" (`:29-32`). |
| `MOVE` | `"MOVE"` | "Chuyen" (`:34-37`). |
| `RECEIVE` | `"RECEIVE"` | "Nhan" (`:39-42`). |
| `ASSIGN` | `"ASSIGN"` | "Phan cong" (`:44-47`). |
| `UPLOAD` | `"UPLOAD"` | "Tai file" (`:51-54`). |
| `DOWNLOAD` | `"DOWNLOAD"` | "Luu ve" (`:56-59`). |
| `SENDMAIL` | `"SENDMAIL"` | "Gui mail" (`:61-64`). |
| `APPROVE` | `"APPROVE"` | "Chap thuan" (`:68-71`). |

Tat ca 12 hang so deu co XML doc ngan gon (1 tu tieng Viet). Xem canh bao ve khai bao `public class` (khong static) tai muc 4.

### 2.9 SRTypePermissions.cs

Khong co hang so nao trong file nay co XML doc `/// <summary>` rieng (chi co `[DisplayName]`/`[Description]` gan tren tung nested class). Moi permission-code duoc sinh boi mau `$"{CommonBaseConstant.APPCode}.<MODULE>[.<SUBMODULE>].{SRTypeActions.<ACTION>}"`, voi `APPCode` = `"FTEL.SR"`.

#### 2.9.1 Nhom `Requests` — `[DisplayName("Request")]` / `[Description("Yêu cầu")]`

| Ten hang so | Gia tri thuc (chuoi da noi) | Dong khai bao |
|---|---|---|
| `REQUEST_VIEW` | `FTEL.SR.REQUEST.VIEW` | `:16` |
| `CREATE_VIEW` | `FTEL.SR.REQUEST.CREATE.VIEW` | `:26` |
| `CREATE_UPLOAD` | `FTEL.SR.REQUEST.CREATE.UPLOAD` | `:28` |
| `CREATE_CREATE` | `FTEL.SR.REQUEST.CREATE.CREATE` | `:30` |
| `CREATE_SENDMAIL` | `FTEL.SR.REQUEST.CREATE.SENDMAIL` | `:32` |
| `DETAIL_MOVE` | `FTEL.SR.REQUEST.DETAIL.MOVE` | `:38` |
| `DETAIL_VIEW` | `FTEL.SR.REQUEST.DETAIL.VIEW` | `:40` |
| `DETAIL_ASSIGN` | `FTEL.SR.REQUEST.DETAIL.ASSIGN` | `:42` |
| `DETAIL_CANCEL` | `FTEL.SR.REQUEST.DETAIL.CANCEL` | `:44` |
| `DETAIL_UPDATE` | `FTEL.SR.REQUEST.DETAIL.UPDATE` | `:46` |
| `DETAIL_RECEIVE` | `FTEL.SR.REQUEST.DETAIL.RECEIVE` | `:48` |
| `TICKET_VIEW` | `FTEL.SR.REQUEST.DETAIL.TICKET.VIEW` | `:60` |
| `TICKET_CREATE` | `FTEL.SR.REQUEST.DETAIL.TICKET.CREATE` | `:62` |
| `HISTORY_VIEW` | `FTEL.SR.REQUEST.DETAIL.HISTORY.VIEW` | `:68` |
| `PROCESS_VIEW` | `FTEL.SR.REQUEST.DETAIL.PROCESS.VIEW` | `:74` |
| `DOCUMENT_VIEW` | `FTEL.SR.REQUEST.DETAIL.DOCUMENT.VIEW` | `:80` |
| `DOCUMENT_UPLOAD` | `FTEL.SR.REQUEST.DETAIL.DOCUMENT.UPLOAD` | `:82` |
| `DOCUMENT_DELETE` | `FTEL.SR.REQUEST.DETAIL.DOCUMENT.DELETE` | `:84` |
| `DOCUMENT_DOWNLOAD` | `FTEL.SR.REQUEST.DETAIL.DOCUMENT.DOWNLOAD` | `:86` |
| `WORKFLOW_VIEW` | `FTEL.SR.REQUEST.DETAIL.WORKFLOW.VIEW` | `:92` |
| `DISCUSSION_VIEW` | `FTEL.SR.REQUEST.DETAIL.DISCUSSION.VIEW` | `:98` |
| `DISCUSSION_UPLOAD` | `FTEL.SR.REQUEST.DETAIL.DISCUSSION.UPLOAD` | `:100` |
| `DISCUSSION_DOWNLOAD` | `FTEL.SR.REQUEST.DETAIL.DISCUSSION.DOWNLOAD` | `:102` — co ghi chu `// TODO` ngay sau khai bao trong source. |
| `DISCUSSION_DELETE` | `FTEL.SR.REQUEST.DETAIL.DISCUSSION.DELETE` | `:104` — co ghi chu `// TODO` ngay sau khai bao trong source. |

#### 2.9.2 Nhom `Tickets` — `[DisplayName("Ticket")]` / `[Description("Ticket")]`

| Ten hang so | Gia tri thuc | Dong khai bao |
|---|---|---|
| `TICKET_VIEW` | `FTEL.SR.TICKET.VIEW` | `:121` |
| `DETAIL_VIEW` | `FTEL.SR.TICKET.DETAIL.VIEW` | `:129` |
| `DETAIL_UPDATE` | `FTEL.SR.TICKET.DETAIL.UPDATE` | `:131` |
| `DETAIL_ASSIGN` | `FTEL.SR.TICKET.DETAIL.ASSIGN` | `:133` |
| `DETAIL_RECEIVE` | `FTEL.SR.TICKET.DETAIL.RECEIVE` | `:135` |
| `DETAIL_APPROVE` | `FTEL.SR.TICKET.DETAIL.APPROVE` | `:137` |
| `DETAIL_CANCEL` | `FTEL.SR.TICKET.DETAIL.CANCEL` | `:139` |
| `SUBTICKET_VIEW` | `FTEL.SR.TICKET.DETAIL.SUBTICKET.VIEW` | `:149` |
| `SUBTICKET_CREATE` | `FTEL.SR.TICKET.DETAIL.SUBTICKET.CREATE` | `:151` |
| `DISCUSSION_VIEW` | `FTEL.SR.TICKET.DETAIL.DISCUSSION.VIEW` | `:157` |
| `DISCUSSION_UPLOAD` | `FTEL.SR.TICKET.DETAIL.DISCUSSION.UPLOAD` | `:159` |
| `DISCUSSION_DELETE` | `FTEL.SR.TICKET.DETAIL.DISCUSSION.DELETE` | `:161` — co ghi chu `// TODO`. |
| `DISCUSSION_DOWNLOAD` | `FTEL.SR.TICKET.DETAIL.DISCUSSION.DOWNLOAD` | `:163` — co ghi chu `// TODO`. |
| `DOCUMENT_VIEW` | `FTEL.SR.TICKET.DETAIL.DOCUMENT.VIEW` | `:169` |
| `DOCUMENT_UPLOAD` | `FTEL.SR.TICKET.DETAIL.DOCUMENT.UPLOAD` | `:171` |
| `DOCUMENT_DELETE` | `FTEL.SR.TICKET.DETAIL.DOCUMENT.DELETE` | `:173` |
| `DOCUMENT_DOWNLOAD` | `FTEL.SR.TICKET.DETAIL.DOCUMENT.DOWNLOAD` | `:175` |
| `WORKFLOW_VIEW` | `FTEL.SR.TICKET.DETAIL.WORKFLOW.VIEW` | `:181` |

#### 2.9.3 Nhom `Configs` — `[DisplayName("Config")]` / `[Description("Cấu hình")]`

| Ten hang so | Gia tri thuc | Dong khai bao |
|---|---|---|
| `CONFIG_VIEW` | `FTEL.SR.CONFIG.VIEW` | `:198` |
| `PROCESS_VIEW` | `FTEL.SR.CONFIG.PROCESS.VIEW` | `:208` |
| `PROCESS_CREATE` | `FTEL.SR.CONFIG.PROCESS.CREATE` | `:210` |
| `PROCESS_UPDATE` | `FTEL.SR.CONFIG.PROCESS.UPDATE` | `:212` |
| `PRIORITY_VIEW` | `FTEL.SR.CONFIG.PRIORITY.VIEW` | `:218` |
| `PRIORITY_CREATE` | `FTEL.SR.CONFIG.PRIORITY.CREATE` | `:220` |
| `PRIORITY_UPDATE` | `FTEL.SR.CONFIG.PRIORITY.UPDATE` | `:222` |
| `PRIORITY_DELETE` | `FTEL.SR.CONFIG.PRIORITY.DELETE` | `:224` |

#### 2.9.4 Nhom `Employees` — `[DisplayName("Employee")]` / `[Description("Nhân sự")]`

| Ten hang so | Gia tri thuc | Dong khai bao |
|---|---|---|
| `EMPLOYEE_VIEW` | `FTEL.SR.EMPLOYEE.VIEW` | `:241` |
| `CALENDAR_VIEW` | `FTEL.SR.EMPLOYEE.CALENDAR.VIEW` | `:251` |
| `CALENDAR_CREATE` | `FTEL.SR.EMPLOYEE.CALENDAR.CREATE` | `:253` |
| `CALENDAR_UPDATE` | `FTEL.SR.EMPLOYEE.CALENDAR.UPDATE` | `:255` |
| `CALENDAR_DELETE` | `FTEL.SR.EMPLOYEE.CALENDAR.DELETE` | `:257` |
| `EMPLOYEE_DETAIL_VIEW` | `FTEL.SR.EMPLOYEE.EMPLOYEE.VIEW` | `:263` — chuoi co doan lap "EMPLOYEE.EMPLOYEE", xem canh bao muc 4. |

#### 2.9.5 Nhom `Dashboards` — `[DisplayName("Dashboards")]` / `[Description("Trang chủ thống kê")]`

| Ten hang so | Gia tri thuc | Dong khai bao |
|---|---|---|
| `DASHBOARD_VIEW` | `FTEL.SR.DASHBOARD.VIEW` | `:280` |

Method `GetRegisteredPermissions()` duoc mo ta chi tiet o muc 3.6.

### 2.10 RoleSRConstant.cs

| Ten hang so | Gia tri thuc | Y nghia (theo XML doc) |
|---|---|---|
| `REQUESTOR` | `FTEL.SR.REQUESTOR` | "Nhân viên Tạo yêu cầu" (`:7-10`). |
| `HANDLER_MANAGER` | `FTEL.SR.HANDLER_MANAGER` | "CBQL Nhân viên xử lý yêu cầu" (`:13-16`). |
| `HANDLER_MASTER` | `FTEL.SR.HANDLER_MASTER` | "NV Xử lý yêu cầu Master" (`:19-22`). |
| `TICKET_MASTER` | `FTEL.SR.T_MASTER` | "NV xử lý ticket Master" (`:25-28`); **luu y** gia tri thuc dung hau to `T_MASTER` chu khong phai `TICKET_MASTER` nhu ten hang so — xem canh bao muc 4. |
| `HANDLER` | `FTEL.SR.HANDLER` | "Nhân viên Xử lý yêu cầu" (`:31-34`). |
| `SUPERVISOR` | `FTEL.SR.SUPERVISOR` | "Nhân viên giám sát" (`:37-40`). |
| `EMPLOYEE_ADMIN` | `FTEL.SR.NV.ADMIN` | "Nhân viên Admin" (`:43-46`). |
| `ADMIN` | `FTEL.SR.ADMIN` | "Admin hệ thống" (`:49-52`). |

Ca 8 hang so deu duoc `RoleDataConstant.ComplexityForRolesSR` tham chieu de anh xa sang `RoleSR` (`RoleData/RoleDataConstant.cs:12-39`).

### 2.11 RoleSR.cs (enum)

| Gia tri enum | Gia tri int (ngam dinh, tang dan tu `ALL = 1`) | Y nghia (theo XML doc) |
|---|---|---|
| `ALL` | `1` | "Có quyền xem và xử lý tất cả các thông tin." (`:5-8`). |
| `MY_DIVISION` | `2` | "Được xem và xử lý các thông tin của bản thân, nhân sự được nhận quản lý và các thông tin thuộc địa bàn kiêm nhiệm." (`:10-13`). |
| `EMPLOYEE_MANAGER` | `3` | "Được xem và xử lý các thông tin của bản thân, nhân sự được nhận quản lý." (`:15-18`). |
| `MASTER_TICKET` | `4` | "Được xem và xử lý các thông tin của bản thân, nhân sự được nhận quản lý . [ BỔ SUNG: Có quyền duyệt các yêu cầu Miễn Giảm, CHS6T, Duyệt CLG trên toàn quốc ]" (`:20-24`). |
| `ASSIGNMENT` | `5` | "Được xem và xử lý các thông tin của bản thân tạo và được phân công." (`:26-29`). |
| `ONLY_CREATE` | `6` | "Được xem và xử lý các thông tin của bản thân tạo." (`:31-34`). |
| `ADMIN` | `7` | "Chỉ được xử lý lịch trực và nhân sự và các thông tin do bản thân tạo." (`:36-39`) — noi dung nay **mau thuan** voi ten `ADMIN` va voi comment cua `RoleSRConstant.ADMIN` ("Admin hệ thống"); xem canh bao muc 4. |

## 3. Cac ham co logic (khong chi la gia tri constant)

### 3.1 CachedBaseConstant.RandomTimeCache

**Signature**
```csharp
public static double RandomTimeCache(double time, double percent = 0.1)
```
**Muc dich** — Giam thoi gian cache mot luong ngau nhien (jitter) de tranh nhieu cache het han cung luc (cache stampede), theo dung XML doc tai `CachedBaseConstant.cs:24-27`.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `time` | `double` | Co | Khong co validate trong code (khong kiem tra am, khong kiem tra NaN/Infinity). | Khong co |
| `percent` | `double` | Khong | Khong co validate trong code (khong kiem tra trong khoang `[0,1]`). | `0.1` |

**Output** — Tra ve `double` = `time - (time * Random.Shared.NextDouble() * percent)` (`CachedBaseConstant.cs:39`). Vi `Random.Shared.NextDouble()` tra ve gia tri trong `[0, 1)`, ket qua nam trong khoang `(time * (1 - percent), time]` — luon nho hon hoac bang `time`, khong bao gio lon hon.

**Dieu kien xu ly** — Khong co nhanh re, khong co `if`/`switch`; chi 1 bieu thuc tinh toan duy nhat.

**Side effect** — Khong co (khong ghi log, khong goi ngoai, khong mutate tham so).

**Error handling** — Khong co try/catch. Neu `time` la `NaN` hoac `Infinity`, ket qua se la `NaN`/`Infinity` tuong ung (khong duoc chan).

**Khi nao NEN dung** — Khi can tinh TTL cho cache va muon rai deu thoi diem het han giua nhieu entry/instance de tranh dong loat nap lai cache cung luc.

**Khi nao KHONG dung** — Khi can gia tri TTL co dinh, xac dinh truoc (vi jitter lam gia tri thay doi moi lan goi).

**Gioi han** — Khong validate `time < 0` hay `percent` ngoai khoang hop ly; neu truyen `percent > 1`, ket qua co the am. Trong repo, ham nay chi duoc goi voi `time = 5` (hardcode) tai `HttpClientUtilizes.cs:243,261`, khong dung 3 hang so `ShortTime`/`MediumTime`/`LongTime` cua chinh class nay.

### 3.2 CommonBaseConstant.DateTimeUtc

**Signature**
```csharp
public static DateTime DateTimeUtc(int addHour = 7)
```
**Muc dich** — Tra ve "Ngay mac dinh he thong" (`CommonBaseConstant.cs:41-46`), thuc chat la thoi gian UTC he thong cong them so gio truyen vao.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `addHour` | `int` | Khong | Khong co validate (co the truyen am, 0, hoac so lon bat thuong). | `7` |

**Output** — `DateTime` = `TimeProvider.System.GetUtcNow().DateTime.AddHours(addHour)` (`CommonBaseConstant.cs:49`). Voi `addHour` mac dinh la `7`, ket qua la gio UTC+7 (gio Viet Nam) nhung **kieu `DateTime` khong mang thong tin timezone/`Kind`** (vi `.DateTime` tren `DateTimeOffset` tra ve `Kind = Unspecified`).

**Dieu kien xu ly** — Khong co nhanh re.

**Side effect** — Khong co.

**Error handling** — Khong co try/catch; neu `addHour` qua lon co the nem `ArgumentOutOfRangeException` tu `AddHours` (hanh vi cua .NET, khong duoc code nay bat).

**Khi nao NEN dung** — Khi can lay "thoi gian hien tai theo quy uoc cua he thong SR" de gan cho cac truong audit (`CreatedDate`, `ModifiedDate`...).

**Khi nao KHONG dung** — Khi can gia tri UTC thuan (dung de so sanh voi du lieu luu UTC chuan, vi mac dinh `addHour=7` se lech 7 gio).

**Gioi han** — Ten ham `DateTimeUtc` gay hieu nham la tra ve UTC thuan, trong khi gia tri mac dinh thuc te la UTC+7 (day chinh la hanh vi da duoc file KB cu `Data-SQL-UnitOfWork-DbContexts.md` va `Data-MongoDB-CoreMongoDB.md` ghi nhan — xem doi chieu tai muc 4).

### 3.3 CommonBaseConstant.ConfigLoggerExceptionByConsole

**Signature**
```csharp
public static void ConfigLoggerExceptionByConsole(
    string className, string methodName, Exception exception, string description = "")
```
**Muc dich** — Ghi 1 dong log muc loi (`[ERR]`) ra `Console` (khong qua `ILogger`), phuc vu cac noi trong thu vien chua tiem duoc logger (VD ben trong `ProjectToExtensions`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `className` | `string` | Co | Khong validate null/empty. | Khong co |
| `methodName` | `string` | Co | Khong validate null/empty. | Khong co |
| `exception` | `Exception` | Co | Khong validate null — neu truyen `null`, `exception.Message` se nem `NullReferenceException` ngay trong ham. | Khong co |
| `description` | `string` | Khong | Khong validate. | `""` |

**Output** — `void`.

**Dieu kien xu ly** — Khong co nhanh re; luon ghi 1 dong duy nhat.

**Side effect** — Ghi ra `Console.WriteLine` (khong ghi file, khong ghi DB, khong goi ngoai).

**Error handling** — Khong co try/catch trong ham nay. Neu `exception` la `null`, viec truy cap `exception.Message` (`CommonBaseConstant.cs:63`) se nem `NullReferenceException` chua duoc xu ly, lam loi lan truyen nguoc len caller.

**Khi nao NEN dung** — Khi can ghi log loi tai cac vi tri khong the/khong nen phu thuoc `ILogger` (VD static helper dung chung, hoac muon dam bao log luon xuat hien ngay ca khi pipeline logging chinh loi).

**Khi nao KHONG dung** — Khi da co `ILogger` kha dung — nen dung `ILogger` de log co cau truc, co the loc/route, thay vi ghi thang ra `Console`.

**Gioi han** — Log ra Console khong the loc theo level/khong tich hop voi Serilog sink (Kafka, v.v.) — cac loi bi "im lang" o goc do cac tang enrich/sink khac. Da duoc cac KB cu (`Data-SQL-CoreSQL.md:435`, `Data-SQL-CoreSQL-TwoEntity.md:398`, `Data-MongoDB-CoreMongoDB.md:130`) ghi nhan khi mo ta `ProjectToExtensions`.

### 3.4 CommonBaseConstant.ConfigLoggerInformationByConsole

**Signature**
```csharp
public static void ConfigLoggerInformationByConsole(
   string className, string methodName, string description)
```
**Muc dich** — Tuong tu `ConfigLoggerExceptionByConsole` nhung ghi muc `[INF]`, khong kem exception. Khong co XML doc `<summary>` (chi co the tag param rong tai `CommonBaseConstant.cs:66-72`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `className` | `string` | Co | Khong validate. | Khong co |
| `methodName` | `string` | Co | Khong validate. | Khong co |
| `description` | `string` | Co | Khong validate. | Khong co |

**Output** — `void`.

**Dieu kien xu ly** — Khong co nhanh re.

**Side effect** — Ghi ra `Console.WriteLine`.

**Error handling** — Khong co try/catch; khong co tham so nao co the gay exception ngay trong than ham (khac voi ham 3.3 vi khong co `exception.Message`).

**Khi nao NEN dung** — Ghi log thong tin (khong phai loi) tai cac vi tri khong tiem duoc `ILogger`.

**Khi nao KHONG dung** — Tuong tu 3.3, nen uu tien `ILogger` khi co san.

**Gioi han** — Duoc goi thuc te 3 lan trong `TenantSRKafkaSinkExtensions.cs` (`:56` trong `SetupConfiguration`; `:105` va `:123` trong `EmitBatchAsync`), du file nay khong thuoc danh sach 11 file nguon cua module nay (grep toan repo `CommonBaseConstant.ConfigLoggerInformationByConsole` xac nhan 3 vi tri goi ngoai khai bao). Danh gia truoc do trong tai lieu nay ("khong tim thay noi nao... goi ham nay") la SAI — chi gioi han trong pham vi 11 file nguon cua module ma khong mo rong grep toan repo nhu da lam voi cac hang so khac trong cung tai lieu (VD `ClaimTypesConstant`, `HeaderConstant`), dan den ket luan thieu chinh xac ve muc do su dung.

### 3.5 RoleDataConstant.GetRoleData

**Signature**
```csharp
public static RoleSR GetRoleData(List<string> roles)
```
**Muc dich** — Quy doi danh sach role-code (chuoi, lay tu claim `SR.SRRoles`) cua 1 user thanh 1 gia tri `RoleSR` duy nhat, dai dien cho muc do phuc tap/pham vi truy cap cao nhat ma user do co.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `roles` | `List<string>` | Co | Duoc kiem tra `null` hoac `Count == 0` o dau ham (`RoleDataConstant.cs:50`). | Khong co |

**Output** — `RoleSR`:
- Neu `roles is null` hoac `roles.Count is 0` -> tra ve `RoleSR.ONLY_CREATE` (`:52`).
- Neu co it nhat 1 phan tu cua `roles` khop voi 1 trong cac danh sach role-code duoc dinh nghia trong `ComplexityForRolesSR` -> tra ve `RoleData` cua tuple co `Order` **nho nhat** trong so cac tuple khop (`:55-56`).
- Neu khong co tuple nao khop (`FirstOrDefault()` tra ve `default` cua tuple, tuc `RoleData` = `(RoleSR)0`) -> ham kiem tra `result is 0` va tra ve `RoleSR.ONLY_CREATE` thay vi gia tri `0` khong hop le (`:58`) — day la ly do `ONLY_CREATE` khong can xuat hien trong `ComplexityForRolesSR` van co the la ket qua tra ve.

**Dieu kien xu ly (theo thu tu thuc thi)**
1. Guard clause: `roles is null || roles.Count is 0` -> tra ve som `ONLY_CREATE`.
2. `Where(x => x.RolesSR.Exists(y => roles.Contains(y)))`: giu lai cac tuple ma **it nhat 1** role-code trong `roles` xuat hien trong `x.RolesSR`.
3. `OrderBy(x => x.Order)`: sap xep tang dan theo `Order` — **`Order` chinh la gia tri `(int)RoleSR` tuong ung**, khong phai thu tu khai bao trong danh sach.
4. `FirstOrDefault()`: lay tuple co `Order` nho nhat (uu tien cao nhat).
5. Guard cuoi: neu `result is 0` (khong co tuple nao khop) -> tra `ONLY_CREATE`.

**Side effect** — Khong co (thuan tinh toan, khong ghi log/DB).

**Error handling** — Khong co try/catch; khong nem exception chu dong. `roles` chua phan tu `null` van an toan vi `Contains`/`Exists` xu ly duoc `null` trong `List<string>`.

**Khi nao NEN dung** — Moi khi can xac dinh 1 gia tri "muc do phuc tap vai tro" duy nhat tu danh sach role-code cua user (dang duoc dung trong `UserAudit.RoleData`, `SRLogEventEnricherExtensions`, `SerilogHandlerMiddleWare` de gan vao log).

**Khi nao KHONG dung** — Khi can biet **toan bo** cac role-code cua user (ham nay chi tra ve 1 gia tri tong hop, mat thong tin cac role con lai).

**Gioi han** — **Thu tu uu tien thuc te KHONG theo thu tu khai bao trong `ComplexityForRolesSR`** (`RoleData/RoleDataConstant.cs:9-40`) ma theo gia tri int ngam dinh cua enum `RoleSR`. Vi du: tuple `MASTER_TICKET` (`Order = (int)RoleSR.MASTER_TICKET = 4`) duoc khai bao **truoc** tuple `EMPLOYEE_MANAGER` (`Order = (int)RoleSR.EMPLOYEE_MANAGER = 3`) trong list (`:22-29`), nhung vi `OrderBy(x => x.Order)` sap xep lai theo gia tri so, mot user co ca 2 role `TICKET_MASTER` va `HANDLER_MANAGER` se nhan ket qua `EMPLOYEE_MANAGER` (Order=3) chu khong phai `MASTER_TICKET` (Order=4) — du comment tai dong 21 mo ta `MASTER_TICKET` co quyen "duyet cac ticket... tren toan quoc", ngu y muc do quan trong cao. Day la hanh vi thuc te cua code, khong phai gia dinh; xem them muc 4.

### 3.6 SRTypePermissions.GetRegisteredPermissions

**Signature**
```csharp
public static List<string> GetRegisteredPermissions()
```
**Muc dich** — "Returns a list of Permissions" (XML doc tai `SRTypePermissions.cs:287-290`): dung reflection de quet toan bo permission-code (const string) duoc khai bao trong cac nested class cua `SRTypePermissions`.

**Input hop le** — Khong co tham so.

**Output** — `List<string>` chua toan bo gia tri (dang chuoi) cua moi field public static tim duoc trong 5 nested class (`Requests`, `Tickets`, `Configs`, `Employees`, `Dashboards`) — tong **57 phan tu** dua theo so hang so liet ke tai muc 2.9 (khong loai trung, khong sap xep).

**Dieu kien xu ly**
1. Khoi tao `List<string> permissions = []`.
2. `typeof(SRTypePermissions).GetNestedTypes()` lay toan bo nested type (5 class).
3. `.SelectMany(c => c.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))`: lay toan bo field public static cua tung nested type (chinh la cac hang so `const string`).
4. Voi moi field: `GetValue(null)`; neu `null` thi `continue` (`:299-302`); nguoc lai `Add($"{propertyValue}")` (ep ve string).

**Side effect** — Khong co (chi doc metadata bang reflection, khong ghi log/DB).

**Error handling** — Khong co try/catch. Ve ly thuyet, `GetFields`/`GetValue` co the nem exception phan xa (VD security exception trong mot so moi truong bi gioi han reflection) nhung khong duoc code nay bat.

**Khi nao NEN dung** — Khi can danh sach toan bo permission-code de seed vao DB/IAM, hoac de sinh tai lieu/kiem tra tinh day du cua cau hinh phan quyen.

**Khi nao KHONG dung** — Khong dung de lay permission-code kem theo `DisplayName`/`Description` cua nhom (ham nay chi tra ve chuoi permission-code, khong tra ve metadata nhom).

**Gioi han** — Neu 2 hang so o 2 nhom khac nhau vo tinh co cung gia tri chuoi, ham van tra ve ca hai (danh sach co the co phan tu trung). Nhanh `if (propertyValue is null) continue` hien la **code phong thu khong bao gio kich hoat** trong trang thai hien tai, vi tat ca field trong 5 nested class deu la `const string` co gia tri, khong co field `null`. Khong tim thay noi nao trong repo nay goi ham `GetRegisteredPermissions()` (xem muc 4).

## 4. Van de da biet

| # | Van de | Vi tri | Anh huong |
|---|---|---|---|
| 1 | `DelimiterConstant.CHAR_APOSTROPHE` va `STRING_APOSTROPHE` mang gia tri `/` (slash) nhung ten hang so goi la "APOSTROPHE" (dau `'`). | `DelimiterConstant.cs:8,14` | Nham lan khi doc code: nguoi dung ten se doan sai gia tri thuc. `HttpClientUtilizes.cs:65,95` dang dung `CHAR_APOSTROPHE` nhu mot dau `/` phan tach duong dan HTTP — dung ve mat gia tri nhung sai ve mat ten goi. |
| 2 | `RoleSRConstant.TICKET_MASTER` co ten hang so la `TICKET_MASTER` nhung gia tri chuoi thuc te dung hau to `T_MASTER` (`"FTEL.SR.T_MASTER"`), khong phai `"FTEL.SR.TICKET_MASTER"`. | `RoleSRConstant.cs:29` | Neu co consumer ben ngoai (API, DB seed) suy doan gia tri tu ten bien se sai; can luon doc gia tri thuc te, khong doan tu ten. |
| 3 | 3 hang so `CachedBaseConstant.ShortTime`, `MediumTime`, `LongTime` duoc khai bao nhung khong duoc bat ky noi nao trong repo su dung; `RandomTimeCache()` chi duoc goi voi gia tri hardcode `5` tai `HttpClientUtilizes.cs:243,261`. | `CachedBaseConstant.cs:12,17,22`; `HttpClientUtilizes.cs:243,261` | Hang so "mo ta cap do thoi gian cache chuan" hien khong lien ket voi bat ky logic thuc te nao — de gay nham lan la dang duoc ap dung dau do. |
| 4 | Toan bo 5 hang so `STRING_*` cua `DelimiterConstant` (`STRING_COMMA`, `STRING_DOT`, `STRING_SEMICOLON`, `STRING_APOSTROPHE`, `STRING_DASH`) va 2 hang so `CHAR_SEMICOLON`, `CHAR_DASH` khong duoc noi nao trong repo su dung. | `DelimiterConstant.cs:5-16` | 7/10 hang so trong file la "dead constant" tinh den commit `89c1ce9`. |
| 5 | 4 hang so cua `ClaimTypesConstant` (`BranchId`, `TitleCode`, `LocationId`, `Organization`) khong duoc noi nao trong repo doc gia tri; `SRPermissions` chi xuat hien trong code da bi comment (`// TODO`). | `ClaimTypesConstant.cs:9,11,13,17,21`; `SRLogEventEnricherExtensions.cs:88-95`; `SerilogHandlerMiddleWare.cs:43-50` | Khong the xac nhan tu source code trong repo nay rang cac loai claim nay dang duoc phat/doc o dau; co the duoc dung o service khac ngoai pham vi da doc. |
| 6 | `SerilogConstant.ErrorCodePropertyName` (`"ErrorCode"`) khong duoc noi nao trong repo gan gia tri vao log, trong khi cac property loi khac (`ErrorMessagePropertyName`, `ErrorCategoryPropertyName`) deu duoc dung. | `SerilogConstant.cs:65` | Truong `ErrorCode` co the bi thieu trong cau truc log thuc te du hang so da duoc dinh nghia san. |
| 7 | 3/6 hang so cua `OpenTelemetryConstant` (`MongoDBActivitySource`, `SqlResilienceActivitySource`, `MongoResilienceActivitySource`) duoc dung de tao `ActivitySource` (hoac la ten source cua thu vien ngoai) nhung **khong** duoc truyen vao `AddSource`/`AddMeter` trong `OpenTelemetryExtensions.cs` (file duy nhat trong repo goi 2 API nay); `HttpResilienceActivitySource` con tro toi 1 class (`FTELSRCore.Utilizes.Policies.HttpResiliencePolicyFactory`) **khong ton tai** trong repo. | `OpenTelemetryConstant.cs:8,12,14,16`; `OpenTelemetryExtensions.cs:9-36` (chi dang ky dong 15,16 cho `CoreCacheActivitySource`, `LoggingBehaviorActivitySource`) | Cac span tao boi `SqlResiliencePolicyFactory`/`MongoResiliencePolicyFactory` co the khong duoc tracer thu thap trong pipeline hien tai neu khong co noi khac (ngoai file da doc) dang ky bo sung; khong xac dinh duoc tu source code trong repo nay co noi nao khac lam dieu do hay khong. |
| 8 | `SRTypeActions` duoc khai bao la `public class SRTypeActions` (khong co tu khoa `static`), khac voi tat ca cac class hang so con lai trong module (deu la `public static class`). | `SRTypeActions.cs:3` | Ve mat ky thuat co the bi khoi tao instance (`new SRTypeActions()`) mot cach vo nghia vi class chi chua `const`; day la su khac biet phong cach so voi phan con lai cua module, khong gay loi runtime. |
| 9 | `SRTypePermissions.Employees.EMPLOYEE_DETAIL_VIEW` sinh ra chuoi `"FTEL.SR.EMPLOYEE.EMPLOYEE.VIEW"` — doan `EMPLOYEE` bi lap 2 lan (mot lan tu module `EMPLOYEE`, mot lan tu submodule cung ten `EMPLOYEE`) trong khi cac permission-code khac dung tu khoa rieng cho submodule (VD `DETAIL`, `CALENDAR`). | `SRTypePermissions.cs:263` | Gia tri permission-code trong nhom nay khong theo cung quy uoc dat ten voi phan con lai cua file; neu day la loi danh may (du dinh la `EMPLOYEE.DETAIL.VIEW`) thi permission-code thuc te dang khac voi thiet ke du kien — khong xac dinh duoc y do thiet ke thuc su tu source code, chi ghi nhan hien trang. |
| 10 | Comment XML cua `RoleSR.ADMIN` ("Chỉ được xử lý lịch trực và nhân sự và các thông tin do bản thân tạo.") mo ta mot vai tro **bi han che**, trong khi ten `ADMIN` va comment cua `RoleSRConstant.ADMIN` ("Admin hệ thống") ham y vai tro **quan tri toan he thong**. | `RoleSR.cs:36-39`; `RoleSRConstant.cs:49-52` | Hai comment mo ta 2 y nghia khac nhau cho cung 1 khai niem "ADMIN"; theo nguyen tac uu tien source code, ban tai lieu nay chi ghi nhan noi dung tung comment dung nguyen van, khong tu suy dien y nghia dung. |
| 11 | Thu tu uu tien thuc te trong `RoleDataConstant.GetRoleData()` duoc quyet dinh boi gia tri `(int)RoleSR` (ngam dinh tang dan tu `ALL=1`), **khong** theo thu tu khai bao trong list `ComplexityForRolesSR`. Cu the, `MASTER_TICKET` (Order=4) duoc khai bao truoc `EMPLOYEE_MANAGER` (Order=3) nhung se co do uu tien **thap hon** khi ca 2 cung khop. | `RoleData/RoleDataConstant.cs:9,17-29,55-56`; `Enum/RoleSR.cs:3-40` | Nguoi doc code theo thu tu tu-tren-xuong-duoi de doan "vai tro nao manh hon" se hieu nham; hanh vi thuc te phai doc qua gia tri enum, khong phai vi tri khai bao. Day la hanh vi hien tai cua code, chua ro co phai chu dinh cua nguoi viet hay khong (khong xac dinh duoc tu source code). |
| 12 | `RoleDataConstant.GetRoleData()` tra ve `RoleSR.ONLY_CREATE` trong 2 truong hop khac nhau (roles rong/null, VA khong co role nao khop trong `ComplexityForRolesSR`) nhung `ONLY_CREATE` khong xuat hien trong bat ky tuple nao cua `ComplexityForRolesSR`. | `RoleData/RoleDataConstant.cs:9-40,50-58` | `ONLY_CREATE` la gia tri "fallback ngam" — khong co role-code nao (trong `RoleSRConstant`) tuong ung truc tiep voi `ONLY_CREATE`; day la thiet ke hop ly (vai tro yeu nhat = khong co role dac biet nao) nhung can luu y khi doc code de khong tim nham "role-code cua ONLY_CREATE". |
| 13 | Doi chieu voi KB cu: `Data-MongoDB-CoreMongoDB.md:97`, `Data-SQL-UnitOfWork-DbContexts.md:877` va `Data-SQL-CoreSQL.md:435` mo ta hanh vi cua `CommonBaseConstant.DateTimeUtc()` va `CommonBaseConstant.ConfigLoggerExceptionByConsole()` **khop dung** voi source code doc lai trong tai lieu nay (gia tri `addHour=7`, cong thuc `TimeProvider.System.GetUtcNow().DateTime.AddHours(addHour)`, hanh vi log-ra-console-va-bo-qua-loi). Khong phat hien sai lech nao giua 8 file KB cu va source code cho phan `CommonBaseConstant` duoc doi chieu. | `CommonBaseConstant.cs:47-50,60-64`; doi chieu voi `Data-MongoDB-CoreMongoDB.md:94-97,1864`, `Data-SQL-UnitOfWork-DbContexts.md:61,871-900,1167`, `Data-SQL-CoreSQL.md:435` | Khong co hanh dong sua doi can thuc hien; ghi nhan de xac nhan tinh nhat quan giua tai lieu KB da co va source code. |

