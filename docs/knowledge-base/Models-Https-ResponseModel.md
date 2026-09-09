# ResponseModel (Https)

> Nguon: `FTELSRCore.Shared/Models/Https/ResponseModel/*.cs` (13 file, moi file 1 model/nhom model)
> Loai: record / class POCO (DTO), khong co logic
> Trang thai: **moi duoc dua vao repo trong working tree, chua co commit** — nguon goc la
> `SRWebCoreAPI.Shared/Models/Https/ResponseModel/*.cs` cua repo `sr-request-api` (commit `f2da98f`
> cua repo do), duoc chuyen (clone) nguyen trang vao `FTELSRCore.Shared` de dung chung cho nhieu service.
> Neu repo tiep tuc thay doi, phai doc lai source truoc khi tin noi dung tai lieu nay.

## 1. Tong quan

Day la tap **response model** (DTO) mo ta hinh dang JSON tra ve tu **13 he thong nguon** (upstream) khac nhau ma cac service SR goi qua HTTP. Moi model ung voi mot "hop dong" response rieng cua mot he thong ngoai — **khong** co diem chung ve field (mot so dung `Code`, mot so dung `StatusCode`, mot so khong co code nao), duoc chuan hoa boi tang `HandlerResponseHttpClientUtilizes` (xem `Utilizes-HandlerResponseHttpClientUtilizes.md`) thanh mot kieu `Result<TResult>` duy nhat.

> [!NOTE]
> Cac model nay **chi la POCO/record khai bao field** — khong co method, khong co validate, khong co logic deserialize rieng. Viec deserialize (System.Text.Json qua `PropertyNameCaseInsensitive`) va viec anh xa sang `Result<TResult>` nam o noi khac (`HttpContentExtensionsUtilizes.ReadAsStreamAsync`, `HandlerResponseHttpClientUtilizes`).

### 1.1 Danh muc model

| File | Model | Kieu khai bao | He thong nguon (suy tu ten) | Generic |
|---|---|---|---|---|
| `BaseResponseModel.cs` | `BaseResponseModel<TData>` | `class` | Khong xac dinh — dung khi he thong nguon **khong co wrapper rieng** | `<TData> where TData : notnull` |
| `SRBaseResponseModel.cs` | `SRBaseResponseModel` / `SRBaseResponseModel<TModel>` | `record` (base + generic ke thua) | He thong SR moi | `<TModel> where TModel : notnull` |
| `SRBaseV1ResponseModel.cs` | `SRBaseV1ResponseModel` / `SRBaseV1ResponseModel<TModel>` | `record` (base + generic ke thua) | He thong SR cu (v1) | `<TModel> where TModel : notnull` |
| `PaymentResponseModel.cs` | `PaymentResponseModel` / `PaymentResponseModel<TModel>` | `record` (base + generic ke thua) | He thong thanh toan | `<TModel> where TModel : notnull` |
| `CoreReinventResponseModel.cs` | `CoreReinventResponseModel<TData>` | `class` | He thong Reinvent | `<TData> where TData : notnull` |
| `ProductInternetResponseModel.cs` | `ProductInternetResponseModel<TData>` | `class` | He thong nguon Internet | `<TData> where TData : notnull` |
| `TicketSupportResponseModel.cs` | `TicketSupportResponseModel<TData>` | `class` | He thong Ticket Support | `<TData> where TData : notnull` |
| `ACSResponseModel.cs` | `ACSResponseModel<TData>` | `class` | He thong ACS | `<TData> where TData : notnull` |
| `InsideResponseModel.cs` | `InsideResponseModel<TData>` | `class` | He thong Inside | `<TData> where TData : notnull` |
| `ProductPartnerResponseModel.cs` | `ProductPartnerResponseModel<TData>` | `class` | He thong doi tac san pham | `<TData> where TData : notnull` |
| `LoyaltyResponseModel.cs` | `LoyaltyResponseModel<TData>` | `class` | He thong Loyalty (khach hang than thiet) | `<TData> where TData : notnull` |
| `TransactionResponseModel.cs` | `TransactionResponseModel<TData>` | `class` | He thong Transaction | `<TData> where TData : notnull` |
| `CPEResponseModel.cs` | `CPEResponseModel<TData>` | `class` | He thong CPE (thiet bi dau cuoi khach hang) | `<TData> where TData : notnull` |

Tong: **13 file**, **16 type** (3 file co ca dang khong-generic lam base va dang generic ke thua: `SRBaseResponseModel`, `SRBaseV1ResponseModel`, `PaymentResponseModel`).

> [!IMPORTANT]
> **Ten "he thong nguon" trong bang tren la suy dien tu ten class/comment, khong phai tu spec API chinh thuc nao trong repo.** Repo nay khong chua tai lieu OpenAPI/Swagger cua cac he thong ngoai do. Khi tich hop that, phai doi chieu voi tai lieu API cua tung he thong ngoai truoc khi tin cay ten field.

