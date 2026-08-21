# Infrastructure - Header forwarding & Redis connection pool

> Nguon: `FTELSRCore.Shared/Infrastructure/Extensions/Helpers/HeaderHttpClientExtensions/IPAddressForwardExtensions.cs`, `FTELSRCore.Shared/Infrastructure/Extensions/Helpers/HeaderHttpClientExtensions/UserAgentForwardExtensions.cs`, `FTELSRCore.Shared/Infrastructure/Extensions/Helpers/ConnectionPoolRedisExtensions/ConnectionPoolRedisExtensions.cs`
> Loai: class (`IPAddressForwardExtensions` — `DelegatingHandler`), class (`UserAgentForwardExtensions` — `DelegatingHandler`), class (`ConnectionPoolRedisExtensions` — `IDisposable`)
> Cap nhat theo commit: `89c1ce9`

## 1. Tong quan

Module nay gom 3 lop khong lien quan truc tiep ve mat runtime nhung cung nam trong tang ha tang
(`Infrastructure/Extensions/Helpers`) cua `FTELSRCore.Shared`:

- `IPAddressForwardExtensions` va `UserAgentForwardExtensions` la hai `DelegatingHandler` dung de gan vao
  pipeline cua `HttpClient` (outbound HTTP call) nham sao chep gia tri `X-Forwarded-For` va `User-Agent`
  tu request HTTP dang xu ly (inbound, qua `IHttpContextAccessor`) sang request di ra (outbound).
- `ConnectionPoolRedisExtensions` la mot pool ket noi Redis tu quan ly (khong dung DI container de tao
  tung ket noi) dua tren mang `Lazy<IConnectionMultiplexer>`, chon ket noi theo round-robin.

Ca 3 lop deu la thanh phan ha tang cap thap (infrastructure helper), duoc cac tang phia tren (vi du
`HttpClient` handler pipeline hoac lop truy cap cache) su dung gian tiep. **Khong tim thay bat ky diem
dang ky DI (`AddHttpMessageHandler`, `services.AddSingleton<ConnectionPoolRedisExtensions>`, v.v.) hoac
diem goi constructor nao cho ca 3 lop trong pham vi repo `sr-core-helper`** (da `grep` toan repo, xem muc
1.2 va muc 3) — cau hinh thuc te (gia tri `poolSize`, `ConfigurationOptions`, gia tri `userAgent` mac
dinh, vong doi DI) do repo tieu thu (consumer) quyet dinh, **khong xac dinh duoc tu source code trong
repo nay**.

### 1.1 Pham vi chuc nang

| Lam duoc | Khong lam duoc |
|---|---|
| Doc IP client tu header inbound (`Forwarded`, `X-Forwarded-For`, `X-Real-IP`) qua `ConvertHelpers.GetClientIpAddress` va ghi de header `X-Forwarded-For` tren request outbound (IPAddressForwardExtensions.cs:33-43) | Khong xac thuc nguon goc cua cac header IP nay (khong kiem tra reverse proxy tin cay, khong co danh sach `KnownProxies`/`KnownNetworks`) — xem "Gioi han" |
| Doc `User-Agent`/`UserAgent` tu header inbound qua `ConvertHelpers.GetUserAgent`, fallback ve gia tri `_userAgent` truyen qua constructor neu header rong, roi ghi de header `User-Agent` outbound (UserAgentForwardExtensions.cs:36-52) | Khong co co che lay `_userAgent` mac dinh tu cau hinh ung dung ben trong lop nay — gia tri nay phai do caller truyen vao constructor |
| Cap mot `IConnectionMultiplexer` theo round-robin tu mot pool kich thuoc co dinh, khoi tao luoi (`Lazy`) (ConnectionPoolRedisExtensions.cs:22-29) | Khong tu dong do lai (reconnect) hay thay the mot `IConnectionMultiplexer` da bi loi/mat ket noi trong pool — moi phan tu `Lazy` chi duoc tao mot lan, khong co health-check |
| Giai phong dung nhung ket noi Redis da thuc su duoc khoi tao (`IsValueCreated`) khi `Dispose()` (ConnectionPoolRedisExtensions.cs:40-46) | Khong co gioi han tren cho `poolSize` (chi co san toi thieu la 1 qua `Math.Max(1, poolSize)`, ConnectionPoolRedisExtensions.cs:13) |
| Bat exception va log lai (khong throw ra ngoai pipeline HTTP) o ca 2 handler forward header (IPAddressForwardExtensions.cs:45-48, UserAgentForwardExtensions.cs:54-57) | Khong co cau hinh timeout rieng trong `ConnectionPoolRedisExtensions` — timeout Redis (neu co) nam trong `ConfigurationOptions` do caller truyen vao, khong xac dinh duoc gia tri that tu 3 file nguon nay |

### 1.2 Dependency

