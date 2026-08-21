# Models - Https (AuthModel, ErrorModel, HttpOptionModel, TokenResultModel, BaseApiModel)

> Nguon: FTELSRCore.Shared/Models/Https/AuthModel.cs, FTELSRCore.Shared/Models/Https/BaseApiModel.cs, FTELSRCore.Shared/Models/Https/ErrorModel.cs, FTELSRCore.Shared/Models/Https/HttpOptionModel.cs, FTELSRCore.Shared/Models/Https/TokenResultModel.cs
> Loai: record (AuthModel, BaseApiModel, ErrorModel, HttpOptionModel, HttpOptionModel<T>) + class (TokenResultModel)
> Cap nhat theo commit: 89c1ce9

## 1. Tong quan

Day la nhom 5 model du lieu (DTO/POCO) trong namespace `FTELSRCore.Models.Https` (khai bao o dong 1 cua tung file — luu y namespace **khong** trung voi duong dan thu muc `FTELSRCore.Shared/Models/Https`), dung de mang du lieu cho tang goi HTTP cua Shared library: cau hinh request (`HttpOptionModel`/`HttpOptionModel<T>`), ket qua loi (`ErrorModel`), thong tin dang nhap (`AuthModel`), ket qua token (`TokenResultModel`) va mot response mac dinh dang OK (`BaseApiModel`). Ca 5 file deu **chi khai bao property/field**, khong co logic nghiep vu dang ke — ngoai tru `ErrorModel.ErrorDeConstruct`, method duy nhat trong toan bo module. Day la tang model thuan (POCO), nam duoi tang `Utilizes` (`CallApiWithHttp<TRequest, TResponse>`, `CallApi<TResponse>`) — cac lop utilize nay tieu thu truc tiep `HttpOptionModel`/`HttpOptionModel<T>` va `ErrorModel`.

**Xac nhan tham chieu that te trong repo (grep toan repo, khong tinh `.claude/worktrees`):**
- `HttpOptionModel` / `HttpOptionModel<T>` va `ErrorModel`: duoc dung trong `FTELSRCore.Shared/Utilizes/CallApiWithHttp.cs` va `FTELSRCore.Shared/Utilizes/HttpClientUtilizes.cs`.
- `AuthModel`, `BaseApiModel`, `TokenResultModel`: **khong tim thay noi nao khac trong source code cua repo nay goi/khoi tao/tham chieu** ngoai chinh file khai bao. Khong xac dinh duoc tu source code trong repo nay muc dich su dung thuc te cua 3 model nay (co the duoc project ngoai repo tieu thu, vi day la thu vien Shared, nhung dieu do khong kiem chung duoc tu code hien co).
- Chu y: mot so ket qua grep khac ("ResultFTelCoreErrorModel" trong `Wrappers/IResult.cs`, `Wrappers/Result.cs`, `ExceptionHandlerMiddleWare.cs`) la mot **kieu khac hoan toan**, khong lien quan den `FTELSRCore.Models.Https.ErrorModel` cua module nay — khong duoc nham lan hai kieu nay.

### 1.1 Pham vi chuc nang

