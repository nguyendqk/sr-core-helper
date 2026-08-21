# JSON & Convert Helpers

> Nguon: FTELSRCore.Shared/Helpers/JSonParseHelpers.cs, FTELSRCore.Shared/Helpers/ConvertHelpers.cs
> Loai: static class (`ConvertHelpers`) va static partial class (`JSonParseHelpers` - chia thanh 2 khoi `partial` trong CUNG mot file: khoi 1 chua 3 method public, khoi 2 chua config/converter noi bo)
> Cap nhat theo commit: 89c1ce9

## 1. Tong quan

`JSonParseHelpers` (namespace `FTELSRCore.Helpers`) cung cap lop serialize/deserialize JSON dung chung
cho toan repo: chuyen doi tuong sang chuoi JSON (`ToJSon`), thu deserialize JSON/`BsonDocument` sang kieu
cu the ma khong throw ra ngoai (`JSonTryParse`), va mot bo `JsonConverter` noi bo (int/long/double/decimal/
bool/DateTime) giup `System.Text.Json` linh hoat hon voi du lieu "long lo" (so dang chuoi, null thay 0,
dinh dang ngay MongoDB `$date`...). `ConvertHelpers` (cung namespace) la tap hop ham tien ich khong lien
quan truc tiep den nhau: doc thong tin HTTP request (User-Agent, IP), che so dien thoai, lay
`Description` cua enum/const, doi chuoi sang enum/DateTime, bo dau tieng Viet, doi so sang chu tieng Viet,
va validate/phan loai so dien thoai Viet Nam. Ca hai file nam o tang **Shared/Helpers** - duoc goi truc
tiep boi cac tang cao hon (Data/MongoDB, Data/SQL, Extensions/Kafka, Utilizes/CallApi...) nhung ban
than khong phu thuoc nguoc lai vao cac tang do.

### 1.1 Pham vi chuc nang

| Lam duoc | Khong lam duoc |
|---|---|
| Serialize object bat ky sang JSON, tu dong fallback sang Newtonsoft khi `System.Text.Json` khong ho tro (`ToJSon`, JSonParseHelpers.cs:19-62) | Khong dam bao chuoi JSON output tu `ToJSon` la hop le neu ca `System.Text.Json` va `Newtonsoft` deu that bai - khi do method nem exception ra ngoai (catch block khong bat duoc exception cua `JsonConvert.SerializeObject` trong nhanh fallback, xem muc 3) |
| Deserialize JSON string/`BsonDocument` sang kieu `T` ma KHONG throw exception ra ngoai, tra `bool` bao thanh cong/khong (`JSonTryParse`, JSonParseHelpers.cs:75-195) | Khong phan biet duoc "JSON hop le nhung la object/array rong" voi "JSON loi" - ca hai deu tra `false` (JSonParseHelpers.cs:98-103, 152-158) |
| Doc so nguyen/so thuc/DateTime/bool tu JSON du du lieu o dang chuoi (vi du `"123"` thay `123`) nho bo converter noi bo (JSonParseHelpers.cs:281-800) | Khong tu dong nhan biet timezone/millisecond khi parse DateTime tu chuoi - `DateTimeConverter`/`DateTimeNullAbleConverter` **cat bo** ky tu `Z` va phan thap phan giay truoc khi parse (JSonParseHelpers.cs:554-562, 641-649) |
| Lay User-Agent, IP client tu `HttpContext` voi nhieu header fallback (RFC 7239, X-Forwarded-For, X-Real-IP) (ConvertHelpers.cs:21-95) | Khong xac thuc/lam sach gia tri IP lay duoc - tin tuong hoan toan header do client gui, co the bi gia mao (ConvertHelpers.cs:61-89) |
| Che so dien thoai Viet Nam hop le, giu 3 so cuoi (`MaskPhoneNumber`, ConvertHelpers.cs:103-116) | Khong che duoc so khong hop le theo `VietnamesePhoneValidator` - tra nguyen so goc khong bao loi (ConvertHelpers.cs:105-109) |
| Validate va phan loai so dien thoai VN (mobile/geographic/toll-free/premium) + doan nha mang (`VietnamesePhoneValidator`, ConvertHelpers.cs:508-583) | **Khong phan loai dung**: pattern `GeoPattern` duoc kiem tra TRUOC `MobilePattern` va thuc te khop voi hau het so di dong 10 chu so hop le, nen rat nhieu so di dong bi gan nham `Type = "geographic"`, `Carrier = null` (xem muc 3, da kiem chung bang regex) |
| Doi chuoi sang `DateTime?` thu nhieu dinh dang, co fallback parse tong quat (`ConvertStringToDateTime`, ConvertHelpers.cs:380-412) | Khi truyen `format` cu the ma parse that bai, KHONG tu dong thu lai voi danh sach `AllDateFormats` - chi con fallback `DateTime.TryParse` tong quat (ConvertHelpers.cs:393-403) |
| Doi so (`decimal`) sang chu tieng Viet, ho tro don vi den "ty" (`NumberToVietnameseWords`, ConvertHelpers.cs:421-502) | Khong xu ly so am (tra ve chuoi rac `" đồng"`) va **throw exception khong bat** voi so >= 10^12 (vuot don vi "ty", xem muc 3) |
| Bo dau tieng Viet theo 2 each khac nhau (`UnsignViet`, `RemoveDiacritics`) | `UnsignViet` KHONG bo duoc dau cua "Đ"/"đ" (khac voi `RemoveDiacritics` co xu ly rieng) - da kiem chung, xem muc 3 |
| Lay `Description` cua enum member hoac cua field const co gan `[Description]` (`GetDescriptionInEnum`, `DescriptionForProperty`) | `DescriptionForProperty` tra ve chuoi rong (khong phai ten field) khi field const **khong co** `[Description]`, do dung sai API reflection (`GetProperty` thay vi doc lai `nameProperty`/`fieldInfo.Name`) - xem muc 3 |

### 1.2 Dependency

| Thanh phan | Muc dich su dung |
|---|---|
| `System.Text.Json` (`JsonSerializer`, `JsonConverter<T>`, `Utf8JsonReader`, `Utf8JsonWriter`) | Engine serialize/deserialize chinh cua `ToJSon`/`JSonTryParse` va cua toan bo converter noi bo |
| `Newtonsoft.Json` (`JsonConvert`) | Fallback khi `System.Text.Json` nem `NotSupportedException` trong `ToJSon` (JSonParseHelpers.cs:33-35); dung de serialize object vao message log loi (khong lien quan JSON tra ve) |
| `MongoDB.Bson` (`BsonValue`, `BsonDocument`, `BsonElement`) | `ToJSon` uu tien goi `BsonValue.ToJson()`; `JSonTryParse(this BsonDocument, ...)` nhan dau vao la ket qua truy van Mongo |
| `Microsoft.Extensions.Logging.ILogger` (global using) | Tham so `logger` tuy chon trong `ToJSon`/`JSonTryParse`; neu duoc truyen, ghi loi qua extension `ErrorException` |
| `FTELSRCore.Extensions.Loggers.LoggerExtensions.ErrorException` (global using) | Ham extension thuc su ghi log loi khi co `ILogger` (LoggerExtensions.cs:625) |
| `FTELSRCore.Constants.CommonBaseConstant.ConfigLoggerExceptionByConsole` (global using) | Ghi loi ra Console khi KHONG co `ILogger` duoc truyen (nhanh `default` trong `switch (logger)`) |
| `Microsoft.AspNetCore.Http.HttpContext`, `Microsoft.Extensions.Primitives.StringValues` | Doc header HTTP trong `GetUserAgent`, `GetClientIpAddress` |
| `System.Security.Claims.ClaimsPrincipal` | Doc claim trong `ConvertClaimsPrincipalToData` |
| `System.Net.HttpStatusCode` | Tra ten status code trong `ConvertHttpStatusCodeCodeByName` |
| `System.ComponentModel.DescriptionAttribute`, `System.Reflection` | Doc attribute `[Description]` tren enum/field const trong `GetDescriptionInEnum`, `DescriptionForProperty` |
| `System.Text.RegularExpressions.Regex` | `UnsignViet`, `VietnamesePhoneValidator` (cac pattern compiled) |
| `System.Globalization` (`CultureInfo`, `DateTimeStyles`, `CharUnicodeInfo`, `NormalizationForm`) | Parse/format DateTime; `RemoveDiacritics` dung `NormalizationForm`/`UnicodeCategory` |

### 1.3 Danh muc API

| API | Nhom | Mo ta ngan |
|---|---|---|
| `ToJSon<T>(this T, ILogger)` | JSonParseHelpers | Serialize object sang JSON, fallback Newtonsoft, nuot loi tra `""` |
| `JSonTryParse<T>(this BsonDocument, out T, ILogger)` | JSonParseHelpers | Thu chuyen `BsonDocument` sang `T` qua buoc trung gian JSON string |
| `JSonTryParse<T>(this string, out T, ILogger)` | JSonParseHelpers | Thu deserialize chuoi JSON sang `T`, khong throw |
| `DateTimeConverter` (public class) | JSonParseHelpers | `JsonConverter<DateTime>` doc duoc ca chuoi thuong va dang Mongo `{ "$date": ... }` |
| `DateTimeNullAbleConverter` (public class) | JSonParseHelpers | Ban `DateTime?` cua `DateTimeConverter` |
| `_defaultJsonOptions`, `_jsonSerializerOptions`, `_dateTimeFormats` (internal field) | JSonParseHelpers | Cau hinh `JsonSerializerOptions` va danh sach format ngay dung chung |
| `IntConverter`/`IntNullableConverter`/`LongConverter`/`LongNullableConverter`/`DoubleConverter`/`DoubleNullableConverter`/`DecimalConverter`/`DecimalNullableConverter`/`BooleanConverter` (private sealed class) | JSonParseHelpers | Converter noi bo, khong public nhung quyet dinh hanh vi parse cua `_jsonSerializerOptions` |
| `GetUserAgent(HttpContext)` | ConvertHelpers | Lay User-Agent tu header |
| `GetClientIpAddress(HttpContext)` | ConvertHelpers | Lay IP client qua Forwarded/X-Forwarded-For/X-Real-IP |
| `MaskPhoneNumber(string)` | ConvertHelpers | Che so dien thoai, giu 3 so cuoi |
| `GetDescriptionInEnum<TEnum>(this TEnum)` | ConvertHelpers | Lay `[Description]` cua enum member |
| `ConvertEnum<TEnum>(this string, bool)` | ConvertHelpers | Parse chuoi sang enum nullable |
| `GetMinEnumValue<TEnum>(params TEnum[])` | ConvertHelpers | Lay enum nho nhat (theo `int`) trong danh sach |
| `ConvertHttpStatusCodeCodeByName(this int)` | ConvertHelpers | Doi status code so sang ten enum `HttpStatusCode` |
| `UnsignViet(string)` | ConvertHelpers | Bo dau tieng Viet (Regex + NFD/NFC) |
| `RemoveDiacritics(string)` | ConvertHelpers | Bo dau tieng Viet (manual, co xu ly Đ/đ) |
| `CapitalizeFirstLetter(string)` | ConvertHelpers | Viet hoa ky tu dau |
| `ConvertClaimsPrincipalToData(ClaimsPrincipal, string, string)` | ConvertHelpers | Lay gia tri claim theo loai |
| `DescriptionForProperty(Type, object)` | ConvertHelpers | Lay `Description` cua field const qua reflection |
| `ConvertStringToDateTime(this string, string, IFormatProvider, DateTimeStyles, bool)` | ConvertHelpers | Doi chuoi sang `DateTime?`, nhieu format fallback |
| `NumberToVietnameseWords(decimal, bool)` | ConvertHelpers | Doi so sang chu tieng Viet |
| `VietnamesePhoneValidator.Validate(string)` + `record ValidationResult` | ConvertHelpers (nested static class) | Validate + phan loai so dien thoai VN |

