# Enum & Customer-type helpers

> Nguon: `FTELSRCore.Shared/Enum/StatusMessageHistories.cs`, `FTELSRCore.Shared/Enum/TypeInfomationCustomerEnum.cs`, `FTELSRCore.Shared/Helpers/TypeInfomationCustomerHelpers.cs`
> Loai: enum (2) + static class (1)
> Cap nhat theo commit: `89c1ce9`

## 1. Tong quan

Module nay gom 2 enum doc lap (khong lien quan truc tiep ve nghiep vu voi nhau) va 1 static helper class:

- `StatusMessageHistories` (`StatusMessageHistories.cs:7`): enum trang thai cho lich su gui/nhan tin nhan (message history).
- `TypeInfomationCustomerEnum` (`TypeInfomationCustomerEnum.cs:3`): enum liet ke cac "loai thong tin dinh danh khach hang" co the dung de tim kiem (ma KH, so hop dong, SDT, CMND/CCCD, MST, email, ho ten).
- `TypeInfomationCustomerHelpers` (`TypeInfomationCustomerHelpers.cs:5`): static class chua logic doan (guess/classify) mot chuoi input tu do thuoc loai nao trong `TypeInfomationCustomerEnum`, dua tren tap regex heuristic, va logic phu tro xac dinh loai tim kiem nao can khop chinh xac (exact).

Ve vi tri kien truc: day la cac thanh phan dung chung (shared) nam trong project `FTELSRCore.Shared`, duoc du kien tai su dung boi cac service khac (vi du module tim kiem khach hang / module luu lich su tin nhan) o tang nghiep vu ben tren. **Trong pham vi repo nay chi co duy nhat 1 csproj (`FTELSRCore.Shared.csproj`)** nen khong quan sat duoc noi tieu thu (consumer) thuc te; xem muc 5 ve so lieu grep tham chieu.

### 1.1 Pham vi chuc nang

| Lam duoc | Khong lam duoc |
|---|---|
| Khai bao ma trang thai chuan hoa (`StatusMessageHistories`) cho lich su gui/nhan tin nhan | Khong co logic chuyen doi trang thai (state machine), chi la danh sach hang so |
| Khai bao danh sach loai thong tin dinh danh khach hang (`TypeInfomationCustomerEnum`) | Khong co mo ta (XML doc) cho enum nay va cac gia tri cua no |
| Doan (heuristic) 1 chuoi input tu do thuoc 1 hoac nhieu loai `TypeInfomationCustomerEnum` dua tren regex (`TypeInfomationCustomerEnums`) | Khong validate dinh dang input theo tieu chuan nghiep vu (vi du khong kiem tra checksum CMND/CCCD, khong kiem tra dinh dang email day du chuan RFC) |
| Xac dinh loai tim kiem nao trong 7 gia tri enum can khop chinh xac (exact) va loai nao khong (`GetExactForTypeInfomationCustomerEnum`) | Khong loai tru lan nhau: 1 input co the duoc gan dong thoi nhieu loai (vi du vua la ObjId vua la IdentityNo) |
| | Khong ghi log, khong goi DB/API ngoai, thuan la ham tinh toan trong bo nho |

### 1.2 Dependency

| Thanh phan | Muc dich su dung |
|---|---|
| `System.Text.RegularExpressions.Regex` | Toan bo logic doan loai du lieu trong `TypeInfomationCustomerHelpers` dua tren `Regex.IsMatch` |
| `System.Linq` (`Any`, `Distinct`, `OrderBy`) | Khu trung va sap xep ket qua tra ve cua `TypeInfomationCustomerEnums` |

Khong co dependency toi cac model/extension da duoc tai lieu hoa trong 8 file Knowledge Base hien co (`AuditModel`, `HttpOptionModel`, `ErrorModel`, `CustomException`, `ProjectToExtensions`, `PrecateBuilderExtensions`, `MeasureExecutionTimeExtensions.InvokeForHTTP`, `MongoResiliencePolicyFactory`, `BaseEntityMongoDB`/`BaseEntitySQL`) — da doc toan bo 3 file source, khong xuat hien tham chieu nao toi cac kieu nay, nen khong ap dung buoc doi chieu nguoc cho module nay.

### 1.3 Danh muc API

| API | Nhom | Mo ta ngan |
|---|---|---|
| `StatusMessageHistories` (enum) | Enum trang thai | 8 gia tri trang thai lich su tin nhan |
| `TypeInfomationCustomerEnum` (enum) | Enum phan loai | 7 gia tri loai thong tin dinh danh khach hang |
| `IsSearch` | Detector (private) | Doan input co dang "ho ten, ten cong ty" (co dau phay) |
| `IsPhone` | Detector (private) | Doan input dang so dien thoai VN |
| `IsContract` | Detector (private) | Doan input dang so hop dong (chu + so) |
| `IsName` | Detector (private) | Doan input "khong bat dau bang so/dau +/khoang trang" |
| `IsPassport` | Detector (private) | Doan input dang CMND (9 so) hoac CCCD/Passport (12 so) |
| `IsIdentityNo` | Detector (private) | Doan input co chua tu 5 chu so lien tiep tro len |
| `IsEmail` | Detector (private) | Doan input dang dia chi email |
| `IsTaxCode` | Detector (private) | Doan input dang ma so thue theo tien to `mst` |
| `IsObjId` | Detector (private) | Doan input toan bo la chu so (ma doi tuong/ObjId) |
| `TypeInfomationCustomerEnums` | Entry point (public) | Chay tat ca detector tren 1 input, tra ve danh sach loai co the khop |
| `GetExactForTypeInfomationCustomerEnum` | Entry point (public) | Tra ve loai tim kiem co can khop chinh xac (exact) hay khong |