| Lam duoc | Khong lam duoc |
|---|---|
| Mang du liEu cau hinh mot HTTP request qua `HttpOptionModel` (Client, BaseAddress, Token, AuthType, Uri, SystemOwner, CompletionOption) va bien the generic `HttpOptionModel<T>` mang them body/query `Value` | Khong tu validate gia tri (khong co Data Annotation, khong co logic kiem tra null/format tren bat ky property nao trong ca 5 file) |
| Mang ket qua loi HTTP chuan hoa qua `ErrorModel` (Code, Message, Succeeded) va cho phep "destructure" ra bien rieng qua `ErrorDeConstruct` | `ErrorModel` khong tu gan gia tri — viec gan `Code`/`Message`/`Succeeded` do code ben ngoai (`HttpContentExtensionsUtilizes` trong `HttpClientUtilizes.cs`) thuc hien, ban than record khong co logic gan |
| Cung cap gia tri mac dinh san cho mot so property (`AuthType = "Bearer"`, `SystemOwner = "Service Request"`, `CompletionOption = ResponseContentRead`, `Status = "OK"`, `Code = 200`, `Message = "OK"`) | Khong co factory method / constructor tuy bien nao khac ngoai property initializer — khoi tao chi qua object initializer (`new HttpOptionModel { ... }`) |
| Mang thong tin dang nhap don gian qua `AuthModel` (UserName, Password, PolicyName, EmployeeCode) | Khong ma hoa/hash Password, khong co logic xu ly dang nhap — chi la data container |
| Mang ket qua token qua `TokenResultModel` (Type, Token, ExpiresAt dang `long`) | Khong tu kiem tra token het han — logic do nam o `TokenExpirationHelperUtilizes` (file khac), khong duoc goi tu model nay |
| Cung cap response mac dinh "OK" qua `BaseApiModel` (Status/Code/Message deu mac dinh la thanh cong) | Khong co bien the "loi" — khong co constructor/property nao dat san gia tri loi; muon bieu dien loi phai tu gan lai `Code`/`Message`/`Status` |

### 1.2 Dependency

| Thanh phan | Muc dich su dung |
|---|---|
| `System.Net.HttpStatusCode` | `BaseApiModel` dung `HttpStatusCode.OK` de lay gia tri mac dinh cho `Code` (ep kieu `int`) va `Message` (`.ToString()`) — `BaseApiModel.cs:8-9` |
| `System.Net.Http.HttpClient` | `HttpOptionModel.Client` mang truc tiep instance `HttpClient` do caller cung cap — `HttpOptionModel.cs:5` |
| `System.Net.Http.HttpCompletionOption` | `HttpOptionModel.CompletionOption` dung enum nay, mac dinh `ResponseContentRead` — `HttpOptionModel.cs:17` |
| `FTELSRCore.Utilizes.HttpClientUtilizes` / `HttpContentExtensionsUtilizes` (file `HttpClientUtilizes.cs`) | Tieu thu `HttpOptionModel`/`HttpOptionModel<T>` (tao instance qua `GetUri`, doc `Client`/`BaseAddress`/`Token`/`AuthType`/`CompletionOption` trong `ConfigHttpClient`) va gan gia tri vao `ErrorModel` (`ErrorException`, `ErrorCanceledException`, `EnsureSuccessOrException`) |
| `FTELSRCore.Utilizes.CallApiWithHttp<TRequest, TResponse>` (file `CallApiWithHttp.cs`) | Nhan `HttpOptionModel<TRequest>` lam tham so cho tat ca method GET/POST/PUT/DELETE, tra ve tuple co `ErrorModel` |

### 1.3 Danh muc API

| API | Nhom | Mo ta ngan |
|---|---|---|
| `AuthModel` | Model (record) | 4 property mang thong tin dang nhap: `UserName`, `Password`, `PolicyName`, `EmployeeCode` |
| `BaseApiModel` | Model (record) | 3 property mang response mac dinh "thanh cong": `Status`, `Code`, `Message` |
| `ErrorModel` | Model (record) | 3 property (`Code`, `Message`, `Succeeded`) + 1 method `ErrorDeConstruct` |
| `ErrorModel.ErrorDeConstruct` | Method | Xuat `Message`/`Code` cua instance ra 2 tham so `out` |
| `HttpOptionModel` | Model (record, base) | 7 property cau hinh HTTP request: `Client`, `BaseAddress`, `Token`, `AuthType`, `Uri`, `SystemOwner`, `CompletionOption` |
| `HttpOptionModel<T>` | Model (record, ke thua `HttpOptionModel`) | Bo sung 1 property `Value` (kieu `T`, rang buoc `where T : notnull`) |
| `TokenResultModel` | Model (class) | 3 property mang ket qua token: `Type`, `Token`, `ExpiresAt` |