| Thanh phan | Muc dich su dung |
|---|---|
| `Microsoft.AspNetCore.Http` (`IHttpContextAccessor`, `HttpContext`) | Lay `HttpContext` cua request inbound dang xu ly de doc header IP/User-Agent |
| `FTELSRCore.Helpers.ConvertHelpers` (`GetClientIpAddress`, `GetUserAgent`) | Ham tien ich tach IP/User-Agent tu header cua `HttpContext` (doc thang tu `Request.Headers`, khong xac thuc nguon) |
| `FTELSRCore.Constants.HeaderConstant` (`ForwardedHeaderKey`, `UserAgentHeaderKey`) | Hang so ten header dung de ghi len request outbound (`X-Forwarded-For`, `User-Agent`) |
| `FTELSRCore.Extensions.Loggers.LoggerExtensions.ErrorException` | Extension method ghi log loi co cau truc khi `SendAsync` bat duoc exception |
| `Microsoft.Extensions.Logging.ILogger<T>` | Logger duoc inject qua constructor cho tung handler |
| `System.Net.Http` (`DelegatingHandler`, `HttpRequestMessage`, `HttpResponseMessage`) | Nen tang handler pipeline cua `HttpClient` |
| `StackExchange.Redis` (`ConfigurationOptions`, `IConnectionMultiplexer`, `ConnectionMultiplexer.Connect`) | Tao va quan ly ket noi Redis that trong `ConnectionPoolRedisExtensions` |
| `System.Threading.Interlocked` | Tang chi so round-robin an toan giua nhieu thread trong `GetConnection()` |
| `System.ObjectDisposedException.ThrowIf` | Guard chong goi `GetConnection()` sau khi pool da `Dispose()` |

### 1.3 Danh muc API

| API | Nhom | Mo ta ngan |
|---|---|---|
| `IPAddressForwardExtensions(ILogger<IPAddressForwardExtensions>, IHttpContextAccessor)` | Constructor | Khoi tao handler forward IP, nhan logger va context accessor qua DI |
| `IPAddressForwardExtensions.SendAsync(HttpRequestMessage, CancellationToken)` | Override (`protected`) | Ghi de header `X-Forwarded-For` tren request outbound truoc khi gui |
| `UserAgentForwardExtensions(ILogger<UserAgentForwardExtensions>, string, IHttpContextAccessor)` | Constructor | Khoi tao handler forward User-Agent, nhan logger, User-Agent mac dinh (chuoi tho, khong qua `IOptions`), va context accessor |
| `UserAgentForwardExtensions.SendAsync(HttpRequestMessage, CancellationToken)` | Override (`protected`) | Ghi de header `User-Agent` tren request outbound truoc khi gui |
| `ConnectionPoolRedisExtensions(ConfigurationOptions, int)` | Constructor | Tao pool `Lazy<IConnectionMultiplexer>` kich thuoc `Math.Max(1, poolSize)` |
| `ConnectionPoolRedisExtensions.GetConnection()` | Method (`public`) | Tra ve mot `IConnectionMultiplexer` trong pool theo round-robin, khoi tao luoi khi lan dau duoc chon |
| `ConnectionPoolRedisExtensions.Dispose()` | Method (`public`, `IDisposable`) | Giai phong moi `IConnectionMultiplexer` da duoc khoi tao trong pool |

## 2. Chi tiet API

### 2.1 IPAddressForwardExtensions (constructor)

**Signature**
```csharp
public IPAddressForwardExtensions(
    ILogger<IPAddressForwardExtensions> logger,
    IHttpContextAccessor httpContextAccessor)
```

**Muc dich** - Khoi tao handler, luu `logger` va `httpContextAccessor` vao field private de dung trong
`SendAsync` (IPAddressForwardExtensions.cs:13-20). Khong co logic nao khac ngoai gan field.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `logger` | `ILogger<IPAddressForwardExtensions>` | Co | Khong validate null trong constructor | Khong co |
| `httpContextAccessor` | `IHttpContextAccessor` | Co | Khong validate null trong constructor | Khong co |

**Output** - Khong co (constructor).

**Dieu kien xu ly** - Gan truc tiep, khong co nhanh re.

**Side effect** - Khong co (chi gan field).

**Error handling** - Khong co try/catch; neu `logger`/`httpContextAccessor` la `null`, constructor van
chay thanh cong (khong throw ngay), loi `NullReferenceException` se chi xay ra khi `SendAsync` truy cap
`_httpContextAccessor.HttpContext` — nhung khi do lai bi `catch (Exception exception)` trong `SendAsync`
bat lai (xem 2.2), nen hanh vi cuoi cung la request outbound van duoc gui voi header rong, khong throw ra
ngoai.

**Khi nao NEN dung** - Khi dang ky `IPAddressForwardExtensions` lam `DelegatingHandler` cho mot
`HttpClient` can forward IP client goc sang service downstream, thong qua DI container (khong tim thay vi
du dang ky that trong repo nay).

**Khi nao KHONG dung** - Khi khoi tao thu cong (`new IPAddressForwardExtensions(...)`) ngoai vong doi DI
ma khong dam bao `IHttpContextAccessor` co `HttpContext` hop le tai thoi diem goi `SendAsync` (vi du chay
ngoai request HTTP, background job) — khi do `GetClientIpAddress` se nhan `HttpContext == null`.

**Gioi han** - Khong guard-clause cho tham so null.

### 2.2 IPAddressForwardExtensions.SendAsync