---

## 2. StatusMessageHistories (enum)

**Nguon**: `FTELSRCore.Shared/Enum/StatusMessageHistories.cs`
**Namespace**: `SRWebCoreAPI.Shared.Enum` (`StatusMessageHistories.cs:1`)
**Kieu underlying**: `byte` (`StatusMessageHistories.cs:7`)

Enum bieu dien trang thai xu ly cua 1 ban ghi lich su tin nhan (gui / nhan). Tat ca 8 gia tri deu co XML doc `/// <summary>` mo ta bang tieng Viet trong source.

| Ten gia tri | Gia tri thuc | Y nghia (theo comment trong code) | Noi dung tham chieu |
|---|---|---|---|
| `Pending` | 0 | "Tin nhắn đang chờ xử lý." (`StatusMessageHistories.cs:12`) | Khong tim thay noi su dung nao khac trong repo ngoai file dinh nghia (xem muc 5) |
| `SendSuccess` | 1 | "Gửi tin nhắn thành công." (`StatusMessageHistories.cs:17`) | nt |
| `SendFail` | 2 | "Gửi tin nhắn thất bại." (`StatusMessageHistories.cs:22`) | nt |
| `SendError` | 3 | "Lỗi khi gửi tin nhắn." (`StatusMessageHistories.cs:27`) | nt |
| `ReceiveSuccess` | 4 | "Nhận tin nhắn thành công." (`StatusMessageHistories.cs:32`) | nt |
| `ReceiveFail` | 5 | "Nhận tin nhắn thất bại." (`StatusMessageHistories.cs:37`) | nt |
| `ReceiveError` | 6 | "Lỗi khi nhận tin nhắn." (`StatusMessageHistories.cs:42`) | nt |
| `RetryFaild` | 99 | "Thử lại nhưng vẫn thất bại." (`StatusMessageHistories.cs:47`) | nt |

Ghi chu: ten hang so `RetryFaild` giu nguyen chinh ta goc trong source (khong phai "RetryFailed") theo yeu cau exact-casing cua tai lieu nay.

---

## 3. TypeInfomationCustomerEnum (enum)

**Nguon**: `FTELSRCore.Shared/Enum/TypeInfomationCustomerEnum.cs`
**Namespace**: `FTELSRCore.Enum` (`TypeInfomationCustomerEnum.cs:1`)
**Kieu underlying**: `byte` (`TypeInfomationCustomerEnum.cs:3`)

Enum khong co bat ky XML doc nao (khong `/// <summary>` cho enum, khong cho tung gia tri). Y nghia duoc suy ra tu ten hang so va tu cach `TypeInfomationCustomerHelpers.cs` su dung tung gia tri (xem muc 4).

| Ten gia tri | Gia tri thuc | Y nghia (suy tu context su dung, khong co comment trong code) | Noi dung tham chieu |
|---|---|---|---|
| `ObjId` | 0 (khai bao rieng, `TypeInfomationCustomerEnum.cs:5`) | Ma doi tuong / ID noi bo, dang chuoi toan chu so — suy tu `IsObjId` (`TypeInfomationCustomerHelpers.cs:133-140`) | Dung trong `TypeInfomationCustomerHelpers.cs` (2 noi: khai bao va su dung) |
| `ContractNo` | 1 (ngam dinh, tang tu ObjId) | So hop dong — suy tu `IsContract` (`TypeInfomationCustomerHelpers.cs:42-49`) | nt |
| `PhoneNumber` | 2 (ngam dinh) | So dien thoai — suy tu `IsPhone` (`TypeInfomationCustomerHelpers.cs:28-35`) | nt |
| `IdentityNo` | 3 (ngam dinh) | So CMND/CCCD/Passport hoac chuoi >=5 chu so — suy tu `IsPassport`, `IsIdentityNo` (`TypeInfomationCustomerHelpers.cs:71-93`) | nt |
| `BusinessTaxCode` | 4 (ngam dinh) | Ma so thue doanh nghiep — suy tu `IsTaxCode`, `IsIdentityNo` (`TypeInfomationCustomerHelpers.cs:85-93`, `117-125`) | nt |
| `Email` | 5 (ngam dinh) | Dia chi email — suy tu `IsEmail` (`TypeInfomationCustomerHelpers.cs:101-109`) | nt |
| `FullName` | 6 (ngam dinh) | Ho ten khach hang hoac ten cong ty — suy tu `IsName`, `IsSearch` (`TypeInfomationCustomerHelpers.cs:12-20`, `56-63`) va fallback mac dinh trong `TypeInfomationCustomerEnums` (`TypeInfomationCustomerHelpers.cs:154`, `173`) | nt |

Chi 2 file trong repo tham chieu enum nay: chinh file dinh nghia va `TypeInfomationCustomerHelpers.cs`. Khong quan sat duoc consumer nghiep vu (controller/service) nao trong pham vi repo hien tai.

---