## 2. Chi tiet API

### 2.1 AuthModel

**Signature**
```csharp
public record AuthModel
{
    public string UserName { get; set; }
    public string Password { get; set; }
    public string PolicyName { get; set; }
    public string EmployeeCode { get; set; }
}
```
Nguon: `AuthModel.cs:3-9`.

**Muc dich** — Mang du lieu dang nhap (username/password) kem 2 truong bo sung `PolicyName` va `EmployeeCode`. Ten cac property goi y day la model auth cho luong dang nhap/xac thuc noi bo (co the theo policy dang nhap va co gan voi ma nhan vien), nhung **khong co bat ky code nao trong repo nay khoi tao hoac doc cac property cua `AuthModel`** de xac nhan dieu do — day la suy dien tu ten, khong phai tu hanh vi da xac thuc.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `UserName` | `string` | Khong (khong co `[Required]`/validate) | Khong co | `null` |
| `Password` | `string` | Khong | Khong co | `null` |
| `PolicyName` | `string` | Khong | Khong co | `null` |
| `EmployeeCode` | `string` | Khong | Khong co | `null` |

**Output** — Khong ap dung (day la kieu du lieu, khong co method tra ve).

**Dieu kien xu ly** — Khong ap dung (record chi co property tu dong sinh get/set, khong co logic dieu kien).

**Side effect** — Khong co.

**Error handling** — Khong co (khong co method nao co the nem loi).

**Khi nao NEN dung** — Khong xac dinh duoc tu source code (khong co call site trong repo de suy ra tinh huong su dung thuc te).

**Khi nao KHONG dung** — `Password` la `string` thuan, khong ma hoa/hash; neu dung de truyen mat khau qua cac tang co the bi serialize vao log (nhu cach `HttpOptionModel` bi serialize nguyen ven trong `CallApiWithHttp.cs`, xem `Utilizes-CallApiWithHttp.md`), can ranh bo tuong tu truoc khi dung `AuthModel` cho du lieu nhay cam thuc.

**Gioi han** — Khong co validate dinh dang (email/username), khong co ho tro mask/an `Password` khi serialize (khong co `[JsonIgnore]` hay tuong tu).

### 2.2 BaseApiModel

**Signature**
```csharp
public record BaseApiModel
{
    public string Status { get; set; } = "OK";
    public int Code { get; set; } = (int)HttpStatusCode.OK;
    public string Message { get; set; } = HttpStatusCode.OK.ToString();
}
```
Nguon: `BaseApiModel.cs:5-10`.

**Muc dich** — Cung cap mot "shape" response mac dinh dai dien cho truong hop thanh cong: `Status = "OK"`, `Code = 200` (`(int)HttpStatusCode.OK`), `Message = "OK"` (`HttpStatusCode.OK.ToString()`). Không có code nào trong repo tạo instance của `BaseApiModel` hoặc kế thừa nó — không xác định được từ source code vai trò thực tế (ví dụ: base class cho response API khác) trong hệ thống này.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `Status` | `string` | Khong | Khong co | `"OK"` |
| `Code` | `int` | Khong | Khong co | `200` (`(int)HttpStatusCode.OK`) |
| `Message` | `string` | Khong | Khong co | `"OK"` (`HttpStatusCode.OK.ToString()`) |

**Output** — Khong ap dung.

**Dieu kien xu ly** — Khong co.

**Side effect** — Khong co.

**Error handling** — Khong co.

**Khi nao NEN dung** — Khong xac dinh duoc tu source code (khong co call site trong repo).

**Khi nao KHONG dung** — Muon bieu dien mot response loi thi phai tu gan lai ca 3 property (khong co bien the/factory nao dung san cho truong hop loi).

**Gioi han** — `Status` (string tu do) va `Code`/`Message` (theo `HttpStatusCode`) deu mang thong tin "trang thai" nhung khong lien ket voi nhau bang logic nao — neu mot noi nao gan lai `Code` ma quen gan `Status`/`Message` tuong ung, 3 truong se khong dong bo (day la rui ro thiet ke, khong phai bug da xay ra vi khong co call site).