### 1.2 Diem khac biet ve hinh dang field

Day la diem quan trong nhat can nam khi doc file nay: **khong co field nao xuat hien dong nhat tren ca 13 model**. Bang duoi doi chieu truc tiep tung field:

| Model | Field "code" | Field message | Field data | Field co trang thai rieng | Field khac |
|---|---|---|---|---|---|
| `BaseResponseModel<TData>` | — | — | `Data` | — | — |
| `SRBaseResponseModel<TModel>` | `Code` (int) | `Messages` (`List<string>`) | `Data` (ke thua tu base) | `Succeeded` (bool) | `Status` (string), `System` (string) |
| `SRBaseV1ResponseModel<TModel>` | `Code` (int) | `Message` (string, **so nhieu — khac ban SR moi**) | `Data` | `Succeeded` (bool) | `Status`, `System` |
| `PaymentResponseModel<TModel>` | `Code` (int) | `Message` (string) | `Data` | `Succeeded` (bool) | `Status` la **`dynamic`** (khong phai `string`), `System` |
| `CoreReinventResponseModel<TData>` | — (khong co code so) | `Message`, `Description` | `Data` | `Success` (bool) | `ErrorData`, `ExceptionMessage`, `ClientRequestId` |
| `ProductInternetResponseModel<TData>` | `Error` (`int?`, **khac ten `Code`**) | `Message` | `Data` | — | `ErrorData` (JSON: `error_data`) |
| `TicketSupportResponseModel<TData>` | `ErrorCode` (`int?`) | `ErrorDescription` (**khong phai `Message`**) | `Data` | — | — |
| `ACSResponseModel<TData>` | `Code` (int, **luon co, khong nullable**) | `Message` | `Data` (JSON key `"Data"` viet hoa) | — | — |
| `InsideResponseModel<TData>` | `StatusCode` (int, **khac ten `Code`**) | `Message` | `Data` **va** `Result` (2 field cung kieu `TData`) | — | `Error` (`dynamic`) |
| `ProductPartnerResponseModel<TData>` | — | `Message` | `Data` | `Success` (bool) | `DataError` (`List<string>`), `ExceptionMessage`, `ClientRequestId` |
| `LoyaltyResponseModel<TData>` | `StatusCode` (int) | — (khong co field message) | `Data` | `Success` (bool) | — |
| `TransactionResponseModel<TData>` | `StatusCode` (int) | — (khong co field message) | `Data` | — | `Title` (string) |
| `CPEResponseModel<TData>` | — (khong co code so) | `Message` | `Data` | — | `StatusKey` (JSON: `status_key`, kieu `string`) |

> [!CAUTION]
> He qua truc tiep cua bang tren: **khong the viet 1 ham generic duy nhat** de doc "code"/"message" cho ca 13 model — day chinh la ly do ton tai 13 extension method rieng trong `HandlerResponseHttpClientUtilizes` (xem file do) thay vi 1 ham chung.

### 1.3 Serialize attribute

| Nhom | `[JsonPropertyName]` | Ghi chu |
|---|---|---|
| `SRBaseResponseModel`, `SRBaseV1ResponseModel`, `PaymentResponseModel`, `BaseResponseModel`, `TransactionResponseModel` | **Khong co** | Dua vao ten property + `PropertyNameCaseInsensitive` cua `System.Text.Json` (cau hinh tai `HttpContentExtensionsUtilizes`, xem `Utilizes-HttpClientUtilizes.md`) de match JSON khong phan biet hoa/thuong |
| `CoreReinventResponseModel`, `ProductInternetResponseModel`, `TicketSupportResponseModel`, `ACSResponseModel`, `InsideResponseModel`, `ProductPartnerResponseModel`, `LoyaltyResponseModel`, `CPEResponseModel` | **Co**, tren tung property | Anh xa JSON key khac casing/ten C# — vi du `ProductInternetResponseModel.ErrorData` <- JSON `"error_data"` (snake_case), `CPEResponseModel.StatusKey` <- JSON `"status_key"` |

### 1.4 Rang buoc generic

Tat ca 16 type deu rang buoc `where TData : notnull` (hoac `TModel : notnull`) — **khong the** dung kieu value-type nullable (`int?`, `string` **duoc phep** vi `notnull` chi chan `null` literal/kieu nullable value type, khong chan reference type) lam `TData` neu kieu do co the gan `null`. Trong thuc te `TData`/`TModel` luon la mot DTO nghiep vu (class/record) do caller tu dinh nghia.

## 2. Khi nao dung model nao

> [!IMPORTANT]
> **Khong tu tao model moi neu he thong nguon co the map vao 1 trong 13 model o tren.** Truoc khi viet them 1 response model moi cho mot he thong ngoai, hay doi chieu bang muc 1.2: neu hinh dang JSON (ten field code/message/data) trung khop mot model da co, **tai su dung** model do thay vi nhan ban.