## 4. TypeInfomationCustomerHelpers (static class)

**Nguon**: `FTELSRCore.Shared/Helpers/TypeInfomationCustomerHelpers.cs`
**Namespace**: `FTELSRCore.Helpers` (`TypeInfomationCustomerHelpers.cs:3`)

### 4.1 Tong quan

Static class chua toan bo logic "doan loai du lieu" (data-type guessing) cho 1 chuoi input tu do (vi du tu 1 o tim kiem khach hang), va logic phu xac dinh loai tim kiem nao yeu cau khop chinh xac. Khong co bat ky truy cap DB, HTTP, hay I/O nao — thuan la xu ly chuoi bang regex trong bo nho.

### 4.2 Pham vi chuc nang

| Lam duoc | Khong lam duoc |
|---|---|
| Chay 9 regex heuristic doc lap tren cung 1 chuoi input da duoc `Trim()`, gop tat ca ket qua khop vao 1 danh sach, khu trung va sap xep theo gia tri enum (`TypeInfomationCustomerEnums`) | Khong dam bao 1 input chi khop dung 1 loai — nhieu detector co the cung khop tren 1 input (vi du chuoi toan so >=5 ky tu khop ca `IsObjId`, `IsPassport` neu du 9/12 so, va `IsIdentityNo`) |
| Tra ve fallback `FullName` khi input rong/whitespace hoac khong khop bat ky detector nao | Khong tra ve danh sach rong va khong tra ve `null` (luon co it nhat 1 phan tu) |
| Xac dinh co dung 1 gia tri trong 7 gia tri enum can tim kiem exact hay khong (`GetExactForTypeInfomationCustomerEnum`) | Khong validate/chuan hoa dinh dang du lieu that (khong kiem tra checksum, khong kiem tra domain email co ton tai) |
| | Khong xu ly ky tu co dau tieng Viet day du trong detector `IsSearch` (xem muc 5) |

### 4.3 Dependency

Chi dung `System.Text.RegularExpressions.Regex` va LINQ (`Distinct`, `OrderBy`, `Any`) tren `List<TypeInfomationCustomerEnum>`. Khong dependency ngoai (DB/HTTP/Mongo...).

### 4.4 Danh muc API

| API | Nhom | Mo ta ngan |
|---|---|---|
| `IsSearch` | Detector (private extension) | Input co dang liet ke ten, cach nhau boi dau phay |
| `IsPhone` | Detector (private extension) | Input dang so dien thoai VN (`0` + 9-10 so) |
| `IsContract` | Detector (private extension) | Input dang 3-6 chu cai + toi thieu 3 so |
| `IsName` | Detector (private extension) | Ky tu dau khong phai so/`+`/whitespace |
| `IsPassport` | Detector (private extension) | Input dung 9 hoac 12 chu so |
| `IsIdentityNo` | Detector (private extension) | Input co chua >=5 chu so lien tiep |
| `IsEmail` | Detector (private extension) | Input dang dia chi email |
| `IsTaxCode` | Detector (private extension) | Input dang `mst` + 10 so (+ tuy chon `-` + 3 so) |
| `IsObjId` | Detector (private extension) | Input toan bo la chu so |
| `TypeInfomationCustomerEnums` | Entry point (public static) | Chay toan bo detector, tra ve danh sach loai co the khop |
| `GetExactForTypeInfomationCustomerEnum` | Entry point (public static) | Tra ve co (typeSearch, exact) cho 1 gia tri enum |

### 4.5 Chi tiet API

#### 4.5.1 IsSearch

**Signature**
```csharp
private static void IsSearch(this string input, ref List<TypeInfomationCustomerEnum> type)
```
**Muc dich** - Doan input co dang liet ke nhieu ten/cum tu cach nhau boi dau phay (vi du "Nguyen Van A, Cong ty B") de gan loai `FullName`.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `input` | `string` | Co (extension `this`) | Khop regex `([\s*a-zA-Z0-9'-Š\s*]+?[,]{1}[\s*a-zA-Z0-9'-Š\s*]+)+` (`TypeInfomationCustomerHelpers.cs:14`) | Khong co |
| `type` | `ref List<TypeInfomationCustomerEnum>` | Co | Khong validate null; se NullReferenceException neu `type` la `null` khi goi `type.Add` | Khong co |

**Output** - `void`. Ket qua tra ve gian tiep qua tham so `ref type`: neu khop, `TypeInfomationCustomerEnum.FullName` duoc them vao list; neu khong khop, list khong thay doi.

**Dieu kien xu ly** - Chi 1 nhanh duy nhat: `Regex.IsMatch(input, pattern)` (`TypeInfomationCustomerHelpers.cs:16`) → dung thi `Add(FullName)`.

**Side effect** - Mutate truc tiep list `type` duoc truyen vao (them phan tu). Khong ghi log, khong I/O.

**Error handling** - Khong co try/catch. Neu `input` la `null`, `Regex.IsMatch` nem `ArgumentNullException` khong duoc bat.

**Khi nao NEN dung** - Khong goi truc tiep tu ngoai class (private); chi duoc `TypeInfomationCustomerEnums` goi noi bo khi can doan input dang danh sach ten cach nhau boi phay.

**Khi nao KHONG dung** - Khong dung de validate 1 ten don le (khong yeu cau dau phay).

