# ConfigurationHelpers (MongoDB) / MongoPipelineOptimizerHelper / MongoResiliencePolicyFactory

> Nguon:
> - `FTELSRCore.Shared/Data/MongoDB/Helpers/ConfigurationHelpers.cs` (276 dong)
> - `FTELSRCore.Shared/Data/MongoDB/Helpers/MongoPipelineOptimizerHelper.cs` (171 dong)
> - `FTELSRCore.Shared/Data/MongoDB/Helpers/Policies/MongoResiliencePolicyFactory.cs` (240 dong)
>
> Loai:
> - `ConfigurationHelpers`: `public static class`; lop long ben trong `VietnamDateTimeSerializer` la `public class`, ke thua `SerializerBase<DateTime?>`
> - `MongoPipelineOptimizerHelper`: `public class` (khong phai `static class`, xem muc 4)
> - `MongoResiliencePolicyFactory`: `public class` (khong phai `static class`, xem muc 4)
>
> Cap nhat theo commit: `89c1ce9`

## 1. Tong quan

Module nay gom ba thanh phan doc lap thuoc tang truy cap du lieu MongoDB, deu nam trong (hoac ngay duoi) namespace `FTELSRCore.Data.MongoDB.Helpers`.

`ConfigurationHelpers` cung cap ba ham tinh: dung `IMongoDatabase.GetCollection` (`SetCollection`), dung `MongoClientSettings.FromConnectionString` roi ghi de 5 tham so timeout/pool (`GetSettingConnection`), va chay lenh `{ping:1}` de kiem tra ket noi co timeout huy duoc (`IsCheckConnection`). Class con chua mot `BsonSerializer` tuy chinh xu ly `DateTime?` theo gio Viet Nam (`VietnamDateTimeSerializer`).

`MongoPipelineOptimizerHelper` la mot bo toi gian hoa (khong phai toi uu chi phi truy van) cho mang `List<BsonDocument>` dai dien mot aggregation pipeline: loai bo cac stage vo nghia (`$skip: 0`, `$match` rong, `$addFields`/`$set` rong, `$unset` rong) va gop `$and` long trong `$match` khi khong xung dot ten field, de quy vao ca `$facet` va pipeline con cua `$lookup`/`$unionWith`.

`MongoResiliencePolicyFactory` la factory cau hinh **Polly v8 resilience pipeline** cho ket noi MongoDB, cung cap hai ham cau hinh tach biet cho luong doc (`ConfigureReadPolicy`) va luong ghi (`ConfigureWritePolicy`). Day chinh la factory duoc `Data-MongoDB-CoreMongoDB.md` (muc 1.6) mo ta gian tiep qua `_pipelineRead`/`_pipelineWrite` cua `CoreMongoDB<TTable>`.

> [!IMPORTANT]
> Ca ba class deu **khong co bat ky lenh `using FTELSRCore.Data.MongoDB.Helpers` / goi ten day du nao** tu noi khac trong repo (grep toan bo `*.cs` cua solution, tru `.claude/worktrees`). Cu the: `ConfigurationHelpers.SetCollection` / `GetSettingConnection` / `IsCheckConnection`, `MongoPipelineOptimizerHelper.Optimize`, va `MongoResiliencePolicyFactory.ConfigureReadPolicy` / `ConfigureWritePolicy` **khong co noi goi nao trong repo `sr-core-helper`** (repo chi co dung mot project `FTELSRCore.Shared.csproj`). `VietnamDateTimeSerializer` chi duoc tham chieu trong 6 dong comment bi **comment out** (`BaseEntityMongoDB.cs:32,56,88,120,143,158`, dang `//[BsonSerializer(typeof(VietnamDateTimeSerializer))]`). Day co the la code dung cho mot project ben ngoai repo nay tieu thu thu vien — **khong xac dinh duoc tu source code cua repo nay**.

### 1.1 Pham vi chuc nang

| Lam duoc | Khong lam duoc |
|---|---|
| Lay `IMongoCollection<TModel>` tu `IMongoDatabase` + ten collection (`SetCollection`) | Khong cache, khong validate `table` rong/`null` — truyen thang cho driver |
| Dung `MongoClientSettings.FromConnectionString` roi ghi de 5 tham so: `SocketTimeout`, `ConnectTimeout`, `ServerSelectionTimeout` = 30s; `MaxConnectionPoolSize` = 2000; `MinConnectionPoolSize` = 100 (`GetSettingConnection`) | Khong doc bat ky tham so tu `IConfiguration`/`appsettings` — ca 5 gia tri hardcode; khong cho tuy bien qua overload |
| Nem `CustomException("Khong tim thay cau hinh ket noi.")` khi `connectionString` rong/trang/`null` (`GetSettingConnection`) | Khong validate cau truc connection string (uy quyen hoan toan cho `MongoClientSettings.FromConnectionString` cua driver) |
| Ping thu ket noi MongoDB voi timeout huy duoc qua `CancellationTokenSource` (`IsCheckConnection`) | Khong tra ve chi tiet loi (chi `bool`); loi (tru `OperationCanceledException`) chi duoc ghi ra **Console**, khong nem lai |
| Cache `MongoClient` theo connection string trong `ConcurrentDictionary` tinh (static) de tranh tao pool moi lien tuc | Khong co co che xoa/het han cache; `MongoClient` da tao **khong bao gio bi loai bo** khoi `_healthCheckClients` trong vong doi ung dung |
| Serialize/deserialize `DateTime?` sang gio Viet Nam (`SE Asia Standard Time`) qua `VietnamDateTimeSerializer`, ho tro doc ca 4 dang BSON: `DateTime`, `String`, `Int64`, `Document` (extended JSON `$date`) | Khong tu dong ap dung serializer nay cho entity nao — moi noi dung `[BsonSerializer(typeof(VietnamDateTimeSerializer))]` trong repo deu **dang bi comment out** |
| Loai bo stage `$skip: 0`, `$match` rong, `$addFields`/`$set` rong, `$unset` rong khoi mang `BsonDocument[]`/`List<BsonDocument>` (`MongoPipelineOptimizerHelper.Optimize`) | Khong toi uu chi phi thuc thi (khong doi thu tu stage, khong gop `$match` + `$match` lien tiep, khong day `$match` len dau) |
| Gop `$and` long trong `$match` thanh cac dieu kien ngang hang khi **khong** trung ten field (`TryUnwrapAnd`) | Tu choi gop (giu nguyen `$and`) ngay khi phat hien **bat ky** field trung ten — khong gop phan, khong merge gia tri (vi du `{$gt, $lt}` tren cung field) |
| De quy vao pipeline con cua `$facet`, `$lookup.pipeline`, `$unionWith.pipeline` (`OptimizeFacet`, `OptimizeNestedPipeline`) | Khong xu ly to hop `$lookup` dang cu (`localField`/`foreignField`, khong co `pipeline`) — nhanh nay tra ve stage nguyen ven khong doi |
| Cau hinh Polly pipeline doc/ghi rieng: retry + circuit breaker + OpenTelemetry span + log (`ConfigureReadPolicy`, `ConfigureWritePolicy`) | Khong tao/tra ve `ResiliencePipeline` — chi mutate `ResiliencePipelineBuilder` duoc truyen vao (`void`); khong doc cau hinh tu `IConfiguration` |
| Phan loai loi MongoDB theo exception type de quyet dinh retry/mo circuit breaker (`IsRetryable`, `IsConnectionLevel`) | Khong phan loai theo error code cua MongoDB (khac SQL: khong co `HashSet<int>` ma loi); chi dung kieu `Exception` (`is` pattern) |

### 1.2 Dependency

| Thanh phan | Muc dich su dung |
|---|---|
| `MongoDB.Driver` (v3.10.0) | `IMongoCollection<>`, `IMongoDatabase`, `MongoClient`, `MongoClientSettings`, `Command<BsonDocument>`; cac exception `MongoNotPrimaryException`, `MongoNodeIsRecoveringException`, `MongoConnectionException`, `MongoExecutionTimeoutException` |
| `MongoDB.Bson` / `MongoDB.Bson.IO` / `MongoDB.Bson.Serialization` / `...Serializers` | `BsonDocument`, `BsonType`, `IBsonReader`, `SerializerBase<DateTime?>`, `BsonDeserializationContext`, `BsonSerializationContext`, `BsonUtils` |
| `System.Collections.Concurrent` | `ConcurrentDictionary<string, MongoClient>` cho `_healthCheckClients` |
| `System.Globalization` | `CultureInfo.InvariantCulture`, `DateTimeStyles.RoundtripKind` khi parse chuoi ngay |
| `Polly` (v8.7.0) — `ResiliencePipelineBuilder`, `DelayBackoffType` | Doi tuong duoc mutate trong `ConfigureReadPolicy`/`ConfigureWritePolicy` |
| `Polly.CircuitBreaker` — `CircuitBreakerStrategyOptions` | Cau hinh circuit breaker |
| `Polly.Retry` — `RetryStrategyOptions` | Cau hinh retry |
| `System.Net.Sockets` — `SocketException` | Duoc coi la loi connection-level va retryable (khi doc) |
| `System.Diagnostics` — `ActivitySource`, `Activity`, `ActivityKind` | Tao span OpenTelemetry cho CB/retry trong `MongoResiliencePolicyFactory` |
| `OpenTelemetryConstant.MongoResilienceActivitySource` | Ten `ActivitySource` = `"FTELSRCore.Data.MongoDB.Helpers.Policies.MongoResiliencePolicyFactory"` (`Constants/OpenTelemetryConstant.cs:14`) |
| `ILogger` (global using `Microsoft.Extensions.Logging`) + `LoggerExtensions.Warning` (`Extensions/Loggers/LoggerExtensions.cs:254`) | Ghi log CB open/closed/half-open va retry |
| `FTELSRCore.Exceptions.CustomException` | Nem loi nghiep vu khi `connectionString` rong (`GetSettingConnection`) |
| `FTELSRCore.Constants.CommonBaseConstant` | `DateTimeUtc()` va `ConfigLoggerExceptionByConsole(...)` dung trong `IsCheckConnection` |

### 1.3 Danh muc API