### 2.3 ErrorModel

**Signature**
```csharp
public record ErrorModel
{
    public int Code { get; set; }
    public string Message { get; set; }
    public bool Succeeded { get; set; }

    public void ErrorDeConstruct(out string message, out int statusCode)
}
```
Nguon: `ErrorModel.cs:3-16`.

**Muc dich** — Model ket qua loi/thanh cong chuan hoa, duoc `HttpContentExtensionsUtilizes` (trong `HttpClientUtilizes.cs`, file khac cung Shared library) gan gia tri thong qua cac method noi bo `ErrorException`, `ErrorCanceledException`, `EnsureSuccessOrException` — ban than `ErrorModel` khong tu gan gia tri cho chinh no. Day la model duoc `CallApiWithHttp<TRequest, TResponse>` va `CallApi<TResponse>` (tang `Utilizes`) tra ve kem theo response o hau het method GET/POST/PUT/DELETE.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `Code` | `int` | Khong | Khong co | `0` (default `int`) |
| `Message` | `string` | Khong | Khong co | `null` |
| `Succeeded` | `bool` | Khong | Khong co | `false` (default `bool`) |

**Output** — Khong ap dung cho cac property. Rieng method `ErrorDeConstruct` xem muc rieng ben duoi.

**Dieu kien xu ly** — Khong co logic dieu kien tren property.

**Side effect** — Khong co tren property. Luu y: instance `ErrorModel` moi khoi tao (`new ErrorModel()`, vi du `CallApiWithHttp.cs:46`) co `Code = 0`, `Succeeded = false`, `Message = null` — day la trang thai "loi mac dinh" cho toi khi mot trong 3 method o `HttpClientUtilizes.cs` gan lai gia tri.

**Error handling** — Khong co tren property.

**Khi nao NEN dung** — Dung nhu kieu tra ve chuan cho ket qua goi HTTP (da duoc `CallApiWithHttp`/`CallApi` ap dung).

**Khi nao KHONG dung** — Khong nen doc `Message` tren nhanh thanh cong de hien thi cho end-user: theo cach `EnsureSuccessOrException` gan gia tri (`HttpClientUtilizes.cs:408-410`), `Message = httpResponseMessage.ReasonPhrase` co the la `null` ke ca khi thanh cong (dieu nay da duoc `Utilizes-CallApiWithHttp.md` ghi nhan, xem muc 3).

**Gioi han** — Khong co `[Required]`/validate; `Code = 0` mac dinh khong phai la ma HTTP status hop le (dieu nay tao ra tinh huong "Code = 0" khi mot code path bo qua viec gan lai `ErrorModel`, nhu da duoc `Utilizes-CallApi.md` ghi nhan cho `PostFormDataAsJSonAsync`).

#### 2.3.1 ErrorDeConstruct

**Signature**
```csharp
public void ErrorDeConstruct(out string message, out int statusCode)
```
Nguon: `ErrorModel.cs:11-15`.

**Muc dich** — Cho phep "destructure" mot instance `ErrorModel` thanh 2 bien rieng (`message`, `statusCode`) trong mot lan goi, thay vi doc rieng `instance.Message` va `instance.Code`. Khong co `/// <summary>` trong source; mo ta nay duoc rut ra tu doc truc tiep than ham.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `message` (out) | `string` | Co (out param) | Khong co — luon duoc gan | Khong ap dung |
| `statusCode` (out) | `int` | Co (out param) | Khong co — luon duoc gan | Khong ap dung |

**Output** — `void`. Sau khi goi: `message == this.Message` (co the `null`), `statusCode == this.Code` (co the `0` neu chua duoc gan).

**Dieu kien xu ly** — Khong co nhanh re; luon thuc hien 2 phep gan `message = Message; statusCode = Code;` (`ErrorModel.cs:13-14`).

