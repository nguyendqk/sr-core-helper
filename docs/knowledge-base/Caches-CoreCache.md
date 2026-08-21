# Cache layer (CoreCacheExtension, Redis cache)

> Nguon: `FTELSRCore.Shared/Caches/CoreCacheExtension.cs`, `FTELSRCore.Shared/Caches/ICoreCacheExtension.cs`, `FTELSRCore.Shared/Caches/Helpers/CoreCacheHelper.cs`, `FTELSRCore.Shared/Caches/Redis/CoreRedisCacheExtension.cs`, `FTELSRCore.Shared/Caches/Redis/ICoreRedisCacheExtension.cs`
> Loai: class (`CoreCacheExtension`, `CoreRedisCacheExtension`) + interface (`ICoreCacheExtension`, `ICoreRedisCacheExtension`) + static class (`CoreCacheHelper`) + enum (`StepCache`)
> Cap nhat theo commit: `89c1ce9`

## 1. Tong quan

`CoreCacheExtension` la lop boc (wrapper) tren thu vien **ZiggyCreatures.Caching.Fusion** (`IFusionCache`), cung cap API get/set/xoa cache dang chuoi JSON cho toan he thong, voi kha nang chon tang cache (memory / Redis / ca hai) qua enum `StepCache`. `CoreCacheHelper` dinh nghia cau hinh mac dinh (TTL, fail-safe, timeout, circuit breaker) dung lam baseline cho moi loi goi khong truyen `options` rieng. `CoreRedisCacheExtension` la lop thao tac truc tiep tren `IConnectionMultiplexer` (StackExchange.Redis) de liet ke/xoa key theo pattern va chay Lua script. **Chi 2/4 method thuc su KHONG di qua FusionCache**: `GetAllKeyAsync` (chi liet ke key qua `IServer.Keys`) va `LUAAtomicCacheAsync` (chay `ScriptEvaluateAsync` truc tiep). Hai method con lai **CO di qua FusionCache** mot cach gian tiep: `GetAllDataAsync` goi `ICoreCacheExtension.GetCacheByKeyAsync` cho tung key (CoreRedisCacheExtension.cs:32-34) va `ClearDataWithKeys` goi `ICoreCacheExtension.ClearAllCacheAsync` (CoreRedisCacheExtension.cs:85) — ca hai deu la API cua `CoreCacheExtension` chay tren `IFusionCache`. Module nam o tang ha tang (Infrastructure/Shared), duoc cac tang nghiep vu phia tren inject qua `ICoreCacheExtension` / `ICoreRedisCacheExtension`.

Toan bo gia tri duoc cache trong `CoreCacheExtension` deu la `string` (JSON da serialize san) — kieu generic thuc te truyen cho `IFusionCache` luon la `<string>` (CoreCacheExtension.cs:92, CoreCacheExtension.cs:277), khong phai kieu doi tuong `TOut` goc. `TOut` chi duoc serialize/deserialize o tang `CoreCacheExtension`, khong phai o tang FusionCache/Redis.

### 1.1 Pham vi chuc nang

| Lam duoc | Khong lam duoc |
|---|---|
| Get-or-create cache voi TTL tuy bien theo phut, tu serialize/deserialize JSON (CoreCacheExtension.cs:28-167) | Khong cache kieu object goc truc tiep — luon ep qua JSON string truoc khi gui cho FusionCache |
| Chon tang cache ap dung qua `StepCache` (None/Local/Distributed) cho tung loi goi (CoreCacheExtension.cs:55-87) | Khong ho tro cache theo nhieu tang khac ngoai memory (L1) va Redis (L2) cua FusionCache |
| Fail-open khi Redis/FusionCache loi: log loi roi tra `null`/`string.Empty`, khong throw ra ngoai o hau het method (CoreCacheExtension.cs:161-166, 307-312, 399-402, 481-484, 582-585) | Khong tu dong retry hoac fallback sang nguon du lieu khac ngoai `func` do caller cung cap |
| Khong cache lai ket qua rong/null/`"{}"`/`"[]"` (tranh cache "poison") (CoreCacheExtension.cs:101-110, 137-143, 197-203) | Khong co co che canh bao/metric rieng khi phat hien du lieu rong, chi don gian la bo ghi cache |
| Xoa 1 hoac nhieu key qua FusionCache (`RemoveAsync`) (CoreCacheExtension.cs:498-586) | Khong ho tro xoa theo pattern/wildcard o tang `CoreCacheExtension` (phai dung `CoreRedisCacheExtension` voi Redis SCAN) |
| Liet ke toan bo key/gia tri Redis theo pattern qua `IServer.Keys` (CoreRedisCacheExtension.cs:16-38) | Khong co co che fail-safe/try-catch o `CoreRedisCacheExtension` — loi ket noi Redis se nem exception thang ra ngoai |
| Chay Lua script atomically tren Redis (`ScriptEvaluateAsync`) (CoreRedisCacheExtension.cs:95-102) | Khong validate script/keys/values dau vao truoc khi gui toi Redis |

### 1.2 Dependency

| Thanh phan | Muc dich su dung |
|---|---|
| `ZiggyCreatures.Caching.Fusion` (`IFusionCache`, `FusionCacheEntryOptions`, `FusionCacheOptions`) | Engine cache 2 tang (memory L1 + distributed L2), fail-safe, soft/hard timeout, circuit breaker |
| `StackExchange.Redis` (`IConnectionMultiplexer`, `IServer`, `IDatabase`, `RedisResult`, `RedisKey`, `RedisValue`) | Truy cap truc tiep Redis server de SCAN key va chay Lua script (`CoreRedisCacheExtension`) |
| `FTELSRCore.Helpers.JSonParseHelpers` (`ToJSon`, `JSonTryParse`) | Serialize/deserialize object ⇄ JSON string bang System.Text.Json. **Fallback Newtonsoft.Json CHI ton tai o `ToJSon` (serialize)** khi gap `NotSupportedException` (JSonParseHelpers.cs:33-35) — `JSonTryParse` (deserialize, chuoi → `T`) **KHONG co fallback Newtonsoft**: moi exception khi `JsonSerializer.Deserialize<T>` that bai chi bi log roi tra `false`/`default` (JSonParseHelpers.cs:160-194), khong thu lai bang Newtonsoft |
| `FTELSRCore.Helpers.CancellationTokenHelper.CreateLinkedTokenWithTimeout` | Tao `CancellationTokenSource` lien ket token ngoai + timeout theo giay |
| `FTELSRCore.Extensions.Loggers.LoggerExtensions` (`Info`, `ConnectionErrorRedis`, `Request`, `Response`) | Ghi log Info/Error co cau truc (category `DB_REDIS` khi loi) |
| `System.Diagnostics.ActivitySource` (`OpenTelemetryConstant.CoreCacheActivitySource`) | Tao Activity/span cho OpenTelemetry tracing (`cache.get`, `cache.set`, `cache.clear`) |
| `Newtonsoft.Json.Linq.JToken` | Kiem tra JSON rong (`JsonIsNullOrEmpty`, dung noi bo, hien khong thay diem goi trong 5 file nay) |
| `FTELSRCore.Constants.DelimiterConstant.CHAR_COMMA` | Ky tu phan tach khi log danh sach key bi huy (`ClearAllCacheAsync`) |
| `FTELSRCore.Helpers.CollectionHelpers.IsNullOrEmpty<T>` | Kiem tra mang key null/rong an toan (null-safe) truoc khi xoa |

### 1.3 Danh muc API

