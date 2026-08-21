# Validator Extensions

> Nguon: FTELSRCore.Shared/Extensions/Validators/AbstractValidatorExtentions.cs, FTELSRCore.Shared/Extensions/Validators/ValidatorRequestExtensions.cs
> Loai: static class (2 file, cung namespace `FTELSRCore.Extensions.Validators`)
> Cap nhat theo commit: 89c1ce9

## 1. Tong quan

Module gom hai `static class` cung cap lop mo rong xay dung tren nen **FluentValidation** (package `FluentValidation` version `12.1.1`, xem `FTELSRCore.Shared/FTELSRCore.Shared.csproj:23`):

- **`AbstractValidatorExtensions`** (file `AbstractValidatorExtentions.cs`) — goi mot `AbstractValidator<T>` cu the len mot model va chuyen `ValidationResult` cua FluentValidation thanh mot tuple don gian `(bool IsSuccess, List<string> Messages)`, giup caller khong can biet chi tiet API cua FluentValidation.
- **`ValidatorRequestExtensions`** (file `ValidatorRequestExtensions.cs`) — tap hop 15 extension method mo rong `IRuleBuilder<T, TProperty>` (chuoi rule builder cua FluentValidation, dung ben trong khai bao `RuleFor(...)`) de tai su dung cac rule kiem tra pho bien: chuoi khong rong + gioi han do dai + regex, chong mot so payload XSS don gian, ngay gio hop le/so sanh hien tai, parse chuoi thanh ngay theo format, predicate tuy bien, so dien thoai Viet Nam, chuoi so.

Ca hai lop thuoc tang Cross-cutting/Shared Kernel (project `FTELSRCore.Shared`), duoc cac validator nghiep vu (ke thua `AbstractValidator<T>` o tang Application/API) tham chieu de khai bao rule va thuc thi validate truoc khi xu ly business logic. **Khong tim thay loi goi nao toi hai file nay o bat ky noi khac trong repo `sr-core-helper`** (da `grep` toan repo, khong co ket qua) — day la thu vien chia se (`FTELSRCore.Shared`), nguoi dung thuc te la cac project consumer ben ngoai repo nay, nen khong the quan sat cach goi thuc te tu chinh repo.

### 1.1 Pham vi chuc nang
| Lam duoc | Khong lam duoc |
|---|---|
| Goi mot validator FluentValidation co constructor khong tham so len mot model va tra ve tuple `(IsSuccess, Messages)` — khong throw exception khi model khong hop le (`AbstractValidatorExtentions.cs:26-48`) | Khong ho tro validator can Dependency Injection (constructor co tham so), vi `where TValidator : AbstractValidator<T>, new()` bat buoc constructor khong tham so (`AbstractValidatorExtentions.cs:27`) |
| Cung cap 15 extension method rule builder: gioi han do dai + regex chuoi, chan ky tu XSS co ban, kiem tra `DateTime?` hop le, so sanh ngay voi hien tai, parse chuoi ngay theo format tuy chinh, predicate tuy bien, so dien thoai VN, chuoi so (`ValidatorRequestExtensions.cs:14-196`) | Khong co ban `async`/`ValidateAsync` nao trong hai file nay |
| Cho phep tuy bien message loi theo ten field truyen vao (tham so `message`) hoac dung `{PropertyName}` mac dinh cua FluentValidation | Khong tu throw `ValidationException` (khong goi `ValidateAndThrow`/`ValidateAndThrowAsync`) — caller phai tu kiem tra `IsSuccess`/`ValidationResult.IsValid` |
| Xac thuc so dien thoai Viet Nam theo nhieu loai (di dong, co dinh, 1800, 1900) qua `ConvertHelpers.VietnamesePhoneValidator` | `IsXSSPayload` chi *phat hien* va bao loi, khong sanitize/loai bo ky tu nguy hiem khoi du lieu |

### 1.2 Dependency
| Thanh phan | Muc dich su dung |
|---|---|
| `FluentValidation` (`AbstractValidator<T>`, `IRuleBuilder<T,TProperty>`, `IRuleBuilderOptions<T,TProperty>`, `ValidationResult`) | Nen tang validate — `AbstractValidatorExtentions.cs:1-2`, `ValidatorRequestExtensions.cs:1` |
| `System.Text.RegularExpressions.Regex` | Kiem tra dinh dang bang regex, phat hien payload XSS — `ValidatorRequestExtensions.cs:3`, dung tai dong 90 |
| `System.Globalization.CultureInfo` | Parse ngay gio theo `InvariantCulture` — `ValidatorRequestExtensions.cs:2`, dung tai dong 102, 123, 149, 159 |
| `static FTELSRCore.Helpers.ConvertHelpers` → `VietnamesePhoneValidator.Validate(...)` | Xac thuc so dien thoai Viet Nam (phan loai mobile/geographic/toll-free/premium, nhan dien nha mang) — import tai `ValidatorRequestExtensions.cs:4`, dung tai dong 181 |
| `FTELSRCore.Constants.CommonBaseConstant.DateTimeUtc()` | Lay thoi diem hien tai (UTC + 7 gio theo mac dinh) de so sanh — dung tai `ValidatorRequestExtensions.cs:140`. Class nay **khong co `using FTELSRCore.Constants;` khai bao truc tiep trong file** — co the truy cap duoc nho `global using FTELSRCore.Constants;` khai bao tai `FTELSRCore.Shared/GlobalUsing.cs:3` |

### 1.3 Danh muc API
| API | Nhom | Mo ta ngan |
|---|---|---|
| `AbstractValidatorExtensions.Validate<TValidator, T>(T model)` | Thuc thi validator | Chay `TValidator` len `model`, tra ve `(IsSuccess, Messages)` |
| `RegexString<T>(ruleBuilder, int maximumLength, int minimumLength = 0)` | Chuoi — do dai & regex | Khong rong + max/min length + khop regex mac dinh, message dung `{PropertyName}` |
| `RegexString<T>(ruleBuilder, string message, int maximumLength, int minimumLength = 0)` | Chuoi — do dai & regex | Nhu tren nhung message tuy bien theo `message` |
| `RegexString<T>(ruleBuilder, int maximumLength = 40)` | Chuoi — do dai & regex | Khong rong + max length (mac dinh 40) + regex mac dinh, dung `{PropertyName}` |
| `RegexString<T>(ruleBuilder, string message, int maximumLength = 40)` | Chuoi — do dai & regex | Nhu tren, message tuy bien |
| `RegexString<T>(ruleBuilder, string message, string characterNotMatches, int maximumLength = 40)` | Chuoi — do dai & regex tuy bien | Khong rong + max length + khop regex **do caller cung cap** (khong dung hang so mac dinh) |
| `RegexStringDescription<T>(ruleBuilder, string message, int maximumLength = 40)` | Chuoi — do dai | Chi khong rong + max length, **khong co buoc kiem tra regex** |
| `IsXSSPayload<T>(ruleBuilder, string message)` | Chuoi — an toan | Tu choi chuoi khop mot so pattern XSS don gian (script tag, `on*=`, `javascript:`, hoac chua `<`/`>`) |
| `IsDateTime<T>(ruleBuilder)` (DateTime?) | Ngay gio | Parse + kiem tra nam trong khoang hop le cua SQL DateTime, message dung `{PropertyName}` |
| `IsDateTime<T>(ruleBuilder, string message)` (DateTime?) | Ngay gio | Nhu tren, message tuy bien |
| `DateTimeGreaterThanCurrentDate<T>(ruleBuilder, string message)` (DateTime) | Ngay gio | Yeu cau gia tri **khong duoc lon hon** thoi diem hien tai (`CommonBaseConstant.DateTimeUtc()`) |
| `IsStringToDate<T>(ruleBuilder, string message, string format = "yyyy-MM-dd")` | Ngay gio — chuoi sang ngay | Parse chuoi theo `format` bang `TryParseExact` + khong rong, message tuy bien |
| `IsStringToDate<T>(ruleBuilder, string format = "yyyy-MM-dd")` | Ngay gio — chuoi sang ngay | Nhu tren, dung `{PropertyName}` |
| `IsCustomer<T, TType>(ruleBuilder, Func<TType,bool> predicate, string message)` | Predicate tuy bien | Wrapper mong cho `.Must(predicate).WithMessage(message)` |
| `IsNumberPhone<T>(ruleBuilder, string message, int maximumLength = 20, int minimumLength = 10)` | So dien thoai | Khong rong + hop le theo `VietnamesePhoneValidator` + gioi han do dai chuoi goc |
| `IsNumber<T>(ruleBuilder, string message)` | Chuoi so | Khong rong + khop `^\d+(\.\d+)?$` (so nguyen/thap phan khong dau) |