| Neu he thong nguon tra JSON co dang | Dung model |
|---|---|
| `{ code, status, succeeded, system, messages: [] }` | `SRBaseResponseModel<TModel>` |
| `{ code, status, succeeded, system, message }` (message **so it**, khong phai mang) | `SRBaseV1ResponseModel<TModel>` hoac `PaymentResponseModel<TModel>` (khac o `Status` la `dynamic`) |
| `{ errorData, exceptionMessage, clientRequestId, message, description, success, data }` | `CoreReinventResponseModel<TData>` |
| `{ error, error_data, message, data }` | `ProductInternetResponseModel<TData>` |
| `{ ErrorCode, ErrorDescription, data }` | `TicketSupportResponseModel<TData>` |
| `{ code, Data, message }` (chu y `Data` viet hoa trong JSON) | `ACSResponseModel<TData>` |
| `{ statusCode, data, result, message, error }` | `InsideResponseModel<TData>` |
| `{ success, message, data, dataError, exceptionMessage, clientRequestId }` | `ProductPartnerResponseModel<TData>` |
| `{ statusCode, success, data }` (khong co message) | `LoyaltyResponseModel<TData>` |
| `{ data, statusCode, title }` (khong co message) | `TransactionResponseModel<TData>` |
| `{ status_key, data, message }` | `CPEResponseModel<TData>` |
| Response **khong co wrapper**, chi tra thang object du lieu | `BaseResponseModel<TData>` (boc `Data` thu cong, hoac deserialize thang neu API design cho phep) |

## 3. Van de da biet

| # | Van de | Vi tri | Anh huong |
|---|---|---|---|
| 1 | Khong co field nao dong nhat giua 13 model (xem bang 1.2) — **khong the** viet ham generic doc "co loi hay khong" ma khong biet truoc kieu model cu the | Toan bo thu muc | Thap ve rui ro runtime, nhung la ly do bat buoc phai co 13 extension method rieng trong `Utilizes-HandlerResponseHttpClientUtilizes.md` thay vi 1 ham dung chung |
| 2 | `PaymentResponseModel.Status` khai bao kieu `dynamic`, khac `SRBaseResponseModel.Status`/`SRBaseV1ResponseModel.Status` la `string` | `PaymentResponseModel.cs:7` | Thap. De nham lan khi so sanh 2 model tuong tu nhau; deserialize JSON string van hoat dong (dynamic nhan moi kieu) nhung mat kiem tra kieu luc bien dich |
| 3 | `InsideResponseModel<TData>` co **2 field cung kieu `TData`**: `Data` va `Result` — khong ro he thong Inside dung field nao la chinh; tang xu ly phia tren (`InsideResultHttpClient`, xem `Utilizes-HandlerResponseHttpClientUtilizes.md`) **chi doc `Data`**, bo qua `Result` hoan toan | `InsideResponseModel.cs:8-14` | Trung binh. Neu he thong Inside thuc te tra du lieu trong `Result` thay vi `Data`, tang xu ly hien tai se luon coi la "khong tim thay thong tin" |
| 4 | `TransactionResponseModel<TData>` co field `Title` nhung khong co field message/status nao khac; tang xu ly phia tren khong doc `Title` o bat ky nhanh nao | `TransactionResponseModel.cs:9` | Thap. `Title` hien khong co tac dung trong luong xu ly response hien tai |
| 5 | `CoreReinventResponseModel`/`ProductInternetResponseModel` co field mo ta loi chi tiet (`ErrorData`, `ExceptionMessage`, `ClientRequestId`, `Description`, `Error`) nhung tang xu ly phia tren khong doc cac field nay — chi doc `Message`/`Data`/`Success` | `CoreReinventResponseModel.cs`, `ProductInternetResponseModel.cs` | Thap-Trung binh. Thong tin debug chi tiet tu he thong nguon bi bo qua, chi con message ngan gon (hoac message mac dinh tieng Viet) den tay caller |
| 6 | Khong co annotation `[Required]`/validate nao tren bat ky property — deserialize thanh cong ke ca khi field bat buoc (theo nghiep vu) bi thieu trong JSON, ket qua la property `null`/`default` | Toan bo thu muc | Trung binh. Rui ro `NullReferenceException` o tang doc field (`HandlerResponseHttpClientUtilizes`) neu he thong nguon tra response khong day du field nhu mong doi |

---

**Xem them:** [`Models-Https.md`](Models-Https.md) (model ha tang dung chung: `ErrorModel`, `HttpOptionModel`, `AuthModel`...), [`Utilizes-HandlerResponseHttpClientUtilizes.md`](Utilizes-HandlerResponseHttpClientUtilizes.md) (tang anh xa 13 model nay sang `Result<TResult>`), [`Wrappers-Result.md`](Wrappers-Result.md) (`Result<T>`, kieu tra ve chuan hoa cuoi cung).