**Signature**
```csharp
protected override Task<HttpResponseMessage> SendAsync(
    HttpRequestMessage request, CancellationToken cancellationToken)
```

**Muc dich** - Truoc khi request outbound duoc gui tiep trong pipeline (`base.SendAsync`), doc IP client
tu request inbound hien tai va ghi de len header `X-Forwarded-For` cua request outbound
(IPAddressForwardExtensions.cs:24-51).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `request` | `HttpRequestMessage` | Co | Khong null-check truc tiep; neu `null`, `request.Headers.Remove(...)` (dong 41) se nem `NullReferenceException`, bi `catch` bat lai (dong 45-48) | Khong co |
| `cancellationToken` | `CancellationToken` | Co | Duoc kiem tra ngay dau bang `ThrowIfCancellationRequested()` (dong 29) | Khong co |

**Output** - `Task<HttpResponseMessage>`: luon tra ket qua cua `base.SendAsync(request, cancellationToken)`
(dong 50) — nghia la **response that cua request outbound**, khong phu thuoc viec phan ghi header o tren
thanh cong hay that bai (khoi `try/catch` chi bao quanh phan chinh sua header, khong bao `base.SendAsync`).
Neu `cancellationToken` da bi huy, `ThrowIfCancellationRequested()` nem `OperationCanceledException` —
exception nay bi `catch (Exception exception)` (dong 45) bat lai va chi log, **khong throw tiep**, sau do
ham van tiep tuc goi `base.SendAsync(request, cancellationToken)` o dong 50 (nam ngoai try/catch) — tuc la
viec huy token bi "nuot" o buoc ghi header, nhung `base.SendAsync` van duoc goi va co the tu nem
`OperationCanceledException`/`TaskCanceledException` rieng cua no neu handler ben duoi ton trong token.

**Dieu kien xu ly** (theo thu tu thuc thi)
1. `cancellationToken.ThrowIfCancellationRequested()` (dong 29).
2. Khoi tao `ipAddress = string.Empty` (dong 31).
3. Goi `ConvertHelpers.GetClientIpAddress(_httpContextAccessor.HttpContext)` (dong 33-34) — ham nay tu
   bat exception noi bo va tra `string.Empty` khi loi hoac `httpContext == null` (xem
   `ConvertHelpers.cs:51-95`).
4. Neu ket qua khong rong/khong whitespace → `ipAddress = getIPAddress` (dong 36-39); nguoc lai giu
   `ipAddress = string.Empty`.
5. `request.Headers.Remove(HeaderConstant.ForwardedHeaderKey)` — xoa header `X-Forwarded-For` cu neu co
   (dong 41).
6. `request.Headers.TryAddWithoutValidation(HeaderConstant.ForwardedHeaderKey, ipAddress)` — them lai
   header, **luon thuc hien du `ipAddress` rong** (dong 43) → request outbound luon co header
   `X-Forwarded-For` (co the la chuoi rong).
7. Bat ky exception nao trong buoc 1-6 → nhanh `catch` (dong 45-48): log loi qua
   `_logger.ErrorException(nameof(IPAddressForwardExtensions), nameof(SendAsync), e: exception)`, khong
   re-throw.
8. Luon ket thuc bang `return base.SendAsync(request, cancellationToken)` (dong 50), nam ngoai
   try/catch, khong bi anh huong boi loi o buoc 1-6.

**Side effect** - Mutate header cua `request` (tham so dau vao, la object dung chung trong pipeline
`HttpClient`); ghi log loi qua `_logger.ErrorException` khi co exception. Khong ghi DB, khong goi service
ngoai nao khac ngoai `base.SendAsync`.

**Error handling** - Bat `Exception` (bao gom `OperationCanceledException` do `ThrowIfCancellationRequested`
nem ra) tai khoi `try/catch` bao quanh phan doc/ghi header; log lai bang `LoggerExtensions.ErrorException`,
khong throw lai, khong tra early — ham luon tiep tuc goi `base.SendAsync`.

**Khi nao NEN dung** - Khi can dam bao moi request outbound qua `HttpClient` mang theo IP client goc cua
request inbound (vi du de service downstream ghi log/audit theo IP nguoi dung that).

**Khi nao KHONG dung** - Trong moi truong khong co reverse proxy dang tin cay dung truoc, hoac chua cau
hinh middleware `ForwardedHeaders`/whitelist IP proxy — xem "Gioi han" ve rui ro gia mao.

**Gioi han**
- **Rui ro gia mao (spoofing) IP nghiem trong**: `ConvertHelpers.GetClientIpAddress` (duoc goi tai dong
  34) doc thang gia tri tu header HTTP do client gui len (`Forwarded`, `X-Forwarded-For`, `X-Real-IP` —
  theo thu tu uu tien tai `ConvertHelpers.cs:61-89`), **khong co bat ky buoc xac thuc nguon goc nao**
  (khong kiem tra IP cua reverse proxy gui request, khong dung danh sach `KnownProxies`/`KnownNetworks`
  cua ASP.NET Core `ForwardedHeadersOptions`). Da `grep` toan repo `sr-core-helper` cho
  `UseForwardedHeaders`, `ForwardedHeadersOptions`, `KnownProxies`, `KnownNetworks` — **khong tim thay ket
  qua nao**, tuc la khong co middleware chuan hoa/xac thuc forwarded-header nao duoc cau hinh trong repo
  nay. Neu ung dung tieu thu (consumer) module nay cung khong tu cau hinh viec loc theo reverse proxy tin
  cay truoc khi request di toi day, mot client bat ky co the tu chen header `X-Forwarded-For`/`Forwarded`/
  `X-Real-IP` voi gia tri tuy y, va gia tri do se duoc `IPAddressForwardExtensions` forward tiep cho
  service downstream nhu the la IP da duoc xac thuc.