| API | Nhom | Mo ta ngan |
|---|---|---|
| `CoreCacheExtension.GetOrCreateAsync<TOut>` | GET | Lay cache theo key, neu miss thi goi `func` de tao du lieu roi cache lai |
| `CoreCacheExtension.GetCacheByKeyAsync` | GET | Lay gia tri JSON string tho theo key, khong tu tao moi khi miss |
| `CoreCacheExtension.SetCacheByKeyAsync(key, value, expiredMinutes, ...)` | SET | Ghi cache voi TTL tinh bang phut (tu build `options` mac dinh/override) |
| `CoreCacheExtension.SetCacheByKeyAsync(key, value, options, ...)` | SET | Ghi cache voi `FusionCacheEntryOptions` do caller cung cap truc tiep (bat buoc) |
| `CoreCacheExtension.ClearAllCacheAsync` | DELETE | Xoa danh sach key (khu trung lap truoc khi xoa) |
| `CoreCacheExtension.IsFailSafeEnabled` *(private)* | CONFIG | Quyet dinh co bat fail-safe hay khong dua tren `Duration` |
| `CoreCacheExtension.FusionCacheEntryOptions` *(private)* | CONFIG | Build `FusionCacheEntryOptions` theo `expiredMinutes` + override tu options truyen vao |
| `CoreCacheExtension.GetResultAsync<TOut>` *(private)* | HELPER | Goi `func`, chuan hoa ket qua null/rong JSON thanh `default` |
| `CoreCacheExtension.ClearCacheAsync` *(private)* | HELPER | Xoa 1 key qua `fusionCache.RemoveAsync` |
| `CoreCacheHelper.FusionCacheOptionsDefault` | CONFIG | Tra ve `FusionCacheOptions` mac dinh (circuit breaker + entry options mac dinh) |
| `CoreCacheHelper.FusionCacheEntryOptionsDefault` | CONFIG | Tra ve `FusionCacheEntryOptions` mac dinh (TTL 5 phut, fail-safe, timeout) |
| `StepCache` (enum) | CONFIG | `None`/`Local`/`Distributed` — chon tang cache ap dung cho 1 loi goi |
| `CoreRedisCacheExtension.GetAllDataAsync` | REDIS | Liet ke toan bo key + gia tri theo pattern (SCAN + lay gia tri qua `ICoreCacheExtension`) |
| `CoreRedisCacheExtension.GetAllKeyAsync` | REDIS | Liet ke toan bo key theo pattern (chi key, khong lay gia tri) |
| `CoreRedisCacheExtension.ClearDataWithKeys` | REDIS | Xoa danh sach key (bo prefix instance roi goi `ClearAllCacheAsync`) |
| `CoreRedisCacheExtension.LUAAtomicCacheAsync` | REDIS | Chay Lua script atomically tren 1 database Redis |

## 2. Chi tiet API

### 2.1 GetOrCreateAsync&lt;TOut&gt;

**Signature**
```csharp
public async ValueTask<TOut> GetOrCreateAsync<TOut>(string key,
                                                    double expiredMinutes,
                                                    Func<ValueTask<TOut>> func,
                                                    StepCache step = StepCache.None,
                                                    FusionCacheEntryOptions options = null,
                                                    int cancellationTokenTime = 3,
                                                    CancellationToken cancellationToken = default) where TOut : class
```
**Muc dich** - Lay du lieu tu cache theo `key`; neu cache-miss (hoac rong/null), goi `func` de lay du lieu that, serialize sang JSON va ghi lai vao cache, sau do tra ve doi tuong da deserialize (CoreCacheExtension.cs:28-167). Day la API "get-or-create" duy nhat trong module co tu goi lai nguon du lieu khi miss.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `key` | `string` | Co | Neu null/whitespace → tra `null` ngay, khong goi `func` (CoreCacheExtension.cs:45-48) | — |
| `expiredMinutes` | `double` | Co | Neu ≤ 0 → ep ve 5 phut (CoreCacheExtension.cs:617) | — |
| `func` | `Func<ValueTask<TOut>>` | Co | Khong duoc null (khong co null-check — neu null se NRE khi factory chay) | — |
| `step` | `StepCache` | Khong | `Local`: chi memory (bo qua Redis doc/ghi); `Distributed`: chi Redis (bo qua memory doc/ghi); `None`: ca hai tang (CoreCacheExtension.cs:55-87) | `StepCache.None` |
| `options` | `FusionCacheEntryOptions` | Khong | Neu null → dung `CoreCacheHelper.FusionCacheEntryOptionsDefault().Duplicate()`; neu co → `Duplicate()` roi override `Duration`/`IsFailSafeEnabled`/`FailSafeMaxDuration` (CoreCacheExtension.cs:610-624) | `null` |
| `cancellationTokenTime` | `int` | Khong | Giay timeout cho `CancellationTokenSource` lien ket (CoreCacheExtension.cs:50-51) | `3` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` ngay dau ham (CoreCacheExtension.cs:36) | `default` |

**Output** - `ValueTask<TOut>`: tra object da deserialize khi co du lieu hop le trong cache hoac tu `func`; tra `null` khi: `key` rong, cache-miss va `func` tra ve null/JSON rong, JSON trong cache khong parse duoc thanh `TOut`, bi huy trong luc dang chay (`OperationCanceledException` bat duoc trong `try`), hoac bat ky exception khac (Redis loi, v.v.) xay ra sau khi vao `try`. **Luu y: tuyen bo "khong throw" chi dung cho phan than ham tu dong `try` tro di** — neu `cancellationToken` da bi huy **truoc khi** goi ham, dong `cancellationToken.ThrowIfCancellationRequested()` (CoreCacheExtension.cs:36) nam **truoc** `try` se nem `OperationCanceledException` thang ra `ValueTask` tra ve cho caller (khong bi catch noi bo) — xem muc 3, van de #11.

**Dieu kien xu ly** (theo thu tu thuc thi)
1. Kiem tra `cancellationToken` da bi huy chua → neu co thi `ThrowIfCancellationRequested()` nem ra ngay (khong bi bat trong `try`, vi nam truoc `try`).
2. Tao Activity tracing `cache.get`, gan tag `cache.key`, `cache.name`, `DisplayName = "GET {key}"`.
3. Neu `key` rong/whitespace → tra `null` (CoreCacheExtension.cs:45-48).
4. Build `options` theo `expiredMinutes` (xem muc 2.7 `FusionCacheEntryOptions`).
5. Theo `step`, set them `SkipDistributedCacheRead/Write`, `SkipMemoryCacheRead/Write`, `AllowBackgroundDistributedCacheOperations`, `DistributedCacheSoftTimeout`/`HardTimeout` (= `cancellationTokenTime`s / `cancellationTokenTime+1`s) (CoreCacheExtension.cs:55-87).
6. Goi `fusionCache.GetOrSetAsync<string>(key, factory, options, token)`:
   - Factory noi bo goi `GetResultAsync(func)` (muc 2.6) de lay `TOut`, serialize bang `ToJSon()`.
   - Neu JSON rong/`"null"`/`"{}"`/`"[]"` → set `ctx.Options.SkipDistributedCacheWrite = true` va `SkipMemoryCacheWrite = true`, tra `null` cho factory (khong cache gia tri rong) (CoreCacheExtension.cs:101-110).
7. Neu `resultString` rong hoac `JSonTryParse` that bai → tra `null` (CoreCacheExtension.cs:117-121).
8. Nguoc lai tra `result` da deserialize.

**Side effect** - Ghi cache (memory va/hoac Redis theo `step`) khi `func` tra du lieu hop le; ghi log `Info`/`ConnectionErrorRedis` khi co loi; tao Activity/span OpenTelemetry. Khong mutate tham so dau vao cua caller (chi mutate `options` cuc bo da `Duplicate()`).

**Error handling**
- `FusionCacheSerializationException` (loi (de)serialize o tang FusionCache): log `Info`, **bo qua cache, goi lai truc tiep `func`** qua `GetResultAsync`, serialize, roi tu goi `SetCacheByKeyAsync` de ghi lai cache, cuoi cung tra `result` (khong tra `null`) (CoreCacheExtension.cs:125-153). Day la duong xu ly duy nhat tu phuc hoi du lieu khi cache loi serialize.
- `OperationCanceledException`: log `Info` (message co `"OperationCanceledException {key} with SLA {cancellationTokenTime}s"`), tra `null` (CoreCacheExtension.cs:154-160).
- `Exception` (khac, gom ca loi ket noi Redis): log `ConnectionErrorRedis` (category `DB_REDIS`), tra `null` (CoreCacheExtension.cs:161-166). Day la hanh vi **fail-open**: khi Redis down, ham khong throw ma tra `null`.

**Khi nao NEN dung** - Khi can pattern "cache-aside" tu dong: doc cache, neu miss thi tu tinh lai va cache lai, khong can caller tu viet logic get/set rieng.

**Khi nao KHONG dung** - Khi chi can doc cache tho khong muon side-effect goi lai nguon du lieu (dung `GetCacheByKeyAsync`); khi can du lieu khong phai class (`TOut` bi constraint `class`, khong dung duoc voi struct/value type khong nullable).

**Gioi han**
- `TOut` bi ep kieu `class` — khong dung truc tiep cho `int`, `struct`, v.v.
- Khong co null-check cho `func` — truyen `null` se nem `NullReferenceException` khi factory goi `func()` (CoreCacheExtension.cs:188), nhung vi loi nay xay ra ben trong `try` nen bi `catch (Exception exception)` (dong 161-166) nuot va log thanh `ConnectionErrorRedis`, tra `null` — khong crash ra ngoai, nhung de gay hieu nham la loi ket noi Redis.
- Neu `cancellationToken` truyen vao da bi huy **truoc khi** goi `GetOrCreateAsync`, ham se nem `OperationCanceledException` ra ngoai caller ngay tai dong 36 (truoc `try`) — khong duoc catch noi bo, khac voi hanh vi "fail-open, luon tra `null`" mo ta o muc Output/Error handling (chi ap dung cho loi huy *trong luc* ham dang chay).
- Co che chong "cache stampede" (nhieu request cung luc cache-miss cung 1 key) phu thuoc hoan toan vao co che noi bo cua `fusionCache.GetOrSetAsync` (thu vien ZiggyCreatures.Caching.Fusion) — **khong xac dinh duoc tu source code cua repo nay** vi logic khoa/gop request nam trong thu vien ngoai, khong phai trong `CoreCacheExtension.cs`.
- Khi `FusionCacheSerializationException` xay ra, ham goi lai `func` **dong bo trong luong hien tai** (khong co khoa/dedupe rieng) — neu nhieu request cung gap loi serialize cung luc, moi request deu tu goi `func` rieng (khong co bao ve stampede bo sung o tang nay).

### 2.2 GetCacheByKeyAsync

**Signature**
```csharp
public async Task<string> GetCacheByKeyAsync(string key,
                                             StepCache step = StepCache.None,
                                             FusionCacheEntryOptions options = null,
                                             int cancellationTokenTime = 1,
                                             CancellationToken cancellationToken = default)