## 2. Chi tiet API

### 2.1 ToJSon<T>

**Signature**
```csharp
public static string ToJSon<T>(this T obj, ILogger logger = null)
```
**Muc dich** - Serialize mot object bat ky sang chuoi JSON, dung rieng cho `BsonValue` (goi `ToJson()` cua
MongoDB.Driver) va dung `System.Text.Json` (`_defaultJsonOptions`) cho cac truong hop con lai.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `obj` | `T` (generic, extension `this`) | Co | Kiem tra `obj is null` truoc khi serialize (dong 21) | khong co |
| `logger` | `ILogger` | Khong | Neu null se ghi log qua Console thay vi `ILogger` | `null` |

**Output** - `string`: chuoi JSON hop le khi thanh cong; `string.Empty` khi `obj` la `null` (dong 21) HOAC
khi qua trinh serialize gap `Exception` (khac `NotSupportedException`) va bi nuot (dong 61); chuoi JSON
tu `Newtonsoft.JsonConvert.SerializeObject` khi `System.Text.Json` nem `NotSupportedException` (dong 35).

**Dieu kien xu ly**
1. `obj is null` -> tra `string.Empty` ngay (dong 21).
2. `obj is BsonValue` -> goi `bsonValue.ToJson()` (dong 27).
3. Truong hop con lai -> `JsonSerializer.Serialize(obj, _defaultJsonOptions)` (dong 29-30).
4. Neu (3) nem `NotSupportedException` (vi du kieu chua vong lap tham chieu ma `ReferenceHandler` khong
   xu ly duoc, hoac kieu khong the serialize) -> fallback `Newtonsoft.Json.JsonConvert.SerializeObject(obj)`
   (dong 35).
5. Neu nem `Exception` khac -> ghi log (xem Side effect) roi tra `string.Empty` (dong 61).

**Side effect** - Ghi log loi: qua `logger.ErrorException(...)` neu `logger != null` (dong 46-47), hoac qua
`CommonBaseConstant.ConfigLoggerExceptionByConsole(...)` ra Console neu `logger == null` (dong 53-54). Ca
hai truong hop deu goi lai `Newtonsoft.Json.JsonConvert.SerializeObject(obj)` de dung lam noi dung message
log (dong 40) - **ban than buoc nay co the throw** neu `Newtonsoft` cung khong serialize duoc `obj`, va
exception do KHONG duoc bat (nam ngoai try-catch chinh, xem muc 3).

**Error handling** - Bat rieng `NotSupportedException` (fallback Newtonsoft, khong log), bat chung
`Exception` con lai (log + tra `""`). Khong bao gio de exception thoat ra ngoai method trong duong bay
chinh (tru truong hop noi tren khi build message log).

**Khi nao NEN dung** - Serialize nhanh mot object de ghi vao log/message, khong can chuoi JSON output
phai hoan hao (chi can "co gang tot nhat").

**Khi nao KHONG dung** - Khi can biet CHINH XAC serialize co thanh cong hay khong (vi `""` co the la ket
qua thanh cong that (obj thanh mot object khong co field) hoac la ket qua loi/obj null - khong phan biet
duoc tu return value).

**Gioi han** - (a) Loi im lang: goi cho object gay loi luon nhan lai `""` ma khong biet co loi hay khong
neu khong truyen `logger`/khong doc Console; (b) buoc build message log co the throw ra ngoai (dong 40),
pha vo tinh "khong bao gio throw" ma XML doc goi y; (c) khong co gioi han kich thuoc/do sau object.

### 2.2 JSonTryParse<T>(this BsonDocument obj, out T result, ILogger logger = null)