| API | Nhom | Mo ta ngan |
|---|---|---|
| `ConfigurationHelpers.SetCollection<TModel>(IMongoDatabase, string)` | public static | Wrapper mong cho `IMongoDatabase.GetCollection<TModel>` |
| `ConfigurationHelpers.GetSettingConnection(string)` | public static | Tao `MongoClientSettings` tu connection string + ghi de 5 tham so timeout/pool |
| `ConfigurationHelpers.IsCheckConnection(string, string, int)` | public static | Ping MongoDB (co timeout huy duoc), tra `bool`; loi (tru huy) chi log Console |
| `ConfigurationHelpers.VietnamDateTimeSerializer` | public nested class | `SerializerBase<DateTime?>` — quy doi UTC <-> gio Viet Nam khi (de)serialize |
| `MongoPipelineOptimizerHelper.Optimize(List<BsonDocument>)` | public static | Diem vao: loc bo stage vo nghia trong toan pipeline, de quy vao sub-pipeline |
| `MongoPipelineOptimizerHelper.OptimizeStage(BsonDocument)` | private static | Xu ly 1 stage theo ten operator dau tien (`$skip`, `$match`, `$addFields`, `$set`, `$unset`, `$facet`, `$lookup`, `$unionWith`) |
| `MongoPipelineOptimizerHelper.OptimizeFacet(BsonDocument)` | private static | Toi gian hoa tung nhanh sub-pipeline ben trong `$facet` |
| `MongoPipelineOptimizerHelper.OptimizeNestedPipeline(string, BsonDocument)` | private static | Toi gian hoa `pipeline` long trong `$lookup`/`$unionWith` |
| `MongoPipelineOptimizerHelper.TryUnwrapAnd(BsonDocument)` | private static | Gop `$and` vao cung cap voi `$match` khi khong trung field |
| `MongoResiliencePolicyFactory.ConfigureReadPolicy(ResiliencePipelineBuilder, ILogger)` | public static | Gan CB (60%/5req/10s, break 20s) + retry (3 lan, base 150ms) cho luong doc |
| `MongoResiliencePolicyFactory.ConfigureWritePolicy(ResiliencePipelineBuilder, ILogger)` | public static | Gan CB (50%/10req/15s, break 60s) + retry (1 lan, base 300ms) cho luong ghi |
| `MongoResiliencePolicyFactory.IsRetryable(Exception, bool)` | private static | Phan loai exception co retry duoc hay khong, theo che do `handleAllTransient` |
| `MongoResiliencePolicyFactory.IsConnectionLevel(Exception)` | private static | Phan loai exception la loi connection/failover-level (dieu kien mo CB) |

---

## 2. ConfigurationHelpers

### 2.1 `SetCollection<TModel>`

**Signature**

```csharp
public static IMongoCollection<TModel> SetCollection<TModel>(IMongoDatabase configDatabase, string table) where TModel : class
```