**Gioi han** - Bieu thuc `[\s*a-zA-Z0-9'-Š\s*]` dinh nghia mot khoang ky tu tu `'` (U+0027) den `Š` (U+0160, chu La tinh mo rong) — day la mot day ky tu rat rong (bao gom nhieu ky hieu ASCII) nhung **khong bao gom** phan lon ky tu tieng Viet co dau to hop (vi du `ả`, `ệ`, `ố`... nam o vung Latin Extended Additional, U+1EA0-U+1EF9, cao hon U+0160 rat nhieu). Ket qua: ten tieng Viet co dau kieu Unicode composed thong thuong se **khong** duoc regex nay coi la ky tu hop le trong tung "tu", du dau phay van co the khop nho backtracking rong cua `.+?`. Xem them muc 5.

---

#### 4.5.2 IsPhone

**Signature**
```csharp
private static void IsPhone(this string input, ref List<TypeInfomationCustomerEnum> type)
```
**Muc dich** - Doan input la so dien thoai Viet Nam.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `input` | `string` | Co | Khop toan bo chuoi (`^...$`) voi pattern `^0\d{9,10}$` (`TypeInfomationCustomerHelpers.cs:30`): bat dau bang `0`, theo sau 9-10 chu so → tong do dai 10 hoac 11 ky tu | Khong co |
| `type` | `ref List<TypeInfomationCustomerEnum>` | Co | Khong validate null | Khong co |

**Output** - `void`; them `PhoneNumber` vao `type` neu khop.

**Dieu kien xu ly** - 1 nhanh `if (Regex.IsMatch(...))` duy nhat.

**Side effect** - Mutate `type`.

**Error handling** - Khong co; `input == null` → `ArgumentNullException` khong bat.

**Khi nao NEN dung** - La 1 buoc trong pipeline `TypeInfomationCustomerEnums` khi input co dang toan chu so bat dau bang 0, dai 10-11 ky tu.

**Khi nao KHONG dung** - Khong xu ly dinh dang co dau `+84`, dau cach, dau gach ngang; nhung dinh dang nay se KHONG khop.

**Gioi han** - Khong kiem tra dau so hop le (10xx, 09xx...) theo quy hoach dau so nha mang, chi kiem tra do dai/tien to `0`.

---

#### 4.5.3 IsContract

**Signature**
```csharp
private static void IsContract(this string input, ref List<TypeInfomationCustomerEnum> type)
```
**Muc dich** - Doan input la so hop dong dang chu + so.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `input` | `string` | Co | Khop toan bo chuoi voi `^[a-zA-Z]{3,6}\d{3,}$` (`TypeInfomationCustomerHelpers.cs:44`): 3-6 chu cai (khong dau, hoa/thuong deu duoc) roi toi thieu 3 chu so, khong ky tu khac | Khong co |
| `type` | `ref List<TypeInfomationCustomerEnum>` | Co | Khong validate null | Khong co |

**Output** - `void`; them `ContractNo` neu khop.

**Dieu kien xu ly** - 1 nhanh if duy nhat.

**Side effect** - Mutate `type`.

**Error handling** - Khong co.

**Khi nao NEN dung** - Input dang "ABC123456" hoac tuong tu (ma hop dong noi bo).

**Khi nao KHONG dung** - So hop dong co dau gach ngang, khoang trang, hoac nhieu hon 6 ky tu chu cai dau se KHONG khop.

**Gioi han** - Khong gioi han tren cho phan so (`\d{3,}` khong co upper bound); chap nhan ca chu cai co dau tren 6 ky tu se bi loai du la ma hop dong hop le trong thuc te.

---

#### 4.5.4 IsName

**Signature**
```csharp
private static void IsName(this string input, ref List<TypeInfomationCustomerEnum> type)
```
**Muc dich** - Doan input co the la ten khach hang/ten cong ty, dua tren dieu kien ky tu dau tien.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `input` | `string` | Co | Khop pattern `^[^0-9+\s]` (`TypeInfomationCustomerHelpers.cs:58`) — **chi kiem tra ky tu dau tien** cua chuoi (khong co anchor `$` cuoi, khong co dinh luong lap toan chuoi) | Khong co |
| `type` | `ref List<TypeInfomationCustomerEnum>` | Co | Khong validate null | Khong co |

**Output** - `void`; them `FullName` neu ky tu dau tien khong phai chu so 0-9, dau `+`, hoac khoang trang.

**Dieu kien xu ly** - 1 nhanh if duy nhat, dieu kien chi phu thuoc ky tu dau tien.

**Side effect** - Mutate `type`.

**Error handling** - Khong co.

**Khi nao NEN dung** - La 1 detector "long" (loose) trong pipeline, giup bat cac truong hop ten khong dau phay (khac voi `IsSearch`).

**Khi nao KHONG dung** - Khong dung doc lap de xac nhan chac chan la ten, vi dieu kien qua long.

**Gioi han** - Vi chi kiem tra ky tu dau tien, hau het chuoi chu-so nhu ma hop dong (`ABC123456`) hoac email (`a@b.com`) cung se khop va bi gan them `FullName` song song voi loai chinh xac hon cua no. Day la nguyen nhan chinh khien `TypeInfomationCustomerEnums` thuong tra ve NHIEU loai cho 1 input duy nhat (xem muc 4.5.10 va muc 5).

---

#### 4.5.5 IsPassport