**Signature**
```csharp
public static bool JSonTryParse<T>(this BsonDocument obj, out T result, ILogger logger = null)
```
**Muc dich** - Chuyen mot `BsonDocument` (thuong la ket qua truy van MongoDB) sang kieu `T` bang cach di
qua buoc trung gian "serialize sang JSON string" (`ToJSon`) roi "deserialize string" (overload
`JSonTryParse(this string, ...)`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `obj` | `BsonDocument` | Co | Kiem tra null (dong 80), `ElementCount == 0` (dong 83), va truong hop 1 element duy nhat la `BsonNull` (dong 86-91) | khong co |
| `result` | `out T` | Co (out) | Luon gan `default` truoc khi xu ly (dong 77) | khong co |
| `logger` | `ILogger` | Khong | Dung de log khi co exception | `null` |

**Output** - `bool`: `true` khi buoc serialize sang JSON thanh cong va KHONG rong/null/`{}`/`[]`
(dong 98-107); `false` khi `obj` null/rong/chi chua `BsonNull`, HOAC khi chuoi JSON sau serialize la
rong/`"null"`/`"{}"`/`"[]"`, HOAC khi co exception (bat va tra `false`, dong 109-135).

**Dieu kien xu ly**
1. `obj is null` -> `false` (dong 80).
2. `obj.ElementCount == 0` -> `false` (dong 83).
3. `obj.ElementCount == 1` va element duy nhat co `IsBsonNull == true` -> `false` (dong 86-91). Chi kiem
   tra khi CHI CO 1 element - `BsonDocument` co nhieu element deu la null se KHONG bi loai o day (xem
   Gioi han).
4. Serialize `obj` sang JSON qua `obj.ToJSon()` (dong 95).
5. Neu JSON rong/whitespace hoac (khong phan biet hoa thuong) bang `"null"`/`"{}"`/`"[]"` sau khi trim ->
   `false` (dong 98-103).
6. Goi `json.JSonTryParse(out result)` (dong 105) **nhung KHONG kiem tra gia tri tra ve cua loi goi nay**
   - dong 107 luon `return true` bat ke buoc (6) thanh cong hay that bai (xem muc 3, day la mot bug ro
   rang).
7. Bat `Exception` bat ky trong toan bo qua trinh -> log + tra `false`, `result = default` (dong 109-135).

**Side effect** - Ghi log loi qua `logger.ErrorException` hoac `CommonBaseConstant.ConfigLoggerExceptionByConsole`
khi co exception (dong 118-119, 125-126), tuong tu `ToJSon`.

**Error handling** - Bat `Exception` chung quanh toan bo than ham (tru buoc goi `ToJSon` ben trong, ban
than da tu bat exception rieng). Khong throw ra ngoai trong moi truong hop.

**Khi nao NEN dung** - Chuyen doi nhanh `BsonDocument` tu MongoDB sang DTO/model mong doi, khi chap nhan
tra `false`/`default` neu co van de.

**Khi nao KHONG dung** - Khi can biet chinh xac buoc deserialize JSON->T co thanh cong hay khong, vi
method nay **luon tra `true`** mien la buoc serialize BsonDocument->JSON thanh cong, du buoc JSON->T
(dong 105) co that bai hay khong.

**Gioi han** - (1) **Bug nghiem trong**: dong 105-107 goi `json.JSonTryParse(out result)` nhung bo qua
gia tri `bool` tra ve, roi luon `return true`. Neu kieu `T` khong khop voi cau truc JSON (vi du field
sai kieu, thieu constructor phu hop) thi buoc (6) tra `false` va gan `result = default`, nhung ham nay
van bao "thanh cong" (`true`) voi `result` la gia tri `default` (co the la `null` cho reference type) -
**day chinh la kieu bug "du lieu bi mat am tham"** ma yeu cau tai lieu nhac toi: caller thay `true` se
tin tuong `result` hop le, trong khi thuc te co the la `null`/`default`. (2) Kiem tra "chi 1 element va
element do null" (dong 86-91) khong bao phu truong hop nhieu element deu null.

### 2.3 JSonTryParse<T>(this string obj, out T result, ILogger logger = null)

**Signature**
```csharp
public static bool JSonTryParse<T>(this string obj, out T result, ILogger logger = null)
```
**Muc dich** - Thu deserialize mot chuoi JSON sang kieu `T` bang `System.Text.Json` voi bo converter noi
bo (`_jsonSerializerOptions`), khong throw ra ngoai.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `obj` | `string` | Co | Rong/whitespace, hoac (trim, khong phan biet hoa thuong) bang `"null"`, hoac bang `"{}"`/`"[]"` deu bi coi la khong hop le | khong co |
| `result` | `out T` | Co (out) | Gan `default` truoc khi tra `false` | khong co |
| `logger` | `ILogger` | Khong | | `null` |

**Output** - `bool`: `true` va `result` la object deserialize duoc khi thanh cong (dong 162-166); `false`
va `result = default` khi `obj` khong hop le theo dieu kien tren (dong 152-158) HOAC khi
`JsonSerializer.Deserialize<T>` nem exception bat ky (dong 168-193).

**Dieu kien xu ly**
1. `string.IsNullOrWhiteSpace(obj)` hoac trim bang `"null"` (khong phan biet hoa/thuong) hoac bang
   `"{}"`/`"[]"` -> `result = default`, tra `false` ngay, KHONG thu deserialize (dong 152-158).
2. Nguoc lai, goi `JsonSerializer.Deserialize<T>(obj, _jsonSerializerOptions)` (dong 162-164) - dung bo
   converter tuy bien (Int/Long/Double/Decimal/Boolean/DateTime, xem muc 2.4-2.6).
3. Neu (2) thanh cong -> tra `true`.
4. Neu (2) nem exception (vi du `JsonException` tu chinh cac converter noi bo, hoac loi cau truc JSON) ->
   log + `result = default` + tra `false` (dong 168-194).

**Side effect** - Ghi log loi qua `ILogger` hoac Console khi co exception, giong 2 ham tren.

**Error handling** - Bat toan bo `Exception` phat sinh tu `JsonSerializer.Deserialize`, khong phan biet
loai loi (cu phap JSON sai, kieu khong khop, converter throw `JsonException`...) - tat ca deu quy ve
`false`/`default`. **Day la diem "nuot loi im lang" quan trong nhat cua module**: mot JSON hop le ve cu
phap nhung khong khop kieu `T` (vi du field kieu `int` nhung JSON tra chuoi khong parse duoc) se lam
`JsonException` bi bat va bien mat, chi con lai `false` - nguoi goi khong the phan biet "JSON rong/vo
nghia theo quy uoc" (buoc 1) voi "JSON that ra co du lieu nhung loi cau truc" (buoc 4) neu khong doc log.

**Khi nao NEN dung** - Deserialize an toan chuoi JSON co nguon goc khong dang tin tuyet doi (payload Kafka,
response ben ngoai...), noi chap nhan "khong parse duoc thi bo qua".

**Khi nao KHONG dung** - Khi JSON hop le dai dien cho object/array rong CO Y NGHIA nghiep vu (vi du danh
sach rong `[]` la ket qua hop le) - ham nay se coi day la "khong hop le" va tra `false` (dong 154), day la
gioi han da duoc xac nhan cheo boi `docs/knowledge-base/Extensions-Kafka.md` (muc "Van de da biet" #2 cua
file do, dan JSonParseHelpers.cs:152-158).

**Gioi han** - Xem "Error handling" o tren; ngoai ra, cac converter tuy bien trong `_jsonSerializerOptions`
(muc 2.4) co the tu thay doi gia tri (vi du chuoi `"true"`/`"false"` -> `bool`, so o dang chuoi -> so) ma
khong co canh bao nao ve viec da "ep kieu" du lieu dau vao.

### 2.4 Bo JsonConverter noi bo (Int/Long/Double/Decimal/Boolean) va cau hinh JsonSerializerOptions

Cac converter nay la `private sealed class` (khong phai API public) nhung quyet dinh truc tiep hanh vi
cua `JSonTryParse<T>(this string, ...)` vi chung nam trong `_jsonSerializerOptions.Converters`
(JSonParseHelpers.cs:220-227). Tom tat hanh vi `Read` cua tung converter:

| Converter | Token `Number` | Token `String` | Token `Null` | Token khac |
|---|---|---|---|---|
| `IntConverter` (int, JSonParseHelpers.cs:281-324) | `TryGetInt32`; neu fail thu `TryGetDouble` roi ep `(int)` | `int.TryParse`; fail -> throw `JsonException` | tra `0` | throw `JsonException` |
| `IntNullableConverter` (int?, dong 326-374) | Giong tren | Giong tren (throw neu fail) | tra `null` | throw `JsonException` |
| `LongConverter` (long, dong 376-419) | `TryGetInt64`; neu fail thu `TryGetDouble` roi ep **`(int)`** (dong 393) - xem muc 3 | `long.TryParse`; fail -> throw | tra `0` | throw `JsonException` |
| `LongNullableConverter` (long?, dong 421-471) | Giong `LongConverter` (van ep `(int)` khi fallback double, dong 438) | Giong tren | tra `null` | throw |
| `DoubleConverter` (double, dong 473-498) | `TryGetDouble` | `double.TryParse` | tra `0` | throw |
| `DoubleNullableConverter` (double?, dong 676-706) | `TryGetDouble` | `double.TryParse` | tra `null` | throw |
| `DecimalConverter` (decimal, dong 708-734) | `TryGetDecimal` | `decimal.TryParse` | tra `0` | throw |
| `DecimalNullableConverter` (decimal?, dong 736-768) | `TryGetDecimal` | `decimal.TryParse` | tra `null` | throw |
| `BooleanConverter` (bool, dong 770-800) | `TryGetInt32`; khac `0` la `true` | Chi nhan CHINH XAC `"true"`/`"false"` (phan biet hoa/thuong), gia tri khac throw `JsonException` khong co message | Khong co nhanh rieng - token `Null` roi vao `_ => throw` | **Da kiem chung sai trong ban truoc**: token `True`/`False` (JSON boolean chuan, truong hop pho bien nhat) DUOC XU LY RIENG va tra thang `true`/`false` (dong 776, 778) - KHONG throw. Chi cac token con lai (Array/Object/...) moi roi vao `_ => throw JsonException("Json bool default")` |

**Cau hinh chung** - `_defaultJsonOptions` (dong 207-212, dung trong `ToJSon`) va `_jsonSerializerOptions`
(dong 214-228, dung trong `JSonTryParse<T>(string)`) deu: `PropertyNameCaseInsensitive = true`,
`ReferenceHandler.IgnoreCycles`, `DefaultIgnoreCondition = WhenWritingNull`. Chi `_jsonSerializerOptions`
co gan them **9** converter noi bo o tren (Boolean/Int/IntNullable/Long/LongNullable/Double/DoubleNullable/
Decimal/DecimalNullable - da dem lai tu `Converters = { ... }`, JSonParseHelpers.cs:220-227, KHONG PHAI
10 nhu ban truoc ghi nham) cong voi `DateTimeConverter`/`DateTimeNullAbleConverter`, tong cong 11 converter.

`_dateTimeFormats` (dong 230-277) la mang **32** chuoi format (da dem lai tung phan tu tu source, KHONG
PHAI 29 nhu ban truoc ghi nham) dung boi 2 converter DateTime ben duoi; chuoi `"yyyy-MM-ddTHH:mm:ss"`
xuat hien **2 lan** trong mang (dong 241 va dong 275) - du thua khong anh huong logic nhung la du lieu
hardcode trung lap.

**Gioi han chung cua bo converter** - `BooleanConverter` throw `JsonException` KHONG co message khi gap
chuoi khac `"true"`/`"false"` (dong 789, `throw new JsonException()`), khac voi cac converter khac co
message ro rang - kho debug hon khi tra cuu log.

### 2.5 DateTimeConverter

**Signature**
```csharp
public class DateTimeConverter : JsonConverter<DateTime>
```
**Muc dich** - Doc gia tri `DateTime` tu JSON o 2 dang: chuoi ngay thang thong thuong, hoac object kieu
MongoDB Extended JSON `{ "$date": "..." }`.

**Input hop le** - Token JSON dau vao khi `Read` duoc goi boi `System.Text.Json`: `JsonTokenType.String`
(chuoi ngay) hoac `JsonTokenType.StartObject` voi property dau tien ten `"$date"` co gia tri la chuoi
(dong 594-624). Cac token khac (Number, True/False, Null...) khong duoc xu ly rieng.

**Output** - `DateTime`: gia tri parse duoc; **`DateTime.MinValue`** khi chuoi rong/null (dong 636), khi
khong parse duoc theo bat ky format nao trong `_dateTimeFormats`/style nao trong `stylesToTry` (dong 667),
hoac khi token JSON khong phai String/StartObject-hop-le (dong 624 - roi qua `switch` ma khong khop
case nao).

**Dieu kien xu ly**
1. Token `String` -> `ConvertDateTimeByString(reader.GetString())`.
2. Token `StartObject` -> doc property tiep theo; neu ten property la `"$date"` va gia tri la `String` ->
   `ConvertDateTimeByString(value)`; nguoc lai (property khac ten, hoac gia tri khong phai string) ->
   `break` khoi `switch`, roi xuong `return DateTime.MinValue` (dong 624).
3. `ConvertDateTimeByString`: neu chuoi chua `'Z'` -> **cat bo ky tu cuoi** (`value[..^1]`, dong 643,
   GIA DINH `Z` luon la ky tu cuoi cung); neu chua `'.'` -> **cat bo toan bo phan sau dau `.`** (millisecond,
   dong 646-649, mat du lieu phan thap phan giay); sau do thu `DateTime.TryParseExact` voi tung
   `DateTimeStyle` trong `[None, AssumeUniversal|AdjustToUniversal, RoundtripKind]` (dong 651-665), tra
   ket qua dau tien khop.

**Side effect** - Khong co.

**Error handling** - Bat rieng `FormatException` tu logic ben trong va **nem lai chinh no** voi message
moi (dong 669-671, `throw new FormatException(...)`) - đây la truong hop **converter nay CO THE THROW ra
ngoai** (khong nuot loi), khac voi phan lon ham khac trong 2 file nay. Exception nay se lan truyen len
`JsonSerializer.Deserialize` roi bi `JSonTryParse<T>(string)` bat lai o tang tren (muc 2.3) va quy ve
`false`.

**Khi nao NEN dung** - Field `DateTime` (non-null) trong DTO deserialize tu JSON co nguon MongoDB hoac
chuoi ngay dang ISO/thuong gap.

**Khi nao KHONG dung** - Khi can giu nguyen thong tin timezone/offset hoac phan giay le - converter nay
loai bo ca hai truoc khi parse.

**Gioi han** - (1) Loai bo `Z`/phan thap phan giay TRUOC khi thu parse (dong 641-649) -> mat thong tin
UTC/millisecond mot cach am tham (khong log, khong canh bao); (2) gia dinh `Z` luon o vi tri cuoi cung
(`value[..^1]`) - neu chuoi co dang offset (`+07:00`) thay vi `Z`, nhanh nay khong duoc kich hoat va chuoi
giu nguyen offset, hanh vi khac voi truong hop `Z`; (3) tra `DateTime.MinValue` cho ca "chuoi rong" va
"khong parse duoc" - khong phan biet duoc 2 truong hop tu gia tri tra ve.

### 2.6 DateTimeNullAbleConverter

**Signature**
```csharp
public class DateTimeNullAbleConverter : JsonConverter<DateTime?>
```
**Muc dich, Input, dieu kien xu ly** - Giong hoan toan `DateTimeConverter` (muc 2.5), chi khac kieu tra ve
la `DateTime?`.

**Output** - `DateTime?`: gia tri parse duoc; **`null`** (thay vi `DateTime.MinValue`) khi chuoi rong/null
(dong 549), khi khong parse duoc (dong 581), hoac khi token khong hop le (dong 534).

**Side effect** - Khong co.

**Error handling** - Giong `DateTimeConverter`: nem lai `FormatException` (dong 583-586) khi co loi format
trong logic parse (truong hop hiem, vi cac nhanh chinh deu tra `null` thay vi throw).

**Khi nao NEN dung / KHONG dung / Gioi han** - Giong `DateTimeConverter` (muc 2.5), khac biet duy nhat la
gia tri "khong xac dinh duoc" tra ve `null` (an toan hon de kiem tra `HasValue` o tang goi) thay vi
`DateTime.MinValue` de tram co the bi hieu nham la mot ngay thuc.

**Van de tham chieu voi KB cu** - `docs/knowledge-base/Abstractions-DomainPrimitives.md` (dong 31) co
nhac den `FTELSRCore.Helpers.JSonParseHelpers.DateTimeNullAbleConverter` nhu mot dependency truc tiep cua
`BaseEntityMongoDB.cs:4` (qua `using static`), va noi ro "khong duoc tai lieu hoa o day" - phu hop, khong
mau thuan voi tai lieu nay.

---
### 2.7 GetUserAgent

**Signature**
```csharp
public static string GetUserAgent(HttpContext httpContext)
```
**Muc dich** - Lay gia tri header User-Agent cua request.

**Input hop le** | Tham so | Kieu | Bat buoc | Rang buoc | Mac dinh |
|---|---|---|---|---|
| `httpContext` | `HttpContext` | Co | Khong kiem tra null truoc - neu null se throw `NullReferenceException` va bi bat boi `catch` chung (dong 39-42) | khong co |

**Output** - `string`: gia tri header `"User-Agent"` neu co (dong 27-30); neu khong, thu header
`"UserAgent"` (dong 32-35); neu ca hai khong co hoac `httpContext` null/loi -> `string.Empty` (dong 37,
41).

**Dieu kien xu ly** - Uu tien header chuan `"User-Agent"`, sau do fallback header khong chuan `"UserAgent"`
(it gap, co the do he thong noi bo/legacy client tu dat).

**Side effect** - Khong co.

**Error handling** - `try/catch` bao toan bo than ham, bat MOI `Exception` (khong khai bao kieu) va tra
`string.Empty` - khong log, khong phan biet loai loi (`httpContext == null` vs loi khac).

**Khi nao NEN dung** - Lay User-Agent de ghi audit log/thong ke, khong quan trong viec phan biet "khong co
header" voi "loi runtime".

**Khi nao KHONG dung** - Khi can biet chinh xac ly do tra ve rong (thieu header hay `httpContext` null).

**Gioi han** - Nuot toan bo exception ma khong log - neu `httpContext` null do loi logic o tang goi, ham
nay se am tham tra `""` thay vi bao loi ro. Dang duoc dung tai it nhat 3 file khac trong repo (grep
`.GetUserAgent(`).

### 2.8 GetClientIpAddress

**Signature**
```csharp
public static string GetClientIpAddress(HttpContext httpContext)
```
**Muc dich** - Lay dia chi IP cua client tu cac header proxy pho bien, theo thu tu uu tien.

**Input hop le** | `httpContext` (`HttpContext`, co the `null` - duoc kiem tra rieng, dong 55-58).

**Output** - `string`: gia tri IP (hoac chuoi "for=" da duoc xu ly) tu header dau tien khop; `string.Empty`
neu `httpContext == null`, hoac khong header nao trong 3 header duoc kiem tra co gia tri, hoac co
exception.

**Dieu kien xu ly (theo thu tu, dung header dau tien tim thay)**
1. `httpContext == null` -> `""` ngay (dong 55-58).
2. Header `"Forwarded"` (chuan RFC 7239) - tach theo `,`, tim phan tu bat dau bang `"for="` (khong phan
   biet hoa/thuong), cat bo tien to `for=` (4 ky tu) va trim ky tu `"`, `[`, `]` (dong 61-74). Neu header
   co nhung khong tim thay phan tu `for=` hop le -> **khong return, roi tiep xuong buoc 3** (khong co
   `else`, chi la `if` don, dong 70).
3. Header `"X-Forwarded-For"` - tach theo `,`, lay phan tu dau, trim (dong 77-80). Ket qua co the la
   `null` (`FirstOrDefault()` tren mang rong ly thuyet khong xay ra vi Split luon co it nhat 1 phan tu,
   nhung `?.Trim()` van duoc dung phong ho).
4. Header `"X-Real-IP"` - tra nguyen gia tri (dong 83-86).
5. Khong header nao co -> `string.Empty` (dong 89).

**Side effect** - Khong co.

**Error handling** - `try/catch` chung, bat moi `Exception`, tra `string.Empty`, khong log (dong 91-94).

**Khi nao NEN dung** - Ghi log/audit IP nguon, khong dung cho quyet dinh bao mat quan trong.

**Khi nao KHONG dung** - Cho muc dich bao mat/chan IP (rate-limit, whitelist) - vi gia tri hoan toan lay
tu header do CLIENT/PROXY tu khai bao, khong co buoc xac thuc proxy tin cay nao, de bi gia mao bang cach
tu them header `X-Forwarded-For` vao request.

**Gioi han** - (1) Khong validate dinh dang IP (khong kiem tra co phai IPv4/IPv6 hop le); (2) tin tuong
tuyet doi noi dung header tu client; (3) khong lay `RemoteIpAddress` thuc te cua connection TCP lam
fallback cuoi cung (comment dong 88 "4. Remote IP" nhung code thuc te CHỈ return `string.Empty`, khong he
doc `httpContext.Connection.RemoteIpAddress` - **mau thuan giua comment va code**, tin theo code: khong
co fallback Remote IP thuc su); (4) **loi bo sot moi phat hien khi doc lai source (dong 61-73)**: nhanh
xu ly header `"Forwarded"` chi `.Trim('"', '[', ']')` sau khi cat tien to `for=` - `Trim` chi xoa ky tu
o HAI DAU chuoi, khong xoa ky tu o giua. Voi dia chi IPv6 kem cong theo dung chuan RFC 7239 (vi du
`for="[2001:db8::1]:4711"`), sau `Substring(4)` con lai `"[2001:db8::1]:4711"`; `Trim` chi bo duoc dau `"`
va `[` o dau chuoi, KHONG bo duoc dau `]` o GIUA chuoi (truoc `:4711`) vi day khong con la ky tu o bien
- ket qua tra ve la chuoi loi `2001:db8::1]:4711` (con du `]` va dinh kem ca so cong), khong phai mot dia
chi IP sach. Chi truong hop IPv6 KHONG kem cong (vi du `for="[2001:db8::1]"`) moi duoc trim dung vi dau
`]` luc do nam sat bien cuoi chuoi. Day la gioi han rieng cua nhanh "Forwarded", khong anh huong 2 nhanh
`X-Forwarded-For`/`X-Real-IP` (2 nhanh do khong xu ly bracket/port).

### 2.9 MaskPhoneNumber

**Signature**
```csharp
public static string MaskPhoneNumber(string phoneNumber)
```
**Muc dich** - Che so dien thoai Viet Nam hop le bang ky tu `*`, chi giu lai 3 so cuoi.

**Input hop le** | `phoneNumber` (`string`) - phai khong rong/whitespace VA duoc
`VietnamesePhoneValidator.Validate` xac nhan `Valid == true` (dong 105-109).

**Output** - `string`: chuoi da che (vi du `0987654321` -> `*******321`) khi hop le; **tra nguyen
`phoneNumber` khong doi** (khong che) khi rong/whitespace hoac khong hop le theo validator (dong 105-109).

**Dieu kien xu ly**
1. `phoneNumber` rong/whitespace HOAC `VietnamesePhoneValidator.Validate(phoneNumber).Valid == false` ->
   tra nguyen `phoneNumber` (dong 105-109).
2. Nguoc lai -> lay 3 ky tu cuoi (`phoneNumber[^3..]`, dong 111), tao chuoi `*` co do dai
   `phoneNumber.Length - 3` (dong 113), noi lai (dong 115).

**Side effect** - Khong co.

**Error handling** - Khong co try/catch rieng trong ham nay; vi buoc validate da dam bao `phoneNumber` du
dai (>= 8 ky tu theo cac pattern trong `VietnamesePhoneValidator`), `phoneNumber[^3..]` khong co rui ro
`IndexOutOfRangeException` trong dieu kien binh thuong.

**Khi nao NEN dung** - Hien thi so dien thoai ra giao dien/log ma van can bao mat mot phan.

**Khi nao KHONG dung** - Voi chuoi khong phai so dien thoai VN (se bi tra nguyen, khong che) - **luu y**:
day KE THUA truc tiep han che cua `VietnamesePhoneValidator` (xem muc 3 - nhieu so di dong hop le van bi
validator gan nham loai "geographic", nhung vi `MaskPhoneNumber` chi doc co `Valid` (khong doc `Type`) nen
KHONG bi anh huong boi loi phan loai do).

**Gioi han** - Neu so dau vao hop le nhung chi co dung 3 ky tu can che (vi du gia dinh mot dinh dang toi
thieu), toan bo so se hien thi nguyen (masked rong) - trong thuc te khong xay ra vi do dai toi thieu cua
pattern hop le >= 8.

### 2.10 GetDescriptionInEnum<TEnum>

**Signature**
```csharp
public static string GetDescriptionInEnum<TEnum>(this TEnum value) where TEnum : struct, System.Enum
```
**Muc dich** - Lay noi dung attribute `[Description]` gan tren mot gia tri enum.

**Input hop le** | `value` (`TEnum`, rang buoc `struct, System.Enum`) - luon "hop le" ve kieu do constraint,
khong the null.

**Output** - `string`: noi dung `Description` neu field tuong ung co gan attribute; **ten enum member**
(`value.ToString()`) neu KHONG co attribute HOAC khong tim thay field (vi du gia tri enum khong ton tai
trong danh sach member - xem Gioi han); cung `value.ToString()` neu co exception.

**Dieu kien xu ly**
1. `value.GetType().GetField(value.ToString())` - lay field co ten dung bang chuoi bieu dien cua `value`
   (dong 129).
2. Neu tim duoc field va co attribute `DescriptionAttribute` -> tra `Description` (dong 131-133).
3. Neu khong tim duoc field (`field == null`, do `value` la enum khong dinh nghia, vi du ket qua
   `Enum.TryParse` voi so nguyen tuy y - xem `ConvertEnum` muc 2.11) hoac field khong co attribute -> tra
   `value.ToString()` (dong 133).

**Side effect** - Khong co.

**Error handling** - `try/catch` chung, bat moi `Exception`, tra `value.ToString()` (dong 135-138). Vi
cac buoc ben trong hau nhu khong throw voi enum hop le, nhanh catch hiem khi kich hoat trong thuc te.

**Khi nao NEN dung** - Hien thi mo ta than thien cho enum co gan `[Description]`.

**Khi nao KHONG dung** - Voi enum co `[Flags]` va gia tri la to hop nhieu co (combined value) - `ToString()`
tra chuoi ten cac co noi voi dau phay, `GetField` voi chuoi do se tra `null`, ket qua chi la chuoi
`ToString()` goc (khong loi nhung khong lay duoc Description cua tung co rieng).

**Gioi han** - Ket hop voi `ConvertEnum` (muc 2.11), mot gia tri enum "khong ton tai" (parse tu so nguyen
tuy y) se lam ham nay tra ve dung so nguyen do duoi dang chuoi ma khong co bao loi/canh bao ve viec gia
tri khong hop le.

### 2.11 ConvertEnum<TEnum>

**Signature**
```csharp
public static TEnum? ConvertEnum<TEnum>(this string value, bool ignoreCase = true) where TEnum : struct, System.Enum
```
**Muc dich** - Chuyen chuoi thanh gia tri enum nullable.

**Input hop le** | `value` (`string`, co the null/rong) | `ignoreCase` (`bool`, mac dinh `true`).

**Output** - `TEnum?`: gia tri enum neu `System.Enum.TryParse` thanh cong; `null` neu that bai (dong 151).

**Dieu kien xu ly** - Goi truc tiep `System.Enum.TryParse<TEnum>(value, ignoreCase, out result)`, tra
`result` neu `true`, `null` neu `false`. Khong co logic bo sung.

**Side effect** - Khong co.

**Error handling** - Khong co try/catch - tuy nhien `Enum.TryParse` cua .NET tu no khong throw (tra `bool`),
nen khong can bat exception.

**Khi nao NEN dung** - Parse chuoi (tu form/query string/config) sang enum khi ten member khop chinh xac
(hoac gan dung, khong phan biet hoa/thuong theo mac dinh).

**Khi nao KHONG dung** - Khi can dam bao gia tri tra ve la mot member THUC SU duoc dinh nghia trong enum -
`Enum.TryParse` cho phep parse **chuoi so nguyen bat ky** (vi du `"999"`) thanh cong ngay ca khi `999`
khong phai gia tri enum nao duoc dinh nghia, vi day la hanh vi goc cua .NET `Enum.TryParse` (khong phai
loi cua ham nay, nhung ham nay khong bo sung buoc kiem tra `Enum.IsDefined` de chan truong hop do).

**Gioi han** - Xem "Khi nao KHONG dung" - day la nguyen nhan co the tao ra gia tri enum "khong hop le ve
mat nghiep vu" troi qua he thong ma khong bi phat hien, dac biet khi ket hop voi `GetDescriptionInEnum`
(muc 2.10) o cac buoc xu ly sau. Duoc dung tai 2 file khac trong repo (grep `.ConvertEnum<`).

### 2.12 GetMinEnumValue<TEnum>

**Signature**
```csharp
public static TEnum GetMinEnumValue<TEnum>(params TEnum[] items)
```
**Muc dich** - Lay gia tri nho nhat (theo `int`) trong mot danh sach gia tri enum.

**Input hop le** | `items` (`params TEnum[]`) - **KHONG co rang buoc generic** `where TEnum : Enum` (khac
voi cac ham enum khac trong file nay dung `struct, System.Enum`) - ve mat ky thuat co the goi voi `TEnum`
khong phai enum, se throw runtime khi `Convert.ToInt32` khong ho tro kieu do.

**Output** - `TEnum`: gia tri co `Convert.ToInt32(e)` nho nhat trong `items`.

**Dieu kien xu ly** - `items.OrderBy(e => Convert.ToInt32(e)).First()` (dong 163) - sap xep tang dan roi
lay phan tu dau.

**Side effect** - Khong co.

**Error handling** - **KHONG co try/catch nao** - day la ham DUY NHAT trong nhom xu ly enum/const cua file
nay khong nuot loi. Neu `items` rong (`GetMinEnumValue<TEnum>()` khong truyen phan tu nao) -> `.First()`
throw `InvalidOperationException` khong duoc bat, lan truyen ra ngoai. Neu `TEnum` khong convert duoc
sang `int` (vi du enum voi kieu nen `ulong` vuot pham vi `int`, hoac `TEnum` khong phai enum) ->
`Convert.ToInt32` co the throw `OverflowException`/`InvalidCastException`, cung khong bat.

**Khi nao NEN dung** - Khi CHAC CHAN `items` co it nhat 1 phan tu va `TEnum` la enum voi gia tri nam trong
pham vi `int`.

**Khi nao KHONG dung** - Khi `items` co the rong (se throw), hoac khi enum dung kieu nen lon (`long`,
`ulong`) co gia tri vuot `int.MaxValue`/`int.MinValue`.

**Gioi han** - Thieu constraint generic + thieu kiem tra rong + khong try/catch - khong dong nhat voi
phong cach "nuot loi, tra gia tri an toan" cua toan bo cac ham con lai trong `ConvertHelpers`. Khong tim
thay noi goi ham nay trong repo hien tai (grep `.GetMinEnumValue(` khong co ket qua) - **khong xac dinh
duoc muc do su dung thuc te tu source code**.

### 2.13 ConvertHttpStatusCodeCodeByName

**Signature**
```csharp
public static string ConvertHttpStatusCodeCodeByName(this int httpStatusCode)
```
**Muc dich** - Doi ma so HTTP status code sang ten enum `HttpStatusCode` tuong ung.

**Input hop le** | `httpStatusCode` (`int`) - khong rang buoc gia tri, nhan bat ky so nguyen.

**Output** - `string`: ten enum (vi du `"OK"`, `"NotFound"`) neu `httpStatusCode` khop mot gia tri dinh
nghia trong `System.Net.HttpStatusCode`; `string.Empty` neu khong khop (dong 177, `?? string.Empty`).

**Dieu kien xu ly** - Goi truc tiep `System.Enum.GetName(typeof(HttpStatusCode), httpStatusCode)`.

**Side effect** - Khong co.

**Error handling** - Khong co try/catch - `Enum.GetName` khong throw voi input hop le kieu `int` (chi
throw neu kieu enum truyen vao sai, khong ap dung o day vi kieu co dinh `HttpStatusCode`).

**Khi nao NEN dung** - Hien thi/log ten status code de de doc hon so voi chi hien so.

**Khi nao KHONG dung** - Khong co han che dang ke; ham don gian, an toan.

**Gioi han** - Chi cac gia tri co trong enum `HttpStatusCode` cua .NET moi tra duoc ten (vi du cac status
code tuy bien/it dung khong co trong enum se tra `""`).

### 2.14 UnsignViet

**Signature**
```csharp
public static string UnsignViet(string text)
```
**Muc dich** - Loai bo dau tieng Viet (dau thanh/dau nguyen am) khoi chuoi bang Regex + Unicode
Normalization.

**Input hop le** | `text` (`string`, co the null/rong - duoc kiem tra dau ham).

**Output** - `string`: chuoi da bo dau; `string.Empty` neu `text` rong/whitespace (dong 188-191, LUU Y:
tra `string.Empty` ke ca khi input la `null`, khac voi `RemoveDiacritics` tra nguyen `text` - xem muc 3).

**Dieu kien xu ly**
1. `text` rong/whitespace -> `string.Empty` (dong 188-191).
2. Chuan hoa `text` ve `NormalizationForm.FormD` (tach ky tu co dau thanh ky tu goc + dau ket hop).
3. Dung `Regex(@"\p{M}")` (khop moi ky tu thuoc Unicode category "Mark") de xoa cac dau ket hop.
4. Chuan hoa lai ve `NormalizationForm.FormC` (dong 193-195).

**Side effect** - Khong co.

**Error handling** - Khong co try/catch - `Regex`/`Normalize` voi chuoi hop le khong throw trong dieu kien
thong thuong.

**Khi nao NEN dung** - Bo dau nhanh cho chuoi CHI chua nguyen am co dau kieu Latin thong thuong (a, e, i,
o, u, y voi cac dau thanh).

**Khi nao KHONG dung** - **Voi chuoi chua ky tu "Đ"/"đ"** - xem Gioi han, day la loi da kiem chung.

**Gioi han** - **Đa kiem chung bang cach chuan hoa Unicode thuc te**: ky tu "Đ" (U+0110, LATIN CAPITAL
LETTER D WITH STROKE) va "đ" (U+0111) KHONG duoc Unicode dinh nghia la mot ky tu co dau ket hop (combining
mark) ma la MOT KY TU DOC LAP (giong "Ø") - do do `Normalize(FormD)` KHONG tach duoc "gach ngang" cua no
thanh mot combining mark de `Regex(@"\p{M}")` xoa. Vi du thuc te: `UnsignViet("Đà Nẵng")` cho ket qua
**`"Đa Nang"`** (giu nguyen "Đ", chi bo dau cua "à" va "ẵ"), KHONG PHAI `"Da Nang"` nhu ky vong thong
thuong khi "bo dau tieng Viet". Day la khac biet ro rang so voi `RemoveDiacritics` (muc 2.15), ham co xu
ly rieng cho "Đ"/"đ" truoc khi normalize (ConvertHelpers.cs:212). Repo hien co ca 2 ham lam "cung mot
viec" nhung cho 2 ket qua khac nhau tren du lieu thuc te tieng Viet - rui ro dung nham ham trong cac ngu
canh can ket qua dong nhat (vi du tao slug/URL, so sanh khong dau).

### 2.15 RemoveDiacritics

**Signature**
```csharp
public static string RemoveDiacritics(string text)
```
**Muc dich** - Loai bo dau tieng Viet, xu ly rieng truong hop "Đ"/"đ" (ky tu tieng Viet khong the bo dau
bang normalize thong thuong - xem muc 2.14).

**Input hop le** | `text` (`string`, co the null/rong).

**Output** - `string`: chuoi da bo dau; **tra nguyen `text`** (co the la `null`) neu rong/whitespace
(dong 206-209 - KHAC voi `UnsignViet` tra `string.Empty`, xem muc 3).

**Dieu kien xu ly**
1. `text` rong/whitespace -> tra nguyen `text` (dong 206-209).
2. Thay the thu cong `"Đ"` -> `"D"`, `"đ"` -> `"d"` (dong 212).
3. Chuan hoa `NormalizationForm.FormD`.
4. Duyet tung ky tu, giu lai ky tu KHONG thuoc `UnicodeCategory.NonSpacingMark` (dong 217-224) - cach
   lam tuong duong `UnsignViet` nhung tu viet vong lap thay vi dung `Regex`.
5. Chuan hoa lai `NormalizationForm.FormC`.

**Side effect** - Khong co.

**Error handling** - Khong co try/catch.

**Khi nao NEN dung** - Can ket qua bo dau tieng Viet DAY DU va DUNG (bao gom ca "Đ"/"đ") - vi du tao slug
URL, ten file khong dau, so sanh tim kiem khong phan biet dau.

**Khi nao KHONG dung** - Khong co han che dang ke doi voi tieng Viet chuan.

**Gioi han** - Neu `text` la `null`, ham tra ve `null` (khong phai `string.Empty`) - caller phai tu kiem
tra null truoc khi dung tiep chuoi ket qua (vi du goi `.ToUpper()` tren ket qua se `NullReferenceException`
neu input goc la `null`).

### 2.16 CapitalizeFirstLetter

**Signature**
```csharp
public static string CapitalizeFirstLetter(string input)
```
**Muc dich** - Viet hoa ky tu dau tien cua chuoi, giu nguyen phan con lai.

**Input hop le** | `input` (`string`, co the null/rong).

**Output** - `string`: chuoi voi ky tu dau viet hoa; `string.Empty` neu `input` null/rong (dong 237).

**Dieu kien xu ly** - `char.ToUpper(input[0]) + input[1..]` (dong 239, dung `input[default]` tuong duong
`input[0]`).

**Side effect** - Khong co. Duoc goi noi bo boi `NumberToVietnameseWords` (muc 2.20, dong 449) de viet
hoa ket qua cuoi.

**Error handling** - Khong co try/catch; an toan vi da kiem tra rong truoc.

**Khi nao NEN dung** - Chuan hoa hien thi ten rieng/cau don gian.

**Khi nao KHONG dung** - Khong ho tro Unicode surrogate pair dac biet (it xay ra voi tieng Viet/Latin).

**Gioi han** - Dung `char.ToUpper(char)` khong chi dinh `CultureInfo` - phu thuoc culture hien tai cua
thread thuc thi (mac dinh khong anh huong voi chu Latin/Viet thong thuong).

### 2.17 ConvertClaimsPrincipalToData

**Signature**
```csharp
public static string ConvertClaimsPrincipalToData(
    ClaimsPrincipal claimsPrincipal, string claimType, string setDataDefault = "")
```
**Muc dich** - Lay gia tri (da trim) cua mot claim cu the tu `ClaimsPrincipal`.

**Input hop le** | `claimsPrincipal` (co the null) | `claimType` (co the null/rong) | `setDataDefault`
(mac dinh `""`).

**Output** - `string`: gia tri claim da `.Trim()` neu tim thay; `setDataDefault` neu khong tim thay/loi.

**Dieu kien xu ly**
1. `claimsPrincipal is null && string.IsNullOrWhiteSpace(claimType)` (dong 255, dung `&&` - CA HAI dieu
   kien phai dung moi return som) -> tra `setDataDefault`.
2. Truong hop con lai (bao gom ca "chi mot trong hai dieu kien dung"): `claimsPrincipal?.FindFirst(claimType)?.Value?.ToString().Trim() ?? setDataDefault`
   (dong 260) - chuoi `?.` dam bao an toan ngay ca khi `claimsPrincipal` null (buoc 1 khong bat duoc
   truong hop nay neu `claimType` khac rong).

**Side effect** - Khong co.

**Error handling** - `try/catch` chung, bat moi `Exception`, tra `setDataDefault` (dong 262-265).

**Khi nao NEN dung** - Lay claim tu `HttpContext.User` trong middleware/controller khi chap nhan gia tri
mac dinh neu khong co claim.

**Khi nao KHONG dung** - Khong co han che dang ke.

**Gioi han** - Dieu kien guard o buoc 1 dung `&&` chu khong phai `||` - doc code de hieu nham la "chi can
1 trong 2 tham so invalid la return som", nhung thuc te CHỈ return som khi CA HAI cung invalid. Tuy
nhien hanh vi cuoi cung KHONG bi sai vi buoc 2 da dung toan `?.` (null-conditional) de tu bao ve truoc
`claimsPrincipal == null` - **day la mot bat thuong ve code style/doc-dung-y hon la mot bug chuc nang**.
Dung tai 2 file khac trong repo (grep `.ConvertClaimsPrincipalToData(`).

### 2.18 DescriptionForProperty

**Signature**
```csharp
public static string DescriptionForProperty(Type containingClass, object member)
```
**Muc dich** - Lay `Description` (qua attribute `[Description]`) cua mot field const, tim field do bang
cach so sanh GIA TRI cua no voi `member`.

**Input hop le** | `containingClass` (`Type`, vi du `typeof(ConsCodeDetail.Source)`) | `member` (`object`,
vi du chinh gia tri const, thuong truyen qua `nameof(...)` theo doc XML nhung thuc te logic so sanh dung
GIA TRI chu khong dung TEN - xem duoi).

**Output** - `string`: noi dung `Description` cua field neu tim thay VA field co gan attribute; **chuoi
rong** trong TAT CA cac truong hop con lai, bao gom ca khi tim duoc field nhung field KHONG co
`[Description]` (xem Gioi han - day la bug).

**Dieu kien xu ly**
1. `containingClass is null || member is null` -> `""` (dong 280-283).
2. Goi ham local `HaveValueInClassConstants(containingClass, member)`: lap qua toan bo field
   `public static` cua `containingClass`, so sanh `field.GetValue(null).ToString() == value.ToString()`
   (dong 329) - **so sanh theo GIA TRI cua field, khong phai theo TEN** (mau thuan voi vi du trong XML
   doc `/// EX: nameof(ConsCodeDetail.Source.SR)` goi y dung ten, nhung logic thuc te can `member` la
   GIA TRI (hoac chuoi trung khop gia tri) de tim dung field - neu truyen dung `nameof(...)` (la chuoi ten
   field, vi du `"SR"`) thi so sanh se la `"102".ToString() == "SR"` -> khong bao gio khop, tra `null`
   luon, tuc **vi du trong XML doc (dong 272-273) neu ap dung dung nhu mo ta se KHONG hoat dong** - xem
   muc 3).
3. Neu `nameProperty` (ten field tim duoc) la `null` -> `""` (dong 287-290).
4. `propertyInfo = containingClass.GetProperty(nameProperty)` (dong 292) - **tim PROPERTY co ten
   `nameProperty`**, trong khi `nameProperty` thuc te la ten mot FIELD const - voi cac lop hang so
   (`public const int X = ...`) thong thuong KHONG co property cung ten, nen `propertyInfo` hau het la
   `null`.
5. `if (string.IsNullOrWhiteSpace(nameProperty))` (dong 294) - **nhanh nay hau nhu khong bao gio dung**
   vi buoc 3 da loai truong hop `nameProperty == null`, va ten field hop le khong the la whitespace -
   code chet (dead code) trong thuc te.
6. `fieldInfo = containingClass.GetField(nameProperty)` (dong 299) - lay dung field const.
7. Neu `fieldInfo` khong null va co `[Description]` -> tra `Description` (dong 301-309).
8. **Nguoc lai** (fieldInfo null HOAC khong co `[Description]`) -> `return propertyInfo?.Name ?? string.Empty`
   (dong 312) - do `propertyInfo` hau het la `null` (buoc 4), nhanh nay hau nhu LUON tra `string.Empty`
   thay vi tra ten field (`fieldInfo.Name`/`nameProperty`) nhu ky vong hop ly.

**Side effect** - Khong co.

**Error handling** - `try/catch` bao toan bo than ham chinh (khong bao gom ham local), tra `""` khi loi
(dong 314-317). Ham local `HaveValueInClassConstants` co try/catch RIENG, tra `null` khi loi (dong
337-340).

**Khi nao NEN dung** - Lay `Description` cua field const khi field CHAC CHAN co gan `[Description]` va
tham so `member` truyen vao la GIA TRI cua field (khong phai ten).

**Khi nao KHONG dung** - Khi mong doi ham tra ve TEN field lam fallback neu field khong co
`[Description]` - hanh vi thuc te la tra chuoi rong (xem muc 3).

**Gioi han** - Xem chi tiet buoc 2 va buoc 8 - day la mot ham co logic reflection dat sai cho
(`GetProperty` thay vi tai su dung `nameProperty`/`fieldInfo.Name`), dan den fallback luon la chuoi rong
thay vi ten field. **Da kiem tra lai bang grep toan repo (`DescriptionForProperty`, khong chi
`.DescriptionForProperty(`)**: KHONG tim thay noi nao khac trong code (`.cs`) hoac tai lieu KB nao khac
goi ham nay ngoai chinh noi dinh nghia (`ConvertHelpers.cs:276`) - ban truoc ghi "dung tai 1 file khac
trong repo" la SAI, thuc te khong xac dinh duoc muc do su dung thuc te tu source code (tuong tu truong
hop `GetMinEnumValue`, muc 2.12).