## 2. Chi tiet API

### 2.1 Validate&lt;TValidator, T&gt;
**Signature**
```csharp
public static (bool IsSuccess, List<string> Messages) Validate<TValidator, T>(T model)
    where TValidator : AbstractValidator<T>, new()
    where T : class
```
**Muc dich** — Khoi tao `TValidator` (constructor khong tham so), chay `Validate(model)` cua FluentValidation, va rut gon `ValidationResult` thanh tuple `(IsSuccess, Messages)` de caller khong phai lam viec truc tiep voi `ValidationResult`/`ValidationFailure`.

**Input hop le**
| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `TValidator` (generic) | `AbstractValidator<T>` | Bat buoc | Phai co constructor public khong tham so (`new()` constraint, dong 27) | Khong co |
| `T` (generic) | `class` | Bat buoc | Phai la reference type (dong 28) | Khong co |
| `model` | `T` | Bat buoc (theo signature) | Khong co kiem tra `null` tuong minh trong ham nay | Khong co |

**Output** — `(bool IsSuccess, List<string> Messages)`:
- `IsSuccess = true, Messages = []` khi `result.IsValid == true` (dong 34-37).
- `IsSuccess = false, Messages = [...]` khi co loi; `Messages` la danh sach `ErrorMessage` cua cac `ValidationFailure` co `ErrorMessage` khong rong/khong chi chua khoang trang (dong 41-47).
- **Truong hop dac biet**: neu `IsValid == false` nhung tat ca `ErrorMessage` deu rong/whitespace (bi loc o dong 44), ket qua tra ve la `IsSuccess = false, Messages = []` — false nhung khong co thong tin loi kem theo.

**Dieu kien xu ly** (theo thu tu thuc thi)
1. Tao instance moi `TValidator validator = new();` (dong 30) — **moi lan goi deu tao instance moi**, khong cache/singleton.
2. Goi `validator.Validate(model)` (dong 32).
3. Neu `result.IsValid is true` → tra ve thanh cong ngay (dong 34-37), khong xet tiep `result.Errors`.
4. Neu khong, kiem tra `result.Errors is not null && result.Errors.Count > 0` (dong 41-42) truoc khi loc message — day la kiem tra thu cong, khong dua vao null-conditional.
5. Loc `result.Errors` bang `Where(x => !string.IsNullOrWhiteSpace(x.ErrorMessage))` roi `Select(x => x.ErrorMessage)` (dong 44).
6. Tra ve `(false, [.. messages])`.

**Side effect** — Khong co (khong ghi log, khong goi service ngoai, khong mutate `model`). Viec tao instance `TValidator` moi moi lan goi la chi phi runtime nho nhung khong phai side effect quan sat duoc tu ben ngoai.

**Error handling** — Khong co `try/catch` trong ham. Neu `validator.Validate(model)` (hoac bat ky rule ben trong `TValidator`) nem exception, exception do **lan truyen thang len caller** — ham nay khong nuot hoac chuyen doi exception thanh message loi. Hanh vi cua `FluentValidation.AbstractValidator<T>.Validate(T)` khi `model` la `null` **khong xac dinh duoc tu source code cua hai file nay** (phu thuoc implementation noi bo cua thu vien FluentValidation 12.1.1).

**Khi nao NEN dung** — Khi validator dich chi can constructor rong (khong DI) va caller muon nhan ket qua dang boolean + list message thay vi lam viec truc tiep voi `ValidationResult`.

**Khi nao KHONG dung** — Khi `TValidator` can inject dependency qua constructor (vi du can `IServiceProvider`, repository, v.v.) — rang buoc `new()` se khien code khong compile duoc voi validator nhu vay; khi caller can throw exception ngay khi invalid (nen dung truc tiep `ValidateAndThrow` cua FluentValidation).