**Side effect** — Khong co (khong mutate `this`, chi doc).

**Error handling** — Khong co try/catch; khong the nem loi vi chi la 2 phep gan don gian.

**Khi nao NEN dung** — Khi caller muon dung cu phap destructuring (`errorModel.ErrorDeConstruct(out var msg, out var code)`) de lay ca message va code trong mot dong.

**Khi nao KHONG dung** — Khong bat buoc phai dung — doc truc tiep `errorModel.Message`/`errorModel.Code` cho ket qua tuong duong; **khong tim thay noi nao trong repo nay thuc su goi `ErrorDeConstruct`** (grep toan repo, ngoai `.claude/worktrees`, chi tra ve dinh nghia method, khong co call site).

**Gioi han** — Ten method (`ErrorDeConstruct`) khong theo quy uoc chuan `Deconstruct` cua C# record (chu D hoa giua "Error" va "Deconstruct" — `ErrorDeConstruct` khong phai `Deconstruct`), nen **khong** kich hoat cu phap deconstruction ngam dinh cua ngon ngu (`var (msg, code) = errorModel;`) — phai goi ro ten method.

### 2.4 HttpOptionModel / HttpOptionModel<T>

**Signature**
```csharp
public record HttpOptionModel
{
    public HttpClient Client { get; set; }
    public string BaseAddress { get; set; }
    public string Token { get; set; }
    public string AuthType { get; set; } = "Bearer";
    public string Uri { get; set; }
    public string SystemOwner { get; set; } = "Service Request";
    public HttpCompletionOption CompletionOption { get; set; } = HttpCompletionOption.ResponseContentRead;
}

public record HttpOptionModel<T> : HttpOptionModel where T : notnull
{
    public T Value { get; init; }
}
```
Nguon: `HttpOptionModel.cs:3-23`.

**Muc dich** — Mang toan bo thong tin can de goi mot HTTP request qua tang `Utilizes` (`CallApiWithHttp<TRequest, TResponse>`, `CallApi<TResponse>`): `HttpClient` dung chung do caller cung cap, dia chi/dinh danh request, token xac thuc, va (voi bien the generic) du lieu request (`Value` — dung lam query params hoac body tuy method goi).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `Client` | `HttpClient` | Khong bat buoc o cap model, nhung cac method `Utilizes` doc truc tiep khong null-check (xem `Utilizes-CallApiWithHttp.md`/`Utilizes-CallApi.md`) | Khong co validate trong `HttpOptionModel` | `null` |
| `BaseAddress` | `string` | Khong | Khong co | `null` |
| `Token` | `string` | Khong | Khong co | `null` |
| `AuthType` | `string` | Khong | Khong co | `"Bearer"` |
| `Uri` | `string` | Khong | Khong co | `null` |
| `SystemOwner` | `string` | Khong | Khong co | `"Service Request"` |
| `CompletionOption` | `HttpCompletionOption` | Khong | Khong co | `HttpCompletionOption.ResponseContentRead` |
| `Value` (chi tren `HttpOptionModel<T>`) | `T` (rang buoc `where T : notnull`) | Khong — chi co `init`, khong co gia tri mac dinh gan san | Rang buoc generic `notnull` la **compile-time**; khong co runtime check nao dam bao `Value` khac null sau khi khoi tao | `default(T)` (vi khong co property initializer) |

**Output** — Khong ap dung (day la kieu du lieu dau vao cho cac method o tang `Utilizes`).

**Dieu kien xu ly** — Khong co logic dieu kien trong chinh 2 record nay; toan bo logic doc/dung cac property (vi du: chi gan `Authorization` khi `Token` khac rong, chi gan `BaseAddress` khi khac rong) nam trong `HttpContentExtensionsUtilizes.ConfigHttpClient` (`HttpClientUtilizes.cs:343-360`) — thuoc pham vi tai lieu cua `Utilizes-CallApiWithHttp.md`/`Utilizes-CallApi.md`, khong thuoc file model nay.