**Signature**
```csharp
private static void IsPassport(this string input, ref List<TypeInfomationCustomerEnum> type)
```
**Muc dich** - Doan input la so CMND (9 so) hoac CCCD/Passport (12 so).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `input` | `string` | Co | Khop toan bo chuoi voi `^\d{9}$\|^\d{12}$` (`TypeInfomationCustomerHelpers.cs:73`): dung 9 chu so HOAC dung 12 chu so | Khong co |
| `type` | `ref List<TypeInfomationCustomerEnum>` | Co | Khong validate null | Khong co |

**Output** - `void`; them `IdentityNo` neu khop.

**Dieu kien xu ly** - 1 nhanh if duy nhat.

**Side effect** - Mutate `type`.

**Error handling** - Khong co.

**Khi nao NEN dung** - Input toan chu so, dung chinh xac 9 hoac 12 ky tu.

**Khi nao KHONG dung** - CMND/CCCD/Passport co ky tu chu (mot so passport quoc te co chu cai) se KHONG khop; comment code goi day la "CMND/CCCD hoac Passport" (`TypeInfomationCustomerHelpers.cs:66`) nhung pattern chi chap nhan toan chu so, khong ho tro passport dang chu+so.

**Gioi han** - Trung lap pham vi voi `IsIdentityNo` (chuoi 9/12 so cung luon khop `\d{5,}`), dan den `IdentityNo` co the duoc `Add` 2 lan boi 2 detector khac nhau truoc khi `Distinct()` loai bo o buoc cuoi.

---

#### 4.5.6 IsIdentityNo

**Signature**
```csharp
private static void IsIdentityNo(this string input, ref List<TypeInfomationCustomerEnum> type)
```
**Muc dich** - Theo comment: "Có ít nhất 5 chử số" (`TypeInfomationCustomerHelpers.cs:81`) — doan input co chua thanh phan so du dai de co the la CMND/CCCD hoac MST.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `input` | `string` | Co | Khop (khong anchor) pattern `\d{5,}` (`TypeInfomationCustomerHelpers.cs:87`) — chi can CHUA it nhat 5 chu so LIEN TIEP o bat ky vi tri nao trong chuoi, khong can toan chuoi la so | Khong co |
| `type` | `ref List<TypeInfomationCustomerEnum>` | Co | Khong validate null | Khong co |

**Output** - `void`; neu khop, them **DONG THOI CA 2** gia tri `IdentityNo` VA `BusinessTaxCode` vao `type` (`TypeInfomationCustomerHelpers.cs:90-91`).

**Dieu kien xu ly** - 1 nhanh if duy nhat, nhung nhanh nay them 2 phan tu cung luc (khac voi tat ca detector khac chi them 1 phan tu).

**Side effect** - Mutate `type` (them 2 phan tu).

**Error handling** - Khong co.

**Khi nao NEN dung** - Bo sung cho `IsPassport`/`IsTaxCode` khi input co chua chuoi so dai nhung khong khop dung dinh dang 9/12 so hoac dinh dang `mst...`.

**Khi nao KHONG dung** - Khong dung de xac dinh CHINH XAC la CMND hay MST — detector nay co chu dinh mo hồ (them ca 2 loai) vi khong the phan biet duoc chi tu do dai chuoi so.

**Gioi han** - Ten ham `IsIdentityNo` gay nham lan vi ham nay khong chi lien quan `IdentityNo` ma con luon them ca `BusinessTaxCode`; do khong co anchor, ca mot chuoi dai co lan mot doan >=5 so (vi du mot ten cong ty co chua ma so) cung se khop va bi gan nham 2 loai nay.

---

#### 4.5.7 IsEmail

**Signature**
```csharp
private static void IsEmail(this string input, ref List<TypeInfomationCustomerEnum> type)
```
**Muc dich** - Doan input la dia chi email.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `input` | `string` | Co | Khop toan bo chuoi voi `^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9-]+(?:\.[a-zA-Z0-9-]+)*$` (`TypeInfomationCustomerHelpers.cs:104`) | Khong co |
| `type` | `ref List<TypeInfomationCustomerEnum>` | Co | Khong validate null | Khong co |

**Output** - `void`; them `Email` neu khop.

**Dieu kien xu ly** - 1 nhanh if duy nhat. Co 1 dong pattern cu bi comment out ngay tren (`TypeInfomationCustomerHelpers.cs:103`), khong con duoc su dung — giu lai trong source nhu tham khao lich su.

**Side effect** - Mutate `type`.

**Error handling** - Khong co.

**Khi nao NEN dung** - Input dang chuan `local-part@domain`.

**Khi nao KHONG dung** - Khong dam bao domain co TLD hop le hay khong (chi can co it nhat 1 nhom sau `@`, khong bat buoc dau `.` — vi du `a@localhost` van khop do phan `(?:\.[a-zA-Z0-9-]+)*` cho phep 0 lan).

**Gioi han** - Khong kiem tra do dai toi da theo RFC 5321/5322; day chi la regex kiem tra hinh thuc co ban.

---

#### 4.5.8 IsTaxCode