- Header `X-Forwarded-For` outbound luon duoc set (ke ca rong) — service downstream khong co cach phan
  biet "khong xac dinh duoc IP" voi "IP rong do loi doc header" chi dua vao viec header co ton tai hay
  khong.
- Khong co gioi han do dai hay ky tu hop le cho gia tri ghi vao header (`TryAddWithoutValidation` bo qua
  validate cu phap header cua .NET) — co the ghi gia tri header khong hop le theo RFC neu `getIPAddress`
  chua ky tu dac biet (`ConvertHelpers.GetClientIpAddress` khong escape/validate).
- Loi khi huy `cancellationToken` bi "nuot" o buoc ghi header (bi log roi bo qua) — hanh vi huy thuc te
  chi co tac dung o loi goi `base.SendAsync` phia duoi, khong dung som phan code ghi header.

### 2.3 UserAgentForwardExtensions (constructor)

**Signature**
```csharp
public UserAgentForwardExtensions(
    ILogger<UserAgentForwardExtensions> logger,
    string userAgent,
    IHttpContextAccessor httpContextAccessor)
```

**Muc dich** - Khoi tao handler, luu `logger`, `userAgent` (gia tri User-Agent mac dinh dung lam fallback)
va `httpContextAccessor` vao field private (UserAgentForwardExtensions.cs:15-25).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `logger` | `ILogger<UserAgentForwardExtensions>` | Co | Khong validate null | Khong co |
| `userAgent` | `string` | Co | Khong validate null/rong tai constructor; la tham so kieu `string` tho — **khong** qua `IOptions<T>` hay cau hinh (`appsettings`) trong lop nay | Khong co |
| `httpContextAccessor` | `IHttpContextAccessor` | Co | Khong validate null | Khong co |

**Output** - Khong co (constructor).

**Dieu kien xu ly** - Gan truc tiep, khong co nhanh re.

**Side effect** - Khong co.

**Error handling** - Khong co try/catch.

**Khi nao NEN dung** - Khi caller can cung cap mot chuoi User-Agent mac dinh co dinh (vi du ten/dinh danh
service) de dung khi request inbound khong co header `User-Agent`/`UserAgent`.

**Khi nao KHONG dung** - Khong phu hop neu muon User-Agent mac dinh co the thay doi dong theo cau hinh ma
khong khoi tao lai handler, vi `userAgent` duoc chot tai thoi diem constructor chay (khong doc lai tu
`IOptionsMonitor` hay tuong tu).

**Gioi han** - Vi `userAgent` la tham so `string` thuan (khong phai factory/`IOptions`), viec tao instance
nay qua DI container tieu chuan (`services.AddTransient<UserAgentForwardExtensions>()`) se **khong tu
resolve duoc** tru khi consumer dang ky factory tuy chinh de cung cap gia tri `userAgent` — cach dang ky
that khong co trong repo `sr-core-helper` nay, **khong xac dinh duoc tu source code**.

### 2.4 UserAgentForwardExtensions.SendAsync

**Signature**
```csharp
protected override Task<HttpResponseMessage> SendAsync(
    HttpRequestMessage request, CancellationToken cancellationToken)
```

**Muc dich** - Truoc khi gui request outbound, doc `User-Agent` tu request inbound hien tai; neu khong
co, dung `_userAgent` (gia tri truyen vao constructor); roi ghi de header `User-Agent` cua request
outbound (UserAgentForwardExtensions.cs:29-60).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `request` | `HttpRequestMessage` | Co | Khong null-check truc tiep; loi neu `null` bi `catch` o dong 54-57 | Khong co |
| `cancellationToken` | `CancellationToken` | Co | Kiem tra bang `ThrowIfCancellationRequested()` (dong 34) | Khong co |

**Output** - `Task<HttpResponseMessage>`: luon la ket qua cua `base.SendAsync(request, cancellationToken)`
(dong 59), nam ngoai try/catch — giong hoan toan cau truc cua `IPAddressForwardExtensions.SendAsync` (xem
2.2).

**Dieu kien xu ly** (theo thu tu thuc thi)
1. `cancellationToken.ThrowIfCancellationRequested()` (dong 34).
2. Goi `ConvertHelpers.GetUserAgent(_httpContextAccessor.HttpContext)` (dong 38-39) — ham nay kiem tra
   header `"User-Agent"` truoc, sau do `"UserAgent"`, tra `string.Empty` neu khong co hoac loi
   (`ConvertHelpers.cs:21-43`).