**Gioi han**
- Bat buoc `new()` → khong dung duoc voi validator co constructor tham so (DI).
- Khong co overload `async`.
- Neu tat ca `ErrorMessage` rong/whitespace, `IsSuccess=false` nhung `Messages` rong — caller khong co thong tin gi de hien thi cho nguoi dung cuoi (xem muc 3, #1).

---

### 2.2 RegexString&lt;T&gt;(ruleBuilder, int maximumLength, int minimumLength = 0)
**Signature**
```csharp
public static IRuleBuilderOptions<T, string> RegexString<T>(
    this IRuleBuilder<T, string> ruleBuilder, int maximumLength, int minimumLength = 0)
```
**Muc dich** — Gan chuoi rule chuan cho mot property `string`: khong rong, do dai trong khoang `[minimumLength, maximumLength]`, va khop regex mac dinh `MatchsMatches = "^[^#$%^*<>]+$"` (khong chua `#$%^*<>`).

**Input hop le**
| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `ruleBuilder` | `IRuleBuilder<T, string>` | Bat buoc | Khong co | Khong co |
| `maximumLength` | `int` | Bat buoc | Khong duoc code nay kiem tra hop le (vi du khong chan so am) | Khong co |
| `minimumLength` | `int` | Tuy chon | Khong kiem tra hop le | `0` |

**Output** — `IRuleBuilderOptions<T, string>` (tiep tuc cho phep chain them rule cua FluentValidation, vi du `.When(...)`).

**Dieu kien xu ly** (thu tu cac rule duoc gan vao rule chain, dong 17-22)
1. `Must(propertyName => !string.IsNullOrWhiteSpace(propertyName))` → message `"{PropertyName} khong duoc rong."`
2. `MaximumLength(maximumLength)` → message `"{PropertyName} do dai ki tu khong lon hon " + maximumLength`
3. `MinimumLength(minimumLength)` → message `"{PropertyName} do dai ki tu toi thieu " + minimumLength`
4. `Matches(MatchsMatches)` → message `"{PropertyName} " + MatchsMatchesMessage` (message nay **chua nguyen van pattern regex** `^[^#$%^*<>]+$`, xem muc 3, #2)

**Side effect** — Khong co.

**Error handling** — Khong co try/catch; day chi la khai bao rule, loi validate duoc FluentValidation gom vao `ValidationResult.Errors`, khong throw tai thoi diem khai bao hay chay rule (tru khi predicate ben trong nem exception, nhung o day cac predicate/ham goi — `IsNullOrWhiteSpace`, `MaximumLength`, `MinimumLength`, `Matches` — khong co nhanh throw trong code hien tai).

**Khi nao NEN dung** — Field chuoi bat buoc, co ca gioi han min va max length, muon dung ten property mac dinh (`{PropertyName}`) trong message loi.

**Khi nao KHONG dung** — Khi can message tuy bien theo ten tieng Viet cua field (dung overload 2.3); khi field cho phep de trong (rule `Must` o day luon coi rong la loi).

**Gioi han** — `minimumLength` mac dinh `0` khien rule `MinimumLength(0)` gan nhu vo nghia (moi chuoi co do dai ≥ 0); khong kiem tra `maximumLength >= minimumLength`.

---

### 2.3 RegexString&lt;T&gt;(ruleBuilder, string message, int maximumLength, int minimumLength = 0)
**Signature**
```csharp
public static IRuleBuilderOptions<T, string> RegexString<T>(
    this IRuleBuilder<T, string> ruleBuilder, string message, int maximumLength, int minimumLength = 0)
```
**Muc dich** — Giong 2.2 nhung thay `{PropertyName}` bang chuoi `message` do caller cung cap trong tat ca message loi (dong 28-33).

**Input hop le** — Giong 2.2, them `message` (`string`, bat buoc, khong duoc validate null/rong trong ham — neu `message` la `null`, cac chuoi noi suy `$"{message} ..."` se chi hien thi message rong chu khong loi).

**Output** — `IRuleBuilderOptions<T, string>`.

**Dieu kien xu ly** — Cung thu tu 4 rule nhu 2.2: khong rong → `MaximumLength` → `MinimumLength` → `Matches(MatchsMatches)`, chi khac noi dung message.

**Side effect / Error handling** — Khong co (giong 2.2).

**Khi nao NEN dung** — Khi can message loi tieng Viet tu nhien hon (VD: "So dien thoai khong duoc rong.") thay vi dung ten property ky thuat.

**Khi nao KHONG dung** — Khi field cho phep trong.

**Gioi han** — Giong 2.2; ngoai ra neu `message` chua ky tu dac biet cua message-template FluentValidation (vi du `{`, `}`), co the gay hien thi sai — **khong xac dinh duoc tu source code** vi khong co xu ly escape trong hai overload nay.

---

### 2.4 RegexString&lt;T&gt;(ruleBuilder, int maximumLength = 40)
**Signature**
```csharp
public static IRuleBuilderOptions<T, string> RegexString<T>
    (this IRuleBuilder<T, string> ruleBuilder, int maximumLength = 40)
```
**Muc dich** — Ban rut gon cua 2.2, **khong co `MinimumLength`**, `maximumLength` co gia tri mac dinh `40`.

**Input hop le** — `maximumLength` tuy chon, mac dinh `40`.

**Output** — `IRuleBuilderOptions<T, string>`.

**Dieu kien xu ly** (dong 39-43) — Khong rong → `MaximumLength(maximumLength)` → `Matches(MatchsMatches)`. **Khong co buoc `MinimumLength`.**

**Side effect / Error handling** — Khong co.

**Khi nao NEN dung** — Field chuoi ngan, chi can gioi han max length + regex ky tu dac biet, khong can rang buoc do dai toi thieu, dung `{PropertyName}`.

**Khi nao KHONG dung** — Khi can rang buoc do dai toi thieu (dung 2.2/2.3).

**Gioi han** — Vi thieu `MinimumLength`, mot chuoi 1 ky tu hop le ve regex van pass rule nay du co the khong hop ly ve nghiep vu.

---

### 2.5 RegexString&lt;T&gt;(ruleBuilder, string message, int maximumLength = 40)
**Signature**
```csharp
public static IRuleBuilderOptions<T, string> RegexString<T>
    (this IRuleBuilder<T, string> ruleBuilder, string message, int maximumLength = 40)
```
**Muc dich** — Giong 2.4 nhung dung `message` tuy bien thay `{PropertyName}` (dong 49-53). Luu y message "khong duoc rong" o day **khong co dau cham cuoi cau** (`$"{message} khong duoc rong"`, dong 51) khac voi cac overload khac deu co dau cham — su khac biet nho ve format message giua cac overload.

**Input hop le** — `message` bat buoc, `maximumLength` mac dinh `40`.

**Output** — `IRuleBuilderOptions<T, string>`.

**Dieu kien xu ly** — Khong rong → `MaximumLength` → `Matches(MatchsMatches)`. Khong co `MinimumLength`.

**Side effect / Error handling** — Khong co.

**Khi nao NEN dung** / **KHONG dung** — Tuong tu 2.4, chon khi can message tuy bien.

**Gioi han** — Thieu `MinimumLength` (giong 2.4); message "khong duoc rong" thieu dau cham so voi cac overload khac (khong anh huong logic, chi khac hien thi).

---

### 2.6 RegexString&lt;T&gt;(ruleBuilder, string message, string characterNotMatches, int maximumLength = 40)
**Signature**
```csharp
public static IRuleBuilderOptions<T, string> RegexString<T>
(this IRuleBuilder<T, string> ruleBuilder, string message, string characterNotMatches,
    int maximumLength = 40)
```
**Muc dich** — Bien the cho phep caller **tu cung cap regex** (`characterNotMatches`) thay vi dung hang so `MatchsMatches` co dinh cua cac overload khac (dong 60-64).

**Input hop le**
| Tham so | Kieu | Bat buoc | Rang buoc | Mac dinh |
|---|---|---|---|---|
| `message` | `string` | Bat buoc | Khong validate | — |
| `characterNotMatches` | `string` | Bat buoc | Phai la regex pattern hop le cho `Matches()` cua FluentValidation — neu khong hop le, `RegexParseException` duoc nem ra **ngay khi ham `RegexString(...)` nay thuc thi** (tai lenh `.Matches(characterNotMatches)`, dong 64), **khong phai** doi toi khi `Validate()` chay tren mot model cu the — **da kiem chung thuc nghiem voi FluentValidation 12.1.1** (.NET 8): `RuleFor(x => x.Name).Matches("[invalid(regex")` nem `RegexParseException` ngay tai loi goi, truoc khi validator duoc dung de validate bat ky model nao (`Matches(string)` compile `Regex` eager, khong lazy) | — |
| `maximumLength` | `int` | Tuy chon | Khong validate | `40` |

**Output** — `IRuleBuilderOptions<T, string>`.

**Dieu kien xu ly** — Khong rong → `MaximumLength(maximumLength)` → `Matches(characterNotMatches)` voi message `"{message} khong chua cac ki tu {characterNotMatches}"` (message nay cung in nguyen van pattern regex do caller truyen vao — cung kieu van de nhu muc 3, #2). Khong co `MinimumLength`.

**Side effect / Error handling** — Khong co try/catch. **Da xac minh sai so voi mo ta truoc day**: neu `characterNotMatches` la regex pattern khong hop le, `RegexParseException` duoc nem ra **ngay trong ham nay**, tai thoi diem khai bao rule (tuc trong constructor cua validator nghiep vu khi goi `RuleFor(...).RegexString(message, characterNotMatches, ...)`) — **khong phai** "tai thoi diem rule thuc thi"/luc `Validate()` nhu tung mo ta. He qua: mot `characterNotMatches` sai cu phap se lam **toan bo constructor cua validator nghiep vu nem exception ngay khi khoi tao** (vi du khi DI container resolve validator, hoac khi `AbstractValidatorExtensions.Validate<TValidator,T>` goi `new TValidator()` o muc 2.1) — anh huong rong hon mot lan validate don le, va overload nay khong bat hoac chuyen doi exception do.

**Khi nao NEN dung** — Khi can mot bo ky tu cam khac voi bo mac dinh `#$%^*<>`.

**Khi nao KHONG dung** — Khi bo ky tu cam mac dinh da phu hop (dung 2.2-2.5 de tranh lap lai literal regex o nhieu noi).

**Gioi han** — Khong kiem tra `characterNotMatches` co phai regex hop le truoc khi dung; message loi hien thi luon ca cu phap regex ky thuat cho nguoi dung cuoi; regex khong hop le se sap ngay luc khai bao rule (xem Error handling), khong phai mot loi validate thong thuong co the bat bang `AbstractValidatorExtensions.Validate`.

---

### 2.7 RegexStringDescription&lt;T&gt;
**Signature**
```csharp
public static IRuleBuilderOptions<T, string> RegexStringDescription<T>
    (this IRuleBuilder<T, string> ruleBuilder, string message, int maximumLength = 40)
```
**Muc dich** — Chi kiem tra "khong rong" va "do dai toi da" — **khong co buoc kiem tra regex ky tu dac biet** (khac voi toan bo overload `RegexString` o tren).

**Input hop le** — `message` bat buoc; `maximumLength` tuy chon, mac dinh `40`.

**Output** — `IRuleBuilderOptions<T, string>`.

**Dieu kien xu ly** (dong 70-73) — `Must(!IsNullOrWhiteSpace)` → message `"{message} khong duoc rong."` → `MaximumLength(maximumLength)` → message `"{message} do dai ki tu khong lon hon " + maximumLength`.

**Side effect / Error handling** — Khong co.

**Khi nao NEN dung** — Field mo ta/tu do (free text, vi du ghi chu, mo ta) noi khong muon gioi han ky tu dac biet nhung van can chan rong + gioi han do dai — dung nhu ten method goi y ("Description").

**Khi nao KHONG dung** — Khi field can chan ky tu nguy hiem/dac biet (dung cac overload `RegexString` hoac ket hop them `IsXSSPayload`).

**Gioi han** — Khong co `MinimumLength`; khong co kiem tra regex nen field dung method nay co the chua bat ky ky tu nao (ke ca cac ky tu bi cam o `RegexString`).

---

### 2.8 IsXSSPayload&lt;T&gt;
**Signature**
```csharp
public static IRuleBuilderOptions<T, string> IsXSSPayload<T>
    (this IRuleBuilder<T, string> ruleBuilder, string message)
```
**Muc dich** — Tu choi chuoi khop mot so dau hieu payload XSS don gian.

**Input hop le** — `message` bat buoc (dung trong cau thong bao loi duy nhat cua rule nay).

**Output** — `IRuleBuilderOptions<T, string>`.

**Dieu kien xu ly** (dong 79-91)
1. Pattern regex noi bo: `@"<script\b[^>]*>.*?</script>|on\w+="".*?""|href=""javascript:.*?""|(<|>)"` (dong 83), so khop khong phan biet hoa/thuong (`RegexOptions.IgnoreCase`).
2. Neu `propertyName` la `null`/whitespace → predicate tra `true` (dong 85-88), nghia la **gia tri rong duoc coi la hop le** doi voi rule nay (khong tu chan rong — can ket hop `NotEmpty`/`RegexString` rieng neu muon bat buoc).
3. Nguoc lai, tra `!Regex.IsMatch(propertyName, xssPattern, RegexOptions.IgnoreCase)` (dong 90) — hop le khi **khong** khop bat ky nhanh nao cua pattern.
4. Message khi that bai: `"{message} khong hop le vi chua nhung ky tu ma HTML/JavaScript doc hai."`

**Side effect** — Khong co.

**Error handling** — Khong co try/catch; `Regex.IsMatch` voi pattern co dinh (khong phai input dong) nen rui ro exception thap trong dieu kien van hanh thong thuong.

**Khi nao NEN dung** — Field text tu do co the hien thi lai tren UI (rich text, mo ta, ghi chu) can chan tho mot so payload script co ban.

**Khi nao KHONG dung** — Khi field hop le co the chua `<` hoac `>` cho muc dich khong lien quan toi HTML/script (vi du bieu thuc so sanh, code snippet) — nhanh cuoi cua pattern `(<|>)` se tu choi **bat ky** chuoi chi chua mot trong hai ky tu nay, khong chi chuoi thuc su nguy hiem.

**Gioi han**
- Day la kiem tra **phat hien**, khong sanitize du lieu.
- Pattern khong toan dien: chi bat cac dang `<script>`, `on*="..."`, `href="javascript:..."`, va moi chuoi chua `<`/`>` — khong phai giai phap chong XSS day du (vi du khong chan payload khong dung dau `<`/`>`/`on...=` nhu mot so ky thuat encode khac).
- **Da sua lai phan tich truoc day ve tinh "du" (redundant) cua cac nhanh — mo ta cu sai va da duoc kiem chung lai bang vi du cu the**: chi nhanh `<script\b[^>]*>.*?</script>` la thuc su du (redundant) so voi nhanh cuoi `(<|>)`, vi nhanh nay bat buoc co ky tu `<` literal trong pattern nen bat ky chuoi khop nhanh nay chac chan cung khop `(<|>)`. Nguoc lai, nhanh `href="javascript:.*?"` va nhanh `on\w+=".*?"` **KHONG** yeu cau ky tu `<`/`>` trong pattern cua chung — vi du chuoi `href="javascript:alert(1)"` hoac `onclick="alert(1)"` (khong chua `<`/`>`) van khop hai nhanh nay nhung **khong** khop nhanh `(<|>)`. Do do hai nhanh nay **khong du thua** — chung mo rong that pham vi phat hien so voi chi dung rieng `(<|>)`. Chi nhanh `<script>...</script>` la du; khong xac dinh duoc tu source lieu day la chu dich (phong ho khi nhanh cuoi bi sua) hay code thua.

---

### 2.9 IsDateTime&lt;T&gt;(ruleBuilder)
**Signature**
```csharp
public static IRuleBuilderOptions<T, DateTime?> IsDateTime<T>
    (this IRuleBuilder<T, DateTime?> ruleBuilder)
```
**Muc dich** — Kiem tra gia tri `DateTime?` khong null va parse duoc thanh ngay hop le nam trong khoang SQL Server DateTime ho tro.

**Input hop le** — Khong co tham so ngoai `ruleBuilder`. Ap dung cho property kieu `DateTime?`.

**Output** — `IRuleBuilderOptions<T, DateTime?>`.

**Dieu kien xu ly** (dong 97-112, theo thu tu khai bao)
1. `Must(...)`:
   - `propertyName is null` → tra `false` (khong hop le) (dong 100).
   - `DateTime.TryParse(propertyName.ToString(), CultureInfo.InvariantCulture, out DateTime dateTime)` (dong 102) — parse chuoi bieu dien cua gia tri (khong phai parse truc tiep gia tri `DateTime?`, ma goi `.ToString()` truoc roi `TryParse` lai).
   - Neu parse thanh cong, kiem tra `dateTime >= new DateTime(1753,1,1,...,Utc)` **va** `<= new DateTime(9999,12,31,23,59,59,Utc)` (dong 105-106) — khoang gia tri hop le cua kieu `datetime` trong SQL Server (`1753-01-01` la gioi han duoi kinh dien cua SQL `datetime`).
   - Neu parse that bai → `false` (dong 109).
   - Message khi that bai: `"{PropertyName} dinh dang thoi gian khong hop le."` (dong 111).
2. `.NotNull()` voi message `"{PropertyName} khong duoc rong."` (dong 112) — duoc gan **sau** `Must`.

**Side effect** — Khong co.

**Error handling** — Khong co try/catch tuong minh; `DateTime.TryParse` khong throw theo hop dong chuan cua .NET (tra `bool`). Khong co exception nao duoc xu ly trong ham.

**Khi nao NEN dung** — Property `DateTime?` can dam bao co gia tri va nam trong pham vi luu tru duoc boi kieu `datetime` cua SQL Server.

**Khi nao KHONG dung** — Khi cot dich trong DB la `datetime2` (khong can gioi han nam 1753) hoac khi property von khong nullable (`DateTime` thuong — dung `DateTimeGreaterThanCurrentDate` hoac rule khac).

**Gioi han**
- **[NGHIEM TRONG — phat hien moi, da kiem chung bang thuc nghiem .NET 8 + FluentValidation 12.1.1]** Dong 102 goi `propertyName.ToString()` **khong truyen tham so** — theo hop dong cua `DateTime.ToString()`, viec nay dinh dang chuoi theo `CultureInfo.CurrentCulture` cua thread hien tai (ngam dinh), **khong phai** `InvariantCulture`. Ngay sau do, `DateTime.TryParse(..., CultureInfo.InvariantCulture, out dateTime)` lai ep buoc parse theo `InvariantCulture`. Da kiem chung thuc nghiem: voi `CurrentCulture = vi-VN` (culture pho bien trong ung dung tieng Viet nhu he thong nay), `new DateTime(2026,8,21,13,5,0).ToString()` → `"21/08/2026 13:05:00"`, nhung `DateTime.TryParse("21/08/2026 13:05:00", CultureInfo.InvariantCulture, out _)` tra ve **`false`** (khong parse duoc, vi `InvariantCulture` mong doi thu tu thang/ngay kieu `8/21/2026`). Ket qua giong nhau voi `de-DE`, `fr-FR`, `ar-SA`. He qua: neu thread/process chay duoi culture khac `en-US`/`Invariant` (rat co the xay ra khi ASP.NET Core bat `RequestLocalization` theo `Accept-Language` hoac OS/container dat culture mac dinh la `vi-VN`), rule `IsDateTime` se bao **"dinh dang thoi gian khong hop le" cho MOI gia tri `DateTime?` hop le**, khong chi cac gia tri ngoai khoang SQL DateTime nhu muc dich ban dau cua ham — day la loi nghiem trong hon nhieu so voi cac gioi han khac cua rule nay (xem them muc 3, #9).
- So sanh khoang gia tri dung `DateTimeKind.Utc` cho ca hai bien nhung gia tri `dateTime` parse ra tu `TryParse` khong duoc ep `Kind`, nen so sanh bien la so sanh theo gia tri ngay/gio thuan (`DateTime` so sanh khong xet `Kind` khi so sanh `>=`/`<=`), nhung viec gan `DateTimeKind.Utc` cho hai moc bien trong khi gia tri dau vao co the la gio local la diem khong ro chu dich — **khong xac dinh duoc tu source code** lieu co tinh den timezone hay khong.
- `Must` va `.NotNull()` cung co the fail khi gia tri null → tuy theo `CascadeMode` mac dinh cua FluentValidation 12.1.1 (khong duoc cau hinh tuong minh trong file nay), co the sinh ra 2 message loi cung luc cho cung mot property khi gia tri la `null` — **khong xac dinh duoc tu source code** vi `CascadeMode` khong duoc set trong hai file nay.

---

### 2.10 IsDateTime&lt;T&gt;(ruleBuilder, string message)
**Signature**
```csharp
public static IRuleBuilderOptions<T, DateTime?> IsDateTime<T>
    (this IRuleBuilder<T, DateTime?> ruleBuilder, string message)
```
**Muc dich** — Giong 2.9, chi khac message dung `message` tuy bien thay `{PropertyName}` (dong 118-133).

**Input/Output/Dieu kien xu ly/Side effect/Error handling** — Giong hoan toan 2.9 (cung logic parse + so khoang ngay SQL DateTime), chi khac noi dung 2 message loi. **Bao gom ca loi culture nghiem trong da neu o 2.9** (dong 123 goi `propertyName.ToString()` khong truyen culture, roi `TryParse` ep `InvariantCulture` — cung co che, cung he qua).

**Khi nao NEN/KHONG dung, Gioi han** — Giong 2.9, bao gom ca rui ro culture nghiem trong neu tren.

---

### 2.11 DateTimeGreaterThanCurrentDate&lt;T&gt;
**Signature**
```csharp
public static IRuleBuilderOptions<T, DateTime> DateTimeGreaterThanCurrentDate<T>
    (this IRuleBuilder<T, DateTime> ruleBuilder, string message)
```
**Muc dich** — Theo ten method, co ve nhu de "yeu cau ngay lon hon hien tai", nhung **hanh vi thuc te trong than ham la nguoc lai**: rule coi gia tri hop le khi **khong lon hon** (`<=`) thoi diem hien tai — nghia la **chan ngay trong tuong lai**, chi cho phep ngay o hien tai hoac trong qua khu.

**Input hop le** — `message` bat buoc; ap dung cho property kieu `DateTime` (khong nullable).

**Output** — `IRuleBuilderOptions<T, DateTime>`.

**Dieu kien xu ly** (dong 139-142)
1. `Must(propertyName => propertyName <= CommonBaseConstant.DateTimeUtc())` — hop le khi gia tri **≤** thoi diem hien tai (UTC+7, theo `CommonBaseConstant.DateTimeUtc()`, `FTELSRCore.Shared/Constants/CommonBaseConstant.cs:47`). Message khi that bai: `"{message} lon hon hien tai."` — tuc la thong bao loi mo ta dung *ly do* invalid (gia tri dang lon hon hien tai), du ten method de gay hieu lam la rule *yeu cau* gia tri lon hon hien tai.
2. `.NotNull()` voi message `"{message} khong duoc rong."` (dong 142) — ap dung cho `DateTime` (struct, khong the `null` tru khi property thuc chat la `Nullable<DateTime>` duoc implicit convert) → voi `IRuleBuilder<T, DateTime>` (non-nullable), rule `NotNull()` co kha nang luon pass (khong co y nghia thuc te) vi gia tri kieu `DateTime` khong bao gio la null.

**Side effect** — Khong co.

**Error handling** — Khong co try/catch; `CommonBaseConstant.DateTimeUtc()` khong co tham so truyen vao o day nen dung gia tri mac dinh `addHour = 7` (`CommonBaseConstant.cs:47`).

**Khi nao NEN dung** — Field ngay phai la ngay trong qua khu hoac hien tai (vi du ngay sinh, ngay phat sinh yeu cau) — **khong phai** de yeu cau ngay trong tuong lai, du ten goi y dieu nguoc lai.

**Khi nao KHONG dung** — Khi thuc su can validate "ngay phai lon hon hien tai" (vi du ngay hen trong tuong lai) — method nay se lam nguoc lai yeu cau do; caller can tu viet `Must(x => x > CommonBaseConstant.DateTimeUtc())` hoac dung `IsCustomer`.

**Gioi han**
- **Ten method mau thuan voi hanh vi thuc te** — xem muc 3, #3 (Sai lech nghiem trong vi ten gay hieu lam hoan toan nguoc hanh vi).
- `.NotNull()` tren kieu `DateTime` (value type khong nullable qua generic constraint `IRuleBuilder<T, DateTime>`) nhieu kha nang la rule vo nghia/dead code.
- Khong truyen `addHour` tuy bien — luon dung UTC+7 mac dinh cua `CommonBaseConstant.DateTimeUtc()`.

---

### 2.12 IsStringToDate&lt;T&gt;(ruleBuilder, string message, string format = "yyyy-MM-dd")
**Signature**
```csharp
public static IRuleBuilderOptions<T, string> IsStringToDate<T>
    (this IRuleBuilder<T, string> ruleBuilder, string message, string format = FormatDate)
```
(`FormatDate` = hang so private `"yyyy-MM-dd"`, `ValidatorRequestExtensions.cs:10`)

**Muc dich** — Kiem tra mot chuoi (`string`) co the parse chinh xac thanh `DateTime` theo `format` chi dinh.

**Input hop le**
| Tham so | Kieu | Bat buoc | Rang buoc | Mac dinh |
|---|---|---|---|---|
| `message` | `string` | Bat buoc | Khong validate | — |
| `format` | `string` | Tuy chon | Phai la format string hop le cho `DateTime.TryParseExact` | `"yyyy-MM-dd"` |

**Output** — `IRuleBuilderOptions<T, string>`.

**Dieu kien xu ly** (dong 148-152, **luu y thu tu**)
1. `Must(propertyName => DateTime.TryParseExact(propertyName, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime _))` — kiem tra **dinh dang ngay TRUOC** (dong 149-150). Message khi that bai: `"{message} dinh dang thoi gian khong hop le."`.
2. `Must(x => !string.IsNullOrWhiteSpace(x))` — kiem tra **khong rong SAU** (dong 152). Message: `"{message} khong duoc rong."`.

Thu tu nay nguoc voi thong le thuong thay trong cac method khac cua cung file (luon kiem tra rong truoc khi kiem tra dinh dang/do dai, vi du 2.2-2.8). Theo tai lieu .NET, `DateTime.TryParseExact` tra ve `false` (khong throw) khi chuoi dau vao la `null` hoac rong, nen thu tu nay khong tu no gay loi runtime, nhung khi propertyName rong, message dau tien tra ve cho nguoi dung se la "dinh dang thoi gian khong hop le" truoc khi thay "khong duoc rong" (tuy `CascadeMode`).

**Side effect / Error handling** — Khong co try/catch; dua vao gia tri `bool` tra ve cua `TryParseExact`.

**Khi nao NEN dung** — Field kieu `string` trong DTO/request dai dien cho ngay theo format co dinh (API nhan string ngay, khong dung `DateTime` truc tiep).

**Khi nao KHONG dung** — Khi property da la `DateTime`/`DateTime?` (dung 2.9-2.11).

**Gioi han** — Thu tu rule "dinh dang truoc, rong sau" co the khien message dau tien hien thi cho nguoi dung gay hieu lam khi field trong.

---

### 2.13 IsStringToDate&lt;T&gt;(ruleBuilder, string format = "yyyy-MM-dd")
**Signature**
```csharp
public static IRuleBuilderOptions<T, string> IsStringToDate<T>
    (this IRuleBuilder<T, string> ruleBuilder, string format = FormatDate)
```
**Muc dich/Input/Output/Dieu kien xu ly/Side effect/Error handling** — Giong hoan toan 2.12, chi khac dung `{PropertyName}` thay `message` tuy bien trong 2 message loi (dong 158-162).

**Khi nao NEN/KHONG dung, Gioi han** — Giong 2.12.

---

### 2.14 IsCustomer&lt;T, TType&gt;
**Signature**
```csharp
public static IRuleBuilderOptions<T, TType> IsCustomer<T, TType>
    (this IRuleBuilder<T, TType> ruleBuilder, Func<TType, bool> predicate, string message)
```
**Muc dich** — Wrapper mong cho phep caller tu dinh nghia logic validate tuy bien bang cach truyen `predicate`, khong them bat ky rule hay guard nao khac. Ve ban chat tuong duong goi truc tiep `.Must(predicate).WithMessage(message)` cua FluentValidation.

**Input hop le**
| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Mac dinh |
|---|---|---|---|---|
| `predicate` | `Func<TType, bool>` | Bat buoc | Khong kiem tra `null` — neu `predicate` la `null`, loi `NullReferenceException`/`ArgumentNullException` xay ra khi FluentValidation goi `Must(null)` hoac khi rule thuc thi, khong phai trong ham nay | — |
| `message` | `string` | Bat buoc | Khong validate | — |

**Output** — `IRuleBuilderOptions<T, TType>`.

**Dieu kien xu ly** (dong 168-169) — Goi truc tiep `ruleBuilder.Must(predicate).WithMessage(message)`, khong co logic guard/thu tu nao khac.

**Side effect** — Khong co (tru side effect nam trong chinh `predicate` do caller viet, nam ngoai pham vi file nay).

**Error handling** — Khong co try/catch; moi exception tu `predicate` se lan truyen len trong qua trinh FluentValidation thuc thi rule.

**Khi nao NEN dung** — Can mot dieu kien validate tuy bien khong khop voi cac helper co san khac trong file nay, muon cu phap ngan gon hon viet truc tiep `.Must(...).WithMessage(...)`.

**Khi nao KHONG dung** — Khi dieu kien can validate da co san helper tuong ung (regex chuoi, ngay gio, so dien thoai, v.v.) — dung helper chuyen biet de tai su dung va nhat quan message.

**Gioi han** — Khong cung cap gia tri gia tang nao ngoai viec dat ten co ngu nghia cho `.Must().WithMessage()`; khong guard input null cho `predicate`/`message`.

---

### 2.15 IsNumberPhone&lt;T&gt;
**Signature**
```csharp
public static IRuleBuilderOptions<T, string> IsNumberPhone<T>(this IRuleBuilder<T, string> ruleBuilder,
    string message, int maximumLength = 20, int minimumLength = 10)
```
**Muc dich** — Kiem tra chuoi la so dien thoai Viet Nam hop le (dua tren `ConvertHelpers.VietnamesePhoneValidator`) ket hop gioi han do dai chuoi goc.

**Input hop le**
| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Mac dinh |
|---|---|---|---|---|
| `message` | `string` | Bat buoc | Khong validate | — |
| `maximumLength` | `int` | Tuy chon | Khong validate | `20` |
| `minimumLength` | `int` | Tuy chon | Khong validate | `10` |

**Output** — `IRuleBuilderOptions<T, string>`.

**Dieu kien xu ly** (dong 175-186, theo thu tu)
1. `Must(x => !string.IsNullOrWhiteSpace(x))` → message `"{message} khong duoc rong."` (dong 176).
2. `Must(propertyName => { if (propertyName is null) return false; var result = VietnamesePhoneValidator.Validate(number: propertyName); return result.Valid; })` (dong 177-183) → message `"{message} khong hop le. Vui long nhap dung dinh dang so dien thoai."` (dong 184). `VietnamesePhoneValidator.Validate` (`ConvertHelpers.cs:508-562`) **tu lam sach chuoi** bang `Regex.Replace(number, @"\D", "")` (loai bo moi ky tu khong phai so — **da sua trich dan dong sai**: dong dung la `ConvertHelpers.cs:539`, khong phai `531`) truoc khi so khop voi 4 pattern: `GeoPattern` (co dinh, dong 517), `MobilePattern` (di dong, dong 520), `TollFreePattern` (1800xxxx, 8-10 ky tu so, dong 511), `PremiumPattern` (1900xxxx, dong 514).
3. `MinimumLength(minimumLength)` → message `"{message} phai co it nhat {minimumLength} chu so."` (dong 185) — ap dung len **chuoi goc chua lam sach** (khong phai chuoi da bo ky tu khong phai so).
4. `MaximumLength(maximumLength)` → message `"{message} khong duoc vuot qua {maximumLength} chu so."` (dong 186) — cung ap dung len chuoi goc.

**Side effect** — Khong co (ben trong `VietnamesePhoneValidator.Validate` chi tinh toan thuan, khong I/O).

**Error handling** — Khong co try/catch; `VietnamesePhoneValidator.Validate` khong throw theo code hien tai (luon tra ve `record ValidationResult`).

**Khi nao NEN dung** — Field so dien thoai Viet Nam can phan loai dung theo cac dau so hien hanh (di dong/co dinh/1800/1900).

**Khi nao KHONG dung** — Khi so dien thoai co the chua dinh dang quoc te phuc tap ngoai cac pattern trong `VietnamesePhoneValidator`, hoac khi input da duoc chuan hoa loai bo ky tu phan tach truoc do (khi do `MinimumLength`/`MaximumLength` mac dinh 10/20 co the khong con phu hop).

**Gioi han**
- **`MinimumLength`/`MaximumLength` ap dung len chuoi goc (co the chua khoang trang, dau gach ngang, `+84`, v.v.) trong khi `VietnamesePhoneValidator.Validate` xac thuc tren chuoi da lam sach** (`ConvertHelpers.cs:539`, **da sua tu trich dan sai `531` truoc day**) — hai lop kiem tra dung hai "phien ban" khac nhau cua cung du lieu dau vao, co the dan toi ket qua khong nhat quan (vi du so co dinh dang `+84 987 654 321` dai 16 ky tu goc nhung chi 11 so sau khi lam sach).
- Gia tri `minimumLength` mac dinh (`10`) co the **mau thuan** voi cac so hop le theo `TollFreePattern`/`PremiumPattern` cua `VietnamesePhoneValidator` (`^1800\d{4,6}$` tai `ConvertHelpers.cs:511` / `^1900\d{4,6}$` tai `ConvertHelpers.cs:514`, do dai 8-10 ky tu sau khi lam sach — **da sua trich dan dong sai `509-512` truoc day**) — mot so toll-free/premium 8 ky tu co the duoc `VietnamesePhoneValidator` bao `Valid = true` nhung van fail rule `MinimumLength(10)` o buoc 3 vi chuoi goc (chua lam sach, nhung cung co the da la so thuan) ngan hon 10 ky tu — xem muc 3, #4.

---

### 2.16 IsNumber&lt;T&gt;
**Signature**
```csharp
public static IRuleBuilderOptions<T, string> IsNumber<T>(this IRuleBuilder<T, string> ruleBuilder,
    string message)
```
**Muc dich** — Kiem tra chuoi khong rong va bieu dien mot so nguyen hoac so thap phan khong dau.

**Input hop le** — `message` bat buoc, khong tham so cau hinh khac (khong co `maximumLength`, khong ho tro so am).

**Output** — `IRuleBuilderOptions<T, string>`.

**Dieu kien xu ly** (dong 192-195)
1. `Must(x => !string.IsNullOrWhiteSpace(x))` → message `"{message} khong duoc rong."`.
2. `Matches(@"^\d+(\.\d+)?$")` → message `"{message} phai la ky tu so."` — chap nhan so nguyen (`123`) hoac so thap phan dung dau `.` (`123.45`); **khong chap nhan dau `-` (so am), dau phay phan nhom, ky hieu khoa hoc, hoac dau `+`.**

**Side effect / Error handling** — Khong co.

**Khi nao NEN dung** — Field chuoi bieu dien so khong am (vi du ma so, so luong dang string) can validate dinh dang so co ban.

**Khi nao KHONG dung** — Khi field co the la so am hoac can ho tro dinh dang so phuc tap hon (dau phan nhom nghin, khoa hoc,...).

**Gioi han** — Khong co gioi han do dai chuoi so; khong convert/parse ra kieu so thuc te de so sanh khoang gia tri (chi kiem tra hinh thuc chuoi).

## 3. Van de da biet

| # | Van de | Vi tri | Anh huong |
|---|---|---|---|
| 1 | `AbstractValidatorExtensions.Validate` co the tra ve `IsSuccess = false` nhung `Messages` la danh sach rong, neu tat ca `ErrorMessage` cua cac `ValidationFailure` deu rong/whitespace (bi loc boi `Where(x => !string.IsNullOrWhiteSpace(x.ErrorMessage))`) | `AbstractValidatorExtentions.cs:41-47` | Caller nhan biet co loi (`IsSuccess=false`) nhung khong co noi dung nao de hien thi cho nguoi dung cuoi |
| 2 | Message loi cua cac overload `RegexString` (2.2-2.5) nhung nguyen van pattern regex ky thuat vao cau thong bao hien thi cho nguoi dung: `MatchsMatchesMessage = $"khong chua cac ki tu dac biet: {MatchsMatches}"` voi `MatchsMatches = "^[^#$%^*<>]+$"` | `ValidatorRequestExtensions.cs:11-12`, dung tai dong 22, 33, 43, 53 | Nguoi dung cuoi thay thong bao loi dang `"... khong chua cac ki tu dac biet: ^[^#$%^*<>]+$"` — lo cu phap regex thay vi mo ta de hieu; overload 2.6 (`characterNotMatches`) cung co van de tuong tu khi in truc tiep regex do caller truyen vao (dong 64) |
| 3 | Ten method `DateTimeGreaterThanCurrentDate` gay hieu lam hoan toan nguoc voi hanh vi thuc te: than ham coi gia tri **hop le** khi `propertyName <= CommonBaseConstant.DateTimeUtc()` (tuc **khong duoc lon hon** hien tai), nghia la rule nay **chan** ngay lon hon hien tai chu khong yeu cau ngay phai lon hon hien tai nhu ten goi y | `ValidatorRequestExtensions.cs:136-143` | Rui ro cao cho developer/AI agent doc ten method roi dung sai muc dich (vi du ap dung cho field can validate "ngay hen trong tuong lai" se bi ap nguoc logic) |
| 4 | `IsNumberPhone` ap dung `MinimumLength`/`MaximumLength` (mac dinh 10/20) len chuoi so dien thoai **goc, chua lam sach**, trong khi `VietnamesePhoneValidator.Validate` (duoc goi ngay truoc do trong cung rule chain) tu lam sach chuoi (`Regex.Replace(number, @"\D", "")`) truoc khi xac thuc dinh dang — hai buoc kiem tra hoat dong tren hai phien ban du lieu khac nhau cua cung input, co the cho ket qua khong nhat quan (so co ky tu phan tach dai hon 20 hoac so toll-free/premium 8 chu so ngan hon 10 co the bi hai lop rule danh gia trai nhau) | `ValidatorRequestExtensions.cs:172-187` (rule chain), `ConvertHelpers.cs:508-562` (`VietnamesePhoneValidator`; `TollFreePattern` dong 511, `PremiumPattern` dong 514, `Regex.Replace` dong 539 — **da sua trich dan dong sai `508-531`/`509-512` truoc day**) | So dien thoai toll-free/premium hop le ve dinh dang van co the bi tu choi boi `MinimumLength(10)`; so co dinh dang dai (dau cach, `+84`) co the vuot `MaximumLength(20)` du so thuc (sau khi lam sach) hop le |
| 5 | Ten file `AbstractValidatorExtentions.cs` (typo "Extentions") khong khop voi ten class `AbstractValidatorExtensions` (chinh ta dung) khai bao ben trong | `AbstractValidatorExtentions.cs:6` | Chi la van de dat ten file, khong anh huong hanh vi runtime, nhung co the gay kho tim kiem/kho doan khi tra cuu theo ten |
| 6 | Hang so `MatchsMatches`/`MatchsMatchesMessage` co ten chua loi chinh ta ("Matchs" thay vi "Matches") | `ValidatorRequestExtensions.cs:11-12` | Khong anh huong hanh vi, chi la van de quy uoc dat ten |
| 7 | `AbstractValidatorExtensions.Validate<TValidator, T>` bat buoc `TValidator` co constructor khong tham so (`new()` constraint) nen khong the dung voi validator can Dependency Injection qua constructor | `AbstractValidatorExtentions.cs:26-28` | Han che pham vi ap dung — cac validator co dependency (vi du can goi repository de validate uniqueness) khong dung duoc helper nay |
| 8 | `.NotNull()` duoc gan them sau `.Must(...)` trong `DateTimeGreaterThanCurrentDate` (ap dung cho `IRuleBuilder<T, DateTime>`, kieu gia tri khong nullable) — ve nguyen tac mot `struct` khong nullable khong the la `null`, khien rule `NotNull()` o day nhieu kha nang khong co tac dung thuc te; khong xac dinh chac chan duoc hanh vi runtime chinh xac cua FluentValidation 12.1.1 trong truong hop nay chi tu 2 file source dang xet | `ValidatorRequestExtensions.cs:142` | Rule co kha nang la dead code, khong phat hien them loi nao ngoai nhung gi `.Must(...)` da kiem tra |
| 9 | **[Nghiem trong, phat hien moi qua kiem chung thuc nghiem]** `IsDateTime<T>` (ca 2 overload) goi `propertyName.ToString()` **khong truyen `CultureInfo`** (dung ngam dinh `CultureInfo.CurrentCulture` cua thread) roi ngay sau do `DateTime.TryParse(...)` lai ep buoc `CultureInfo.InvariantCulture` — hai buoc dung hai culture khac nhau cho cung mot gia tri. Da kiem chung thuc nghiem voi .NET 8: khi `CurrentCulture = vi-VN` (culture rat pho bien cho ung dung tieng Viet), `ToString()` tra ve dang `"21/08/2026 13:05:00"` nhung `TryParse` voi `InvariantCulture` **khong parse duoc** chuoi nay (tra `false`) — tuong tu voi `de-DE`, `fr-FR`, `ar-SA` | `ValidatorRequestExtensions.cs:102` (overload khong message), `ValidatorRequestExtensions.cs:123` (overload co message) | Khi thread/process chay duoi culture khac `en-US`/Invariant, rule `IsDateTime` bao loi "dinh dang thoi gian khong hop le" cho **moi** gia tri `DateTime?` hop le, bat ke gia tri co nam trong khoang SQL DateTime hay khong — muc do anh huong cao hon tat ca cac gioi han khac da ghi nhan cho rule nay |

**Khong tim thay noi goi (call site) nao cua ca hai file nay trong repo `sr-core-helper`** — moi nhan dinh ve "khi nao nen dung/khong nen dung" o tren dua hoan toan vao phan tich than ham, khong dua tren vi du su dung thuc te trong repo.

Module nay **khong tham chieu toi** bat ky kieu/file nao trong danh sach can doi chieu nguoc voi 8 file Knowledge Base hien co (`AuditModel`, `HttpOptionModel`, `ErrorModel`, `CustomException`, `ProjectToExtensions`, `PrecateBuilderExtensions`, `MeasureExecutionTimeExtensions.InvokeForHTTP`, `MongoResiliencePolicyFactory`, `BaseEntityMongoDB`/`BaseEntitySQL`) — da kiem tra bang cach tim kiem cac ten nay trong ca hai file source, khong co ket qua trung khop. Do do khong co noi dung doi chieu nguoc nao de ghi vao muc nay.