```
**Muc dich** - Doc truc tiep gia tri JSON string da cache theo `key`, KHONG goi lai nguon du lieu khi miss (khac voi `GetOrCreateAsync`) (CoreCacheExtension.cs:218-313).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `key` | `string` | Co | Null/whitespace → tra `string.Empty` ngay (CoreCacheExtension.cs:233-236) | — |
| `step` | `StepCache` | Khong | `Local`: `SkipDistributedCacheRead=true`; `Distributed`: `SkipMemoryCacheRead=true` + timeout Redis; `None`: ca hai tang doc (CoreCacheExtension.cs:244-274) | `StepCache.None` |
| `options` | `FusionCacheEntryOptions` | Khong | null → `CoreCacheHelper.FusionCacheEntryOptionsDefault().Duplicate()`; co gia tri → `.Duplicate()` (khong recompute `Duration`/fail-safe nhu `GetOrCreateAsync`) (CoreCacheExtension.cs:238-240) | `null` |
| `cancellationTokenTime` | `int` | Khong | Giay timeout | `1` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` dau ham | `default` |

**Output** - `Task<string>`: tra gia tri JSON string neu tim thay (`result.HasValue == true`); tra `string.Empty` neu `key` rong, khong tim thay trong cache (`HasValue == false`), hoac co `Exception` (khac `OperationCanceledException`); tra `null` khi bi `OperationCanceledException` (CoreCacheExtension.cs:300-306) — **luu y su khac biet: timeout tra `null`, nhung cache-miss/loi khac tra `string.Empty`**, caller can phan biet ky hai gia tri nay neu logic phia tren coi `null` va `""` khac nhau.

**Dieu kien xu ly**
1. Guard `key` rong → tra `string.Empty`.
2. Build `options` (khong qua ham private `FusionCacheEntryOptions()`, chi `Duplicate()` default hoac override).
3. Ap dung `step` vao `options` (doc only — khong co `SkipDistributedCacheWrite`/`SkipMemoryCacheWrite` vi day la API doc).
4. Goi `fusionCache.TryGetAsync<string>(key, options, token)` → nhan `MaybeValue<string>`.
5. `switch (result.HasValue)`: `true` → lay `result.Value`; `false` → giu `value = string.Empty`.

**Side effect** - Khong ghi cache (chi doc); ghi log khi loi; tao Activity tracing `cache.get`.

**Error handling** - `OperationCanceledException` bi huy *trong luc* ham dang chay (sau khi vao `try` o dong 242) → log `Info`, tra `null`. `Exception` khac → log `ConnectionErrorRedis`, tra `string.Empty` (fail-open, khong throw). **Ngoai le**: neu `cancellationToken` da bi huy **truoc khi** goi ham, `ThrowIfCancellationRequested()` o dong 224 (truoc `try`) nem thang `OperationCanceledException` ra caller, khong bi catch noi bo — xem muc 3, van de #11.

**Khi nao NEN dung** - Khi chi can kiem tra/doc cache hien co ma khong muon kich hoat viec tinh lai du lieu.

**Khi nao KHONG dung** - Khi can tu dong tao cache neu chua co (dung `GetOrCreateAsync`).

**Gioi han** - Gia tri tra ve la `string` JSON tho — caller phai tu `JSonTryParse` neu can object; khong tu deserialize nhu `GetOrCreateAsync`. Tra ve khong dong nhat giua `null` (timeout/huy trong luc chay) va `string.Empty` (miss/loi khac) — de gay nham lan logic neu khong doc ky code. Neu `cancellationToken` da bi huy san truoc khi goi ham, ham nem exception ra caller thay vi tra `null`/`string.Empty` (xem Error handling).

### 2.3 SetCacheByKeyAsync (overload co `expiredMinutes`)