### 2.19 ConvertStringToDateTime

**Signature**
```csharp
public static DateTime? ConvertStringToDateTime(this string dateInput,
    string format = null,
    IFormatProvider provider = null,
    DateTimeStyles style = DateTimeStyles.RoundtripKind,
    bool fallbackToGeneralParse = true)
```
**Muc dich** - Chuyen chuoi sang `DateTime?`, thu theo `format` cu the (neu co), hoac theo danh sach
`AllDateFormats` (**13** format co san - da dem lai tung phan tu tu source, KHONG PHAI 14 nhu ban truoc
ghi nham, dong 346-370), roi fallback `DateTime.TryParse` tong quat neu duoc phep.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `dateInput` | `string` | Co (extension `this`) | Rong/whitespace -> tra `null` ngay (dong 386-389) | khong co |
| `format` | `string` | Khong | Neu co gia tri (khong rong/whitespace), CHỈ dung `TryParseExact` voi dung format nay, khong thu `AllDateFormats` | `null` |
| `provider` | `IFormatProvider` | Khong | `??= CultureInfo.InvariantCulture` neu null (dong 391) | `null` |
| `style` | `DateTimeStyles` | Khong | Khong validate | `DateTimeStyles.RoundtripKind` |
| `fallbackToGeneralParse` | `bool` | Khong | Quyet dinh co goi `DateTime.TryParse` tong quat khi cac buoc truoc that bai | `true` |

