# HandlerResponseHttpClientUtilizes

> Nguon: `FTELSRCore.Shared/Utilizes/HandlerResponseHttpClientUtilizes.cs` (dong 1 den 417)
> Loai: static class chi chua extension method (13 method, khong co field/state)
> Trang thai: **moi duoc dua vao repo trong working tree, chua co commit** — clone nguyen logic tu
> `SRWebCoreAPI.Shared/Utilizes/HandlerResponseHttpClientUtilizes.cs` cua repo `sr-request-api`
> (repo do dang o commit `f2da98f`), dieu chinh **duy nhat 1 diem** de tuong thich API cua core:
> doi ten goi `Result<TResult>.Success(...)` (chi co ben `sr-request-api`) thanh
> `Result<TResult>.Succeed(...)` (ten thuc su ton tai tren `FTELSRCore.Wrappers.Result<T>` — xem
> `Wrappers-Result.md`). Toan bo dieu kien if/else, gia tri message/statusCode mac dinh giu **nguyen
> ven 100%** so voi ban goc.
> Neu repo tiep tuc thay doi, phai doc lai source truoc khi tin so dong trong tai lieu nay.

## 1. Tong quan

`HandlerResponseHttpClientUtilizes` la tang **anh xa (mapping)** dung sau khi mot cuoc goi HTTP outbound (qua `CallApiWithHttp<TRequest, TResponse>` hoac `CallApi<TResponse>`, xem `Utilizes-CallApiWithHttp.md` / `Utilizes-CallApi.md`) da tra ve tuple `(TResponse, ErrorModel)`. Moi method trong lop nay la **1 extension method tren 1 kieu tuple cu the** `(TResponseModel<TResult> result, TError errorModel)`, nhan dau vao la cap "response model rieng cua 1 he thong nguon" + `ErrorModel`, va tra ve **`Result<TResult>`** — kieu response envelope chuan cua toan he thong SR (xem `Wrappers-Result.md`).