3. Neu ket qua khong rong/khong whitespace → `userAgent = getUserAgent` (dong 41-44); nguoc lai
   → `userAgent = _userAgent` (gia tri constructor, dong 45-48).
4. `request.Headers.Remove(HeaderConstant.UserAgentHeaderKey)` (dong 50).
5. `request.Headers.TryAddWithoutValidation(HeaderConstant.UserAgentHeaderKey, userAgent)` (dong 52) —
   luon thuc hien, ke ca khi ca `getUserAgent` va `_userAgent` deu rong/`null` (khong co kiem tra them o
   buoc nay).
6. Exception o buoc 1-5 → `catch (Exception exception)` (dong 54-57): log qua
   `_logger.ErrorException(nameof(UserAgentForwardExtensions), nameof(SendAsync), e: exception)`, khong
   re-throw.
7. `return base.SendAsync(request, cancellationToken)` (dong 59), luon thuc thi.

**Side effect** - Mutate header `request.Headers` (tham so dung chung trong pipeline); ghi log loi khi co
exception.

**Error handling** - Giong `IPAddressForwardExtensions.SendAsync`: bat moi `Exception`, log, khong
throw lai, luon tiep tuc goi `base.SendAsync`.

**Khi nao NEN dung** - Khi muon request outbound mang theo `User-Agent` cua client goc (neu co) hoac mot
gia tri mac dinh co dinh do service tu dinh danh khi request inbound khong co `User-Agent`.

**Khi nao KHONG dung** - Khi can phan biet ro "khong co User-Agent" voi "User-Agent rong" o downstream —
logic hien tai luon set header (khong bo qua khi rong).

**Gioi han**
- Neu `_userAgent` (constructor) la `null`/rong **va** request inbound cung khong co
  `User-Agent`/`UserAgent`, header outbound se duoc set thanh gia tri rong/`null` (`TryAddWithoutValidation`
  voi `value = null` — hanh vi thuc te cua `HttpHeaders.TryAddWithoutValidation` khi `value` la `null` la
  van them duoc nhung khong co validate; **khong xac dinh duoc hanh vi runtime chinh xac cho truong hop
  `userAgent == null`** tu 3 file nguon nay, vi phu thuoc implementation cua `HttpHeaders` trong BCL — can
  test thuc te neu can khang dinh).
- Khong co gioi han ky tu/do dai cho gia tri `User-Agent` duoc forward — cung giong
  `IPAddressForwardExtensions`, dung `TryAddWithoutValidation`.
- Cung van de "nuot" loi huy token nhu `IPAddressForwardExtensions.SendAsync`.

### 2.5 ConnectionPoolRedisExtensions (constructor)

**Signature**
```csharp
public ConnectionPoolRedisExtensions(ConfigurationOptions configurationOptions, int poolSize)
```

**Muc dich** - Tao mot mang `Lazy<IConnectionMultiplexer>` kich thuoc `Math.Max(1, poolSize)`, moi phan tu
la mot factory luoi se goi `ConnectionMultiplexer.Connect(configurationOptions)` khi duoc truy cap lan dau
(ConnectionPoolRedisExtensions.cs:11-20). **Khong co ket noi Redis that nao duoc mo tai thoi diem goi
constructor** — viec ket noi chi xay ra khi `GetConnection()` truy cap `.Value` cua mot `Lazy` chua duoc
khoi tao.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `configurationOptions` | `ConfigurationOptions` (StackExchange.Redis) | Co | Khong null-check trong constructor; neu `null`, loi chi phat sinh khi `Lazy.Value` duoc truy cap lan dau trong `ConnectionMultiplexer.Connect(null)` (khong duoc bat o dau trong lop nay — xem "Error handling") | Khong co |
| `poolSize` | `int` | Co | Khong co san/tran validate o tham so; duoc kep toi thieu ve 1 khi tao mang qua `Math.Max(1, poolSize)` (dong 13) — `poolSize <= 0` van hop le, tu dong thanh pool kich thuoc 1; khong co gioi han tren | Khong co |

**Output** - Khong co (constructor); instance sau khi tao co `_pool` la mang `Lazy<IConnectionMultiplexer>`
do dai `Math.Max(1, poolSize)`, tat ca deu **chua duoc khoi tao gia tri** (`IsValueCreated == false`).

**Dieu kien xu ly** - Vong `for` tao tung `Lazy` voi factory `() => ConnectionMultiplexer.Connect(configurationOptions)`
(dong 15-19); khong co nhanh re khac.

**Side effect** - Khong co side effect ngoai (khong mo ket noi Redis tai buoc nay).

**Error handling** - Khong co try/catch trong constructor.

**Khi nao NEN dung** - Khi can mot pool co dinh nhieu ket noi `IConnectionMultiplexer` toi cung mot Redis
endpoint de phan tai theo round-robin, thay vi dung mot `IConnectionMultiplexer` singleton duy nhat.

**Khi nao KHONG dung** - Khi chi can mot ket noi Redis singleton (StackExchange.Redis multiplexer da tu
da luong/multiplex noi bo tren mot ket noi) — tao nhieu multiplexer khong nhat thiet cai thien hieu nang va
ton them tai nguyen ket noi toi Redis server, tuy cau hinh instance Redis.