**Output** - `DateTime?`: gia tri parse duoc tu buoc dau tien thanh cong; `null` neu `dateInput` rong,
hoac tat ca buoc parse deu that bai (bao gom ca khi `fallbackToGeneralParse = false` va cac buoc format
truoc do khong khop).

**Dieu kien xu ly**
1. `dateInput` rong/whitespace -> `null` (dong 386-389).
2. Neu `format` duoc cung cap (khong rong) -> CHỈ thu `DateTime.TryParseExact(dateInput, format, ...)`;
   neu thanh cong tra ket qua (dong 393-399); **neu that bai, KHONG rot xuong thu `AllDateFormats`** (vi
   day la nhanh `if`, khong phai `if/else if` noi voi buoc 3) - chi con duong fallback la buoc 4.
3. Neu `format` KHONG duoc cung cap -> thu `DateTime.TryParseExact(dateInput, AllDateFormats, ...)` (mang
   13 format, dong 400-403); neu thanh cong tra ket qua.
4. Neu (2) hoac (3) khong thanh cong VA `fallbackToGeneralParse == true` -> thu `DateTime.TryParse(dateInput, provider, style, ...)` (dong 405-409); neu thanh cong tra ket qua.
5. Tat ca deu that bai -> `null` (dong 411).

**Side effect** - Khong co.