**Side effect** — Khong co tren chinh model. `HttpOptionModel<T>.Value` dung `init` (khong phai `set`) nen **khong the gan lai** sau khi object da duoc khoi tao bang object initializer.

**Error handling** — Khong co.

**Khi nao NEN dung** — Moi lan goi mot method o `CallApiWithHttp<TRequest, TResponse>`/`CallApi<TResponse>`; dung `HttpOptionModel` (khong generic) khi khong can mang du lieu request/query, dung `HttpOptionModel<T>` khi can (`Value`).

**Khi nao KHONG dung** — Khong dung `HttpOptionModel.Client` de mang mot `HttpClient` da tung goi request truoc do neu can `BaseAddress` khac — `ConfigHttpClient` gan lai `client.BaseAddress` moi lan, co the nem `InvalidOperationException` neu client da gui request (chi tiet trong `Utilizes-CallApiWithHttp.md`).

**Gioi han** —
- Khong co `[JsonIgnore]` tren `Client` (`HttpClient`); khi log serialize nguyen `option` (nhu `CallApiWithHttp.cs` lam trong khoi `finally`), `Client` va `Token` deu nam trong pham vi serialize — day la van de bao mat da duoc `Utilizes-CallApiWithHttp.md` ghi nhan (muc 3, van de token bi log).
- `HttpOptionModel<T>.Value` khong co gia tri mac dinh gan san du co rang buoc `where T : notnull` — rang buoc nay chi ngan caller truyen kieu `Nullable` lam type argument (vi du khong the dung `HttpOptionModel<int?>`), **khong** ngan `Value` nhan gia tri `null` khi `T` la reference type va caller khong gan gi (property khong bat buoc trong object initializer). Cac method trong `CallApiWithHttp.cs` (vi du `GetAsJSonAsync`) co kiem tra `option.Value is null` truoc khi build query string — cho thay chinh code goi cung khong coi `notnull` la bao dam runtime.

### 2.5 TokenResultModel

**Signature**
```csharp
public class TokenResultModel
{
    public string Type { get; set; }
    public string Token { get; set; }
    public long ExpiresAt { get; set; }
}
```
Nguon: `TokenResultModel.cs:3-10`.

**Muc dich** — Mang ket qua mot token (kieu token, gia tri token, thoi diem het han duoi dang `long`). Ten property goi y day la ket qua tra ve tu mot buoc lay/refresh token, nhung **khong co code nao trong repo nay khoi tao hoac doc `TokenResultModel`** de xac nhan luong su dung thuc te.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `Type` | `string` | Khong | Khong co | `null` |
| `Token` | `string` | Khong | Khong co | `null` |
| `ExpiresAt` | `long` | Khong | Khong co | `0` |

**Output** — Khong ap dung.

**Dieu kien xu ly** — Khong co.

**Side effect** — Khong co.

**Error handling** — Khong co.

**Khi nao NEN dung** — Khong xac dinh duoc tu source code (khong co call site trong repo).

**Khi nao KHONG dung** — `ExpiresAt` la `long` khong co don vi ro rang trong ten (giay Unix epoch? milliseconds?) — khong xac dinh duoc tu source code don vi thuc te cua truong nay, vi khong co code nao gan/doc gia tri de doi chieu (so sanh voi `TokenExpirationHelperUtilizes.IsExpiration`/`GetExpirationTime` trong `HttpClientUtilizes.cs`, cac ham nay lam viec truc tiep tren chuoi JWT tho, khong dung `TokenResultModel`).

**Gioi han** — La `class` (reference type, mutable qua `set`) thay vi `record` nhu 4 model con lai trong cung namespace `Https` — khong dong nhat kieu du lieu trong cung module; khong co `Equals`/`ToString` theo gia tri (value-based) nhu cac record khac.

## 3. Van de da biet