**Signature**
```csharp
private static void IsTaxCode(this string input, ref List<TypeInfomationCustomerEnum> type)
```
**Muc dich** - Doan input la ma so thue (MST) theo dinh dang noi bo bat dau bang tien to `mst`.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `input` | `string` | Co | Sau khi lowercase (`TypeInfomationCustomerHelpers.cs:119`), khop toan bo chuoi voi `^mst\d{10}(\-\d{3})?$` (`TypeInfomationCustomerHelpers.cs:120`): tien to `mst` (khong phan biet hoa/thuong nho lowercase truoc) + dung 10 so + tuy chon `-` + 3 so | Khong co |
| `type` | `ref List<TypeInfomationCustomerEnum>` | Co | Khong validate null | Khong co |

**Output** - `void`; them `BusinessTaxCode` neu khop.

**Dieu kien xu ly** - 1 nhanh if duy nhat, sau 1 buoc chuan hoa `input = input.ToLower()` chi anh huong bien local trong ham nay (khong lam thay doi `input` o ham goi, vi day la gan lai tham so gia tri, khong phai `ref`).

**Side effect** - Mutate `type`. Khong lam thay doi input goc cua caller.

**Error handling** - Khong co; `input == null` → `NullReferenceException` tai `input.ToLower()` (`TypeInfomationCustomerHelpers.cs:119`), khong duoc bat.

**Khi nao NEN dung** - Input co dinh dang dac thu he thong: `mst` + 10 so, ep buoc phai co tien to chu "mst" (vi du "mst0123456789").

**Khi nao KHONG dung** - Khong nhan dien MST doanh nghiep thuc te theo chuan Tong cuc Thue (thuong la 10 hoac 13 so, KHONG co tien to chu "mst") — day la dinh dang quy uoc rieng cua ung dung, khong phai dinh dang MST chuan.

**Gioi han** - Ten tham so `type` trong XML doc ghi "validate TaxCode: mst0123456789 || mst0123456789-123" (`TypeInfomationCustomerHelpers.cs:115`) xac nhan dung dinh dang quy uoc noi bo nay.

---

#### 4.5.9 IsObjId

**Signature**
```csharp
private static void IsObjId(this string input, ref List<TypeInfomationCustomerEnum> type)
```
**Muc dich** - Doan input la ma doi tuong (ObjId), quy uoc la chuoi toan chu so.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `input` | `string` | Co | Khop toan bo chuoi voi `^\d+$` (`TypeInfomationCustomerHelpers.cs:135`): toan bo la chu so, toi thieu 1 ky tu | Khong co |
| `type` | `ref List<TypeInfomationCustomerEnum>` | Co | Khong validate null | Khong co |

**Output** - `void`; them `ObjId` neu khop.

**Dieu kien xu ly** - 1 nhanh if duy nhat.

**Side effect** - Mutate `type`.

**Error handling** - Khong co.

**Khi nao NEN dung** - Input toan bo la so, khong gioi han do dai — dung khi nghi ngo la ID noi bo dang so nguyen.

**Khi nao KHONG dung** - Khong phan biet duoc ObjId voi so dien thoai, CMND, hay chuoi so cua MST — deu la "toan chu so" nen se cung khop nhieu detector khac (xem muc 5, vi du minh hoa).

**Gioi han** - Khong co upper bound do dai; chuoi so rat dai (vi du 20 chu so) van duoc coi la `ObjId` hop le du gia tri co the vuot `long`/`int` thuc te khi parse.

---

#### 4.5.10 TypeInfomationCustomerEnums

**Signature**
```csharp
public static List<TypeInfomationCustomerEnum> TypeInfomationCustomerEnums(string input)
```
**Muc dich** - Ham entry point cong khai: nhan 1 chuoi input tu do, chay toan bo 9 detector va tra ve danh sach cac `TypeInfomationCustomerEnum` co the phu hop, da khu trung va sap xep.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `input` | `string` | Co | Khong co ep kieu; duoc kiem tra `string.IsNullOrWhiteSpace` (`TypeInfomationCustomerHelpers.cs:152`) roi `Trim()` (`TypeInfomationCustomerHelpers.cs:159`) truoc khi dua vao detector | Khong co (khong co overload voi default) |

**Output** - `List<TypeInfomationCustomerEnum>`. Y nghia tung truong hop:
- Input null/rong/toan whitespace → tra ve list chi chua `FullName` (`TypeInfomationCustomerHelpers.cs:154-156`).
- Input hop le nhung khong khop bat ky detector nao → tra ve list chi chua `FullName` (fallback, `TypeInfomationCustomerHelpers.cs:173-175`).
- Input khop >=1 detector → tra ve list cac gia tri khop, **da `Distinct()` va `OrderBy(x => (int)x)`** (tang dan theo gia tri byte cua enum) (`TypeInfomationCustomerHelpers.cs:178`).
- Ham **khong bao gio tra ve `null` hoac list rong**.

**Dieu kien xu ly** (theo thu tu thuc thi trong code):
1. Guard: `string.IsNullOrWhiteSpace(input)` → tra ve `[FullName]` ngay, khong chay detector nao (`TypeInfomationCustomerHelpers.cs:152-157`).
2. `input = input.Trim()` (`TypeInfomationCustomerHelpers.cs:159`).
3. Goi tuan tu 9 detector theo dung thu tu trong code: `IsObjId`, `IsContract`, `IsSearch`, `IsPhone`, `IsName`, `IsPassport`, `IsEmail`, `IsTaxCode`, `IsIdentityNo` (`TypeInfomationCustomerHelpers.cs:161-169`) — luu y thu tu goi nay KHAC thu tu khai bao ham trong file, nhung khong anh huong ket qua cuoi vi ket qua duoc sort lai o buoc 5.
4. Guard: neu sau 9 detector, `typeSearches` van rong (`Any() is false`) → tra ve `[FullName]` (`TypeInfomationCustomerHelpers.cs:171-176`).
5. Neu khong rong → `Distinct().OrderBy(x => (int)x).ToList()` roi tra ve (`TypeInfomationCustomerHelpers.cs:178`).