**Error handling** - Khong dung try/catch - toan bo dua vao cac ham `TryParseExact`/`TryParse` cua .NET
(khong throw, tra `bool`), nen ham nay tu nhien "an toan" ma khong can nuot exception.

**Khi nao NEN dung** - Parse ngay tu du lieu dau vao khong chuan hoa (form nguoi dung, file import) khi
muon uu tien mot format ro rang nhung van co fallback.

**Khi nao KHONG dung** - Khi truyen `format` CU THE nhung khong chac chuoi dau vao dung dinh dang do -
ham se KHONG tu dong thu cac format khac trong `AllDateFormats`, chi con fallback tong quat (co the cho
ket qua khac mong doi hoac `null`).

**Gioi han** - (1) Nhu muc (2) tren - hanh vi "co `format` thi bo qua `AllDateFormats`" co the gay bat
ngo; (2) `AllDateFormats` la danh sach CO DINH 13 format (khong the tuy bien them tru sua source); (3)
gia tri mac dinh `DateTimeStyles.RoundtripKind` ket hop voi format khong co ky hieu `K`/offset co thanh
phan Kind khong ro rang trong vai truong hop bien - **khong xac dinh duoc anh huong cu the tu source code
neu khong test runtime**.

### 2.20 NumberToVietnameseWords