| # | Van de | Vi tri | Anh huong |
|---|---|---|---|
| 1 | `AuthModel`, `BaseApiModel`, `TokenResultModel` khong duoc bat ky file source nao khac trong repo nay khoi tao/tham chieu (chi co dinh nghia) | `AuthModel.cs`, `BaseApiModel.cs`, `TokenResultModel.cs` (toan bo repo, ngoai `.claude/worktrees`) | Thap-Trung binh. Khong xac dinh duoc tu source code muc dich su dung thuc te trong he thong nay; co the la model danh cho project ngoai repo tieu thu thu vien Shared, hoac la dead code |
| 2 | `ErrorModel.ErrorDeConstruct` duoc dinh nghia nhung khong co call site nao trong repo | `ErrorModel.cs:11-15` | Thap. Method "du tru", khong anh huong hanh vi hien tai; ten method khong theo quy uoc `Deconstruct` cua C# nen khong the dung cu phap deconstruction ngam dinh |
| 3 | **KB cu mo ta thieu**: `docs/knowledge-base/Utilizes-CallApi.md` dong 34 mo ta `ErrorModel` la "Chua `Code` (int), `Message` (string), `Succeeded` (bool)" — **khong nhac den method `ErrorDeConstruct`**, trong khi source code thuc te co method nay (`ErrorModel.cs:11-15`) va file KB con lai (`Utilizes-CallApiWithHttp.md` dong 42) da mo ta day du hon: "kem `ErrorDeConstruct(out message, out statusCode)`". Hai file KB cu mo ta khong dong nhat ve cung mot kieu `ErrorModel` | `docs/knowledge-base/Utilizes-CallApi.md:34` (thieu) vs `docs/knowledge-base/Utilizes-CallApiWithHttp.md:42` (day du) vs `ErrorModel.cs:11-15` (nguon xac thuc) | Thap. Khong sai ve du lieu (Code/Message/Succeeded van dung), nhung mo ta khong day du/khong dong nhat giua 2 tai lieu KB cho cung mot kieu du lieu — khong sua trong buoc nay, chi ghi nhan de buoc Reconcile xu ly |
| 4 | `HttpOptionModel<T>.Value` khong co gia tri mac dinh gan san; rang buoc generic `where T : notnull` chi la kiem tra bien dich (type argument), khong bao dam gia tri runtime khac null khi `T` la reference type | `HttpOptionModel.cs:20-23` | Trung binh. Cac method o `CallApiWithHttp.cs` phai tu kiem tra `option.Value is null` truoc khi dung — neu mot method moi quen kiem tra, co the gap `NullReferenceException` khong luong truoc |
| 5 | `BaseApiModel.Status` (string tu do, mac dinh `"OK"`) va `BaseApiModel.Code`/`Message` (theo `HttpStatusCode`, mac dinh `200`/`"OK"`) la 2 co che bieu dien trang thai doc lap, khong co logic dong bo giua chung | `BaseApiModel.cs:7-9` | Thap. Neu code ben ngoai chi gan lai mot trong hai (vi du chi doi `Code` sang 500 ma quen doi `Status`), du lieu tra ve se mau thuan noi bo |
| 6 | `TokenResultModel` la `class` (mutable, reference-equality) trong khi 4 model con lai trong cung file/namespace deu la `record` | `TokenResultModel.cs:3` vs `AuthModel.cs:3`, `BaseApiModel.cs:5`, `ErrorModel.cs:3`, `HttpOptionModel.cs:3` | Thap. Khong dong nhat phong cach khai bao kieu du lieu trong cung module `Models/Https` |
| 7 | `ErrorModel`, `HttpOptionModel`, `HttpOptionModel<T>`, `AuthModel`, `BaseApiModel`, `TokenResultModel` deu khong co `/// <summary>` XML doc | Toan bo 5 file | Thap. Phai doc than record/property truc tiep de xac dinh hanh vi (da thuc hien trong tai lieu nay); khong anh huong runtime |