**Signature**
```csharp
public async Task SetCacheByKeyAsync(string key,
                                     string value,
                                     double expiredMinutes = 1,
                                     StepCache step = StepCache.None,
                                     FusionCacheEntryOptions options = null,
                                     int cancellationTokenTime = 1,
                                     CancellationToken cancellationToken = default)
```
**Muc dich** - Ghi gia tri `value` (JSON string) vao cache theo `key`, voi TTL tinh theo phut, tu build `options` mac dinh/override qua ham private `FusionCacheEntryOptions()` (CoreCacheExtension.cs:331-403).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `key` | `string` | Co | Null/whitespace → return ngay, khong ghi gi (CoreCacheExtension.cs:348-351) | — |
| `value` | `string` | Co | Khong co validate noi dung (co the ghi ca string rong) | — |
| `expiredMinutes` | `double` | Khong | ≤ 0 → ep 5 phut (CoreCacheExtension.cs:617) | `1` |
| `step` | `StepCache` | Khong | `Local`: `SkipDistributedCacheWrite=true`; `Distributed`: `SkipMemoryCacheWrite=true` + timeout Redis; `None`: ghi ca hai tang (CoreCacheExtension.cs:357-387) | `StepCache.None` |
| `options` | `FusionCacheEntryOptions` | Khong | null → default `.Duplicate()`; co gia tri → `.Duplicate()`; sau do luon bi `FusionCacheEntryOptions()` override `Duration`/`IsFailSafeEnabled`/`FailSafeMaxDuration` | `null` |
| `cancellationTokenTime` | `int` | Khong | Giay timeout | `1` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` dau ham | `default` |

**Output** - `Task` (void) — khong co gia tri tra ve cho biet ghi thanh cong hay that bai; caller khong the biet cache co duoc ghi hay khong neu co loi (chi co log).

**Dieu kien xu ly**
1. Guard `key` rong → return.
2. Build `options` theo `expiredMinutes` qua `FusionCacheEntryOptions()` (muc 2.7).
3. Ap dung `step` (ghi only).
4. `await fusionCache.SetAsync(key, value, options, token)`.

**Side effect** - Ghi cache (memory/Redis theo `step`); log khi loi; Activity tracing `cache.set`.

**Error handling** - `OperationCanceledException` bi huy *trong luc* ham dang chay (sau khi vao `try` o dong 353) → log `Info`, khong throw, khong lam gi them (return ngam dinh vi cuoi method). `Exception` khac → log `ConnectionErrorRedis`, khong throw (fail-open — cache khong duoc ghi nhung caller khong biet). **Ngoai le**: neu `cancellationToken` da bi huy **truoc khi** goi ham, `ThrowIfCancellationRequested()` o dong 339 (truoc `try`) nem thang `OperationCanceledException` ra caller, khong bi catch noi bo — xem muc 3, van de #11.

**Khi nao NEN dung** - Ghi cache thu cong khi da co `value` JSON string san va muon kiem soat TTL bang so phut don gian.

**Khi nao KHONG dung** - Khi can kiem soat chi tiet toan bo `FusionCacheEntryOptions` (khong muon bi override `Duration`/fail-safe) → dung overload 2.4.

**Gioi han** - Khong tra ve trang thai thanh cong/that bai; loi phat sinh sau khi vao `try` bi "nuot" hoan toan (chi log), caller khong co cach nao phat hien viec ghi cache that bai tu gia tri tra ve. Rieng loi huy token *truoc khi* goi ham van thoat ra ngoai duoi dang exception (khong bi nuot).

### 2.4 SetCacheByKeyAsync (overload co `FusionCacheEntryOptions options` bat buoc)

**Signature**
```csharp
public async Task SetCacheByKeyAsync(string key,
                                     string value,
                                     FusionCacheEntryOptions options,
                                     StepCache step = StepCache.None,
                                     int cancellationTokenTime = 1,
                                     CancellationToken cancellationToken = default)
```
**Muc dich** - Ghi cache voi `options` do caller cung cap truc tiep, khong qua ham build mac dinh `FusionCacheEntryOptions()`, khong `Duplicate()` (CoreCacheExtension.cs:416-485).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `key` | `string` | Co | Null/whitespace → return (CoreCacheExtension.cs:432-435) | — |
| `value` | `string` | Co | Khong validate | — |
| `options` | `FusionCacheEntryOptions` | **Co (khong co default)** | **KHONG co null-check** — neu truyen `null`, switch theo `step` se ghi thuoc tinh len `options` va nem `NullReferenceException` (xem muc 3, #1) | — |
| `step` | `StepCache` | Khong | Giong overload 2.3 nhung mutate truc tiep `options` cua caller (khong `Duplicate()`) | `StepCache.None` |
| `cancellationTokenTime` | `int` | Khong | Giay timeout | `1` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` dau ham | `default` |

**Output** - `Task` (void), tuong tu overload 2.3.

**Dieu kien xu ly**
1. Guard `key` rong → return.
2. `switch (step)` mutate truc tiep len `options` truyen vao (khong copy) — khac biet quan trong so voi overload 2.3.
3. `await fusionCache.SetAsync(key, value, options, token)`.

**Side effect** - Ghi cache; **mutate truc tiep object `options` ma caller truyen vao** (side effect tren tham so dau vao — khac overload 2.3 luon `Duplicate()` truoc khi sua).