**Side effect** - Khong co (khong I/O, khong log). Tao 1 `List<TypeInfomationCustomerEnum>` moi moi lan goi (khong mutate state tinh/static).

**Error handling** - Khong co try/catch trong ham nay; exception tu cac detector con (vi du `NullReferenceException` trong `IsTaxCode` neu logic thay doi trong tuong lai, hoac loi tu Regex) se duoc nem thang len caller. Voi input hien tai da qua guard `IsNullOrWhiteSpace`, truong hop `input == null` bi chan truoc khi vao detector nen khong gay loi trong luong binh thuong.

**Khi nao NEN dung** - Phan loai 1 chuoi tim kiem tu do (vi du tu 1 search box khach hang) thanh cac ung vien loai truong de xay dung dieu kien truy van (WHERE ... OR ...) tren nhieu cot du lieu khac nhau.

**Khi nao KHONG dung** - Khong dung de validate dinh dang chinh xac 1 truong du lieu don (vi du kiem tra 1 o nhap "So dien thoai" co hop le hay khong) — ham nay tra ve NHIEU khả năng chứ khong khang dinh 1 loai duy nhat.

**Gioi han** - Vi cac detector overlap nhieu (xem `IsName`, `IsObjId`, `IsIdentityNo`), voi nhieu input, ket qua tra ve thuong co >1 phan tu. Vi du minh hoa (suy tu source, khong chay thu tren moi truong thuc): input `"123456789"` (9 chu so) se khop `IsObjId` (toan so) → `ObjId`; khop `IsPassport` (dung 9 so) → `IdentityNo`; khop `IsIdentityNo` (>=5 so) → them ca `IdentityNo` va `BusinessTaxCode`. Sau `Distinct()`, ket qua cuoi la `[ObjId, IdentityNo, BusinessTaxCode]` (sap theo gia tri byte 0, 3, 4). Day la vi du de minh hoa co che overlap, **khong phai ket qua da duoc thuc thi kiem chung**.

---

#### 4.5.11 GetExactForTypeInfomationCustomerEnum

**Signature**
```csharp
public static (TypeInfomationCustomerEnum typeSearch, bool exact) GetExactForTypeInfomationCustomerEnum(TypeInfomationCustomerEnum type)
```
**Muc dich** - Voi 1 gia tri `TypeInfomationCustomerEnum` cho truoc, xac dinh xem loai tim kiem nay co nen duoc thuc hien khop CHINH XAC (exact match) hay khong.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `type` | `TypeInfomationCustomerEnum` | Co | Khong validate gia tri enum co hop le (trong dai 0-6) hay khong; neu ep kieu 1 byte ngoai dai (vi du `(TypeInfomationCustomerEnum)55`), roi vao nhanh `default` | Khong co |

**Output** - Tuple `(TypeInfomationCustomerEnum typeSearch, bool exact)`:
- `typeSearch`: luon la chinh gia tri `type` duoc truyen vao (tra nguyen ven, khong bien doi).
- `exact = true` khi `type` la 1 trong: `ObjId`, `ContractNo`, `IdentityNo`, `PhoneNumber`, `FullName`, `Email` (`TypeInfomationCustomerHelpers.cs:191-198`).
- `exact = false` cho MOI gia tri con lai — trong 7 gia tri cua enum, chi con lai duy nhat `BusinessTaxCode` roi vao truong hop nay, cong voi bat ky gia tri byte khong hop le nao ngoai enum (`TypeInfomationCustomerHelpers.cs:200-203`).

**Dieu kien xu ly** - `switch` pattern-matching voi mot nhanh `case ... or ... or ...` gom 6 gia tri → `exact: true`; nhanh `default` → `exact: false`.

**Side effect** - Khong co. Ham thuan (pure function).

**Error handling** - Khong co exception nao duoc nem; moi gia tri (hop le hay khong) deu co 1 nhanh xu ly (khong co truong hop unhandled).

**Khi nao NEN dung** - Sau khi co danh sach loai tu `TypeInfomationCustomerEnums`, dung ham nay cho tung loai de quyet dinh xay dung dieu kien truy van bang `=` (exact) hay `LIKE`/`CONTAINS` (khong exact).

**Khi nao KHONG dung** - Khong dung de kiem tra tinh hop le cua gia tri enum (ham khong phan biet enum hop le/khong hop le theo nghia validation, chi phan biet theo nhanh switch).

**Gioi han** - Logic nghiep vu ("chi `BusinessTaxCode` la khong exact") khong co giai thich trong code hay comment; day la suy dien tu switch, KHONG xac dinh duoc tu source code ly do nghiep vu vi sao rieng MST duoc coi la tim kiem gan dung con lai deu la chinh xac.

---

## 5. Van de da biet