**Signature**
```csharp
public static string NumberToVietnameseWords(decimal number, bool useThousand = true)
```
**Muc dich** - Doi mot so (`decimal`) sang chuoi doc bang chu tieng Viet (vi du `1234000` ->
`"Một triệu hai trăm ba mươi tư nghìn đồng"`), ho tro don vi "nghìn" hoac "ngàn" theo `useThousand`.

**Input hop le** | `number` (`decimal`, KHONG rang buoc khong am - xem Gioi han) | `useThousand` (`bool`,
mac dinh `true`, chon tu "nghìn" hay "ngàn").

**Output** - `string`: cau doc so bang chu, viet hoa ky tu dau, ket thuc bang `" đồng"`; `"Không đồng"`
neu `number == 0` (dong 423). **Voi so am**: tra ve chuoi `" đồng"` (co khoang trang dau, KHONG co so nao
duoc doc, khong bao loi - xem muc 3). **Voi so >= 10^12 (1,000 ty)**: throw
`IndexOutOfRangeException` KHONG duoc bat (xem muc 3).

**Dieu kien xu ly**
1. `number == default` (dung 0) -> `"Không đồng"` ngay (dong 423).
2. `units = ["", "nghìn"/"ngàn", "triệu", "tỷ"]` (4 phan tu, dong 425); `digits` la 10 chu so tieng Viet
   (dong 427).
3. Lap `while (number > 0)`: tach nhom 3 so cuoi (`groupOfThree = (int)(number % 1000)`), chia `number`
   cho 1000 (`number /= 1000`), tang `unitIndex` moi vong; **VONG LAP CHỈ DUNG khi `number <= 0`** - voi
   so am, dieu kien `number > 0` sai NGAY TU DAU nen vong lap khong chay lan nao (dong 433-447).
4. Neu `groupOfThree > 0`, chuyen nhom thanh chu qua ham local `ConvertGroupToWords`, ghep voi don vi
   tuong ung `units[unitIndex]` (dong 439-444) - **`unitIndex` co the vuot qua chi so hop le (0-3) cua
   `units`** neu `number` ban dau >= 10^12, gay `IndexOutOfRangeException` tai chinh dong nay khi truy
   cap `units[unitIndex]` voi `unitIndex >= 4`.
5. Sau vong lap, tra `CapitalizeFirstLetter(result.Trim() + " đồng")` (dong 449).

**Side effect** - Khong co. Goi noi bo `CapitalizeFirstLetter` (muc 2.16).

**Error handling** - **KHONG co try/catch nao trong toan bo ham** - day la ham xu ly so hoc/chuoi PHUC
TAP NHAT trong `ConvertHelpers` nhung lai la mot trong so it ham KHONG nuot loi, khac voi phan lon ham
con lai trong file. Loi `IndexOutOfRangeException` (so qua lon) se lan truyen thang ra ngoai, khong duoc
log, khong duoc bat.

**Khi nao NEN dung** - Doc so tien tren hoa don/chung tu voi gia tri nam trong pham vi tu 0 den duoi
1,000 ty (10^12 - 1), khong am, khong co phan thap phan (le/xu) can giu lai.

**Khi nao KHONG dung** - (1) Voi so am - se cho ket qua vo nghia (`" đồng"` khong co so); (2) voi so >=
1,000 ty - se lam crash chuong trinh (exception khong bat); (3) voi so co phan thap phan (`decimal` co
`.xx`) - phan thap phan bi bo qua am tham do `(int)(number % 1000)` chi lay phan nguyen cua phep chia du
(vi du `1234.56m % 1000` = `234.56m`, ep `(int)` -> `234`, mat `.56`).

**Gioi han** - Ba diem neu tren (so am, vuot 10^12, mat phan thap phan) deu la loi/han che THUC SU cua
logic, khong phai gia dinh - da doi chieu tung buoc voi source code (dong 421-502). Day la ham co RUI RO
CAO NHAT trong ca hai file de gay loi production (crash khong bat, hoac sinh du lieu hien thi sai ma
khong co dau hieu loi nao) neu duoc dung voi input khong duoc validate truoc (vi du gia tri tien am do
loi nghiep vu khac, hoac gia tri cuc lon do loi don vi tinh).

### 2.21 VietnamesePhoneValidator (nested static class)

**Signature**
```csharp
public static class VietnamesePhoneValidator
{
    public record ValidationResult(bool Valid, string Type, string Carrier, string Error = null);
    public static ValidationResult Validate(string number) { ... }
    private static string IdentifyCarrier(string number) { ... }
}
```
**Muc dich** - Validate mot chuoi la so dien thoai Viet Nam hop le, phan loai theo 4 nhom (`geographic`,
`mobile`, `toll-free`, `premium`) va (voi so di dong) doan nha mang.

**Input hop le** | `number` (`string`, tham so cua `Validate`) - KHONG kiem tra null/rong truoc khi goi
`Regex.Replace(number, @"\D", "")` (dong 539) - neu `number` la `null`, ham se throw
`ArgumentNullException`/`NullReferenceException` NGAY, KHONG duoc bat (khong co try/catch trong
`Validate`).

**Output** - `ValidationResult` (record voi 4 thuoc tinh):
- `Valid` (`bool`): `true` neu khop MOT TRONG 4 pattern (theo thu tu kiem tra: `GeoPattern`, sau do
  `MobilePattern`, `TollFreePattern`, `PremiumPattern`); `false` neu khong khop pattern nao.
- `Type`: `"geographic"` | `"mobile"` | `"toll-free"` | `"premium"` | `null` (khi khong hop le).
- `Carrier`: ten nha mang (`"Viettel"`, `"MobiFone"`, `"Vinaphone"`, `"Vietnamobile"`, `"Gmobile"`, hoac
  `"Unknown"` neu prefix khong khop pattern nao trong `IdentifyCarrier`) CHỈ duoc gan khi `Type ==
  "mobile"`; cac truong hop `Type` khac `Carrier` la `null`.
- `Error`: `"Số điện thoại không tồn tại"` khi khong hop le (dong 561); `null` khi hop le.

**Dieu kien xu ly**
1. Loai bo moi ky tu khong phai so khoi `number` (`Regex.Replace(number, @"\D", "")`, dong 539) - vi du
   `"+84 98 765 4321"` -> `"84987654321"` (theo doc comment dong 538, **giu lai ma quoc gia `84` nhu mot
   phan cua chuoi so**, KHONG tu dong doi `84` thanh `0` dau tien - nghia la so nhap dang quoc te `+84...`
   se KHONG khop bat ky pattern nao trong 4 pattern (tat ca pattern deu yeu cau bat dau bang `0`), do do
   se bi bao khong hop le mac du la so thuc te ton tai).
2. Kiem tra `GeoPattern.IsMatch(cleaned)` **TRUOC TIEN** -> neu khop, tra `(true, "geographic", null)`
   NGAY, khong kiem tra cac pattern con lai (dong 541-544).
3. Neu khong khop Geo, kiem tra `MobilePattern` -> neu khop, tra `(true, "mobile", IdentifyCarrier(cleaned))`
   (dong 546-549).
4. Neu khong, kiem tra `TollFreePattern` -> `(true, "toll-free", null)` (dong 551-554).
5. Neu khong, kiem tra `PremiumPattern` -> `(true, "premium", null)` (dong 556-559).
6. Khong khop pattern nao -> `(false, null, null, "Số điện thoại không tồn tại")` (dong 561).

**Side effect** - Khong co.

**Error handling** - **KHONG co try/catch** trong `Validate` hay `IdentifyCarrier` - khac voi da so ham
khac trong file. Input `null` se throw ngay tu `Regex.Replace`.

**Khi nao NEN dung** - Kiem tra nhanh mot chuoi CO PHAI dinh dang so dien thoai VN hop le (chi dung gia
tri `Valid`) - day la cach `MaskPhoneNumber` (muc 2.9) su dung, va KHONG bi anh huong boi loi phan loai
`Type` neu duoi day.

**Khi nao KHONG dung** - **Khi can phan biet dung loai so `"geographic"` vs `"mobile"`** - xem Gioi han,
day la loi nghiem trong da duoc kiem chung.

**Gioi han - DA KIEM CHUNG BANG REGEX THUC TE (khong suy dien)**: da chay lai 2 pattern
`GeoPattern = ^0([2-9]\d{1,2})\d{7,8}$` (ConvertHelpers.cs:517) va
`MobilePattern = ^0(3[2-9]|5[2689]|7[06-9]|8[1-9]|9[0-4689])\d{7}$` (ConvertHelpers.cs:520) tren cac so
di dong 10 chu so hop le thuc te (`0987654321`, `0912345678`, `0321234567`, `0561234567`, `0771234567`,
`0865432112`): **CA 6 SO deu khop `GeoPattern` (`true`)**, dong nghia vi `GeoPattern` duoc kiem tra TRUOC
`MobilePattern` trong `Validate` (dong 541 truoc dong 546), **toan bo 6 so di dong hop le tren se bi
`Validate` tra ve `Type = "geographic"`, `Carrier = null` thay vi `Type = "mobile"` va ten nha mang dung**.
Rieng so `0971234567` (dau so 097, Viettel) con khop `GeoPattern` (`true`) nhung KHONG khop `MobilePattern`
(`false`, vi tap ky tu `9[0-4689]` trong `MobilePattern` KHONG chua so `7` - thieu dau so `097` dang duoc
telco cap that) - nghia la ca khi `GeoPattern` duoc bo qua, `097xxxxxxx` van khong bao gio duoc nhan dien
la `"mobile"` boi pattern hien tai. Ket luan: co che phan loai `Type`/`Carrier` cua `VietnamesePhoneValidator`
**khong hoat dong dung nhu thiet ke voi phan lon so di dong thuc te** - chi `Valid` (true/false) la dang
tin cay o muc chap nhan duoc trong da so truong hop kiem thu; `Type`/`Carrier` (va do do, ca `IdentifyCarrier`)
gan nhu khong bao gio tra ket qua "mobile" dung nhu mong doi.