**Gioi han**
- Khong xac dinh duoc gia tri that cua `poolSize`, timeout, hay bat ky truong nao trong
  `configurationOptions` (vi du `ConnectTimeout`, `SyncTimeout`, `AbortOnConnectFail`) tu 3 file nguon
  thuoc pham vi tai lieu nay, vi **khong tim thay diem goi constructor `ConnectionPoolRedisExtensions(...)`
  nao trong repo `sr-core-helper`** (da `grep` toan repo) — cac gia tri nay do project tieu thu truyen vao
  khi khoi tao, **khong xac dinh duoc tu source code trong repo nay**.
- Khong validate `configurationOptions != null` — neu `null`, loi chi no ra tre (khi `GetConnection()`
  truy cap `.Value` lan dau), khong no ngay tai constructor, co the gay kho truy vet nguon goc loi.

### 2.6 ConnectionPoolRedisExtensions.GetConnection

**Signature**
```csharp
public IConnectionMultiplexer GetConnection()
```

**Muc dich** - Tra ve mot `IConnectionMultiplexer` trong pool theo co che round-robin, khoi tao ket noi
Redis that (qua `Lazy.Value`) neu phan tu duoc chon chua duoc khoi tao truoc do
(ConnectionPoolRedisExtensions.cs:22-29).

**Input hop le** - Khong co tham so.

**Output** - `IConnectionMultiplexer`: ket noi duoc chon trong pool (khong bao gio `null` trong dieu kien
binh thuong, vi `ConnectionMultiplexer.Connect` hoac tra ve multiplexer hoac throw). Neu pool da bi
`Dispose()`, ham khong tra gia tri ma nem `ObjectDisposedException` (xem duoi).

**Dieu kien xu ly**
1. `ObjectDisposedException.ThrowIf(_disposed, this)` (dong 24) — neu `Dispose()` da duoc goi truoc do,
   nem ngay `ObjectDisposedException`, khong tiep tuc.
2. `int next = Interlocked.Increment(ref _index)` (dong 26) — tang bo dem dung chung giua moi thread mot
   cach atomic; `_index` khoi tao la `-1` (dong 8) nen lan goi dau tien `next == 0`.
3. `return _pool[(next & 0x7FFFFFFF) % _pool.Length].Value` (dong 28) — dung `& 0x7FFFFFFF` de xoa bit dau
   truoc khi lay `%`, tranh ket qua am khi `_index` (kieu `int`) tran so (overflow) qua gia tri am sau
   nhieu lan `Increment` lien tuc (khong throw khi overflow vi phep cong int trong ngu canh nay khong
   dung `checked`). Truy cap `.Value` cua `Lazy` se chay factory `ConnectionMultiplexer.Connect(...)` neu
   day la lan dau phan tu nay duoc chon.

**Side effect** - Co the mo ket noi Redis that (I/O ra ngoai) neu phan tu `Lazy` duoc chon chua duoc khoi
tao truoc do. Mutate `_index` (state noi bo dung chung, thread-safe qua `Interlocked`).

**Error handling** - Khong co try/catch trong ham nay. `ObjectDisposedException` duoc nem co chu dich qua
`ThrowIf`. Neu `ConnectionMultiplexer.Connect` that bai (vi du Redis khong ket noi duoc), exception tu
StackExchange.Redis (vi du `RedisConnectionException`) se lan thang ra caller — **khong bi bat hay log o
lop nay**; theo co che cua `Lazy<T>` mac dinh (`LazyThreadSafetyMode.ExecutionAndPublication`), neu factory
nem exception, exception do se duoc cache lai va nem lai o **moi** lan truy cap `.Value` tiep theo cho
dung phan tu do (khong tu thu ket noi lai) — day la he qua cua viec dung `Lazy` mac dinh, khong phai logic
tuong minh trong file nay.

**Khi nao NEN dung** - Moi khi can lay mot `IConnectionMultiplexer` de thuc hien lenh Redis, muon phan tai
deu qua nhieu multiplexer.

**Khi nao KHONG dung** - Trong doan code can dam bao luon lay dung cung mot ket noi cho mot logical
session Redis cu the — co che round-robin o day khong dam bao tinh "sticky" giua cac lan goi.

**Gioi han**
- Neu mot phan tu trong pool tung ket noi loi (factory nem exception), phan tu do **se tiep tuc nem loi
  da cache o moi lan bi chon tiep theo** (do ban chat `Lazy<T>`), lam giam hieu qua round-robin dan theo
  thoi gian neu Redis tung gian doan trong luc mot phan tu pool dang duoc khoi tao lan dau — **khong co
  co che retry hoac loai phan tu loi khoi vong round-robin** trong file nay.
- Khong co health-check dinh ky cho cac `IConnectionMultiplexer` da tao — mot ket noi bi dut sau khi tao
  thanh cong van duoc tra ve nguyen trang boi `GetConnection()` (viec tu hoi phuc ket noi, neu co, hoan
  toan do StackExchange.Redis tu xu ly noi bo, khong phai logic cua lop nay).