> [!NOTE]
> Day la lop **thuan tuy dong bo, khong co side effect**: khong log, khong goi mang, khong throw (tru truong hop duoc neu o muc 3, van de #1). Dau vao la du lieu **da co san trong tay** (ket qua deserialize + `ErrorModel` da duoc gan tu truoc), khong tu doc `HttpResponseMessage`.

### 1.1 Pham vi chuc nang

| Lam duoc | Khong lam duoc |
|---|---|
| Chuan hoa 13 dang response khac nhau (xem `Models-Https-ResponseModel.md`) thanh 1 kieu `Result<TResult>` duy nhat | Tu goi HTTP — khong nhan `HttpResponseMessage`, chi nhan du lieu da deserialize san |
| Uu tien kiem tra `ErrorModel` (do tang goi HTTP gan tu status code) truoc khi xet den `result` | Validate schema/kieu du lieu ben trong `TResult` — chi kiem tra `result`/`result.Data` co null hay khong |
| Voi 1 so model: kiem tra them co trang thai noi tai cua response (`Success`/`Code == 200`) ngoai `Data != null` (xem muc 1.3) | Retry, cache, hay bat cu logic nghiep vu nao khac ngoai anh xa 1-doi-1 |
| Fallback message mac dinh tieng Viet khi message tu he thong nguon rong/null (**tru CPE**, xem muc 3 van de #2) | Ghi log — khong co tham chieu `ILogger` nao trong lop nay |

### 1.2 Dependency

| Thanh phan | Muc dich su dung |
|---|---|
| `ErrorModel` (`FTELSRCore.Models.Https`, xem `Models-Https.md`) | Rang buoc generic `TError : ErrorModel`; doc `Succeeded`/`Code`/`Message` de quyet dinh nhanh loi dau tien |
| `Result<T>` / `Result<T>.Fail(...)` / `Result<T>.Succeed(...)` (`FTELSRCore.Wrappers`, global using — xem `Wrappers-Result.md`) | Kieu tra ve chuan hoa cuoi cung |
| 13 response model (`FTELSRCore.Models.Https.ResponseModel`, xem `Models-Https-ResponseModel.md`) | Kieu dau vao — moi method ung voi dung 1 model |
| `System.Net.HttpStatusCode` | Chi dung de doi chieu hang so (`BadRequest` = 400, `OK` = 200, `BadGateway` = 502), khong lien quan goi mang thuc te |
| `CollectionHelpers.IsNullOrEmpty<T>` (`FTELSRCore.Helpers`, global using) | Chi duoc dung trong `SRBaseResultHttpClient` de kiem tra `result.Messages` rong truoc khi fallback message mac dinh |

### 1.3 Danh muc API

Tong: **13 public static extension method**, khong co overload trung ten. Ca 13 deu theo **1 khuon mau chung**:

1. Neu `errorModel.Succeeded == false` **va** `errorModel.Code > 400` -> `Result<TResult>.Fail(...)` voi message/statusCode lay tu `errorModel`.
2. Neu `result` null hoac `result.Data` null (mot so model kiem tra them dieu kien rieng, xem cot "Dieu kien loi rieng") -> `Result<TResult>.Fail(...)`.
3. Nguoc lai -> `Result<TResult>.Succeed(...)` voi `data = result.Data`.

Diem khac nhau giua 13 method nam o: **model dau vao**, **nguon lay message/statusCode o nhanh loi/thanh cong**, va **dieu kien loi rieng** (neu co).

| # | Method | Model dau vao | Dieu kien loi rieng (ngoai `Data == null`) | Nguon message nhanh loi | statusCode nhanh loi | Nguon message nhanh thanh cong | statusCode nhanh thanh cong |
|---|---|---|---|---|---|---|---|
| 1 | `SRBaseResultHttpClient` | `SRBaseResponseModel<TResult>` | — | `result.Messages` (nguyen List) | `result.Code` | `result.Messages` (fallback "Thực hiện yêu cầu thành công" neu rong) | `result.Code` |
| 2 | `SRBaseV1ResultHttpClient` | `SRBaseV1ResponseModel<TResult>` | — | `result.Message` | `result.Code` | `result.Message` (fallback "Thực hiện thành công") | `result.Code` |
| 3 | `PaymentResultHttpClient` | `PaymentResponseModel<TResult>` | — | `result.Message` | `result.Code` | `result.Message` (fallback "Thực hiện thành công") | `result.Code` |
| 4 | `CoreReinventResultHttpClient` | `CoreReinventResponseModel<TResult>` | `result is null` gop chung dieu kien voi `Data == null` | `result?.Message` (fallback "Không tìm thấy thông tin") | co dinh `400` | co dinh "Thực hiện thành công" | co dinh `200` |
| 5 | `ProductInternetResultHttpClient` | `ProductInternetResponseModel<TResult>` | nhu tren | `result?.Message` (fallback "Không tìm thấy thông tin") | co dinh `400` | co dinh "Thực hiện thành công" | co dinh `200` |
| 6 | `TicketSupportResultHttpClient` | `TicketSupportResponseModel<TResult>` | nhu tren | `result?.ErrorDescription` (fallback "Không tìm thấy thông tin") | co dinh `400` | co dinh "Thực hiện thành công" | co dinh `200` |
| 7 | `BaseResultHttpClient` | `BaseResponseModel<TResult>` | nhu tren | co dinh "Không tìm thấy thông tin" | co dinh `400` | co dinh "Thực hiện thành công" | co dinh `200` |
| 8 | `ACSResultHttpClient` | `ACSResponseModel<TResult>` | nhu tren **+** `result.Code != 200` | `result?.Message` (fallback "Không tìm thấy thông tin.") | co dinh `400` | `result?.Message` (fallback "Thực hiện thành công") | co dinh `200` |
| 9 | `InsideResultHttpClient` | `InsideResponseModel<TResult>` | nhu #4 | co dinh "Không tìm thấy thông tin." | co dinh `400` | co dinh "Thực hiện thành công" | co dinh `200` |
| 10 | `ProductPartnerResultHttpClient` | `ProductPartnerResponseModel<TResult>` | `result is null` **gop vao nhanh loi dau tien** (khac cach #4-#9 gop vao nhanh thu 2) | `result.Message` (1 phan tu, khong fallback) | co dinh `502 (BadGateway)`, **succeeded lay tu `result.Success`** | co dinh "Thực hiện thành công" | co dinh `200` |
| 11 | `LoyaltyResultHttpClient` | `LoyaltyResponseModel<TResult>` | nhu #4 **+** `result.Success == false` | co dinh "Không tìm thấy thông tin." | co dinh `400` | co dinh "Thực hiện thành công" | co dinh `200` |
| 12 | `TransactionResultHttpClient` | `TransactionResponseModel<TResult>` | nhu #4 | co dinh "Không tìm thấy thông tin." | co dinh `400` | co dinh "Thực hiện thành công" | co dinh `200` |
| 13 | `CPEResultHttpClient` | `CPEResponseModel<TResult>` | nhu #4 | `result.Message` (fallback "Không tìm thấy thông tin" neu rong/khoang trang) | co dinh `400` | **`result.Message` — KHONG fallback** (xem muc 3, van de #1 va #2) | co dinh `200` |

> [!IMPORTANT]
> **Ca 13 method deu dung chung 1 dieu kien nhanh loi dau tien:** `errorModel is { Succeeded: false, Code: > 400 }`. Nghia la neu `errorModel.Code <= 400` (vi du `400` chinh no, hoac `errorModel.Succeeded == true` du co the con gia tri `Code` bat thuong), nhanh nay **bi bo qua**, va logic tiep tuc roi vao buoc kiem tra `result`/`result.Data`. Day khong phai loi — la thiet ke co chu dich de tranh false-negative voi cac status hop le nam duoi 400 — nhung can hieu dung khi debug: "loi tu he thong nguon" chi duoc bat khi status **that su > 400**.

## 2. Chi tiet tung nhom

Vi ca 13 method deu theo dung 1 khuon mau (muc 1.3), phan nay chi neu **dau vao/dau ra chi tiet** va **diem khac biet dang chu y** cho tung method — khong lap lai cau truc if/else chung.

### 2.1 SRBaseResultHttpClient (dong 24-43)

**Signature**

```csharp
public static Result<TResult> SRBaseResultHttpClient<TResult, TError>(
    this (SRBaseResponseModel<TResult> result, TError errorModel) value)
    where TError : ErrorModel
    where TResult : notnull
```

**Muc dich** — He thong SR moi. La method **duy nhat** dung `Fail`/`Succeed` overload nhan `List<string> messages` (khong phai `string message` don) va lay `TError` (`errorModel`) truc tiep tu record base — khop voi field `Messages` (so nhieu) cua `SRBaseResponseModel`.

**Diem rieng** — Nhanh thanh cong (dong 39-42) kiem tra `result.Messages.IsNullOrEmpty()` (qua `CollectionHelpers.IsNullOrEmpty`, dependency duy nhat cua ca lop ngoai `Result`/`ErrorModel`) truoc khi quyet dinh dung `result.Messages` hay fallback `["Thực hiện yêu cầu thành công"]`. **Day la method duy nhat trong lop nay lam dieu do** — cac method khac dung `string.IsNullOrWhiteSpace` (1 message) hoac khong fallback.

### 2.2 SRBaseV1ResultHttpClient (dong 59-74)

He thong SR cu (v1). Cau truc **giong het** `PaymentResultHttpClient` (muc 2.3) — chi khac kieu model dau vao (`SRBaseV1ResponseModel<TResult>` so voi `PaymentResponseModel<TResult>`; field trung ten nhung `PaymentResponseModel.Status` la `dynamic`, xem `Models-Https-ResponseModel.md` van de #2).

### 2.3 PaymentResultHttpClient (dong 91-106)

He thong thanh toan. Xem 2.2 — khong co diem khac biet ve logic.

### 2.4 CoreReinventResultHttpClient (dong 122-137)

He thong Reinvent. Tu day tro di (method #4-#13, tru `ProductPartnerResultHttpClient`), khuon mau chuyen sang: nhanh loi dau tien **chi** kiem tra `errorModel` (khong con `|| value.result is null` gop vao cung dieu kien nhu #1-#3); truong hop `result is null` duoc gop **chung** voi `result.Data is null` o nhanh thu 2 (`if (value.result is null || value.result is { Data: null })`), va ca hai deu tra `statusCode` **co dinh 400** (khong con lay tu `result.Code` nhu #1-#3, vi cac model tu #4 tro di **khong co field code o cap response**, xem `Models-Https-ResponseModel.md`).

**Diem rieng** — Message nhanh loi uu tien `result?.Message` (field co that trong model), chi fallback "Không tìm thấy thông tin" khi `result` chinh no la `null` (luc do khong co gi de doc).

### 2.5 ProductInternetResultHttpClient (dong 153-168)

He thong nguon Internet. Cau truc **giong het** 2.4 — chi khac kieu model. Field `Error` (ma loi so, kieu `int?`) cua `ProductInternetResponseModel` khong duoc doc.

### 2.6 TicketSupportResultHttpClient (dong 181-196)

He thong Ticket Support. Giong 2.4/2.5, nhung message nhanh loi lay tu **`result?.ErrorDescription`** (model nay khong co field `Message`, chi co `ErrorCode`/`ErrorDescription`).

### 2.7 BaseResultHttpClient (dong 210-225)

Dung khi he thong nguon **khong co wrapper rieng** — `BaseResponseModel<TResult>` chi co field `Data`. Vi vay message nhanh loi **luon co dinh** "Không tìm thấy thông tin" (khong co field nao khac de doc), khac voi 2.4-2.6 la co uu tien doc field cua model truoc khi fallback.

### 2.8 ACSResultHttpClient (dong 239-255)

He thong ACS. **Method duy nhat** ma dieu kien nhanh loi thu 2 co them ve trai `or { Code: not (int)HttpStatusCode.OK }` (dong 249): tuc la du `result` khac null va `Data` khac null, response van bi coi la loi neu `result.Code != 200`. Nhanh thanh cong cung la **method duy nhat (cung `CPEResultHttpClient`)** giu nguyen `result?.Message` lam message thay vi hardcode "Thực hiện thành công" — nhung khac CPE, o day **co** fallback `?? "Thực hiện thành công"` khi message rong.

### 2.9 InsideResultHttpClient (dong 270-286)

He thong Inside. Cau truc giong 2.4, nhung **chi doc field `Data`** cua `InsideResponseModel<TResult>` — model nay con co field `Result` (cung kieu `TResult`) va `StatusCode`/`Error`, **khong field nao trong so do duoc doc** boi method nay (xem `Models-Https-ResponseModel.md` van de #3).

### 2.10 ProductPartnerResultHttpClient (dong 301-317)

He thong doi tac san pham. **Khac cau truc nhieu nhat** trong nhom #4-#13:

- Dieu kien `result is null` duoc gop **vao nhanh loi dau tien** (dong 306: `value.errorModel is {...} || value.result is null`) — giong nhom #1-#3, **khong** giong nhom #4-#9/#11-#13.
- Nhanh loi thu 2 (`result.Data is null`, dong 311-314) tra `statusCode` **co dinh `502 (BadGateway)`** — **gia tri statusCode duy nhat khac `400` trong toan bo nhom nhanh-loi-thu-2** cua lop nay.
- `succeeded` cua `Result<TResult>.Fail(...)` o nhanh nay **lay nguyen tu `result.Success`** (co the la `true`!) thay vi hardcode `false` nhu 12 method con lai — nghia la co truong hop tra ve mot `Result<TResult>` co `Succeeded = true` nhung **khong co `Data`** (chi co message loi). Xem muc 3, van de #3.

### 2.11 LoyaltyResultHttpClient (dong 332-348)

He thong Loyalty. Cau truc giong 2.4, **them dieu kien** `|| value.result is { Success: false }` (dong 342) — day la 1 trong 2 method (cung `ACSResultHttpClient`) kiem tra co trang thai noi tai cua response ngoai `Data != null`.

### 2.12 TransactionResultHttpClient (dong 362-378)

He thong Transaction. Cau truc giong het 2.7 (`BaseResultHttpClient`) ve mat message co dinh, nhung nhan model rieng `TransactionResponseModel<TResult>`. Field `Title` cua model khong duoc doc.

### 2.13 CPEResultHttpClient (dong 397-415)

He thong CPE. Nhanh loi (dong 407-412) uu tien `result.Message` (fallback "Không tìm thấy thông tin" qua `string.IsNullOrWhiteSpace`), **nhung nhanh thanh cong (dong 414) truyen thang `value.result.Message` khong qua bat ky fallback nao** — khac voi tat ca 12 method con lai (tru trach nhiem rieng cua ACS da neu o 2.8, ACS van con fallback). Xem muc 3, van de #1 va #2 ve rui ro `NullReferenceException` va message rong.

## 3. Van de da biet

| # | Van de | Vi tri | Anh huong |
|---|---|---|---|
| 1 | **`CPEResultHttpClient` co the nem `NullReferenceException`.** Dieu kien `if (value.result is null \|\| value.result is { Data: null })` (dong 407) chap nhan ca truong hop `value.result` la `null`; nhung ngay ben trong nhanh do, bieu thuc `!string.IsNullOrWhiteSpace(value.result.Message)` (dong 410) **truy cap `value.result.Message` ma khong kiem tra null lai** — neu `value.result` thuc su la `null` (khong phai chi `Data` null), dong nay nem `NullReferenceException` ngay lap tuc, thoat khoi ham ma khong tra ve `Result<TResult>.Fail(...)` nhu 12 method con lai deu lam duoc trong tinh huong tuong tu | `HandlerResponseHttpClientUtilizes.cs:407-411` | **Cao.** Day la loi **duy nhat trong ca lop co the crash caller** thay vi tra ve `Result` mieu ta loi mot cach an toan; loi nay **da ton tai y nguyen o ban goc** `SRWebCoreAPI.Shared` (repo `sr-request-api`), khong phai loi phat sinh trong qua trinh clone — xem doi chieu source o dau file. Caller dung `CPEResultHttpClient` bat buoc phai dam bao `result` khac null truoc khi goi, hoac boc `try/catch` |
| 2 | `CPEResultHttpClient` nhanh thanh cong (dong 414) truyen thang `value.result.Message` lam message ma khong fallback ve chuoi mac dinh nhu 12 method con lai deu lam (vi du "Thực hiện thành công") | `HandlerResponseHttpClientUtilizes.cs:414` | Trung binh. Neu he thong CPE tra response 2xx voi `message` rong/null, `Result<TResult>.Messages` cua ket qua thanh cong se chua gia tri rong/null — caller hien thi truc tiep message nay cho end-user co the thay man hinh trong |
| 3 | `ProductPartnerResultHttpClient` nhanh `result.Data == null` (dong 311-314) truyen `succeeded: value.result.Success` thay vi hardcode `false` — neu he thong doi tac tra `success: true` nhung `data: null` (mau thuan noi tai cua chinh response), ket qua tra ve la mot `Result<TResult>.Fail(...)` **nhung co `Succeeded = true`** | `HandlerResponseHttpClientUtilizes.cs:313` | Trung binh. Caller neu chi kiem tra `result.Succeeded` ma khong kiem tra `result.Data != null` co the coi day la thanh cong trong khi thuc chat khong co du lieu tra ve — cung nhom rui ro voi van de #3 cua `Utilizes-CallApiWithHttp.md` (`EnsureSuccessOrException` khong nem voi 4xx) |
| 4 | Nhanh loi dau tien (`errorModel.Succeeded == false && errorModel.Code > 400`) khong kich hoat khi `errorModel.Code <= 400` (vi du dung bang `400`, hoac `errorModel.Succeeded == true` nhung `Code` bat thuong) — logic roi tiep xuong buoc kiem tra `result`/`result.Data` | Ca 13 method | Thap-Trung binh. La thiet ke co chu dich (xem ghi chu muc 1.3), nhung neu he thong nguon tra `errorModel.Code == 400` chinh xac kem `Succeeded = false`, loi do se **khong** duoc bat o nhanh dau ma phu thuoc hoan toan vao buoc kiem tra `result`/`Data` phia sau — neu `result` luc do khac null va `Data` khac null (vi du server tra ca body loi lan `errorModel`), ket qua co the bi coi la thanh cong |
| 5 | Khong co method nao trong lop nay validate `TResult` (kieu du lieu ben trong `Data`) — chi kiem tra `Data == null`, khong kiem tra field ben trong `TResult` co hop le hay khong | Ca 13 method | Thap. Nam ngoai trach nhiem cua tang mapping nay; validate (neu can) phai thuc hien o tang goi sau khi nhan `Result<TResult>` |
| 6 | Lop nay khong log bat ky truong hop loi nao (khac han `CallApiWithHttp`/`CallApi`, xem `Utilizes-CallApiWithHttp.md`/`Utilizes-CallApi.md` — ca hai deu ghi `logger.HttpResultWithTracing` trong `finally`) | Ca 13 method | Thap. Neu can dieu tra ly do 1 response bi coi la loi o tang nay, phai dua vao log da duoc ghi truoc do o tang goi HTTP (`CallApiWithHttp`/`CallApi`), khong co log rieng cua `HandlerResponseHttpClientUtilizes` |
| 7 | Method duoc dat ten theo dang **extension tren tuple** (`this (TModel result, TError errorModel) value)`) — bat buoc caller phai tu tao tuple `(response, errorModel)` truoc khi goi (vi du `(response, errorModel).SRBaseResultHttpClient()`), khong the goi nhu static method thong thuong voi 2 tham so roi | Ca 13 method | Thap. Chi la luu y ve cach goi, khong phai loi hanh vi |

---

**Xem them:** [`Models-Https-ResponseModel.md`](Models-Https-ResponseModel.md) (13 response model dau vao), [`Models-Https.md`](Models-Https.md) (`ErrorModel`), [`Wrappers-Result.md`](Wrappers-Result.md) (`Result<T>`, `Fail`/`Succeed`), [`Utilizes-CallApiWithHttp.md`](Utilizes-CallApiWithHttp.md) va [`Utilizes-CallApi.md`](Utilizes-CallApi.md) (tang goi HTTP tao ra tuple `(TResponse, ErrorModel)` dau vao cho lop nay).