**Muc dich** - Tra ve `IMongoCollection<TModel>` ung voi ten `table` tren database `configDatabase`, khong lam gi khac ngoai goi thang `IMongoDatabase.GetCollection<TModel>(table)` (`ConfigurationHelpers.cs:29-32`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `configDatabase` | `IMongoDatabase` | Co | Khong validate; goi truc tiep `.GetCollection<TModel>` | Khong co |
| `table` | `string` | Co | Khong validate rong/`null`/khoang trang | Khong co |

**Output** - `IMongoCollection<TModel>`: doi tuong collection do driver tra ve. Khong bao gio `null` khi khong nem exception (theo hanh vi cua `MongoDB.Driver`).

**Dieu kien xu ly** - Khong co nhanh re; mot cau lenh duy nhat: `return configDatabase.GetCollection<TModel>(table);` (dong 31).

**Side effect** - Khong co. Khong mo ket noi moi (collection handle la lazy, chi mo ket noi khi co lenh thuc thi).

**Error handling** - Khong co `try`/`catch`. `configDatabase == null` -> `NullReferenceException` nem ngay tai dong 31. `table == null`/rong duoc truyen thang cho driver — hanh vi cu the (nem exception hay tra collection voi ten rong) **khong xac dinh duoc tu source code cua repo nay** (thuoc ve `MongoDB.Driver`).

**Khi nao NEN dung** - Khi can lay `IMongoCollection<TModel>` ma khong muon goi truc tiep `IMongoDatabase.GetCollection` (vi du de dong bo mot diem goi trong toan code base). Trong repo nay hien khong co diem goi nao nhu vay.

**Khi nao KHONG dung** - Khong mang lai loi ich neu chi goi lai `configDatabase.GetCollection<TModel>(table)` — ham khong them validate, khong them log, khong them cache.

**Gioi han**
- Khong co gia tri gia tang so voi goi truc tiep API cua driver: khong validate, khong log, khong cache.
- Generic constraint chi co `where TModel : class`, khong yeu cau ke thua `BaseEntityMongoDB` hay attribute nao.
- Khong co noi goi nao trong repo (xem muc 1 - canh bao IMPORTANT).

---

### 2.2 `GetSettingConnection`

**Signature**

```csharp
public static MongoClientSettings GetSettingConnection(string connectionString)
```

**Muc dich** - Parse `connectionString` thanh `MongoClientSettings` roi ghi de 5 tham so timeout/connection-pool co gia tri hardcode, phuc vu viec tao `MongoClient` on dinh hon cau hinh mac dinh cua driver (`ConfigurationHelpers.cs:41-64`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `connectionString` | `string` | Co | `if (string.IsNullOrWhiteSpace(connectionString)) throw new CustomException("Khong tim thay cau hinh ket noi.")` (dong 43) | Khong co |

**Output** - `MongoClientSettings` da duoc cau hinh 5 tham so:

| Tham so | Gia tri | Dong |
|---|---|---|
| `SocketTimeout` | `TimeSpan.FromSeconds(30)` | 49 |
| `ConnectTimeout` | `TimeSpan.FromSeconds(30)` | 52 |
| `ServerSelectionTimeout` | `TimeSpan.FromSeconds(30)` | 55 |
| `MaxConnectionPoolSize` | `2000` | 58 |
| `MinConnectionPoolSize` | `100` | 61 |

**Dieu kien xu ly**
1. Guard: `connectionString` rong/trang/`null` -> `throw new CustomException("Khong tim thay cau hinh ket noi.")` (dong 43). `CustomException` mac dinh `Code = 500` (`(int)HttpStatusCode.InternalServerError)` — xem `Exceptions/CustomException.cs:5`.
2. `MongoClientSettings.FromConnectionString(connectionString)` (dong 45-46) — moi loi cu phap connection string duoc nem tu day, do `MongoDB.Driver` quyet dinh kieu exception, **khong xac dinh duoc tu source code cua repo nay**.
3. Ghi de 5 tham so theo thu tu code (dong 49-61).
4. `return settings;` (dong 63).

**Side effect** - Khong ghi DB, khong goi API ngoai. Chi tao va mutate mot doi tuong `MongoClientSettings` cuc bo, tra ve cho caller (khong giu tham chieu tinh).

**Error handling** - Nem `CustomException` (khong phai `ArgumentException`) khi dau vao rong. Khong bat exception nao tu `FromConnectionString`; loi duoc nem thang cho caller.

**Khi nao NEN dung** - Truoc khi tao `MongoClient` moi tu connection string cau hinh, de dam bao moi client trong ung dung dung chung mot bo timeout/pool.

**Khi nao KHONG dung**
- Khi can timeout/pool khac 5 gia tri hardcode nay — ham khong nhan tham so tuy chinh, phai sua truc tiep client settings sau khi goi ham nay tra ve.
- Khi `connectionString` co the rong nhung muon xu ly loi mem (khong nem exception) — ham luon nem `CustomException`, khong tra `null`/gia tri mac dinh.

**Gioi han**
- Ca 5 gia tri deu hardcode, khong doc tu `IConfiguration`/`IOptions` (khop voi phat hien tuong tu o `SqlResiliencePolicyFactory`/`Data-SQL-Resilience.md`).
- Message loi tieng Viet co dau ("Khong tim thay cau hinh ket noi.") co the khong khop voi ngu canh thuc su (day khong phai loi "khong tim thay cau hinh" ma la "cau hinh rong/trong") — nhan dinh chu quan, khong anh huong hanh vi.
- Khong co overload cho phep tuy bien 5 tham so nay.

---

### 2.3 `IsCheckConnection`

**Signature**

```csharp
public static bool IsCheckConnection(string connectionDatabase, string databaseName, int timeWait = 1000)
```

**Muc dich** - Kiem tra MongoDB co san sang khong bang lenh `{ping:1}`, dung `MongoClient` duoc cache theo connection string va gioi han thoi gian cho bang `CancellationTokenSource` (`ConfigurationHelpers.cs:74-110`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `connectionDatabase` | `string` | Co | `string.IsNullOrWhiteSpace(...)` -> tra `false` ngay (dong 76-79) | Khong co |
| `databaseName` | `string` | Co | Cung dieu kien nhu tren, kiem tra **gop** voi `connectionDatabase` bang `||` | Khong co |
| `timeWait` | `int` | Khong | Khong validate am/0; truyen thang vao `CancellationTokenSource(timeWait)` | `1000` (ms) |

**Output** - `bool`:

| Truong hop | Gia tri tra ve |
|---|---|
| `connectionDatabase` hoac `databaseName` rong/trang/`null` | `false` (tra ve ngay, khong tao `MongoClient`) |
| Ping thanh cong trong thoi gian `timeWait` | `true` |
| Ping bi huy do het `timeWait` (`OperationCanceledException`) | `false` |
| Ping nem exception khac (loi mang, loi auth, DNS...) | `false` (bat tai `catch (Exception exception)`, khong nem lai) |

**Dieu kien xu ly**
1. Guard rong/trang cho ca hai chuoi dau vao (dong 76-79).
2. `_healthCheckClients.GetOrAdd(connectionDatabase, cs => new MongoClient(cs))` (dong 81) — **cache theo dung gia tri chuoi `connectionDatabase`**; hai chuoi connection string khac nhau (vi du khac whitespace) tao hai `MongoClient` khac nhau trong cache.
3. `client.GetDatabase(databaseName)` (dong 83).
4. `using CancellationTokenSource cancellationTokenSource = new(timeWait)` (dong 91) — huy tu dong sau `timeWait` milliseconds.
5. `database.RunCommandAsync((Command<BsonDocument>)"{ping:1}", cancellationToken: ...).GetAwaiter().GetResult()` (dong 93-96) — **goi bat dong bo nhung cho dong bo** (block thread goi).
6. Thanh cong -> `pingResult = true` (dong 98).
7. `catch (OperationCanceledException)` -> `pingResult = false` (dong 100-103), khong ghi log.
8. `catch (Exception exception)` -> ghi log Console qua `CommonBaseConstant.ConfigLoggerExceptionByConsole(...)` voi `description: $"Database: {database?.DatabaseNamespace}"` (dong 104-107), `pingResult` giu gia tri `false` da khoi tao (dong 85).
9. `return pingResult;` (dong 109).

**Side effect**
- Tao va **giu vinh vien** mot `MongoClient` moi trong `_healthCheckClients` (static `ConcurrentDictionary`) cho moi `connectionDatabase` moi gap lan dau — khong co co che remove/dispose.
- Ghi Console (khong phai `ILogger`) khi gap exception ngoai `OperationCanceledException`.
- Mo mot ket noi TCP toi MongoDB (qua pool cua `MongoClient` da cache).

**Error handling** - Bat rieng `OperationCanceledException` (huy do timeout) va `Exception` (moi loi khac). Khong nem lai bat ky truong hop nao — ham luon tra `bool`, khong bao gio throw ra ngoai (tru khi `connectionDatabase`/`databaseName` gay loi truoc do, nhung guard dong 76-79 da chan truong hop rong).

**Khi nao NEN dung** - Health-check endpoint / readiness probe can biet MongoDB co phan hoi trong X ms hay khong, ma khong can biet chi tiet loi.

**Khi nao KHONG dung**
- Khi can phan biet ly do khong ket noi duoc (auth sai, sai ten database, mang cham...) — ham chi tra `bool`, chi tiet loi nam trong Console log, **khong** qua `ILogger` nen kho tich hop vao he thong log tap trung.
- Khi goi voi nhieu connection string khac nhau lien tuc (vi du multi-tenant) trong thoi gian dai — moi connection string moi tao them mot `MongoClient` (va pool ket noi rieng) khong bao gio duoc giai phong.
- Khi can timeout chinh xac duoi muc millisecond hoac can hoan toan bat dong bo (khong block thread) — `GetAwaiter().GetResult()` block thread goi trong toi da `timeWait` ms.

**Gioi han**
- `_healthCheckClients` khong co gioi han kich thuoc, khong TTL — nguy co ro nho (memory leak) neu ham duoc goi voi nhieu connection string khac nhau qua thoi gian (dong 19).
- `database?.DatabaseNamespace` trong message log dung `database` (co the khac `null` vi da gan o dong 83 truoc `try`), nhung neu `client.GetDatabase` nem exception thi `database` van la bien cuc bo chua gan — thuc te dong 83 nam **ngoai** `try`, nen neu no nem exception, ham se **throw thang** ra ngoai (khong bi bat boi `catch` ben trong, vi try block chi bat dau tu dong 87). Day la mot nhanh loi khong duoc `IsCheckConnection` xu ly: neu `databaseName` khong hop le theo quy tac cua driver, ham **co the nem exception** thay vi tra `false`.
- Khong log khi `OperationCanceledException` xay ra — kho phan biet "timeout" voi "MongoDB tra ve cham nhung van song" chi qua gia tri `false`.
- Tham so `timeWait` khong duoc validate; `timeWait <= 0` truyen cho `CancellationTokenSource` co the nem `ArgumentOutOfRangeException` (theo tai lieu .NET, `CancellationTokenSource(int)` yeu cau `>= -1`; gia tri am khac `-1` nem loi) — hanh vi cu the voi `timeWait` am **khong xac dinh duoc kiem tra rieng trong source code cua repo nay**, phu thuoc runtime .NET.

---

### 2.4 `VietnamDateTimeSerializer`

**Loai**: `public class VietnamDateTimeSerializer : SerializerBase<DateTime?>`, lop long ben trong `ConfigurationHelpers` (`ConfigurationHelpers.cs:112-275`).

**Muc dich tong the** - Serializer BSON tuy chinh cho `DateTime?`: khi doc (deserialize) tu MongoDB, luon quy gia tri UTC luu trong BSON ve gio dia phuong `"SE Asia Standard Time"` (UTC+7); khi viet (serialize), luon quy gia tri `DateTime?` dau vao ve UTC truoc khi luu.

#### 2.4.1 Constructor

**Signature**

```csharp
public VietnamDateTimeSerializer() : this(BsonType.DateTime)
public VietnamDateTimeSerializer(BsonType representation) => _representation = representation;
```

**Muc dich** - Constructor khong tham so mac dinh dung `BsonType.DateTime` lam dang luu tru; constructor co tham so cho phep chon dang luu khac (`String`, `Int64`).

**Input hop le** | `representation` | `BsonType` | Khong bat buoc (co overload rong) | Khong validate gia tri hop le (chi 3 gia tri `DateTime`/`String`/`Int64` duoc xu ly trong `Serialize`, cac gia tri khac se nem `NotSupportedException` **khi goi `Serialize`**, khong phai ngay tai constructor) | `BsonType.DateTime` |

**Side effect** - Khong co. Chi gan field readonly `_representation`.

**Error handling** - Khong co. Constructor khong validate `representation`.

#### 2.4.2 `Deserialize`

**Signature**

```csharp
public override DateTime? Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
```

**Muc dich** - Doc gia tri BSON hien tai va chuyen thanh `DateTime?` theo gio Viet Nam (`ConfigurationHelpers.cs:127-167`).

**Input hop le** - Ca hai tham so do MongoDB driver cung cap tai thoi diem doc du lieu; ham khong tu goi truc tiep.

**Output** - `DateTime?`:

| Dang BSON dau vao | Ket qua |
|---|---|
| `BsonType.Null` | `null` (dong 131-135) |
| `BsonType.DateTime` | `BsonUtils.ToDateTimeFromMillisecondsSinceEpoch(...)` (UTC) roi convert ve gio VN |
| `BsonType.String` | `DateTime.Parse(..., InvariantCulture).ToUniversalTime()` roi convert ve gio VN |
| `BsonType.Int64` | `DateTime.FromBinary(...).ToUniversalTime()` roi convert ve gio VN |
| `BsonType.Document` | Doc field `$date` long trong document (extended JSON), roi convert ve gio VN — xem `ReadDocumentAsDateTime` |
| Cac `BsonType` khac | `throw new NotSupportedException($"Cannot deserialize BsonType {bsonType} to DateTime?")` (dong 163) |

**Dieu kien xu ly** (dung thu tu code, dong 129-166)
1. Doc `bsonType = context.Reader.GetCurrentBsonType()`.
2. `Null` -> doc null, tra `null` ngay.
3. `switch (bsonType)`: 5 nhanh (`DateTime`, `String`, `Int64`, `Document`, `default` nem `NotSupportedException`).
4. Sau switch (khong roi vao nhanh `Null`/exception): `return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, VnTimeZone);` (dong 166) — **luon quy doi ve gio VN**, bat ke dang BSON goc la gi.

**Side effect** - Doc tu `context.Reader` (mutate vi tri doc cua BSON stream) — day la hanh vi binh thuong cua bat ky `BsonSerializer`.

**Error handling** - Khong co `try`/`catch`. Nem `NotSupportedException` khi gap `BsonType` khong duoc ho tro (dong 163). `DateTime.Parse` co the nem `FormatException` neu chuoi khong hop le — khong duoc bat rieng, se nem thang ra ngoai.

**Khi nao NEN dung** - Gan qua attribute `[BsonSerializer(typeof(VietnamDateTimeSerializer))]` cho property `DateTime?` can hien thi/luu theo gio dia phuong VN. **Hien tai khong co property nao trong repo dang thuc su ap dung attribute nay** (tat ca 6 vi tri o `BaseEntityMongoDB.cs` deu bi comment).

**Khi nao KHONG dung**
- Cho property `DateTime` khong nullable — serializer chi ke thua `SerializerBase<DateTime?>`, khong ho tro `DateTime`.
- Khi can giu nguyen `Kind` cua `DateTime` (`Utc`/`Local`/`Unspecified`) sau khi doc lai — `Deserialize` luon tra ve gia tri da quy doi bang `TimeZoneInfo.ConvertTimeFromUtc`, ham nay **luon** tra `Kind = Unspecified` (theo tai lieu .NET), khong giu `Kind = Local`.

**Gioi han**
- `VnTimeZone` doc bang `TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")` (dong 114-115) — day la ID theo Windows; tren mot so runtime .NET chay Linux khong co ICU/tzdata mapping day du, loi `TimeZoneNotFoundException` co the xay ra **ngay khi load type nay lan dau** (field `static readonly`) — **khong xac dinh duoc tu source code cua repo nay** ket qua thuc te tren moi trung production dang chay.
- `BsonType.Document` (extended JSON) chi ho tro dung mot cau truc `{ "$date": ... }`; field nao khac `$date` bi `reader.SkipValue()` bo qua am tham (dong 243), khong canh bao.

#### 2.4.3 `Serialize`

**Signature**

```csharp
public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, DateTime? value)
```

**Muc dich** - Quy doi `value` (duoc coi la gio VN neu chua phai UTC) ve UTC roi viet xuong BSON theo `_representation` (`ConfigurationHelpers.cs:169-208`).

**Output** - `void` (viet truc tiep vao `context.Writer`).

**Dieu kien xu ly**
1. `!value.HasValue` -> `context.Writer.WriteNull(); return;` (dong 171-175).
2. `value.Value.Kind == DateTimeKind.Utc` -> giu nguyen `utcDateTime = value.Value` (dong 180-183).
3. Nguoc lai (`Local` hoac `Unspecified`) -> **coi la gio Viet Nam**: `DateTime.SpecifyKind(value.Value, DateTimeKind.Unspecified)` roi `TimeZoneInfo.ConvertTimeToUtc(..., VnTimeZone)` (dong 185-190).
4. `switch (_representation)`: `DateTime` -> `WriteDateTime(ToMillisecondsSinceEpoch(...))`; `String` -> `WriteString(utcDateTime.ToString("o"))`; `Int64` -> `WriteInt64(utcDateTime.ToBinary())`; `default` -> `throw new NotSupportedException(...)` (dong 192-208).

**Side effect** - Viet vao `context.Writer` (mutate BSON stream dang duoc ghi).

**Error handling** - Khong `try`/`catch`. Nem `NotSupportedException` khi `_representation` khong phai 1 trong 3 gia tri duoc xu ly (dong 207) — vi du neu constructor duoc goi voi `BsonType.ObjectId`.

**Gioi han**
- **Diem can luu y quan trong**: `value.Value.Kind == DateTimeKind.Local` **khong** duoc chuyen doi tu gio may chu ve UTC bang co che chuan (`ToUniversalTime()`); ham chu dong **ep** ve `Unspecified` roi tinh nhu the no dang la gio VN (dong 187). Neu server chay o mui gio khac VN va gia tri `DateTime` co `Kind = Local` thuc su theo mui gio server (khong phai VN), ket qua luu se **sai** theo dung nghia "Local".
- Khong co validate `_representation` tai constructor; loi chi phat hien luc `Serialize` thuc su chay.

---

## 3. MongoPipelineOptimizerHelper

### 3.1 `Optimize` (diem vao)

**Signature**

```csharp
public static List<BsonDocument> Optimize(List<BsonDocument> pipeline)
```

**Muc dich** - Duyet tuan tu tung stage trong `pipeline`, goi `OptimizeStage` cho moi stage; stage nao bi toi gian ve `null` se **bi loai khoi ket qua** (`MongoPipelineOptimizerHelper.cs:7-26`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `pipeline` | `List<BsonDocument>` | Co (nhung ham tu xu ly `null`) | `pipeline == null || pipeline.Count == 0` -> tra ve **chinh `pipeline` dau vao** (dong 9-12, khong tao list moi) | Khong co |

**Output** - `List<BsonDocument>`:

| Truong hop | Gia tri tra ve |
|---|---|
| `pipeline == null` | `null` (tra lai chinh tham so, khong doi thanh `[]`) |
| `pipeline.Count == 0` | Chinh doi tuong `pipeline` rong da truyen vao (cung reference) |
| `pipeline` co phan tu | List **moi** (`new List<BsonDocument>(pipeline.Count)`, dong 14) chi chua cac stage con lai sau toi gian hoa; co the **it hon** so phan tu goc neu co stage bi loai (tra ve `null` tu `OptimizeStage`) |

**Dieu kien xu ly**
1. Guard `null`/rong -> tra ve nguyen ven tham so dau vao (dong 9-12) — **khong** tao ban sao trong truong hop nay.
2. `result = new List<BsonDocument>(pipeline.Count)` (dong 14) — cap suc chua ban dau bang so luong stage goc (co the du thua suc chua neu co stage bi loai).
3. `foreach (var stage in pipeline)`: goi `OptimizeStage(stage)`; neu ket qua **khac `null`** thi `result.Add(optimized)` (dong 16-24). Stage tra ve `null` bi **bo qua hoan toan**, khong them gi vao `result`.
4. `return result;` (dong 25).

**Side effect** - Khong ghi DB, khong ghi log, khong goi API ngoai. **Khong mutate** cac `BsonDocument` cua `pipeline` dau vao — cac stage khong bi toi gian duoc **tra ve cung reference goc** (vi `OptimizeStage` tra `stage` nguyen ven trong nhieu nhanh, xem muc 3.2), nhung cac stage **co** toi gian (`$match`, `$facet`, `$lookup`, `$unionWith`) duoc thay bang **doi tuong `BsonDocument` moi** (khong mutate doi tuong goc).

**Error handling** - Khong co `try`/`catch`. Neu mot phan tu trong `pipeline` la `null`, `OptimizeStage(null)` tra ve `null` (xem muc 3.2) nen phan tu `null` do se bi **loai bo khoi ket qua**, khong nem exception.

**Khi nao NEN dung** - Truoc khi goi `IMongoCollection.Aggregate`/`AggregateAsync` voi mot pipeline duoc build dong (co kha nang sinh ra cac stage rong/vo nghia, vi du tu dieu kien loc dong). **Luu y**: trong repo `sr-core-helper` hien khong co diem goi nao den ham nay (xem canh bao IMPORTANT o muc 1) — `CoreMongoDB<TTable>.FindAllWithAggregateAsync` (cac overload nhan `BsonDocument[]`) **khong** goi `Optimize` truoc khi thuc thi.

**Khi nao KHONG dung**
- Khi pipeline dang dang `PipelineDefinition<TTable, TResult>` (khong phai `List<BsonDocument>`/`BsonDocument[]`) — ham chi nhan `List<BsonDocument>`.
- Khi ky vong toi uu **chi phi thuc thi** (chon index, sap xep lai stage de `$match` chay som hon) — ham **chi** loai bo stage vo nghia va gop `$and`, **khong** sap xep lai thu tu stage, **khong** day `$match` len truoc `$lookup`/`$sort`, **khong** tac dong den ke hoach thuc thi cua MongoDB server.
- Khi thu tu cac stage co the anh huong ket qua (vi du `$skip` truoc `$sort`) va caller ky vong ham nay se "sua" thu tu — ham **giu nguyen tuyet doi thu tu cac stage con lai**, chi bo stage vo nghia, khong bao gio doi vi tri.

**Gioi han**
- Chi nhan `List<BsonDocument>`, khong co overload cho `BsonDocument[]` (kieu ma `CoreMongoDB.FindAllWithAggregateAsync` dang su dung) — muon dung chung phai tu `.ToList()`/`.ToArray()`.
- Truong hop `pipeline` rong hoac `null`: tra ve **chinh tham so goc** (cung reference), khac voi truong hop co phan tu (luon tra list moi) — hanh vi khong dong nhat ve viec co tao ban sao hay khong giua hai nhanh.
- Khong co gioi han do sau de quy cho `$facet`/`$lookup`/`$unionWith` long nhau — pipeline long qua nhieu tang co the gay `StackOverflowException` ve ly thuyet (khong kiem tra duoc gioi han cu the tu source).

---

### 3.2 `OptimizeStage` (noi bo)

**Signature**

```csharp
private static BsonDocument OptimizeStage(BsonDocument stage)
```

**Muc dich** - Xac dinh mot stage co bi loai bo (`null`) hay giu/thay doi, dua theo **ten cua phan tu BSON dau tien** trong stage (`MongoPipelineOptimizerHelper.cs:31-79`).

**Dieu kien xu ly** (dung thu tu code)

1. `stage == null || stage.ElementCount == 0` -> **tra ve `stage` nguyen ven** (dong 33-34) — quan trong: mot document rong (`{}`) hoac `null` **khong** bi coi la "vo nghia can loai", ham tra ve chinh no (`stage`, co the la `null`).
2. `first = stage.GetElement(0)` (dong 36) — **chi doc phan tu dau tien**. Neu stage co nhieu key (vi du mot `BsonDocument` khong dung quy uoc 1-key-1-operator cua aggregation stage), cac key con lai **hoan toan bi bo qua** khi quyet dinh toi gian.
3. `switch (name)` voi `name = first.Name`:

| `name` | Dieu kien | Ket qua |
|---|---|---|
| `"$skip"` | `value.IsNumeric && value.ToInt64() == 0` | `null` (loai bo). Nguoc lai: `stage` nguyen ven |
| `"$match"` | `!value.IsBsonDocument` | `stage` nguyen ven (dong 46) |
| `"$match"` | `value.IsBsonDocument` va `match.ElementCount == 0` | `null` (loai bo, dong 48) |
| `"$match"` | `value.IsBsonDocument`, khong rong, `TryUnwrapAnd(match)` tra ve khac `null` | `new BsonDocument("$match", unwrapped)` — **doi tuong moi** |
| `"$match"` | `value.IsBsonDocument`, khong rong, `TryUnwrapAnd` tra `null` (khong the/khong can gop) | `stage` nguyen ven |
| `"$addFields"` / `"$set"` | `value.IsBsonDocument && value.AsBsonDocument.ElementCount == 0` | `null` (loai bo) |
| `"$addFields"` / `"$set"` | Con lai (khong phai document, hoac document co phan tu) | `stage` nguyen ven |
| `"$unset"` | `value.IsBsonArray && value.AsBsonArray.Count == 0` | `null` (loai bo) |
| `"$unset"` | `value.IsString && string.IsNullOrEmpty(value.AsString)` | `null` (loai bo) |
| `"$unset"` | Con lai (array co phan tu, hoac string khong rong) | `stage` nguyen ven |
| `"$facet"` | `value.IsBsonDocument` | Ket qua cua `OptimizeFacet(value.AsBsonDocument)` |
| `"$facet"` | Khong phai document | `stage` nguyen ven |
| `"$lookup"` / `"$unionWith"` | `value.IsBsonDocument` | Ket qua cua `OptimizeNestedPipeline(name, value.AsBsonDocument)` |
| `"$lookup"` / `"$unionWith"` | Khong phai document | `stage` nguyen ven |
| Ten khac (bat ky operator nao khong nam trong danh sach tren, vi du `$project`, `$sort`, `$group`, `$limit`, `$count`, `$replaceRoot`,...) | — | `stage` nguyen ven (nhanh `default`, dong 76-77) |

**Output** - `BsonDocument` hoac `null`. `null` co nghia "loai bo stage nay khoi pipeline ket qua" (duoc dien giai boi caller `Optimize`).

**Side effect** - Khong mutate `stage` dau vao trong cac nhanh tra ve **cung** `stage`. Trong cac nhanh tao `new BsonDocument(...)` (case `$match` co unwrap, `$facet`, `$lookup`/`$unionWith`), doi tuong tra ve la **moi**, khong anh huong `stage` goc.

**Error handling** - Khong `try`/`catch`. `stage.GetElement(0)` tren mot `BsonDocument` **da qua guard rong** (dong 33) nen luon an toan; khong co truong hop nem `IndexOutOfRangeException`.

**Khi nao NEN dung** - Chi duoc goi noi bo tu `Optimize`/`OptimizeFacet`/`OptimizeNestedPipeline` (dong 18, 92, 113). `private static`, khong the goi tu ngoai class.

**Gioi han**
- **Chi xet key dau tien** cua stage. MongoDB aggregation stage tieu chuan chi co dung 1 key nen gia dinh nay thuong dung, nhung neu `BsonDocument` dau vao khong tuan thu quy uoc nay (vi du bi build sai, co 2 key), cac key sau bi **bo qua hoan toan** khoi logic toi gian (van giu nguyen trong `stage` tra ve, vi ham tra `stage` goc — chi khong duoc **xet** de quyet dinh loai bo).
- `"$match"` document rong (`{}`) bi loai **hoan toan** khoi pipeline (coi nhu "luon dung" — dung ve mat ngu nghia MongoDB, vi `$match: {}` khop moi document), nhung neu day la stage **duy nhat** cua pipeline, ket qua cuoi la pipeline rong `[]`, hanh vi cua `AggregateAsync([])` phu thuoc driver — **khong xac dinh duoc tu source code cua repo nay**.
- Danh sach operator duoc "hieu" chi gom 6 ten: `$skip`, `$match`, `$addFields`, `$set`, `$unset`, `$facet`, `$lookup`, `$unionWith` (8 ten thuc te). Cac operator pho bien khac (`$project`, `$group`, `$sort`, `$limit`, `$count`, `$replaceRoot`, `$graphLookup`,...) **luon** roi vao nhanh `default`, **khong bao gio** duoc toi gian du co the rong/vo nghia (vi du `$project: {}`).

---

### 3.3 `OptimizeFacet` (noi bo)

**Signature**

```csharp
private static BsonDocument OptimizeFacet(BsonDocument facet)
```

**Muc dich** - Voi mot `$facet` (dang `{ "<key>": [<stage>, ...], ... }`), toi gian hoa **tung mang sub-pipeline** ben trong bang cach goi lai `Optimize` (de quy) (`MongoPipelineOptimizerHelper.cs:81-100`).

**Dieu kien xu ly**
1. Tao `newFacet = new BsonDocument()` (dong 83).
2. `foreach (var sub in facet)`: neu `sub.Value.IsBsonArray` -> loc cac phan tu la `BsonDocument` (`Where(x => x.IsBsonDocument)`, **bo qua am tham** phan tu khong phai document, dong 88-90), goi `Optimize(subStages)` de quy, boc lai thanh `BsonArray`, them vao `newFacet` voi ten `sub.Name` (dong 92). Nguoc lai (`sub.Value` khong phai array) -> giu nguyen `sub.Value` (dong 96).
3. `return new BsonDocument("$facet", newFacet);` (dong 99) — **luon tra ve doi tuong moi**, khong bao gio `null` (mot `$facet` khong bao gio bi ham nay loai bo hoan toan, ke ca khi `facet` rong — ket qua se la `$facet: {}`).

**Output** - `BsonDocument` dang `{ "$facet": { ... } }`. Khong bao gio `null`.

**Side effect** - Khong mutate `facet` dau vao (tao `newFacet` moi). Cac phan tu khong phai `BsonDocument` trong mot sub-pipeline array **bi loai am tham** khoi ket qua (dong 89) — day la hanh vi khac voi `Optimize` cap cao nhat (chi loai phan tu tra `null` tu `OptimizeStage`, khong loai phan tu sai kieu).

**Error handling** - Khong `try`/`catch`. Neu `facet == null`, `foreach (var sub in facet)` nem `NullReferenceException` — ham khong guard tham so `null` (khac `OptimizeStage`/`Optimize` cap ngoai).

**Khi nao NEN dung** - Chi goi noi bo tu `OptimizeStage` khi gap `$facet` co `value.IsBsonDocument == true` (dong 67).

**Gioi han**
- Khong guard `facet == null`.
- Sub-pipeline khong phai `BsonArray` duoc giu nguyen (dong 96) ma **khong** canh bao — mot `$facet` sai cau truc (theo dung ngu phap MongoDB, gia tri phai la array) van duoc "toi gian" ma khong bao loi.
- Phan tu trong sub-pipeline array khong phai `BsonDocument` bi am tham loai bo — neu nguoi dung ky vong giu nguyen cau truc goc de debug, day la thay doi ngu nghia (tuy hiem khi xay ra trong pipeline hop le).

---

### 3.4 `OptimizeNestedPipeline` (noi bo)

**Signature**

```csharp
private static BsonDocument OptimizeNestedPipeline(string stageName, BsonDocument body)
```

**Muc dich** - Voi `$lookup`/`$unionWith` dang moi (co field `pipeline`), toi gian hoa mang `pipeline` long ben trong bang de quy `Optimize`; giu nguyen cac field khac (`from`, `as`, `let`,...) (`MongoPipelineOptimizerHelper.cs:102-116`).

**Dieu kien xu ly**
1. `if (!body.Contains("pipeline") || !body["pipeline"].IsBsonArray) return new BsonDocument(stageName, body);` (dong 104-105) — **khong co field `pipeline`**, hoac `pipeline` khong phai array (vi du `$lookup` kieu cu chi co `localField`/`foreignField`) -> boc lai `body` **nguyen ven** (khong doi noi dung, chi doi lop boc ngoai thanh `BsonDocument` moi).
2. Neu co `pipeline` dang array: loc cac phan tu la `BsonDocument` (`Where(x => x.IsBsonDocument)`, dong 108-109) — **phan tu khong phai document trong `pipeline` bi loai am tham** (giong `OptimizeFacet`).
3. `copy = (BsonDocument)body.DeepClone()` (dong 112) — **deep clone toan bo `body`** truoc khi sua, dam bao khong mutate `body` goc.
4. `copy["pipeline"] = new BsonArray(Optimize(subStages))` (dong 113) — ghi de field `pipeline` cua ban sao bang ket qua toi gian de quy.
5. `return new BsonDocument(stageName, copy);` (dong 115).

**Output** - `BsonDocument` dang `{ "<stageName>": {...} }`. Khong bao gio `null`.

**Side effect** - Khong mutate `body` dau vao (dung `DeepClone` truoc khi sua). Deep clone toan bo document long (co the ton kem CPU/memory voi `$lookup` co pipeline lon).

**Error handling** - Khong `try`/`catch`. `body == null` -> `body.Contains("pipeline")` nem `NullReferenceException` (khong guard).

**Khi nao NEN dung** - Chi goi noi bo tu `OptimizeStage` khi gap `$lookup`/`$unionWith` co `value.IsBsonDocument == true` (dong 72-74).

**Gioi han**
- Khong guard `body == null`.
- Chi nhan dien dung mot ten field `"pipeline"` (chu thuong) — dung quy uoc MongoDB, khong co truong hop khac can xu ly.
- `$lookup` kieu cu (`localField`/`foreignField`, khong co `pipeline`) van bi **boc lai thanh doi tuong `BsonDocument` moi** (dong 105) dù noi dung khong doi — nghia la ket qua `Optimize` **khong** giu nguyen reference cho MOI stage khong doi, chi giu nguyen **gia tri**.
- Deep clone toan bo `body` (dong 112) ngay ca khi chi mot phan `pipeline` thay doi — khong toi uu cho `$lookup` co nhieu field phu (`let`, `as`,...) kem theo pipeline lon.

---

### 3.5 `TryUnwrapAnd` (noi bo)

**Signature**

```csharp
private static BsonDocument TryUnwrapAnd(BsonDocument match)
```

**Muc dich** - Voi noi dung cua mot `$match` co chua `$and`, thu gop cac dieu kien trong `$and` thanh cac field ngang hang trong cung mot document, **chi khi dam bao khong doi ngu nghia** (khong co field nao trung ten giua cac phan tu se gop) (`MongoPipelineOptimizerHelper.cs:119-168`).

**Input hop le** | `match` | `BsonDocument` | Co (khong guard `null` — xem Gioi han) | La noi dung ben trong `$match` (da duoc `OptimizeStage` xac nhan `IsBsonDocument` va khong rong truoc khi goi) | Khong co |

**Output** - `BsonDocument` hoac `null`:

| Truong hop | Ket qua |
|---|---|
| `match` khong chua `$and`, hoac `match["$and"]` khong phai array | `null` (dong 121-124) — **bao hieu cho `OptimizeStage`: khong co gi de gop, giu `stage` nguyen ven** |
| `$and` la array **rong** (`[]`) | Ban sao cua `match` voi `$and` bi xoa; neu sau khi xoa **khong con field nao** -> tra `null` (dong 129-135, se lam `OptimizeStage` COI la khong doi -> giu `stage` nguyen ven — **luu y: day la mot nguyen nhan khien optimization "$and: []" khong thuc su co hieu luc**, xem Gioi han) |
| `$and` la array **rong**, con field khac ngoai `$and` | Ban sao `match` (khong con `$and`) — **duoc coi la ket qua gop thanh cong**, `OptimizeStage` se boc thanh `new BsonDocument("$match", ...)` |
| Co it nhat mot phan tu trong `$and` **khong phai** `BsonDocument` | `null` (dong 151-154, thoat de quy ngay, khong gop gi) |
| Co field trung ten giua cac phan tu se gop (ke ca trung voi field ngoai `$and`) | `null` (dong 159-161, thoat ngay tai field trung dau tien phat hien) |
| Khong co field trung ten nao | `BsonDocument` moi la ket qua gop toan bo field (ca field ngoai `$and` va tung field trong moi phan tu cua `$and`) |

**Dieu kien xu ly** (dung thu tu code)
1. `if (!match.Contains("$and") || !match["$and"].IsBsonArray) return null;` (dong 121-124).
2. `andArray = match["$and"].AsBsonArray` (dong 126).
3. `if (andArray.Count == 0)`: deep clone `match`, `Remove("$and")`, tra `null` neu ban sao rong, nguoc lai tra ban sao (dong 129-135).
4. `combined = new BsonDocument()` (dong 137); duyet `match`, **copy moi field tru `$and`** vao `combined` (dong 139-147) — **thu tu field ngoai `$and` duoc giu nguyen thu tu xuat hien trong `match` goc**.
5. Duyet `andArray`: neu phan tu khong phai `BsonDocument` -> `return null` ngay (dong 151-154, **dung toan bo qua trinh**, bo qua ca cac phan tu da xu ly truoc do trong vong lap nay).
6. Voi moi phan tu la document: duyet tung field (`item.AsBsonDocument`); neu `combined.Contains(field.Name)` -> `return null` ngay (dong 159-161); nguoc lai `combined.Add(field)` (dong 163).
7. `return combined;` (dong 167) sau khi duyet het tat ca phan tu ma khong gap trung/loi kieu.

**Side effect** - Khong mutate `match` dau vao (deep clone truoc khi sua trong nhanh #3; tao `combined` moi trong nhanh chinh). Ham thuan tra ve gia tri moi hoac `null`.

**Error handling** - Khong `try`/`catch`, khong nem exception. Neu `match == null`, `match.Contains("$and")` nem `NullReferenceException` — **khong xay ra trong luong goi thuc te** vi `OptimizeStage` da kiem tra `match.ElementCount == 0` truoc khi goi ham nay, nhung ve mat ky thuat ham khong tu bao ve.

**Khi nao NEN dung** - Chi duoc goi noi bo tu `OptimizeStage`, nhanh `"$match"` (dong 49).

**Khi nao KHONG dung/Gioi han**
- **Chi go duy nhat mot lop `$and` o cap cao nhat**. `$and` long trong `$or`, hoac `$and` long trong `$and` (long 2 cap), **khong** duoc de quy go — vi ham chi doc `item.AsBsonDocument` truc tiep cua tung phan tu, khong goi lai `TryUnwrapAnd` cho phan tu do.
- **Phat hien trung ten dung tren TEN FIELD, khong xet gia tri operator ben trong**. Vi du `$and: [{a: {$gt: 1}}, {a: {$lt: 9}}]` (dieu kien khoang gia tri tren cung field `a`) **khong** duoc gop (dung, vi gop se de field `a` sau ghi de field `a` truoc trong mot `BsonDocument`) — ham xu ly dung truong hop nay bang cach tra `null`, giu nguyen `$and` de dam bao dung ngu nghia.
- Truong hop `$and: []` con lai field khac: sau khi go, ket qua document co the **thay doi ngu nghia bieu dien** (tu "AND cua tap dieu kien rong (luon dung) VA cac dieu kien khac" thanh "chi con cac dieu kien khac") — **ve mat logic MongoDB hai cach viet nay tuong duong** (vi `$and: []` luon dung), nen phep bien doi la an toan.
- Duyet theo thu tu phan tu trong `andArray`; neu phan tu dau tien hop le nhung phan tu thu hai khong phai `BsonDocument`, cac field cua phan tu dau tien **da duoc them vao `combined`** truoc khi ham `return null` — nhung vi `combined` la bien cuc bo bi huy khi ham thoat, dieu nay khong gay side effect quan sat duoc tu ben ngoai (chi la chi tiet cai dat).

---

## 4. MongoResiliencePolicyFactory

### 4.1 Bang so sanh cau hinh Read vs Write

Tat ca gia tri duoi day doc truc tiep tu than ham, khong phai tu XML doc.

**Circuit breaker**

| Tham so | `ConfigureReadPolicy` | Dong | `ConfigureWritePolicy` | Dong |
|---|---|---|---|---|
| `ShouldHandle` | `args.Outcome.Exception is { } ex && IsConnectionLevel(ex)` | 26-27 | Giong Read, cung goi `IsConnectionLevel` | 124-125 |
| `FailureRatio` | `0.6` (60%) | 28 | `0.5` (50%) | 126 |
| `MinimumThroughput` | `5` | 29 | `10` | 127 |
| `BreakDuration` | `TimeSpan.FromSeconds(20)` | 30 | `TimeSpan.FromSeconds(60)` | 129 |
| `SamplingDuration` | `TimeSpan.FromSeconds(10)` | 31 | `TimeSpan.FromSeconds(15)` | 128 |
| `OnOpened` | Tao `Activity "mongodb.circuit_breaker.open"` + `logger.Warning` | 32-48 | Giong cau truc Read (cung ten span, cung `logger.Warning`) | 130-146 |
| `OnClosed` | Tao `Activity "mongodb.circuit_breaker.closed"` + `logger.Warning` | 49-64 | Giong Read | 147-162 |
| `OnHalfOpened` | Tao `Activity "mongodb.circuit_breaker.half_open"` + `logger.Warning` | 65-79 | Giong Read (**van la `Warning`**, khac voi `SqlResiliencePolicyFactory.ConfigureWritePolicy` dung `logger.Info` cho `OnHalfOpened` — `SqlResiliencePolicyFactory.cs:186`) | 163-177 |
| `BreakDurationGenerator` / `StateProvider` / `ManualControl` | Khong cau hinh | — | Khong cau hinh | — |

**Retry**

| Tham so | `ConfigureReadPolicy` | Dong | `ConfigureWritePolicy` | Dong |
|---|---|---|---|---|
| `ShouldHandle` | `IsRetryable(ex, true)` | 84-85 | `IsRetryable(ex, false)` | 182-183 |
| `MaxRetryAttempts` | `3` | 86 | `1` (hang so cuc bo `writeMaxRetryAttempts`) | 118, 184 |
| `Delay` (base delay) | `TimeSpan.FromMilliseconds(150)` | 87 | `TimeSpan.FromMilliseconds(300)` | 185 |
| `BackoffType` | `DelayBackoffType.Exponential` | 88 | `DelayBackoffType.Exponential` | 186 |
| `UseJitter` | `true` | 89 | `true` | 187 |
| `MaxDelay` / `DelayGenerator` | Khong cau hinh | — | Khong cau hinh | — |
| `OnRetry` | Tao `Activity "mongodb.retry"` (4 tag) + `logger.Warning`, mau so log hardcode `3` | 90-106 | Tao `Activity "mongodb.retry"` (5 tag, **co them** `retry.max_attempts`) + `logger.Warning`, mau so log dung bien `writeMaxRetryAttempts` | 188-205 |
| Tong so lan goi toi da | 4 (1 lan dau + 3 retry) | 86 | 2 (1 lan dau + 1 retry) | 118 |

**Diem khac biet ban chat**

| Khia canh | Read | Write |
|---|---|---|
| Tap exception duoc retry (`IsRetryable(ex, true/false)`) | `MongoNotPrimaryException`, `MongoNodeIsRecoveringException`, `MongoConnectionException`, `SocketException`, `MongoExecutionTimeoutException`, `TimeoutException` | **Chi** `MongoNotPrimaryException`, `MongoNodeIsRecoveringException` |
| Tap exception mo CB (`IsConnectionLevel`) | `MongoConnectionException`, `MongoNotPrimaryException`, `MongoNodeIsRecoveringException`, `SocketException` | **Giong Read** — cung ham `IsConnectionLevel`, khong tach rieng theo doc/ghi |
| Ly do khac biet retry (theo comment source) | — | `MongoConnectionException`/`SocketException` co the xay ra **sau khi** server da xu ly ghi nhung truoc khi client nhan ack — retry co the tao ban ghi trung/ap update 2 lan (`MongoResiliencePolicyFactory.cs:218-221`) |
| OpenTelemetry span | Co (ca CB va retry) | Co (ca CB va retry) — **khong bat doi xung nhu SQL** (SQL: ConfigureWritePolicy khong co Activity nao) |

> [!IMPORTANT]
> **So sanh voi `Data-MongoDB-CoreMongoDB.md` (muc 1.6)**: bang cau hinh trong file KB do (`FailureRatio`, `MinimumThroughput`, `SamplingDuration`, `BreakDuration`, `MaxRetryAttempts`, `Delay`, `BackoffType`/`UseJitter`, danh sach exception retryable cho ca Read va Write) da duoc doi chieu lai tung dong voi than ham thuc te tai day va **khop hoan toan**, khong phat hien sai lech ve so lieu. Xem chi tiet phat hien bo sung (khong phai sai) tai muc 5.

### 4.2 Thu tu strategy trong pipeline

Ca hai ham deu goi `AddCircuitBreaker(...)` **truoc** `AddRetry(...)` (dong 22-107 cho Read, dong 120-206 cho Write). Trong Polly v8, strategy duoc `Add*` truoc la lop **ngoai cung**, nen cau truc thuc thi la:

```
CircuitBreaker (ngoai cung)
  └── Retry
        └── lenh driver MongoDB
```

Hau qua giong voi phan tich da lam cho `SqlResiliencePolicyFactory` (`Data-SQL-Resilience.md`, muc 2.2): circuit breaker chi ghi nhan **mot** outcome cho moi lan goi `ExecuteAsync` cua toan pipeline (khong phai moi attempt retry rieng le); CB khong retry duoc `BrokenCircuitException` khi dang o trang thai `Open`.

### 4.3 ConfigureReadPolicy

**Signature**

```csharp
public static void ConfigureReadPolicy(ResiliencePipelineBuilder builder, ILogger logger)
```

**Muc dich** - Gan mot `CircuitBreakerStrategyOptions` (dong 23-80) roi mot `RetryStrategyOptions` (dong 81-107) vao `builder`, voi cau hinh khoan dung cao cho luong doc.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `builder` | `ResiliencePipelineBuilder` | Co | **Khong co guard clause trong file nay.** `AddCircuitBreaker`/`AddRetry` la extension method cua Polly; theo XML doc Polly 8.7.0 chung nem `ArgumentNullException` khi `builder`/`options` la `null`, va `ValidationException` khi `options` khong hop le | Khong co |
| `logger` | `ILogger` | Co | Khong co guard clause; duoc capture vao 4 closure (dong 41, 57, 73, 99). `logger.Warning` la extension method (`LoggerExtensions.cs:254`) nen khong nem tai cho goi cau hinh; voi `logger == null`, `NullReferenceException` phat sinh **ben trong callback** khi Polly thuc su goi | Khong co |

**Output** - `void`. Khong tra `builder`, khong tra `ResiliencePipeline`. Caller phai tu goi `builder.Build()`.

**Dieu kien xu ly** (thoi diem cau hinh) - Chi 2 lenh tuan tu: `AddCircuitBreaker` (dong 22-80) roi `AddRetry` (dong 81-107). Khong `if`/`switch`/`try` nao trong than ham chinh.

**Dieu kien xu ly** (thoi diem runtime, do lambda quyet dinh)
1. CB `ShouldHandle` (dong 26-27): `args.Outcome.Exception is { } ex && IsConnectionLevel(ex)`. Ket qua thanh cong (`Exception == null`) -> `false` ngay, khong goi `IsConnectionLevel`.
2. Retry `ShouldHandle` (dong 84-85): `args.Outcome.Exception is { } ex && IsRetryable(ex, true)`.
3. Cac callback `OnOpened`/`OnClosed`/`OnHalfOpened`/`OnRetry`: khong co nhanh dieu kien, luon `StartActivity` + `SetTag` (qua `?.`, an toan khi khong co listener) + `logger.Warning` + `return default`.

**Side effect**

| Side effect | Vi tri | Chi tiet |
|---|---|---|
| Mutate `builder` (them 2 strategy) | 22, 81 | Muc dich chinh cua ham |
| Capture `logger` vao 4 closure song voi vong doi pipeline | 41, 57, 73, 99 | |
| Tao `Activity "mongodb.circuit_breaker.open"`, 4 tag (`db.system`, `resilience.state`, `resilience.type`, `resilience.break_duration_ms`) | 34-39 | |
| Tao `Activity "mongodb.circuit_breaker.closed"`, 3 tag | 51-55 | |
| Tao `Activity "mongodb.circuit_breaker.half_open"`, 3 tag | 67-71 | |
| Tao `Activity "mongodb.retry"`, 4 tag (`db.system`, `resilience.type`, `retry.attempt`, `retry.delay_ms`) | 92-97 | **Khong co tag `retry.max_attempts`** (khac voi `ConfigureWritePolicy`, xem muc 5) |
| Log `Warning` "[CB OPEN] blocking DB for {N}s" kem exception | 41-45 | |
| Log `Warning` "[CB CLOSED] DB restored" khong kem exception | 57-61 | |
| Log `Warning` "[CB HALF-OPEN] probing DB" khong kem exception | 73-76 | |
| Log `Warning` "[RETRY {n}/{3}] wait {ms}ms" kem exception, mau so `3` la **literal hardcode** | 99-103 | |
| Chan truy cap khi CB `Open` | (Polly) | 20 giay |

Khong ghi DB, khong goi API ngoai truc tiep trong than ham.

**Error handling** - Khong `try`/`catch`. Xu ly loi uy quyen hoan toan cho Polly qua `ShouldHandle` va cac callback. Sau 3 lan retry khong thanh cong, exception goc nem lai cho CB roi cho caller — khong fallback.

**Khi nao NEN dung** - Cau hinh pipeline cho cac lenh MongoDB **chi doc** (`Find`, `Count`, `Aggregate` doc) — noi retry nhieu lan an toan vi khong co side effect nghiep vu.

**Khi nao KHONG dung**
- **Khong dung cho luong ghi**: retry toi 3 lan tren lenh co side effect (`InsertOneAsync`, `UpdateOneAsync`,...) co the tao du lieu trung/ap dung thay doi 2 lan neu loi xay ra sau khi server da xu ly nhung truoc khi client nhan ack (xem giai thich trong comment `IsRetryable`, dong 218-221 va muc 4.6). Dung `ConfigureWritePolicy`.
- Khi can budget do tre chat che: toi da 4 lan goi + delay retry (base 150/300/600ms theo exponential trung vi, `UseJitter = true` lam gia tri thuc te dao dong).

**Gioi han**

| # | Gioi han | Dong |
|---|---|---|
| 1 | Toan bo 8 tham so (`FailureRatio`, `MinimumThroughput`, `SamplingDuration`, `BreakDuration`, `MaxRetryAttempts`, `Delay`, `BackoffType`, `UseJitter`) la hardcode, khong bind `IConfiguration`/`IOptions` | 28-31, 86-89 |
| 2 | Khong co `ArgumentNullException.ThrowIfNull` cho `builder`/`logger` | 20-22 |
| 3 | So `3` xuat hien 2 lan doc lap: `MaxRetryAttempts = 3` (86) va literal `{3}` trong message log (103). Doi `MaxRetryAttempts` khong tu dong sua message log | 86, 103 |
| 4 | Tag OpenTelemetry cua `OnRetry` **thieu** `retry.max_attempts` (co 4 tag, khong co tag nay), khac voi `ConfigureWritePolicy` co dung 5 tag (them `retry.max_attempts`) — bat doi xung giua hai ham trong cung mot factory | 94-97 so voi 192-196 |
| 5 | `MaxDelay` khong dat -> khong tran delay; voi 3 lan retry hien khong nghiem trong nhung se thanh van de neu tang `MaxRetryAttempts` sau nay | 81-89 |
| 6 | CB nam ngoai retry -> CB dem 1 don vi cho moi lan goi pipeline (khong phai moi attempt), phan ung cham hon so voi thu tu nguoc | 22, 81 |
| 7 | Ham khong idempotent, khong kiem tra `builder` da co strategy chua -> goi nhieu lan (hoac goi ca `ConfigureReadPolicy` va `ConfigureWritePolicy` tren cung `builder`) se cong don strategy ma khong co guard | 22-107 |
| 8 | Neu `builder` da `Build()`, Polly nem `InvalidOperationException`; ham khong bat | 22, 81 |

### 4.4 ConfigureWritePolicy

**Signature**

```csharp
public static void ConfigureWritePolicy(ResiliencePipelineBuilder builder, ILogger logger)
```

**Muc dich** - Gan mot `CircuitBreakerStrategyOptions` (dong 121-178) roi mot `RetryStrategyOptions` (dong 179-206) vao `builder`, voi cau hinh **than trong hon** `ConfigureReadPolicy`: retry chi 1 lan va chi voi 2 loai loi connection/failover (`MongoResiliencePolicyFactory.cs:116-207`).

**Input hop le** - Giong `ConfigureReadPolicy` (xem muc 4.3): khong guard `builder`/`logger`.

**Output** - `void`.

**Dieu kien xu ly**
1. `const int writeMaxRetryAttempts = 1;` (dong 118) — hang so cuc bo, dung o ca `MaxRetryAttempts` (dong 184), tag `retry.max_attempts` (dong 195) va message log (dong 202) — **ba noi dung cung bien, tu dong bo**, khac voi `ConfigureReadPolicy` dung literal `3` roi rac.
2. `AddCircuitBreaker` (dong 120-178) roi `AddRetry` (dong 179-206).
3. Runtime: CB `ShouldHandle` (dong 124-125) goi **cung** `IsConnectionLevel` nhu Read. Retry `ShouldHandle` (dong 182-183) goi `IsRetryable(ex, false)` — `handleAllTransient = false`.

**Side effect** - Cau truc giong muc 4.3 (Activity + log cho ca 4 callback CB/retry), khac ten method trong `nameof(ConfigureWritePolicy)` va gia tri tham so (xem bang 4.1). **Khac biet duy nhat ve OpenTelemetry so voi Read**: `OnRetry` cua Write co **5 tag** (them `retry.max_attempts`, dong 195), Read chi co **4 tag** (thieu tag nay, dong 94-97).

**Error handling** - Giong muc 4.3. Voi loi **khong** thuoc `MongoNotPrimaryException`/`MongoNodeIsRecoveringException` (vi du `MongoConnectionException`, `SocketException`, `MongoExecutionTimeoutException`, `TimeoutException`), `IsRetryable(ex, false)` tra `false` -> **khong retry, exception nem thang len caller ngay** sau 1 lan thu duy nhat.

**Khi nao NEN dung** - Cau hinh pipeline cho lenh MongoDB co side effect (`InsertOneAsync`, `InsertManyAsync`, `UpdateOneAsync`, `UpdateManyAsync`, `DeleteOneAsync`, `DeleteManyAsync`, `BulkWriteAsync`) — day chinh la nhom lenh duoc `_pipelineWrite` cua `CoreMongoDB<TTable>` boc (xem `Data-MongoDB-CoreMongoDB.md`, muc 1.5).

**Khi nao KHONG dung**
- Khong dung cho luong doc: bo sot retry cho `MongoConnectionException`/`SocketException`/`MongoExecutionTimeoutException`/`TimeoutException` — nhung loi transient pho bien nhat cua truy van doc se **khong** duoc retry o cau hinh nay.
- Khong dung khi can `MinimumThroughput = 10` trong `15s` la nguong de dat voi tan suat ghi thap — CB co the khong bao gio mo.

**Gioi han**

| # | Gioi han | Dong |
|---|---|---|
| 1 | Tham so hardcode, khong bind cau hinh | 126-129, 184-187 |
| 2 | Khong guard null cho `builder`/`logger` | 116-118 |
| 3 | `MinimumThroughput = 10` + `SamplingDuration = 15s` la nguong kha cao; dich vu tan suat ghi thap co the khong bao gio kich hoat CB | 127, 128 |
| 4 | `BreakDuration = 60s` — mot lan mo CB chan toan bo luong ghi 1 phut, khong co `BreakDurationGenerator` giam dan | 129 |
| 5 | `MaxRetryAttempts = 1` + `BackoffType = Exponential`: exponential khong co tac dung voi 1 lan retry duy nhat; `UseJitter = true` van co tac dung (random hoa quanh trung vi 300ms) | 184-187 |
| 6 | Ham khong idempotent, khong kiem tra `builder` da co strategy chua | 120-206 |
| 7 | Neu `builder` da `Build()`, Polly nem `InvalidOperationException`; ham khong bat | 120, 179 |

### 4.5 `IsRetryable` (noi bo)

**Signature**

```csharp
private static bool IsRetryable(Exception ex, bool handleAllTransient)
```

**Muc dich** - Phan loai `ex` co duoc coi la retryable hay khong, theo hai che do `handleAllTransient` (`true` = luong doc, `false` = luong ghi) (`MongoResiliencePolicyFactory.cs:209-233`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `ex` | `Exception` | Co | Khong co `null` check tuong minh. Voi `ex == null`: ca 2 nhanh `is` pattern (`ex is MongoNotPrimaryException or ...`, `ex is MongoConnectionException or SocketException`) tra `false` (pattern matching voi `null` luon `false`, khong nem exception); nhanh cuoi `ex is MongoExecutionTimeoutException or TimeoutException` cung `false` -> ham tra `false` | Khong co |
| `handleAllTransient` | `bool` | Co | Khong validate; khong phai optional parameter (khong co gia tri mac dinh khi goi) | Khong co |

**Output** - `bool`:

| Truong hop | Ket qua |
|---|---|
| `ex is MongoNotPrimaryException or MongoNodeIsRecoveringException` | `true` (bat ke `handleAllTransient`) |
| `ex is MongoConnectionException or SocketException`, `handleAllTransient == true` | `true` |
| `ex is MongoConnectionException or SocketException`, `handleAllTransient == false` | `false` |
| Con lai, `handleAllTransient == true`, `ex is MongoExecutionTimeoutException or TimeoutException` | `true` |
| Con lai, `handleAllTransient == true`, kieu khac | `false` |
| Con lai, `handleAllTransient == false` | `false` |
| `ex == null` | `false` |

**Dieu kien xu ly** (dung thu tu code, dong 213-232)
1. Dong 213-216: `if (ex is MongoNotPrimaryException or MongoNodeIsRecoveringException) return true;` — **uu tien tuyet doi**, khong xet `handleAllTransient`. Comment tai dong 211-212 giai thich: server tu choi ngay vi doi vai tro (khong con la primary) -> thao tac **chua tung duoc xu ly**, an toan retry ca cho luong ghi.
2. Dong 222-225: `if (ex is MongoConnectionException or SocketException) return handleAllTransient;` — Comment dong 218-221: mat ket noi co the xay ra **sau khi** server da xu ly ghi nhung **truoc khi** client nhan ack; retry trong truong hop nay co the tao ban ghi trung/ap update 2 lan. Vi vay **chi coi la retryable o luong doc** (`handleAllTransient == true`), luong ghi luon `false` cho hai loai loi nay.
3. Dong 227-230: `if (handleAllTransient) return ex is MongoExecutionTimeoutException or TimeoutException;` — chi luong doc moi xet timeout.
4. Dong 232: `return false;` — moi truong hop con lai (bao gom toan bo nhanh khi `handleAllTransient == false` da khong khop 2 nhanh dau).

**Side effect** - Khong co. Ham thuan, chi kiem tra kieu bang `is`/pattern matching, khong doc field `static readonly` nao (khac voi `SqlResiliencePolicyFactory.IsRetryable` dung `HashSet<int>` theo ma loi).

**Error handling** - Khong `try`/`catch`, khong nem exception (ke ca `ex == null`), khong ghi log khi tra `false` -> **loi bi loai khoi retry khong de lai dau vet nao** tu ham nay (log thuc te, neu co, den tu noi khac goi ham nay ho hoac tu catch cua caller ben ngoai pipeline).

**Khi nao NEN dung** - Chi duoc goi noi bo tu `ShouldHandle` cua 2 retry strategy (dong 84-85, 182-183). `private static`, khong goi duoc tu ngoai class.

**Khi nao KHONG dung** - Khong dung nhu ham phan loai loi tong quat cho HTTP/SQL/nghiep vu khac — chi nhan dien 6 kieu exception cua MongoDB driver + `SocketException` + `TimeoutException` (kieu .NET chung).

**Gioi han**

| # | Gioi han | Dong |
|---|---|---|
| 1 | Khong duyet `InnerException`/`AggregateException` — chi kiem tra kieu cua **chinh** `ex` truyen vao. Neu `MongoConnectionException` bi boc trong mot exception khac (vi du do lop nghiep vu wrap lai), `IsRetryable` se khong nhan dien duoc | 209-233 |
| 2 | Khong phan loai theo ma loi cu the (khac han `SqlResiliencePolicyFactory` dung `HashSet<int>` cho 10/7 ma loi SQL) — chi dua vao **kieu** exception cua `MongoDB.Driver`, nen moi instance cua cung mot kieu (vi du moi `MongoExecutionTimeoutException` bat ke nguyen nhan) deu duoc xu ly giong nhau | 209-233 |
| 3 | `OperationCanceledException`/`TaskCanceledException` khong duoc nhan dien rieng (giong phat hien da co o `SqlResiliencePolicyFactory.IsRetryable`, `Data-SQL-Resilience.md` muc 2.6, gioi han #7); se roi vao `return false` | 227-232 |
| 4 | Danh sach kieu duoc coi la retryable la hardcode ngay trong than ham (khong co field `static readonly` tap trung nhu SQL) — muon them/bo mot kieu phai sua truc tiep logic `if` | 209-233 |

### 4.6 `IsConnectionLevel` (noi bo)

**Signature**

```csharp
private static bool IsConnectionLevel(Exception ex) =>
    ex is MongoConnectionException
        or MongoNotPrimaryException
        or MongoNodeIsRecoveringException
        or SocketException;
```

(`MongoResiliencePolicyFactory.cs:235-239`)

**Muc dich** - Quyet dinh `ex` co phai la loi connection/failover-level hay khong. Day la dieu kien **duy nhat** de circuit breaker (ca Read va Write) ghi nhan mot lan that bai.

**Input hop le** | `ex` | `Exception` | Co | Khong `null` check; `null is <type>` luon `false` -> ham tra `false` voi `ex == null`, khong nem exception | Khong co |

**Output** - `bool`: `true` neu `ex` la mot trong 4 kieu (`MongoConnectionException`, `MongoNotPrimaryException`, `MongoNodeIsRecoveringException`, `SocketException`); `false` cho moi truong hop khac (bao gom `MongoExecutionTimeoutException`, `TimeoutException`, moi exception nghiep vu khac, va `ex == null`).

**Dieu kien xu ly** - Mot bieu thuc `is` pattern duy nhat voi `or`, khong co nhanh re, khong goi ham nao khac (khac voi `SqlResiliencePolicyFactory.IsConnectionLevel` phai goi `UnwrapSqlException` truoc).

**Side effect** - Khong co. Ham thuan (expression-bodied member).

**Error handling** - Khong `try`/`catch`, khong nem exception, khong ghi log.

**Khi nao NEN dung** - Chi goi noi bo tu `ShouldHandle` cua CB o ca hai ham cau hinh (dong 26-27, 124-125).

**Khi nao KHONG dung** - Khong dung de phan loai cho retry cua luong doc: `IsConnectionLevel` **khong** nhan dien `MongoExecutionTimeoutException`/`TimeoutException` la connection-level, trong khi `IsRetryable(ex, true)` **co** retry hai kieu nay — nghia la timeout lien tuc tren luong doc **khong bao gio** lam mo circuit breaker (xem muc 5, #3).

**Gioi han**

| # | Gioi han | Dong |
|---|---|---|
| 1 | `MongoExecutionTimeoutException`/`TimeoutException` **khong** duoc coi la connection-level -> DB cham/treo hoan toan tren luong doc (moi truy van timeout, khong nem loi ket noi) khong lam CB mo, dua vao `IsRetryable` van tiep tuc retry ma khong co "van an toan" chan lai boi CB | 235-239 |
| 2 | Khong duyet `InnerException` — giong `IsRetryable`, chi xet kieu cua chinh `ex` | 235-239 |
| 3 | `IsConnectionLevel` duoc dung **giong nhau** cho ca Read va Write (khong co tham so `handleAllTransient` nhu `IsRetryable`) — CB cua Read va Write phan ung voi **cung mot tap kieu loi**, chi khac o `FailureRatio`/`MinimumThroughput`/`SamplingDuration`/`BreakDuration`; day la diem **khac voi SQL**, noi `IsConnectionLevel` cung dung chung cho CB nhung ban chat tap ma loi (`ConnectionLevelSqlErrors`) da duoc kiem tra rieng | 26-27, 124-125 |

---

## 5. Van de da biet

| # | Van de | Vi tri | Anh huong |
|---|---|---|---|
| 1 | `ConfigurationHelpers.SetCollection`, `GetSettingConnection`, `IsCheckConnection` va `MongoPipelineOptimizerHelper.Optimize` **khong co noi goi nao** trong repo `sr-core-helper` (grep toan bo `*.cs` cua solution, chi co 1 project `FTELSRCore.Shared.csproj`; khong file nao `using FTELSRCore.Data.MongoDB.Helpers`) | `ConfigurationHelpers.cs` (toan bo), `MongoPipelineOptimizerHelper.cs:7` | Khong the xac nhan hanh vi thuc te qua unit test tich hop trong repo nay; co the la API danh cho project ben ngoai tieu thu thu vien — **khong xac dinh duoc tu source code cua repo nay** |
| 2 | `VietnamDateTimeSerializer` chi duoc tham chieu trong 6 dong comment (`//[BsonSerializer(typeof(VietnamDateTimeSerializer))]`) tai `BaseEntityMongoDB.cs:32,56,88,120,143,158` — khong co property nao dang thuc su ap dung serializer nay | `BaseEntityMongoDB.cs:32,56,88,120,143,158` | Toan bo logic quy doi gio VN <-> UTC trong `VietnamDateTimeSerializer` hien khong duoc kich hoat cho bat ky truong `DateTime?` nao trong repo |
| 3 | `CoreMongoDB<TTable>.FindAllWithAggregateAsync` (cac overload nhan `BsonDocument[]`, xem `Data-MongoDB-CoreMongoDB.md` muc 1.3) **khong** goi `MongoPipelineOptimizerHelper.Optimize` truoc khi thuc thi aggregate | `CoreMongoDB.cs` (khong tim thay tu khoa `Optimize`); `MongoPipelineOptimizerHelper.cs:7` | Cac stage vo nghia (`$skip: 0`, `$match` rong,...) do caller truyen vao **khong** duoc loai bo tu dong boi tang repository; muon dung phai tu goi `Optimize` truoc khi truyen `BsonDocument[]` vao `FindAllWithAggregateAsync` |
| 4 | `MongoResiliencePolicyFactory.ConfigureReadPolicy` va `ConfigureWritePolicy` deu goi `ConfigureReadPolicy`/`ConfigureWritePolicy` — nhung **khong co noi goi nao** trong repo dang ky vao DI (grep `MongoResiliencePolicyFactory\.` chi tim thay khai bao trong chinh file nay). Diem nay **khop voi ghi chu da co** trong `Data-MongoDB-CoreMongoDB.md` (muc 1.6, khoi `[!NOTE]` cuoi cung: "khong tim thay doan code nao goi ConfigureReadPolicy/ConfigureWritePolicy de dang ky vao DI") | `MongoResiliencePolicyFactory.cs:20`, `:116` | Xac nhan lai (khong phai phat hien moi) rang viec noi day pipeline vao `CoreMongoDB` nam ngoai pham vi repo nay |
| 5 | `OnRetry` cua `ConfigureReadPolicy` tao `Activity` voi **4 tag** (thieu `retry.max_attempts`), trong khi `ConfigureWritePolicy` tao `Activity` cung ten voi **5 tag** (co them `retry.max_attempts`) — bat doi xung khong duoc giai thich boi comment nao trong source | `MongoResiliencePolicyFactory.cs:94-97` (Read, 4 tag) so voi `:192-196` (Write, 5 tag) | He thong quan sat (dashboard/trace query) dua tren tag `retry.max_attempts` se thay tag nay **vang mat** tren toan bo span retry cua luong doc |
| 6 | `ActivitySource` ten `"FTELSRCore.Data.MongoDB.Helpers.Policies.MongoResiliencePolicyFactory"` (`OpenTelemetryConstant.cs:14`) **khong duoc dang ky** trong `AddSource(...)` cua `OpenTelemetryExtensions.cs:14-16` (chi dang ky `CoreCacheActivitySource`, `LoggingBehaviorActivitySource`; `SqlResilienceActivitySource` cung KHONG duoc dang ky — da ghi trong `Data-SQL-Resilience.md` muc 2.4 gioi han #7). Neu khong co `ActivityListener` khac lang nghe, `StartActivity` tra `null` va toan bo `SetTag` (dung `?.`) bi bo qua | `Constants/OpenTelemetryConstant.cs:14`; `Infrastructure/Extensions/Helpers/OpenTelemetryExtensions/OpenTelemetryExtensions.cs:14-16` | Toan bo 8 diem `SetTag` trong `MongoResiliencePolicyFactory` (CB open/closed/half-open + retry, ca Read va Write) co the khong bao gio duoc ghi nhan boi backend tracing neu chi dua vao cau hinh `AddFTELSRTracing` hien co trong repo |
| 7 | `IsConnectionLevel` (dung chung cho CB cua ca Read va Write) khong nhan dien `MongoExecutionTimeoutException`/`TimeoutException` la loi connection-level, trong khi `IsRetryable(ex, true)` cua luong doc **co** retry ca hai kieu nay | `MongoResiliencePolicyFactory.cs:227-230` (retry co xet timeout) so voi `:235-239` (CB khong xet timeout) | Timeout lap lai lien tuc tren luong doc se duoc retry (theo `IsRetryable`) nhung **khong bao gio** lam mo circuit breaker — khong co "van an toan" chan tai khi DB cham keo dai do timeout (khac voi mat ket noi thuc su) |
| 8 | `MongoPipelineOptimizerHelper.OptimizeStage` chi xu ly 8 ten operator (`$skip`, `$match`, `$addFields`, `$set`, `$unset`, `$facet`, `$lookup`, `$unionWith`); cac operator pho bien khac co the rong/vo nghia (`$project: {}`, `$group` voi `_id: null` va khong co accumulator,...) khong duoc nhan dien | `MongoPipelineOptimizerHelper.cs:40-78` (nhanh `default` tra `stage` nguyen ven) | Pham vi toi gian hoa hep hon ten goi "Optimizer" co the goi y; day la gioi han thiet ke, khong phai loi, nhung can luu y khi doc ten class |
| 9 | `MongoPipelineOptimizerHelper.Optimize` tra ve **chinh tham so goc** (cung reference) khi `pipeline` la `null` hoac rong, nhung tra ve **list moi** khi `pipeline` co phan tu — khong dong nhat ve viec co tao ban sao du lieu hay khong giua hai nhanh | `MongoPipelineOptimizerHelper.cs:9-12` so voi `:14-25` | Code goi ham nay ma dua vao "ket qua luon la object moi, an toan sua doi doc lap voi input" se sai neu `pipeline` truyen vao rong/`null` |