| # | Van de | Vi tri | Anh huong |
|---|---|---|---|
| 1 | Namespace cua `StatusMessageHistories` la `SRWebCoreAPI.Shared.Enum`, khong khop voi ten project/thu muc thuc te (`FTELSRCore.Shared/Enum`) va khong khop voi namespace cua enum lang gieng `TypeInfomationCustomerEnum` (`FTELSRCore.Enum`) | `StatusMessageHistories.cs:1` | Dat ten khong dong nhat trong cung 1 project co the gay nham lan khi import, va cho thay enum nay co the duoc copy/di chuyen tu 1 project khac (`SRWebCoreAPI`) sang ma chua cap nhat namespace |
| 2 | Grep toan repo (chi co 1 csproj `FTELSRCore.Shared.csproj`) khong tim thay noi nao khac tham chieu `StatusMessageHistories` ngoai file dinh nghia chinh no | `StatusMessageHistories.cs` (toan file) | Enum co the dang khong duoc su dung (dead code) trong pham vi repo nay, hoac chi duoc tieu thu boi 1 repo/service khac khong nam trong workspace hien tai — khong xac dinh duoc tu source code |
| 3 | Khoang cach gia tri lon giua `ReceiveError = 6` va `RetryFaild = 99` (thieu 92 gia tri) | `StatusMessageHistories.cs:42-47` | Khong ro co du dinh danh rieng khoang gia tri cho cac trang thai khac trong tuong lai hay khong — khong xac dinh duoc tu source code |
| 4 | `TypeInfomationCustomerEnum` khong co bat ky XML doc comment nao (enum va tung gia tri) | `TypeInfomationCustomerEnum.cs:3-11` | Y nghia tung gia tri phai suy dien hoan toan tu cach dat ten va cach `TypeInfomationCustomerHelpers` su dung; rui ro sai lech giua ten va muc dich thuc te khi co thay doi trong tuong lai ma khong cap nhat comment |
| 5 | Regex trong `IsSearch` dung khoang ky tu `'-Š` (U+0027 den U+0160) khong bao trum vung Unicode cua ky tu tieng Viet co dau to hop (U+1EA0-U+1EF9) | `TypeInfomationCustomerHelpers.cs:14` | Input ten tieng Viet co dau, cach nhau boi dau phay, co the khong duoc phan loai dung nhu ky vong qua detector nay (van co the duoc `IsName` bat duoc qua co che khac, nhung khong dam bao) |
| 6 | `IsName` chi kiem tra ky tu DAU TIEN cua chuoi (khong co anchor `$`, khong lap toan chuoi) | `TypeInfomationCustomerHelpers.cs:58` | Hau het chuoi khong bat dau bang so/`+`/khoang trang (bao gom email, so hop dong dang chu, v.v.) deu duoc gan them `FullName` song song voi loai chinh xac hon, khien `TypeInfomationCustomerEnums` thuong tra ve nhieu loai cho 1 input |
| 7 | `IsIdentityNo` dung ten gay nham (chi lien quan "identity") nhung luon them ca `IdentityNo` VA `BusinessTaxCode` khi khop, va pattern khong co anchor nen chi can CHUA (khong can toan chuoi) >=5 chu so lien tiep | `TypeInfomationCustomerHelpers.cs:85-93` | Nhieu loai input khong chu dinh la ma so/MST (vi du chuoi hon hop chu-so co doan >=5 so) van bi gan `IdentityNo` va `BusinessTaxCode` |
| 8 | `IsPassport` co comment mo ta "CMND/CCCD hoac Passport" nhung pattern chi nhan chuoi toan chu so (9 hoac 12 ky tu), khong ho tro Passport quoc te co ky tu chu | `TypeInfomationCustomerHelpers.cs:66-77` | Comment rong hon thuc te ham lam duoc — theo nguyen tac uu tien source code, tai lieu nay ghi nhan ham CHI xu ly chuoi toan so |
| 9 | `IsTaxCode` yeu cau tien to van hoc `mst` — day la quy uoc rieng cua he thong, KHONG phai dinh dang MST chuan cua co quan thue (thuong chi la chuoi so, khong tien to chu) | `TypeInfomationCustomerHelpers.cs:117-125` | Neu nguoi dung nhap MST thuc te (chi so, khong "mst") se khong duoc `IsTaxCode` nhan dien loai `BusinessTaxCode` qua nhanh nay (co the van duoc gan qua `IsIdentityNo` neu du >=5 so) |
| 10 | Logic `GetExactForTypeInfomationCustomerEnum` khong co giai thich nghiep vu trong code/comment cho viec chi `BusinessTaxCode` la `exact = false` | `TypeInfomationCustomerHelpers.cs:187-205` | Ly do nghiep vu khong xac dinh duoc tu source code; can hoi nguoi phu trach nghiep vu goc neu can xac nhan |
| 11 | Cac ham `ref List<TypeInfomationCustomerEnum> type` trong tat ca 9 detector khong kiem tra `type == null` truoc khi goi `type.Add(...)` | Vi du `TypeInfomationCustomerHelpers.cs:18`, `33`, tuong tu cho cac ham con lai | Neu co code moi goi truc tiep cac ham private nay (hien tai chi co 1 noi goi la `TypeInfomationCustomerEnums` voi list da `new()`) voi list null se nem `NullReferenceException` khong duoc bat |