## 3. Van de da biet

| # | Van de | Vi tri | Anh huong |
|---|---|---|---|
| 1 | `JSonTryParse<T>(this BsonDocument, out T, ILogger)` goi `json.JSonTryParse(out result)` nhung BO QUA gia tri `bool` tra ve, roi luon `return true` | JSonParseHelpers.cs:105-107 | Khi buoc deserialize JSON->T that bai (sai kieu/cau truc), ham van bao "thanh cong" (`true`) voi `result` la `default` (co the `null`) - dung dung kieu loi "du lieu bi mat am tham" ma yeu cau tai lieu dac biet luu y |
| 2 | `LongConverter`/`LongNullableConverter.Read` khi fallback tu `TryGetDouble` lai ep kieu **`(int)`** thay vi `(long)` truoc khi tra ve gia tri `long`/`long?` | JSonParseHelpers.cs:393, 438 | So dang JSON `Number` vuot pham vi `int` (nhung van trong pham vi `long`) bi cat cut sai gia tri khi phai fallback qua `double` (truong hop `TryGetInt64` that bai) |
| 3 | `ToJSon<T>` xay dung message log bang `Newtonsoft.Json.JsonConvert.SerializeObject(obj)` NGAY TRONG catch-block, khong tu bao ve - neu `Newtonsoft` cung khong serialize duoc `obj`, exception moi nay KHONG duoc bat, thoat khoi try/catch chinh cua ham | JSonParseHelpers.cs:37-40 | Mot ham duoc thiet ke de "khong bao gio throw" (theo XML doc va logic try/catch) van co the throw trong truong hop hiem, gay crash ngoai y muon tai noi goi |
| 4 | `_dateTimeFormats` chua chuoi format `"yyyy-MM-ddTHH:mm:ss"` **trung lap 2 lan** | JSonParseHelpers.cs:241, 275 | Khong anh huong logic (chi du thua), nhung la dau hieu du lieu hardcode chua duoc ra soat |
| 5 | `VietnamesePhoneValidator.Validate`: pattern `GeoPattern` duoc kiem tra TRUOC `MobilePattern` va tren thuc te khop voi hau het so di dong 10 chu so hop le -> phan lon so di dong bi phan loai nham `Type = "geographic"`, `Carrier = null` | ConvertHelpers.cs:517, 520, 541-549 | Da kiem chung bang regex thuc te (xem muc 2.21) tren 6 so di dong mau - toan bo deu bi gan nham loai. Bat ky code nao dua vao `ValidationResult.Type`/`Carrier` de phan biet mobile/geographic hoac hien thi nha mang se cho ket qua sai co he thong |
| 6 | `MobilePattern` (ConvertHelpers.cs:520) dung tap ky tu `9[0-4689]` cho dau so bat dau bang 9, KHONG chua so `7` va `5` -> dau so thuc te `097` (Viettel) khong bao gio khop `MobilePattern` | ConvertHelpers.cs:520 (doi chieu voi `IdentifyCarrier` dong 570 co logic cho `09[6-8]` bao gom ca 7) | Ke ca khi sua duoc van de #5 (doi thu tu kiem tra), dau so `097` van se khong duoc `Validate` nhan la "mobile" do quy dinh dau so trong `MobilePattern` chua day du/loi thoi so voi `IdentifyCarrier` |
| 7 | `NumberToVietnameseWords`: voi so am, dieu kien `while (number > 0)` khong chay lan nao, tra ve chuoi vo nghia `" đồng"` (khong co so nao duoc doc, khong bao loi) | ConvertHelpers.cs:421-449 (dac biet dong 433) | Goi ham voi gia tri am (vi du do loi nghiep vu tinh toan truoc do) se sinh chuoi hien thi sai ma khong co bat ky canh bao/exception nao |
| 8 | `NumberToVietnameseWords`: `unitIndex` co the vuot chi so hop le (0-3) cua mang `units` (4 phan tu: "", nghìn/ngàn, triệu, tỷ) khi `number` dau vao >= 10^12 (1.000 ty), gay `IndexOutOfRangeException` **khong duoc bat** (ham khong co try/catch) | ConvertHelpers.cs:425, 439-444 | Crash khong kiem soat neu dau vao vuot nguong ma khong duoc validate truoc; day la ham DUY NHAT xu ly logic phuc tap nhung lai khong nuot loi nhu phan con lai cua file |
| 9 | `UnsignViet` KHONG bo dau duoc ky tu "Đ"/"đ" (do "Đ" la ky tu Unicode doc lap, khong phai to hop base+combining-mark) trong khi `RemoveDiacritics` co xu ly rieng cho 2 ky tu nay | ConvertHelpers.cs:186-196 (so voi 204-227, dac biet dong 212) | Da kiem chung: `UnsignViet("Đà Nẵng")` = `"Đa Nang"` (sai, con dau cua "Đ"); `RemoveDiacritics("Đà Nẵng")` = `"Da Nang"` (dung). Hai ham "cung chuc nang" cho 2 ket qua khac nhau tren du lieu tieng Viet thuc te - rui ro dung nham ham |
| 10 | `UnsignViet` tra `string.Empty` khi input null/rong; `RemoveDiacritics` tra NGUYEN input (co the la `null`) trong cung tinh huong | ConvertHelpers.cs:188-191 (so voi 206-209) | Khong dong nhat hanh vi giua 2 ham "tuong tu nhau" - code goi sau `RemoveDiacritics(null)` co the nhan `null` va `NullReferenceException` o buoc xu ly tiep theo neu khong kiem tra |
| 11 | `DescriptionForProperty`: buoc tim field dung SO SANH GIA TRI (`field.GetValue(null).ToString() == member.ToString()`), khong dung TEN nhu vi du trong XML doc goi y dung `nameof(...)` | ConvertHelpers.cs:272-273 (XML doc) doi chieu voi dong 285, 321-341 | Neu goi dung theo vi du trong XML doc (`nameof(ConsCodeDetail.Source.SR)` - la CHUOI TEN field, vi du `"SR"`), buoc so sanh gia tri se KHONG khop (vi gia tri thuc su cua field, vi du `102`, khac voi chuoi `"SR"`), dan den ham luon tra `string.Empty` cho truong hop dung theo huong dan trong doc |
| 12 | `DescriptionForProperty`: khi tim duoc `fieldInfo` nhung field KHONG co `[Description]`, ham tra `propertyInfo?.Name ?? string.Empty` - trong khi `propertyInfo` duoc lay qua `GetProperty(nameProperty)` (tim PROPERTY, khong phai FIELD) nen hau het la `null` voi field const | ConvertHelpers.cs:292, 312 | Fallback ky vong hop ly la tra ve TEN field (`nameProperty`/`fieldInfo.Name`) nhung code thuc te tra chuoi RONG trong hau het truong hop - mat thong tin ten field ma khong co dau hieu loi |
| 13 | `ConvertClaimsPrincipalToData`: dieu kien guard dong 255 dung `&&` (ca hai dieu kien phai dung) thay vi `||` nhu ten bien/y do "kiem tra dau vao invalid" thuong ngu y | ConvertHelpers.cs:255 | Khong gay sai chuc nang (buoc sau dung `?.` tu bao ve), nhung la bat thuong ve code-style/logic doc de gay hieu nham khi bao tri |
| 14 | `GetClientIpAddress`: comment dong 88 "4. Remote IP" nhung code thuc te CHỈ `return string.Empty`, KHONG doc `httpContext.Connection.RemoteIpAddress` nhu comment ham y | ConvertHelpers.cs:88-89 | Mau thuan giua comment va code - theo nguyen tac uu tien source code, xac nhan KHONG co fallback doc RemoteIpAddress thuc su; neu tat ca header proxy deu thieu, ham luon tra rong du connection TCP co IP thuc |
| 15 | `GetMinEnumValue<TEnum>` khong co rang buoc generic `where TEnum : Enum`, khong kiem tra `items` rong, khong co try/catch - khac phong cach "nuot loi" cua toan bo cac ham enum/const khac trong `ConvertHelpers` | ConvertHelpers.cs:161-164 | Goi voi mang rong (`GetMinEnumValue<TEnum>()`) se throw `InvalidOperationException` khong bat; goi voi `TEnum` khong convert duoc sang `int` co the throw `OverflowException`/`InvalidCastException`. Khong tim thay noi su dung ham nay trong repo hien tai (grep khong co ket qua) |
| 16 | `VietnamesePhoneValidator.Validate` khong kiem tra `number == null` truoc khi goi `Regex.Replace`, va khong co try/catch | ConvertHelpers.cs:532-539 | Goi voi `number = null` se throw exception khong duoc bat (khac voi da so ham khac trong `ConvertHelpers` deu co try/catch nuot loi) |
| 17 | `GetClientIpAddress`: nhanh xu ly header `"Forwarded"` (RFC 7239) dung `.Trim('"', '[', ']')` sau khi cat `for=` - `Trim` chi xoa ky tu o hai dau chuoi nen KHONG loai bo dung dau `]` cua dia chi IPv6 khi gia tri co kem cong (vi du `for="[2001:db8::1]:4711"`) | ConvertHelpers.cs:61-73 (dac biet dong 72) | Voi header `Forwarded` chua IPv6 kem cong theo dung chuan RFC 7239, ham tra ve chuoi loi dang `2001:db8::1]:4711` (con dau `]` va dinh kem port) thay vi dia chi IP sach - phat hien moi, doc lai truc tiep tu source, chua duoc ban truoc cua tai lieu nay ghi nhan |
| 18 | Doi chieu voi `docs/knowledge-base/Extensions-Kafka.md`, `Data-MongoDB-CoreMongoDB.md`, `Abstractions-DomainPrimitives.md`: ca 3 file nay co trich dan `ToJSon`/`JSonTryParse<T>(string)`/`DateTimeNullAbleConverter` - noi dung trich dan (hanh vi tra `""`/`false` khi rong/null/`{}`/`[]`, nuot exception qua Console/ILogger) **KHOP** voi source code doc lai trong tai lieu nay; KHONG phat hien mo ta sai/thieu can sua trong 3 file KB cu do doi voi pham vi cua 2 ham nay | JSonParseHelpers.cs:19-195 doi chieu Data-MongoDB-CoreMongoDB.md:45,1541,1867; Extensions-Kafka.md:46,119-166,360-361; Abstractions-DomainPrimitives.md:31 | Khong co hanh dong sua doi can thiet doi voi 8 file KB cu trong buoc nay |