- **Race condition giua `GetConnection()` va `Dispose()` chay dong thoi tren nhieu thread**: dong 24
  (`ObjectDisposedException.ThrowIf(_disposed, this)`) chi kiem tra `_disposed` tai mot thoi diem, khong
  co `lock`/dong bo nao giu nguyen trang thai do cho toi khi `.Value` duoc truy cap o dong 28. Neu mot
  thread dang thuc thi `GetConnection()` vua qua duoc buoc kiem tra (khi `_disposed` con `false`) thi mot
  thread khac goi `Dispose()` xong ngay sau do, co hai hau qua co the xay ra tuy vao phan tu `_pool[i]`
  duoc chon: (1) neu phan tu do da `IsValueCreated == true` tu truoc, `Dispose()` se goi
  `lazy.Value.Dispose()` tren dung multiplexer ma `GetConnection()` sap tra ve hoac vua tra ve cho caller
  — caller co the nhan ve (hoac dang dung) mot `IConnectionMultiplexer` da bi dispose, dan toi loi khi
  thuc thi lenh Redis tiep theo tren no; (2) neu phan tu do **chua** duoc khoi tao va vong `foreach` cua
  `Dispose()` (dong 40-46) da di qua chi so do truoc khi `.Value` duoc truy cap, mot ket noi Redis moi se
  duoc mo **sau khi** `Dispose()` da hoan tat — ket noi nay se khong bao gio duoc dispose (ro ri ket noi).
  Day la he qua truc tiep cua viec khong co co che dong bo (`lock`, `SemaphoreSlim`, ...) giua hai ham nay
  trong `ConnectionPoolRedisExtensions.cs`, khong phai suy dien ngoai source code.

### 2.7 ConnectionPoolRedisExtensions.Dispose

**Signature**
```csharp
public void Dispose()
```

**Muc dich** - Giai phong moi `IConnectionMultiplexer` da thuc su duoc khoi tao trong pool, danh dau pool
da dispose de cac lan goi `GetConnection()` sau do bi chan (ConnectionPoolRedisExtensions.cs:31-47).

**Input hop le** - Khong co tham so.

**Output** - `void`.

**Dieu kien xu ly**
1. Neu `_disposed == true` → `return` ngay, khong lam gi them (dong 33-36) — goi `Dispose()` nhieu lan la
   an toan (idempotent).
2. Dat `_disposed = true` (dong 38).
3. Lap qua tung `Lazy` trong `_pool`; neu `lazy.IsValueCreated == true` → goi `lazy.Value.Dispose()`
   (dong 40-46) — **chi dispose nhung ket noi da thuc su duoc tao**, khong truy cap `.Value` cua cac phan
   tu chua khoi tao (tranh vo tinh mo ket noi Redis moi chi de dispose ngay).

**Side effect** - Dong ket noi Redis that (I/O) cho moi multiplexer da tao. Mutate `_disposed`.

**Error handling** - Khong co try/catch; neu `lazy.Value.Dispose()` (dispose cua
`IConnectionMultiplexer`/`ConnectionMultiplexer`) nem exception, exception do lan thang ra caller va co the
lam dung vong lap `foreach`, khien cac phan tu con lai trong `_pool` **khong duoc dispose** (khong co
try/catch rieng cho tung phan tu trong vong lap).

**Khi nao NEN dung** - Khi ket thuc vong doi cua `ConnectionPoolRedisExtensions` (vi du khi ung dung dung,
hoac scope DI ket thuc neu dang ky scoped/singleton disposal).

**Khi nao KHONG dung** - Khong goi lai `GetConnection()` sau khi da `Dispose()` — se luon nem
`ObjectDisposedException` (xem 2.6, buoc 1).

**Gioi han**
- Neu dispose mot phan tu giua vong lap nem exception, cac phan tu con lai trong `_pool` khong duoc
  dispose (ro ri ket noi Redis tiem an) — khong co `try/catch` bao ve tung phan tu.
- Khong implement pattern `Dispose(bool disposing)` / `~ConnectionPoolRedisExtensions()` (finalizer) tieu
  chuan cua .NET (`IDisposable` day du) — neu caller quen goi `Dispose()`, khong co co che tu dong don dep
  o GC.
- Khong co `lock`/dong bo nao giua `Dispose()` va `GetConnection()` chay tren thread khac — xem chi tiet
  race condition tai "Gioi han" cua muc 2.6 (`GetConnection`): mot multiplexer co the bi dispose ngay sau
  khi duoc tra ve cho caller khac, hoac mot ket noi moi co the duoc mo sau khi vong `foreach` dispose
  (dong 40-46) da chay qua, gay ro ri ket noi.

## 3. Van de da biet