**Error handling** - Giong overload 2.3: `OperationCanceledException` bi huy trong luc chay → log `Info`; `Exception` khac (ke ca `NullReferenceException` do `options: null`) → log `ConnectionErrorRedis`; khong throw ra ngoai trong cac truong hop nay. **Ngoai le giong overload 2.3**: neu `cancellationToken` da bi huy **truoc khi** goi ham, `ThrowIfCancellationRequested()` o dong 423 (truoc `try`) nem thang ra caller, khong bi catch noi bo (xem muc 3, van de #11).

**Khi nao NEN dung** - Khi can toan quyen kiem soat `FusionCacheEntryOptions` (vi du set rieng `FailSafeMaxDuration`, `Priority`, v.v.) ma khong muon bi ham noi bo override.

**Khi nao KHONG dung** - Khi khong chac `options` co the null, hoac khong muon object `options` goc bi mutate.

**Gioi han** - Khong co null-check cho `options` (khac toan bo cac method khac trong lop nay deu tu tao default khi `options == null`) → goi voi `options: null` se crash bang `NullReferenceException` khong duoc `try/catch` bat (vi loi xay ra tai dong gan thuoc tinh trong `switch`, nam trong `try`, nhung `NullReferenceException` van bi catch boi `catch (Exception exception)` o cuoi — do do thuc te se KHONG crash ra ngoai ma bi log `ConnectionErrorRedis` va bo qua mot cach "am tham", co the gay hieu nham la loi Redis trong khi thuc chat la loi truyen `options: null`).

### 2.5 ClearAllCacheAsync

**Signature**
```csharp
public async Task ClearAllCacheAsync(
    string[] keys, int cancellationTokenTime = 1, CancellationToken cancellationToken = default)
```
**Muc dich** - Xoa mot danh sach key khoi cache (khu trung lap truoc khi xoa tung key) (CoreCacheExtension.cs:498-536).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `keys` | `string[]` | Co | `keys?.Distinct()?.ToArray()` roi kiem tra `IsNullOrEmpty()` **ben trong `try`**, nhung co dong dung `string.Join(", ", keys)` de set tag Activity **truoc** doan kiem tra null (xem muc 3, #2) | — |
| `cancellationTokenTime` | `int` | Khong | Giay timeout cho tung lan xoa key con | `1` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` dau ham | `default` |

**Output** - `Task` (void). Khong co gia tri nao cho biet da xoa duoc bao nhieu key hay key nao xoa loi.

**Dieu kien xu ly**
1. Khu trung lap `keys`.
2. Tao Activity `cache.clear`, gan tag `cache.key = string.Join(", ", keys)`, `DisplayName = "DELETE {...}"`.
3. Trong `try`: neu `keys.IsNullOrEmpty()` → return (khong lam gi).
4. Nguoc lai, `foreach` goi `ClearCacheAsync(item, ...)` cho tung key (tuan tu, khong parallel).

**Side effect** - Xoa cache (memory + Redis, vi `ClearCacheAsync` khong gioi han theo `StepCache`); log khi loi; Activity tracing.

**Error handling** - `OperationCanceledException` bi huy trong luc `foreach` dang chay (ben trong `try` o dong 512) → log `Info` (dung `nameof(SetCacheByKeyAsync)` lam `methodName` trong log — sai ten method, xem muc 3, #3). `Exception` khac → log `ConnectionErrorRedis` voi `methodName = nameof(ClearAllCacheAsync)` (dung). **Ngoai le**: neu `cancellationToken` da bi huy **truoc khi** goi ham, `ThrowIfCancellationRequested()` o dong 501 (truoc `try`) nem thang `OperationCanceledException` ra caller, khong bi catch noi bo va khong di qua log nao ca — xem muc 3, van de #11.

**Khi nao NEN dung** - Khi can xoa dong thoi nhieu key da biet chinh xac ten (invalidate cache theo lo).

**Khi nao KHONG dung** - Khi can xoa theo pattern/wildcard (dung `CoreRedisCacheExtension.ClearDataWithKeys` + `GetAllKeyAsync` de lay danh sach key khop pattern truoc).

**Gioi han**
- Xoa tuan tu tung key (khong `Task.WhenAll`) — voi danh sach key lon, tong thoi gian xoa co the cham tuyen tinh theo so key.
- **Khong co null-check cho `keys` truoc khi dung trong `string.Join` de set tag Activity** (dong nam ngoai `try`) — neu co `ActivityListener` dang lang nghe (vi du OpenTelemetry export dang bat) va `keys` la `null`, `string.Join(", ", (string[])null)` se nem `ArgumentNullException` khong duoc bat, lam crash toan bo loi goi (xem muc 3, #2).

### 2.6 GetResultAsync&lt;TOut&gt; *(private)*

**Signature**
```csharp
private static ValueTask<TOut> GetResultAsync<TOut>(
    Func<ValueTask<TOut>> func, CancellationToken cancellationToken = default)
```
**Muc dich** - Goi `func` de lay du lieu, roi chuan hoa: neu ket qua null hoac serialize ra JSON rong/`"null"`/`"{}"`/`"[]"` thi tra ve `default(TOut)` (CoreCacheExtension.cs:177-207). Dung noi bo boi `GetOrCreateAsync` (ca nhanh chinh va nhanh catch `FusionCacheSerializationException`).

**Input hop le** - `func` khong co null-check; `cancellationToken` duoc `ThrowIfCancellationRequested()` hai lan (o `GetResultAsync` va lai o `ExecuteAsync` long trong no).

**Output** - `ValueTask<TOut>`: gia tri that cua `func()` neu hop le, hoac `default(TOut)` (thuong la `null` voi `TOut : class`) neu `func()` tra null hoac JSON rong.

**Dieu kien xu ly** - Goi `func()` → neu `null` tra `default`; neu khong null, `ToJSon()` de kiem tra chuoi JSON co "thuc chat" hay khong (loai `"null"`, `"{}"`, `"[]"`) → neu rong tra `default`, nguoc lai tra chinh `dataInput` goc (khong phai ban parse lai tu JSON).

**Side effect** - Khong co (khong cache, khong log).

**Error handling** - Khong co try/catch rieng — moi exception tu `func()` se lan ra loi goi ben ngoai (`GetOrCreateAsync` bat bang `catch (Exception exception)` o tang ngoai).

**Khi nao NEN dung / KHONG dung** - Chi dung noi bo trong lop, khong public.

**Gioi han** - Viec serialize de "kiem tra rong" chay tren moi lan goi (du ket qua se bi bo) — ton them 1 lan serialize JSON so voi viec chi kiem tra qua reflection/interface.

### 2.7 FusionCacheEntryOptions(...) *(private, cau hinh)*

**Signature**
```csharp
private static FusionCacheEntryOptions FusionCacheEntryOptions(
    double expiredMinutes, FusionCacheEntryOptions options = null)
```
**Muc dich** - Chuan hoa `options` dung cho cac loi goi GET/SET co `expiredMinutes`: set `Duration` theo phut, va tu tinh lai `IsFailSafeEnabled`/`FailSafeMaxDuration` (CoreCacheExtension.cs:610-625).

**Input hop le** - `expiredMinutes` ≤ 0 → `Duration = 5 phut`; > 0 → `Duration = TimeSpan.FromMinutes(expiredMinutes)`. `options` null → dung `CoreCacheHelper.FusionCacheEntryOptionsDefault().Duplicate()`; khong null → `options.Duplicate()`.

**Output** - `FusionCacheEntryOptions` moi (luon la ban `Duplicate()`, khong mutate object goc do caller truyen vao).

**Dieu kien xu ly**
1. `Duration` = 5 phut neu `expiredMinutes <= 0`, nguoc lai = `expiredMinutes` phut (**TTL mac dinh xac nhan: 5 phut**, CoreCacheExtension.cs:617).
2. `IsFailSafeEnabled = IsFailSafeEnabled(options.Duration)` — **chi bat fail-safe neu `Duration >= 15 phut`** (muc 2.8) — dieu nay override gia tri `IsFailSafeEnabled = true` mac dinh trong `CoreCacheHelper` khi TTL truyen vao ngan hon 15 phut.
3. `FailSafeMaxDuration = Duration + 15 phut` neu fail-safe bat, nguoc lai `TimeSpan.Zero`.

**Side effect** - Khong co (ham thuan, chi build object moi).

**Error handling** - Khong co try/catch (khong can vi khong co I/O).

**Gioi han** - Vi luon override `IsFailSafeEnabled`/`FailSafeMaxDuration` theo `Duration`, cac cau hinh fail-safe tuy bien trong `options` ma caller truyen vao (neu co) se bi ghi de moi khi goi qua `GetOrCreateAsync`/`SetCacheByKeyAsync(overload co `expiredMinutes`) — chi overload `SetCacheByKeyAsync(options bat buoc)` moi giu nguyen toan bo `options` goc.

### 2.8 IsFailSafeEnabled(TimeSpan) *(private, cau hinh)*

**Signature**
```csharp
private static bool IsFailSafeEnabled(TimeSpan durationTime)
```
**Muc dich** - Quyet dinh co bat fail-safe (giu stale data khi Redis/backend loi) hay khong, dua tren TTL. Comment trong code: "Neu cache duoi 15 phut thi khong can set thoi gian bao hiem" (CoreCacheExtension.cs:592-601).

**Dieu kien xu ly** - `return durationTime >= TimeSpan.FromMinutes(15);` — **nguong xac nhan tu source: 15 phut**.

**Output** - `bool`: `true` neu `durationTime >= 15 phut`, nguoc lai `false`.

**Side effect / Error handling** - Khong co.

**Gioi han** - Nguong 15 phut la hardcode, khong cau hinh duoc tu ben ngoai (muon doi phai sua source).

### 2.9 CoreCacheHelper.FusionCacheEntryOptionsDefault / FusionCacheOptionsDefault

**Signature**
```csharp
public static FusionCacheOptions FusionCacheOptionsDefault();
public static FusionCacheEntryOptions FusionCacheEntryOptionsDefault();
```
**Muc dich** - Cung cap cau hinh mac dinh (singleton `static readonly`) cho toan bo he thong cache khi khong co `options` tuy bien (CoreCacheHelper.cs:5-60).

**Gia tri mac dinh thuc te (CoreCacheHelper.cs:7-55)**

| Thuoc tinh | Gia tri | Y nghia (theo comment/context) |
|---|---|---|
| `Duration` | 5 phut | TTL mac dinh cua entry cache |
| `IsFailSafeEnabled` | `true` | Bat fail-safe — khi Redis/backend loi van tra stale data (luu y: bi `CoreCacheExtension.FusionCacheEntryOptions()` override theo `Duration` thuc te khi goi qua `GetOrCreateAsync`/`SetCacheByKeyAsync(expiredMinutes)`, chi giu nguyen khi dung qua `GetCacheByKeyAsync` hoac `SetCacheByKeyAsync(options)`) |
| `FailSafeMaxDuration` | 30 phut | Thoi gian toi da duoc dung stale data khi co su co |
| `FailSafeThrottleDuration` | 60 giay | Thoi gian "ghim" giua cac lan retry factory khi dang fail-safe |
| `FactoryHardTimeout` | 2 giay | Timeout cung khi backend/factory cham |
| `FactorySoftTimeout` | 200 ms | Timeout mem khi backend/factory tra stale qua thuong xuyen |
| `DistributedCacheSoftTimeout` | 1 giay | Timeout mem cho moi thao tac Redis (L2) |
| `DistributedCacheHardTimeout` | 2 giay | Timeout cung cho moi thao tac Redis (L2) |
| `AllowBackgroundBackplaneOperations` | `true` | Cho phep chay lenh backplane (pub/sub dong bo node) o background |
| `AllowBackgroundDistributedCacheOperations` | `false` | Khong chay thao tac Redis kieu nen, de hanh vi de kiem soat khi co su co |
| `DistributedCacheCircuitBreakerDuration` (FusionCacheOptions) | 2 phut | Circuit breaker: khi Redis loi (timeout/OOM/connection), tam ngung hit Redis trong 2 phut |
| `BackplaneCircuitBreakerDuration` (FusionCacheOptions) | 20 giay | Circuit breaker cho kenh backplane |

**Noi dung tham chieu** - `FusionCacheEntryOptionsDefault()` duoc goi tai 2 vi tri trong `CoreCacheExtension.cs` (dong 239, 614). `FusionCacheOptionsDefault()` **khong co diem goi nao** trong 5 file thuoc pham vi tai lieu nay, va grep toan repo (ngoai chinh file dinh nghia) cung khong tim thay diem goi — **khong xac dinh duoc tu source code** noi (hoac lieu co) `FusionCacheOptionsDefault()` duoc dung de dang ky `AddFusionCache(...)` vao DI container, vi khong tim thay file dang ky DI cho `IFusionCache`/Redis backplane trong repo nay.

### 2.10 StepCache (enum)

**Signature**
```csharp
public enum StepCache : byte
{
    None = 0,
    Local = 1,
    Distributed = 2
}
```

| Gia tri | Gia tri thuc | Y nghia (theo cach dung trong code) | Ghi chu |
|---|---|---|---|
| `None` | 0 | Ap dung ca 2 tang cache (memory L1 + Redis L2) — khong skip tang nao | Gia tri mac dinh cua toan bo tham so `step` trong `ICoreCacheExtension` |
| `Local` | 1 | Chi dung memory cache (L1); skip doc/ghi Redis (L2) | |
| `Distributed` | 2 | Chi dung Redis cache (L2); skip doc/ghi memory (L1) | |

### 2.11 CoreRedisCacheExtension.GetAllDataAsync

**Signature**
```csharp
public async Task<Dictionary<string, string>> GetAllDataAsync(
    string pattern = "*", int database = 1, string instanceName = "SR:v2:", int pageSize = 10_000)
```
**Muc dich** - Liet ke toan bo key khop `pattern` tren Redis (qua `IServer.Keys` — lenh `SCAN`), roi voi moi key, goi `ICoreCacheExtension.GetCacheByKeyAsync` (sau khi bo tien to `instanceName`) de lay gia tri, tra ve `Dictionary<key, value>` (CoreRedisCacheExtension.cs:16-38).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `pattern` | `string` | Khong | Khong validate — truyen thang vao `IServer.Keys(pattern:)` (glob pattern Redis `SCAN`) | `"*"` |
| `database` | `int` | Khong | Khong validate | `1` |
| `instanceName` | `string` | Khong | Dung de `key.Replace(instanceName, string.Empty)` truoc khi goi `GetCacheByKeyAsync` — neu khong khop tien to thuc te tren Redis, `Replace` khong doi gi (am tham) | `"SR:v2:"` |
| `pageSize` | `int` | Khong | Truyen vao `IServer.Keys(pageSize:)` | `10_000` |

**Output** - `Task<Dictionary<string, string>>`: key la **key goc tren Redis** (co tien to `instanceName`), value la JSON string tra ve tu `GetCacheByKeyAsync` (key da bo tien to) — co the la `string.Empty` neu `GetCacheByKeyAsync` khong tim thay gia tri hoac loi, hoac `null` neu bi timeout o `GetCacheByKeyAsync`.

**Dieu kien xu ly**
1. Lay `IServer` dau tien tu `connectionMultiplexer.GetEndPoints().FirstOrDefault()`.
2. `server.Keys(database, pattern, pageSize)` → SCAN toan bo key khop pattern (lazy `IEnumerable`).
3. `foreach` tuan tu: goi `coreCacheExtension.GetCacheByKeyAsync(key: key.Replace(instanceName, ""))`, them vao `Dictionary`.

**Side effect** - Ghi log `Request`/`Response` (chi log ten method, khong co noi dung). Khong ghi/xoa cache (chi doc).

**Error handling** - **Khong co try/catch trong toan ham.** Neu `GetEndPoints()` tra ve rong, `FirstOrDefault()` tra `null`, `connectionMultiplexer.GetServer(null)` se nem exception (khong bat). Neu Redis mat ket noi khi `server.Keys(...)` dang enumerate, exception cung lan thang ra caller — **khac hoan toan voi hanh vi fail-open cua `CoreCacheExtension`**.

**Khi nao NEN dung** - Cong cu van hanh/debug de dump toan bo (hoac theo pattern) du lieu cache hien co tren Redis.

**Khi nao KHONG dung** - Trong luong nghiep vu thoi gian thuc (request API) — vi `SCAN` voi `pattern="*"` tren Redis lon co the cham, va ham goi `GetCacheByKeyAsync` tuan tu cho tung key (khong parallel), co the rat cham voi tap key lon; dong thoi khong co bao ve loi ket noi Redis.

**Gioi han** - Khong doc XML doc `<param name="configurationOptions">` (khong ton tai trong signature thuc te — XML doc thua/loi thoi, xem muc 3). Khong gioi han so luong key tra ve ngoai `pageSize` cua SCAN (khong phai gioi han tong so key, chi la kich thuoc trang quet).

### 2.12 CoreRedisCacheExtension.GetAllKeyAsync

**Signature**
```csharp
public async Task<IEnumerable<string>> GetAllKeyAsync(
    string pattern = "*", int database = 1, int pageSize = 10_000)
```
**Muc dich** - Giong `GetAllDataAsync` nhung chi tra ve danh sach key khop pattern, khong lay gia tri (CoreRedisCacheExtension.cs:47-61).

**Input hop le** - Giong `GetAllDataAsync` (tru `instanceName`, khong co).

**Output** - `Task<IEnumerable<string>>`: danh sach key goc tren Redis (giu nguyen tien to, KHONG `Replace` `instanceName` nhu `GetAllDataAsync`). Chuoi tra ve la `IEnumerable` lazy (boc tu `IServer.Keys` + `Select`) — enumerate tra ve se thuc thi SCAN tai thoi diem do, khong phai tai thoi diem goi ham (vi ham khong `await` gi truoc khi `return`, mac du duoc khai bao `async` — xem muc 3, khai bao `async` khong co `await` nao ben trong, chi co 1 `return` bieu thuc khong phai `Task`, C# se tu boc bang `Task.FromResult` ngam nhung KHONG execute SCAN dong bo truoc khi tra `Task` — thuc te `server.Keys(...)` la lazy enumerator duoc tra nguyen ven, SCAN chi chay khi caller enumerate `IEnumerable<string>` ket qua).

**Dieu kien xu ly** - Lay `IServer` dau tien → goi `server.Keys(...).Select(key => (string)key)` → tra truc tiep (khong `await` gi).

**Side effect** - Log `Request` (khong co `Response` — thieu log Response so voi `GetAllDataAsync`/`ClearDataWithKeys`, xem muc 3).

**Error handling** - Khong co try/catch — exception (ket noi Redis loi, endpoint rong) lan thang ra caller (khi enumerate).

**Khi nao NEN dung/KHONG dung** - Tuong tu `GetAllDataAsync`, dung khi chi can danh sach key (khong can gia tri) de giam so lan goi `GetCacheByKeyAsync`.

**Gioi han** - Ham khai bao `async` nhung khong co `await` ben trong → compiler warning (CS1998) va viec SCAN thuc te bi tri hoan den khi enumerate ket qua (lazy), khac voi cam giac "da chay xong" khi `Task` hoan thanh.

### 2.13 CoreRedisCacheExtension.ClearDataWithKeys

**Signature**
```csharp
public async Task<bool> ClearDataWithKeys(
    List<string> pattern, string instanceName = "SR:v2:", CancellationToken cancellationToken = default)
```
**Muc dich** - Xoa danh sach key (bo tien to `instanceName` truoc) bang cach goi `ICoreCacheExtension.ClearAllCacheAsync` (CoreRedisCacheExtension.cs:69-85). Ten tham so `pattern` gay hieu nham — thuc chat day la **danh sach key cu the can xoa**, khong phai Redis glob pattern (khac voi `pattern` o `GetAllDataAsync`/`GetAllKeyAsync`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `pattern` | `List<string>` | Co | **Khong co null-check** — neu `null`, `pattern.Select(...)` nem `ArgumentNullException` ngay (khong bat) | — |
| `instanceName` | `string` | Khong | Dung de `key?.Replace(instanceName, string.Empty)` cho tung phan tu (co `?.` nen phan tu `null` trong list duoc giu `null`, khong loi) | `"SR:v2:"` |
| `cancellationToken` | `CancellationToken` | Khong | `ThrowIfCancellationRequested()` dau ham | `default` |

**Output** - `Task<bool>`: **luon tra ve `true`** khi chay toi cuoi ham — khong phan anh viec xoa co thuc su thanh cong hay khong (vi `ClearAllCacheAsync` fail-open, tu nuot loi va khong tra gi de `ClearDataWithKeys` biet).

**Dieu kien xu ly**
1. `pattern.Select(key => key?.Replace(instanceName, string.Empty)).ToArray()`.
2. `await coreCacheExtension.ClearAllCacheAsync(keys: keys, cancellationToken: cancellationToken)`.
3. Log `Response`, tra `true`.

**Side effect** - Xoa cache (memory + Redis) cho cac key tuong ung qua `ClearAllCacheAsync`. Log `Request`/`Response`.

**Error handling** - Khong co try/catch rieng o tang nay. Loi trong `ClearAllCacheAsync` (phan ben trong `try` cua no) bi nuot va log; nhung loi phat sinh tu chinh `ClearDataWithKeys` (vi du `pattern == null`) se nem thang ra caller vi khong co `try/catch` boc quanh.

**Khi nao NEN dung** - Khi co danh sach key ro rang can xoa (khong phai wildcard).

**Khi nao KHONG dung** - Khong dung de xoa theo wildcard/pattern thuc su (ten gay hieu lam) — muon xoa theo pattern phai tu goi `GetAllKeyAsync(pattern)` truoc roi truyen danh sach key ket qua vao day.

**Gioi han** - Gia tri tra ve `bool` luon `true`, khong co y nghia xac nhan ket qua thuc te; ten tham so `pattern` (thuc chat la danh sach key) de gay hieu nham khi doc code goi ham nay.

### 2.14 CoreRedisCacheExtension.LUAAtomicCacheAsync

**Signature**
```csharp
public Task<RedisResult> LUAAtomicCacheAsync(
    string script, RedisKey[] keys, RedisValue[] values, int database = 1)
```
**Muc dich** - Chay mot Lua script tren Redis mot cach atomic (qua lenh `EVAL`/`ScriptEvaluateAsync` cua StackExchange.Redis) (CoreRedisCacheExtension.cs:95-102).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `script` | `string` | Co | Khong validate noi dung/do dai | — |
| `keys` | `RedisKey[]` | Co | Khong validate (co the null/rong, tuy StackExchange.Redis xu ly) | — |
| `values` | `RedisValue[]` | Co | Khong validate | — |
| `database` | `int` | Khong | Khong validate | `1` |

**Output** - `Task<RedisResult>`: ket qua tra ve nguyen ban tu Redis `EVAL` (kieu du lieu tuy theo script tra ve — so, string, array, v.v.), khong duoc ham nay xu ly/parse them.

**Dieu kien xu ly** - Lay `IDatabase` theo `database` → goi `table.ScriptEvaluateAsync(script, keys, values)` → tra `Task` truc tiep (khong `await`, khong try/catch, ham khong phai `async`).

**Side effect** - Thuc thi script tren Redis (co the doc/ghi tuy noi dung script — side effect phu thuoc hoan toan vao `script` truyen vao, khong do ham nay kiem soat). Chi log `Request` (khong co `Response` — xem muc 3).

**Error handling** - Khong co try/catch — moi loi (script sai cu phap, Redis loi ket noi, v.v.) lan thang ra caller duoi dang `Task` faulted.

**Khi nao NEN dung** - Khi can dam bao tinh atomic cho nhieu thao tac Redis (vi du check-and-set) ma API `IFusionCache` khong ho tro.

**Khi nao KHONG dung** - Khi khong can atomic hoac khong kiem soat duoc noi dung `script` (rui ro injection neu `script` duoc build tu input nguoi dung khong kiem soat).

**Gioi han** - Khong co bao ve/log loi nao; khong validate `script`/`keys`/`values`; caller phai tu xu ly toan bo exception.

## 3. Van de da biet

| # | Van de | Vi tri | Anh huong |
|---|---|---|---|
| 1 | `SetCacheByKeyAsync(key, value, FusionCacheEntryOptions options, ...)` khong kiem tra `options == null` truoc khi mutate (`options.SkipDistributedCacheWrite = true`, v.v. trong switch) | CoreCacheExtension.cs:437-469 | Goi ham voi `options: null` se nem `NullReferenceException`; exception nay bi `catch (Exception exception)` o cuoi ham nuot va log thanh `ConnectionErrorRedis` (category `DB_REDIS`) — **gay hieu nham la loi ket noi Redis trong khi thuc chat la loi do caller truyen `options: null`** |
| 2 | `ClearAllCacheAsync` goi `string.Join(", ", keys)` de set tag Activity (dong CoreCacheExtension.cs:508) va set `DisplayName` (dong 510) **truoc** khoi `try` (bat dau o dong 512) va truoc diem kiem tra `keys.IsNullOrEmpty()` (dong 514) | CoreCacheExtension.cs:503-514 | Neu `keys` la `null` VA co `ActivityListener` dang lang nghe (OpenTelemetry export bat — he thong nay ro rang co dung OpenTelemetry qua `OpenTelemetryConstant`), `string.Join(", ", (string[])null)` nem `ArgumentNullException` khong duoc `try/catch` bat, lam crash toan bo loi goi `ClearAllCacheAsync` thay vi im lang return nhu code phia duoi du tinh xu ly |
| 3 | Trong catch `OperationCanceledException` cua `ClearAllCacheAsync`, log dung `nameof(SetCacheByKeyAsync)` lam `methodName` thay vi `nameof(ClearAllCacheAsync)` | CoreCacheExtension.cs:529 | Log timeout cua `ClearAllCacheAsync` bi ghi nhan sai ten method, gay kho khan khi tra log/alerting theo method name |
| 4 | Comment mo ta `FailSafeThrottleDuration` la "ghim 30s" nhung gia tri thuc te trong code la `TimeSpan.FromSeconds(60)` | CoreCacheHelper.cs:19-20 | Theo nguyen tac "source code la nguon xac thuc cao nhat": gia tri that la **60 giay**, khong phai 30 giay nhu comment; tai lieu nay lay gia tri 60s lam chuan — comment trong code can duoc cap nhat lai de tranh nham lan cho dev doc sau |
| 5 | `IsFailSafeEnabled` mac dinh `true` va `FailSafeMaxDuration = 30 phut` trong `CoreCacheHelper` (dung nguyen ban khi qua `GetCacheByKeyAsync`), nhung bi `CoreCacheExtension.FusionCacheEntryOptions()` **tinh lai hoan toan** dua tren `Duration` thuc te (chi bat fail-safe neu `Duration >= 15 phut`) khi qua `GetOrCreateAsync`/`SetCacheByKeyAsync(expiredMinutes)` | CoreCacheExtension.cs:610-624 vs CoreCacheHelper.cs:11-20 | Hanh vi fail-safe **khong dong nhat** giua cac API trong cung lop: cung goi voi TTL 5 phut, `GetOrCreateAsync`/`SetCacheByKeyAsync(expiredMinutes)` se tat fail-safe, con `GetCacheByKeyAsync` (doc) van giu fail-safe bat theo default — can luu y khi debug hanh vi tra stale data |
| 6 | `CoreRedisCacheExtension` (`GetAllDataAsync`, `GetAllKeyAsync`, `ClearDataWithKeys`, `LUAAtomicCacheAsync`) hoan toan khong co `try/catch` | CoreRedisCacheExtension.cs (toan file) | Khac biet lon so voi `CoreCacheExtension` (luon fail-open, log roi tra `null`/`default`): moi loi ket noi Redis, endpoint rong, script Lua sai, v.v. trong `CoreRedisCacheExtension` se nem exception thang ra caller — neu caller khong tu boc `try/catch`, co the lam crash luong xu ly |
| 7 | `ClearDataWithKeys` luon tra ve `true` bat ke `ClearAllCacheAsync` ben trong co thuc su xoa thanh cong hay khong (vi `ClearAllCacheAsync` fail-open va khong tra trang thai) | CoreRedisCacheExtension.cs:74-85, CoreCacheExtension.cs:498-536 | Gia tri `bool` tra ve khong phan anh dung thuc te — khong the dung ket qua nay de xac nhan cache da duoc xoa |
| 8 | `GetAllKeyAsync` duoc khai bao `async` nhung than ham khong co `await` nao (chi `return server.Keys(...).Select(...)`) | CoreRedisCacheExtension.cs:47-61 | Viec quet Redis (`SCAN`) thuc chat duoc tri hoan (lazy) den khi caller enumerate ket qua `IEnumerable<string>`, khong xay ra dong bo ngay khi ham "hoan thanh" — co the gay hieu nham ve thoi diem thuc thi so voi `GetAllDataAsync` (da enumerate san thanh `Dictionary` truoc khi return) |
| 9 | Khong tim thay trong repo nay diem goi `AddFusionCache(...)`/dang ky `IFusionCache` voi Redis L2 provider + serializer (JSON.NET/System.Text.Json/MessagePack) cho `IFusionCache`, cung nhu khong tim thay diem goi `CoreCacheHelper.FusionCacheOptionsDefault()` ngoai chinh file dinh nghia no | Toan repo (ket qua grep `AddFusionCache`, `FusionCacheOptionsDefault`) | **Khong xac dinh duoc tu source code cua repo nay**: serializer thuc te dung cho tang L2 (Redis) cua FusionCache, cau hinh backplane, va viec `FusionCacheOptionsDefault()` co duoc dung de dang ky DI o dau do (co the o du an khac tham chieu package nay) |
| 10 | XML doc `<param name="configurationOptions">` xuat hien trong comment nhung tham so nay **khong ton tai** trong signature thuc te. **Da xac minh lai bang grep toan bo 2 file**: tham so ao nay chi xuat hien dung 3 lan — `CoreRedisCacheExtension.cs:12` (comment cua `GetAllDataAsync`), `ICoreRedisCacheExtension.cs:12` (comment cua `GetAllDataAsync`) va `ICoreRedisCacheExtension.cs:23` (comment cua `GetAllKeyAsync`). Comment cua `GetAllKeyAsync` trong **class** `CoreRedisCacheExtension.cs` (dong 44-50) KHONG co tham so ao nay (chi co `pattern`, `pageSize`) | `CoreRedisCacheExtension.cs:12` (chi `GetAllDataAsync`), `ICoreRedisCacheExtension.cs:12,23` (ca `GetAllDataAsync` va `GetAllKeyAsync`) | Comment/XML doc loi thoi, khong khop voi tham so thuc te (`pattern, database, instanceName, pageSize` / `pattern, database, pageSize`) — theo nguyen tac uu tien source code, tai lieu nay mo ta dung theo signature thuc te, bo qua tham so ao trong comment |
| 11 | Ca 5 method public cua `CoreCacheExtension` (`GetOrCreateAsync`, `GetCacheByKeyAsync`, `SetCacheByKeyAsync` x2, `ClearAllCacheAsync`) deu goi `cancellationToken.ThrowIfCancellationRequested()` **truoc** khoi `try...catch` cua ham (CoreCacheExtension.cs:36, 224, 339, 423, 501) | CoreCacheExtension.cs (dau moi method public, truoc dong `try`) | Mo ta "fail-open, luon tra `null`/`string.Empty`/khong throw" o cac muc 2.1-2.5 **chi dung cho loi xay ra sau khi vao `try`**. Neu caller truyen vao mot `cancellationToken` **da bi huy san** truoc khi goi ham, `OperationCanceledException` se nem thang ra ngoai (qua `ValueTask`/`Task` loi) ma KHONG bi catch noi bo, khac voi hanh vi "luon fail-open" ma tai lieu mo ta tong quat o muc 1.1 |