| # | Van de | Vi tri | Anh huong |
|---|---|---|---|
| 1 | `ConvertHelpers.GetClientIpAddress` doc thang `Forwarded`/`X-Forwarded-For`/`X-Real-IP` tu header HTTP client gui len, khong xac thuc nguon (khong co `KnownProxies`/`KnownNetworks`, khong co middleware `UseForwardedHeaders` nao duoc tim thay trong repo) | `ConvertHelpers.cs:61-89` (duoc goi tu `IPAddressForwardExtensions.cs:33-34`) | `IPAddressForwardExtensions` co the forward mot IP gia mao do client tu chen header sang service downstream, khien downstream tin nham day la IP da duoc xac thuc qua reverse proxy — rui ro bao mat ro rang neu khong co tang loc IP nguon phia truoc ung dung tieu thu module nay |
| 2 | Khong tim thay diem dang ky DI hoac diem goi constructor cho ca 3 lop (`IPAddressForwardExtensions`, `UserAgentForwardExtensions`, `ConnectionPoolRedisExtensions`) trong repo `sr-core-helper` | Toan repo (ket qua `grep`) | Khong the xac nhan tu source code trong repo nay: gia tri `poolSize` that, noi dung `ConfigurationOptions` (bao gom timeout Redis), gia tri `userAgent` mac dinh, hay vong doi DI (singleton/scoped/transient) cua cac lop nay — moi phat bieu ve "gia tri that" phai ghi "khong xac dinh duoc tu source code" |
| 3 | `UserAgentForwardExtensions` nhan `userAgent` la tham so `string` tho trong constructor, khong qua `IOptions<T>` | `UserAgentForwardExtensions.cs:17,22` | Khong the tu dong resolve qua DI container tieu chuan (`AddTransient<UserAgentForwardExtensions>()`) ma khong co factory tuy chinh cung cap gia tri `userAgent`; cach cung cap gia tri nay khong co trong repo |
| 4 | `Dispose()` khong bao ve tung phan tu trong vong lap dispose — mot `IConnectionMultiplexer.Dispose()` nem exception se lam dung som, cac phan tu con lai trong `_pool` khong duoc dispose | `ConnectionPoolRedisExtensions.cs:40-46` | Ro ri tai nguyen ket noi Redis tiem an neu mot phan tu dispose loi |
| 5 | `GetConnection()` khong co co che loai bo hoac retry cho phan tu `Lazy` da cache loi ket noi — loi factory bi `Lazy<T>` cache va nem lai o moi lan truy cap `.Value` tiep theo cho dung phan tu do | `ConnectionPoolRedisExtensions.cs:22-29` (he qua cua hanh vi mac dinh `Lazy<T>`, khong phai logic tuong minh) | Mot phan tu pool tung gap loi ket noi mot lan se tiep tuc nem loi o moi luot round-robin sau do, lam giam hieu qua pool theo thoi gian neu khong restart ung dung |
| 6 | Ca 2 handler forward header luon ghi header outbound (ke ca gia tri rong) va "nuot" loi huy `cancellationToken` o buoc ghi header (log roi bo qua, khong dung som) | `IPAddressForwardExtensions.cs:27-48`, `UserAgentForwardExtensions.cs:32-57` | `base.SendAsync` van luon duoc goi ke ca khi phan ghi header that bai hoac token da bi huy tai buoc do — hanh vi huy thuc te phu thuoc hoan toan vao viec `base.SendAsync`/handler ben duoi co ton trong `cancellationToken` hay khong |
| 7 | `GetConnection()` (dong 22-29) va `Dispose()` (dong 31-47) khong co `lock`/dong bo nao giua hai ham khi chay tren nhieu thread dong thoi: `ObjectDisposedException.ThrowIf(_disposed, this)` chi kiem tra `_disposed` tai mot thoi diem, khong giu trang thai do toi khi `.Value` duoc truy cap | `ConnectionPoolRedisExtensions.cs:24-28`, `ConnectionPoolRedisExtensions.cs:31-47` | Mot `IConnectionMultiplexer` da duoc tra ve cho caller co the bi `Dispose()` tu thread khac ngay sau do (dung tiep se loi o lenh Redis ke tiep), hoac mot ket noi moi co the duoc mo sau khi vong `foreach` dispose da chay qua chi so do — ket noi nay se khong bao gio duoc dispose (ro ri ket noi Redis) |
| 8 | Doi chieu voi 8 file Knowledge Base hien co (`Utilizes-CallApiWithHttp.md`, `Utilizes-CallApi.md`, `Data-MongoDB-CoreMongoDB.md`, `Data-SQL-CoreSQL.md`, `Data-SQL-CoreSQL-TwoEntity.md`, `Data-SQL-UnitOfWork-DbContexts.md`, `Data-SQL-Dapper.md`, `Data-SQL-Resilience.md`) va toan bo `docs/knowledge-base/` hien tai | — | Khong co file KB cu nao de cap den `IPAddressForwardExtensions`, `UserAgentForwardExtensions`, `ConnectionPoolRedisExtensions`, hay cac kieu dung lai tu danh sach doi chieu bat buoc (`AuditModel`, `HttpOptionModel`, `ErrorModel`, `CustomException`, `ProjectToExtensions`, `PrecateBuilderExtensions`, `MeasureExecutionTimeExtensions.InvokeForHTTP`, `MongoResiliencePolicyFactory`, `BaseEntityMongoDB`/`BaseEntitySQL`) — module nay khong giao cat voi cac kieu do, nen khong phat hien mo ta sai/thieu nao o 8 file KB cu can ghi nhan tai day |
