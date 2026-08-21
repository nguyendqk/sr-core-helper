# Kafka Extensions

> Nguon: FTELSRCore.Shared/Extensions/KafkaExtensions.cs, FTELSRCore.Shared/Extensions/KafkaTraceContextExtensions.cs
> Loai: class (khong static) + static class + attribute class
> Cap nhat theo commit: 89c1ce9

## 1. Tong quan

Module nay gom hai file doc lap ve chuc nang, cung nam trong namespace `FTELSRCore.Extensions`:

- **KafkaExtensions.cs**: chua cac lop lam **value serializer/deserializer** cho Confluent.Kafka
  (`JSonSerializer<T>` implement `ISerializer<T>`, `JSonDeserializer<T>` implement `IDeserializer<T>`),
  dung de gan vao `ProducerBuilder<TKey,TValue>.SetValueSerializer()` / `ConsumerBuilder<TKey,TValue>.SetValueDeserializer()`
  o noi khac (ngoai pham vi 2 file nay). Ngoai ra file nay con chua mot attribute
  (`CollectionNameAttribute`) va extension method (`CollectionName()`) khong lien quan gi den Kafka
  (xem muc 3 - Van de da biet).
- **KafkaTraceContextExtensions.cs**: la helper **serialize/gan va doc trace context W3C** (thong qua
  `OpenTelemetry.Context.Propagation.TextMapPropagator`, mac dinh la `Propagators.DefaultTextMapPropagator`)
  vao/tu **Kafka message headers** (`Confluent.Kafka.Headers`). Day KHONG phai la code xay dung
  producer/consumer config (khong co `ProducerConfig`, `ConsumerConfig`, `ProducerBuilder`,
  `ConsumerBuilder`, khong ket noi broker) - toan bo logic chi thao tac tren doi tuong `Headers`
  duoc truyen vao tu ben ngoai (KafkaTraceContextExtensions.cs:13, KafkaTraceContextExtensions.cs:31).

Ca hai file khong tim thay bat ky loi goi (call site) nao trong repo hien tai (xem muc 3).

### 1.1 Pham vi chuc nang

| Lam duoc | Khong lam duoc |
|---|---|
| Serialize object `T` sang JSON `byte[]` bang Newtonsoft.Json de Kafka producer gui di (KafkaExtensions.cs:12-15) | Khong tao/cau hinh `ProducerConfig`, `ConsumerConfig`, `ProducerBuilder`, `ConsumerBuilder` hay bat ky ket noi Kafka broker nao (khong co dong code nao lien quan trong 2 file nguon) |
| Deserialize `byte[]` JSON sang object `T` bang Newtonsoft.Json, co buoc "double-check" bang System.Text.Json truoc khi parse thuc su (KafkaExtensions.cs:20-33) | Khong xu ly logic topic/partition/offset, khong dung `SerializationContext` (tham so `context` nhan vao nhung khong doc gia tri nao - KafkaExtensions.cs:12, KafkaExtensions.cs:20) |
| Inject W3C trace context (traceparent) + Baggage hien tai (`Activity.Current`, `Baggage.Current`) vao `Headers` cua message truoc khi producer gui (KafkaTraceContextExtensions.cs:13-29) | Khong tu dong goi ham nay trong luc gui message - caller phai tu goi `InjectProducerTraceContext` truoc khi `Produce`/`ProduceAsync` (khong co wrapper producer nao trong 2 file) |
| Extract `PropagationContext` (trace context + baggage) tu `Headers` cua message da nhan duoc o consumer (KafkaTraceContextExtensions.cs:31-52) | Khong tu dong set `Activity.Current` hay start Activity moi - chi tra ve `PropagationContext`, viec `Activity.SetParentId`/`Baggage.Current = ...` la trach nhiem cua caller |
| Lay ten collection tuy chinh cua mot `Type` qua attribute (KafkaExtensions.cs:39-42) | Khong lien quan Kafka; khong co logic map collection Mongo thuc te trong 2 file nay (xem muc 3) |

### 1.2 Dependency

| Thanh phan | Muc dich su dung |
|---|---|
| `Confluent.Kafka` (`ISerializer<T>`, `IDeserializer<T>`, `SerializationContext`, `Headers`) | Interface chuan cua Confluent.Kafka client de plug vao producer/consumer; `Headers` la doi tuong header cua 1 Kafka message |
| `Newtonsoft.Json` (`JsonConvert`) | Thuc hien serialize/deserialize JSON thuc su trong `JSonSerializer<T>`/`JSonDeserializer<T>` |
| `System.Reflection` (`GetCustomAttribute<T>`) | Doc attribute `CollectionNameAttribute` tren `Type` trong `CollectionExtensions.CollectionName()` |
| `OpenTelemetry` / `OpenTelemetry.Context.Propagation` (`Propagators.DefaultTextMapPropagator`, `TextMapPropagator`, `PropagationContext`, `Baggage`) | Chuan W3C Trace Context propagation - dung de inject/extract traceparent + baggage vao/tu Kafka headers |
| `System.Diagnostics` (`Activity`) | Lay `Activity.Current?.Context` de dong bo trace hien tai vao message khi inject |
| `System.Text` (`Encoding.UTF8`) | Encode/decode chuoi JSON va gia tri header giua `string` va `byte[]` |
| `JSonParseHelpers.JSonTryParse<T>(this string, ...)` (FTELSRCore.Shared/Helpers/JSonParseHelpers.cs:149) | Duoc `JSonDeserializer<T>.Deserialize` goi de "double-check" chuoi JSON truoc khi Newtonsoft deserialize thuc su - ham nay noi bo dung **System.Text.Json**, khac voi Newtonsoft dung o buoc deserialize cuoi (xem muc 3) |

### 1.3 Danh muc API

| API | Nhom | Mo ta ngan |
|---|---|---|
| `KafkaExtensions.JSonSerializer<T>.Serialize(T, SerializationContext)` | Kafka value serializer | Serialize `T` sang JSON UTF-8 `byte[]` bang Newtonsoft.Json |
| `KafkaExtensions.JSonDeserializer<T>.Deserialize(ReadOnlySpan<byte>, bool, SerializationContext)` | Kafka value deserializer | Deserialize `byte[]` JSON sang `T`, tra ve `default` khi null/parse loi |
| `CollectionExtensions.CollectionName(this Type type)` | Reflection helper (khong lien quan Kafka) | Tra ve `CollectionName` tu attribute neu co, nguoc lai tra ve `type.Name` |
| `CollectionNameAttribute(string name)` | Attribute (khong lien quan Kafka) | Gan ten tuy chinh cho mot Type/member bat ky |
| `KafkaTraceContextExtensions.InjectProducerTraceContext(Headers headers)` | Trace context - Producer | Gan traceparent + baggage hien tai vao `Headers` truoc khi gui |
| `KafkaTraceContextExtensions.ExtractConsumerTraceContext(Headers headers)` | Trace context - Consumer | Doc traceparent + baggage tu `Headers` cua message da nhan |

## 2. Chi tiet API

### 2.1 KafkaExtensions.JSonSerializer\<T\>.Serialize

**Signature**
```csharp
public byte[] Serialize(T data, SerializationContext context)
```
(KafkaExtensions.cs:10-16, lop long `JSonSerializer<T> : ISerializer<T>` ben trong lop `KafkaExtensions`)

**Muc dich** - Implement `Confluent.Kafka.ISerializer<T>` de dung lam value serializer cho Kafka
producer: chuyen object `T` thanh `byte[]` JSON (UTF-8) bang `Newtonsoft.Json.JsonConvert.SerializeObject`.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `data` | `T` (generic) | Co | Khong co validate nao trong than ham - `null` cung duoc truyen thang vao `JsonConvert.SerializeObject` (KafkaExtensions.cs:14) | Khong co |
| `context` | `Confluent.Kafka.SerializationContext` | Co (theo signature interface) | Nhan vao nhung **khong duoc doc/su dung** trong than ham (KafkaExtensions.cs:12) | Khong co |

**Output** - `byte[]`: mang byte UTF-8 cua chuoi JSON sinh ra tu `JsonConvert.SerializeObject(data)`.
Neu `data` la `null`, `JsonConvert.SerializeObject(null)` tra ve chuoi `"null"`, nen ket qua la byte
cua chuoi `"null"` (khong throw, khong tra ve mang rong hay `null`) - suy ra tu hanh vi chuan cua
Newtonsoft.Json, khong co xu ly rieng trong ham nay.

**Dieu kien xu ly** - Khong co nhanh re: ham chi co dung 1 buoc `return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data));` (KafkaExtensions.cs:14).

**Side effect** - Khong co (khong ghi log, khong goi ngoai, khong mutate tham so/state chung).

**Error handling** - Khong co try/catch trong ham. Neu `JsonConvert.SerializeObject` throw (vi du `data`
chua reference loop khong xu ly duoc, hoac custom converter loi), exception se duoc nem thang ra ngoai
cho Confluent.Kafka producer xu ly (khong bat, khong nem lai co bien doi).

**Khi nao NEN dung** - Khi can 1 value serializer JSON don gian, dong nhat, cho Kafka producer trong
cac service dung chung `FTELSRCore.Shared`, va chap nhan dung Newtonsoft.Json lam engine serialize.

**Khi nao KHONG dung** - Khi can kiem soat serialization theo `SerializationContext` (vi du serialize
khac nhau theo topic), vi tham so nay bi bo qua hoan toan; hoac khi can dung `System.Text.Json` de dong
bo voi phan con lai cua he thong (xem muc 3 ve su khong dong nhat giua serializer va deserializer).

**Gioi han** - `SerializationContext` (topic, component Key/Value) bi bo qua hoan toan - khong the
tuy bien theo topic. Khong co xu ly rieng cho `data == null` (van serialize ra chuoi `"null"` thay vi
tra mang byte rong hoac `null`).

### 2.2 KafkaExtensions.JSonDeserializer\<T\>.Deserialize

**Signature**
```csharp
public T Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
```
(KafkaExtensions.cs:18-34, lop long `JSonDeserializer<T> : IDeserializer<T>` ben trong lop `KafkaExtensions`)

**Muc dich** - Implement `Confluent.Kafka.IDeserializer<T>` de dung lam value deserializer cho Kafka
consumer: chuyen `byte[]`/`ReadOnlySpan<byte>` JSON nhan duoc thanh object `T`, co buoc kiem tra hop le
truoc khi deserialize thuc su.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `data` | `ReadOnlySpan<byte>` | Co | Duoc decode thanh UTF-8 string 2 lan (mot lan de goi `JSonTryParse`, mot lan de goi `JsonConvert.DeserializeObject`) - KafkaExtensions.cs:27, KafkaExtensions.cs:32 | Khong co |
| `isNull` | `bool` | Co | Neu `true`, ham tra ve ngay `default` va khong dung den `data` (KafkaExtensions.cs:22-25) | Khong co |
| `context` | `SerializationContext` | Co (theo signature interface) | Nhan vao nhung **khong duoc doc/su dung** trong than ham | Khong co |

**Output** - `T`:
- `default(T)` khi `isNull == true` (KafkaExtensions.cs:24).
- `default(T)` khi `Encoding.UTF8.GetString(data).JSonTryParse(out T _)` tra ve `false` (KafkaExtensions.cs:27-30) - bao gom ca truong hop chuoi rong/whitespace, chuoi `"null"`, `"{}"`, `"[]"` (theo logic cua `JSonTryParse<T>(this string,...)` tai JSonParseHelpers.cs:152-158), va ca truong hop JSON khong hop le hoac khong the map vao `T`.
- Ket qua thuc su cua `JsonConvert.DeserializeObject<T>(...)` (Newtonsoft.Json) khi buoc kiem tra tren tra ve `true` (KafkaExtensions.cs:32).

**Dieu kien xu ly** (theo thu tu thuc thi trong than ham)
1. Neu `isNull == true` -> tra ve `default` ngay, khong dong toi `data` (KafkaExtensions.cs:22-25).
2. Decode `data` sang UTF-8 string, goi `JSonTryParse<T>` (dung `System.Text.Json` noi bo, xem JSonParseHelpers.cs:149-194) de kiem tra "parse thu" co thanh cong khong.
3. Neu buoc 2 that bai (`false`) -> tra ve `default` (KafkaExtensions.cs:29).
4. Neu buoc 2 thanh cong -> decode `data` sang UTF-8 string **lan thu hai**, roi goi `JsonConvert.DeserializeObject<T>` (Newtonsoft.Json) va tra ve ket qua nay (KafkaExtensions.cs:32) - **khong** tra ve ket qua `out T _` da parse duoc o buoc 2 (bien nay bi discard bang `_`).

**Side effect** - Khong co ghi log/DB/goi ngoai truc tiep trong ham nay. Tuy nhien `JSonTryParse<T>`
duoc goi **khong truyen `logger`** (dung gia tri mac dinh `logger = null`), nen khi buoc parse thu that
bai vi exception, `JSonTryParse` se roi vao nhanh `default` (ghi log qua console qua
`CommonBaseConstant.ConfigLoggerExceptionByConsole`, theo JSonParseHelpers.cs:182-188) - day la side
effect gian tiep (ghi console) nam ngoai file KafkaExtensions.cs nhung duoc kich hoat tu day.

**Error handling** - Ham `Deserialize` nay **khong co try/catch cua rieng no**. Loi parse tu buoc kiem
tra (`JSonTryParse`) da duoc `JSonTryParse` tu bat va tra ve `false` (khong throw ra ngoai). Nhung neu
buoc 4 - `JsonConvert.DeserializeObject<T>` (Newtonsoft) - throw exception (vi du JSON hop le voi
`System.Text.Json` nhung khong map duoc kieu `T` theo quy uoc cua Newtonsoft, hoac nguoc lai), exception
nay **se khong duoc bat** va se duoc nem thang ra ngoai cho Kafka consumer xu ly.

**Khi nao NEN dung** - Khi can 1 value deserializer JSON co "guard" chong crash cho cac payload rong/
khong hop le co the roi vao Kafka topic (thay vi throw ngay), va he thong producer/consumer dong y quy
uoc "parse loi -> tra `default`" thay vi throw.

**Khi nao KHONG dung** - Khi can phan biet ro giua "message that su la default/empty" va "message loi
parse" (ham nay tra ve cung mot gia tri `default` cho ca hai truong hop); hoac khi payload JSON hop le
la `"{}"`/`"[]"`/`"null"` va ky vong nhan duoc mot instance rong hop le cua `T` (xem muc 3 - day la mot
"Van de da biet").

**Gioi han**
- Dung **2 thu vien JSON khac nhau** trong cung 1 lan deserialize: `System.Text.Json` (qua `JSonTryParse`)
  de kiem tra, roi `Newtonsoft.Json` (qua `JsonConvert.DeserializeObject`) de lay ket qua thuc su - hai
  thu vien nay co the co hanh vi khac nhau voi cung 1 kieu `T` (attribute, naming policy, xu ly enum,
  v.v.), dan den truong hop parse-check thanh cong nhung buoc deserialize thuc su van throw, hoac nguoc
  lai kiem tra sai le loai bo mot JSON hop le voi Newtonsoft.
- Decode `Encoding.UTF8.GetString(data)` **2 lan** cho cung 1 `data` (KafkaExtensions.cs:27 va
  KafkaExtensions.cs:32) - thua 1 lan decode/allocate string khong can thiet.
- `SerializationContext` bi bo qua hoan toan, giong `JSonSerializer<T>`.
- Cac payload JSON hop le nhung la `"{}"`, `"[]"`, hoac `"null"` (chuoi) se bi coi la "parse fail" va
  tra ve `default(T)` thay vi mot instance rong hop le, do phu thuoc vao logic dac biet trong
  `JSonTryParse<T>(this string,...)` (JSonParseHelpers.cs:152-158) - day la hanh vi gian tiep, khong
  the thay doi tu file KafkaExtensions.cs.

### 2.3 CollectionExtensions.CollectionName

**Signature**
```csharp
public static string CollectionName(this Type type)
```
(KafkaExtensions.cs:37-43, lop static `CollectionExtensions`, nam cung file voi `KafkaExtensions` nhung
khong lien quan Kafka - xem muc 3)

**Muc dich** - Tra ve ten "collection" cua mot `Type`: lay tu attribute `CollectionNameAttribute` neu
`Type` co gan attribute nay, nguoc lai fallback ve `type.Name` (ten class thuc te).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `type` (this) | `System.Type` | Co | Khong kiem tra `null` - neu `type` la `null`, `type.GetCustomAttribute<...>()` se throw `NullReferenceException` (khong co guard trong than ham, KafkaExtensions.cs:41) | Khong co |

**Output** - `string`: gia tri `CollectionName` cua attribute (KafkaExtensions.cs:41) neu `type` co gan
`[CollectionName("...")]`; nguoc lai tra ve `type.Name` (ten Type, khong gom namespace).

**Dieu kien xu ly** - Mot dong duy nhat, dung null-conditional + null-coalescing:
`type.GetCustomAttribute<CollectionNameAttribute>()?.CollectionName ?? type.Name` (KafkaExtensions.cs:41).

**Side effect** - Khong co.

**Error handling** - Khong co try/catch. `type == null` se gay `NullReferenceException` khong duoc bat.

**Khi nao NEN dung** - Khi mot Type can co ten collection/bang tuy chinh khac ten class, va attribute
`CollectionNameAttribute` da duoc gan tren Type do.

**Khi nao KHONG dung** - Trong pham vi 2 file nguon cua module Kafka nay: **khong co bat ky call site
nao** trong toan bo repo su dung ham nay (xem muc 3) - khong co bang chung day la mot phan cua luong
xu ly Kafka thuc te.

**Gioi han** - Khong validate `type == null`. Ten va vi tri (nam trong file KafkaExtensions.cs, cung
class voi Kafka serializer) gay nham lan ve pham vi chuc nang (xem muc 3).

### 2.4 CollectionNameAttribute

**Signature**
```csharp
[AttributeUsage(AttributeTargets.All)]
public class CollectionNameAttribute(string name) : Attribute
{
    public string CollectionName { get; } = name;
}
```
(KafkaExtensions.cs:45-49)

**Muc dich** - Attribute tuy chinh de gan mot ten "collection" (chuoi) cho bat ky Type/member nao, doc
lai duoc qua `CollectionExtensions.CollectionName()` (muc 2.3).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `name` (constructor, primary constructor C# 12) | `string` | Co | Khong validate (`null`/rong deu duoc gan thang vao property `CollectionName`, KafkaExtensions.cs:48) | Khong co |

**Output** - Khong ap dung (attribute, khong co return value; property `CollectionName` la get-only,
gan gia tri 1 lan tu constructor).

**Dieu kien xu ly** - Khong co logic dieu kien; day chi la mot property gan tu constructor.

**Side effect** - Khong co.

**Error handling** - Khong co.

**Khi nao NEN dung** - Khi mot kieu du lieu can khai bao ten collection/khong theo ten class, va code
doc lai bang `CollectionExtensions.CollectionName()`.

**Khi nao KHONG dung** - `[AttributeUsage(AttributeTargets.All)]` cho phep gan attribute nay len **bat
ky thanh phan nao** (class, method, property, parameter, return value, v.v.) - pham vi rat rong so voi
muc dich "ten collection" (thuong chi nen ap dung cho class). Khong co bang chung trong repo ve viec
attribute nay duoc su dung thuc te (xem muc 3).

**Gioi han** - `AttributeUsage(AttributeTargets.All)` qua rong so voi muc dich thuc te; khong co
validate `name` (co the gan chuoi rong/`null`).

### 2.5 KafkaTraceContextExtensions.InjectProducerTraceContext

**Signature**
```csharp
public static void InjectProducerTraceContext(Headers headers)
```
(KafkaTraceContextExtensions.cs:13-29)

**Muc dich** - Gan (inject) W3C trace context hien tai (`Activity.Current`) va `Baggage.Current` vao
`Headers` cua mot Kafka message, de producer gui message co kem theo trace context - phuc vu distributed
tracing xuyen qua Kafka. Day la ham **serialize/gan trace context vao Kafka message header**, khong
phai code xay dung producer config.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `headers` | `Confluent.Kafka.Headers` | Co | `ArgumentNullException.ThrowIfNull(headers)` - bat buoc khong `null` (KafkaTraceContextExtensions.cs:15) | Khong co |

**Output** - `void`. Ket qua duoc phan anh qua **side effect** tren tham so `headers` (xem duoi).

**Dieu kien xu ly**
1. Validate `headers` khong `null`, throw ngay neu vi pham (KafkaTraceContextExtensions.cs:15).
2. Tao `PropagationContext` moi tu `Activity.Current?.Context ?? default` (trace context hien tai, hoac
   `default` neu khong co Activity dang chay) va `Baggage.Current` (KafkaTraceContextExtensions.cs:17-18).
3. Goi `Propagator.Inject(propagationContext, headers, setter)` voi `Propagator` la
   `Propagators.DefaultTextMapPropagator` (KafkaTraceContextExtensions.cs:11, KafkaTraceContextExtensions.cs:20-28)
   - propagator se tu quyet dinh cac key duoc ghi (vi du `traceparent`, `tracestate`, `baggage` theo
     chuan W3C - hanh vi cu the phu thuoc implementation cua `Propagators.DefaultTextMapPropagator` cua
     OpenTelemetry, nam ngoai file nay nen **khong xac dinh duoc chinh xac tu source code trong repo**).
4. Voi moi key/value ma propagator can ghi, delegate setter se goi `headers.Add(key, Encoding.UTF8.GetBytes(value))` (KafkaTraceContextExtensions.cs:23-28) - **them** header moi vao `headers` (khong kiem tra
   trung key, `Headers.Add` cua Confluent.Kafka cho phep nhieu header cung key).

**Side effect** - **Mutate tham so `headers` duoc truyen vao** (them 1 hoac nhieu key/value byte[] vao
danh sach header) - day la side effect chinh va duy nhat cua ham. Khong ghi log, khong goi I/O ngoai.

**Error handling** - Chi co 1 diem throw ro rang: `ArgumentNullException` khi `headers == null`
(KafkaTraceContextExtensions.cs:15). Khong co try/catch nao khac trong than ham; neu `Propagator.Inject`
noi bo throw, exception se duoc nem thang ra ngoai (khong bat, khong nem lai co bien doi).

**Khi nao NEN dung** - Goi ngay truoc khi producer gui message (truyen vao `Headers` cua
`Message<TKey,TValue>` sap gui), de propagate trace context tu Activity hien tai sang consumer phia sau.

**Khi nao KHONG dung** - Khong dung khi `headers` co the la `null` va muon bo qua im lang (ham nay se
throw); khong dung de "doc" hay kiem tra trace context (day la ham inject mot chieu, khong tra ve gia
tri).

**Gioi han** - Phu thuoc hoan toan vao `Activity.Current` tai thoi diem goi ham - neu khong co Activity
nao dang active, se inject trace context "rong" (`default`) thay vi bo qua. Khong kiem soat duoc danh
sach header nao se duoc ghi (do `Propagators.DefaultTextMapPropagator` quyet dinh, co the thay doi theo
cau hinh OpenTelemetry cua ung dung, nam ngoai pham vi file nay).

### 2.6 KafkaTraceContextExtensions.ExtractConsumerTraceContext

**Signature**
```csharp
public static PropagationContext ExtractConsumerTraceContext(Headers headers)
```
(KafkaTraceContextExtensions.cs:31-52)

**Muc dich** - Doc (extract) trace context W3C tu `Headers` cua mot Kafka message da nhan duoc o phia
consumer, tra ve `PropagationContext` (gom `ActivityContext` va `Baggage`) de caller tu quyet dinh cach
ap dung (vi du lam parent cho Activity moi).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `headers` | `Confluent.Kafka.Headers` | Khong bat buoc | Neu `headers is null`, ham tra ve `default` ngay, **khong throw** (KafkaTraceContextExtensions.cs:33-36) - khac voi `InjectProducerTraceContext` (throw khi null) | Khong co |

**Output** - `PropagationContext`:
- `default(PropagationContext)` khi `headers == null` (KafkaTraceContextExtensions.cs:35).
- Ket qua tra ve tu `Propagator.Extract(default, headers, getter)` trong cac truong hop khac
  (KafkaTraceContextExtensions.cs:38-51) - gia tri cu the (co tim thay trace context hop le hay khong)
  phu thuoc hoan toan vao noi dung `headers` va logic parse cua `Propagators.DefaultTextMapPropagator`
  (nam ngoai file nay).

**Dieu kien xu ly**
1. Neu `headers is null` -> tra ve `default` ngay (KafkaTraceContextExtensions.cs:33-36).
2. Nguoc lai, goi `Propagator.Extract(default, headers, getter)` (KafkaTraceContextExtensions.cs:38-41).
3. Delegate `getter(carrier, key)` duoc propagator goi cho tung key can tra cuu:
   a. Tim header **cuoi cung** (`LastOrDefault`) trong `carrier` (chinh la `headers`) co `Key` khop
      `key` theo kieu so sanh **khong phan biet hoa/thuong** (`StringComparison.OrdinalIgnoreCase`)
      (KafkaTraceContextExtensions.cs:43-46).
   b. Neu khong tim thay (`header is null`) -> tra ve mang rong `[]` (KafkaTraceContextExtensions.cs:48-49).
   c. Neu tim thay -> tra ve mang 1 phan tu `[Encoding.UTF8.GetString(header.GetValueBytes())]`
      (KafkaTraceContextExtensions.cs:50).

**Side effect** - Khong co (chi doc `headers`, khong mutate, khong ghi log/goi ngoai).

**Error handling** - Khong co try/catch trong than ham. Truong hop `headers == null` duoc xu ly bang
nhanh re tra `default`, khong phai bang exception handling. Neu `header.GetValueBytes()` throw (vi du
gia tri header bi null trong mot so tinh huong cua Confluent.Kafka), exception se khong duoc bat.

**Khi nao NEN dung** - Goi ngay khi consumer nhan duoc message, truyen vao `Headers` cua
`ConsumeResult<TKey,TValue>.Message`, de lay lai trace context da duoc producer inject truoc do (qua
`InjectProducerTraceContext`) va tiep tuc chuoi trace (vi du `Activity.StartActivity(..., ActivityKind.Consumer, parentContext: result.Context)` - viec nay do caller tu thuc hien, khong nam trong ham nay).

**Khi nao KHONG dung** - Khong dung de validate su ton tai cua trace context (ham luon tra ve mot
`PropagationContext`, ke ca khi khong tim thay gi - se la gia tri rong/default cua no, khong `null`,
khong throw, khong co co bao "not found" rieng).

**Gioi han** - Khi co **nhieu header trung key** (case-insensitive), ham chi lay header **cuoi cung**
theo thu tu trong `Headers` (`LastOrDefault`) - neu logic ghi header o producer (hoac broker/middleware
khac) tao nhieu header trung ten voi thu tu khong nhu mong doi, ket qua extract co the sai gia tri mong
muon. Viec so khop key khong phan biet hoa/thuong co the vo tinh khop nham mot header khac ten hoa/thuong
nhung trung nghia (vi du mot he thong khac ghi `Traceparent` thay vi `traceparent`).

## 3. Van de da biet

| # | Van de | Vi tri | Anh huong |
|---|---|---|---|
| 1 | `JSonDeserializer<T>.Deserialize` dung 2 thu vien JSON khac nhau trong cung 1 lan goi: `System.Text.Json` (qua `JSonTryParse`, xem JSonParseHelpers.cs:149-166) de "kiem tra thu", roi `Newtonsoft.Json` (`JsonConvert.DeserializeObject`) de lay ket qua thuc su - hai engine co the khong dong nhat hanh vi voi cung kieu `T` | KafkaExtensions.cs:27, KafkaExtensions.cs:32 | Rui ro: kiem tra parse thanh cong (System.Text.Json) nhung buoc deserialize thuc su (Newtonsoft) van throw exception khong duoc bat trong ham nay; hoac nguoc lai, JSON hop le voi Newtonsoft nhung bi loai vi System.Text.Json parse loi (T tra `default`) |
| 2 | Buoc kiem tra `JSonTryParse<T>(this string,...)` coi cac chuoi JSON hop le nhu `"{}"`, `"[]"`, hoac `"null"` la "parse fail" (tra `false`) | JSonParseHelpers.cs:152-158 (goi tu KafkaExtensions.cs:27) | `JSonDeserializer<T>.Deserialize` tra ve `default(T)` cho ca payload Kafka hop le la object/array rong, giong nhu tra ve cho payload loi thuc su - khong phan biet duoc 2 truong hop tu ben ngoai |
| 3 | `Encoding.UTF8.GetString(data)` duoc goi 2 lan cho cung 1 `ReadOnlySpan<byte> data` | KafkaExtensions.cs:27 va KafkaExtensions.cs:32 | Allocate string 2 lan khong can thiet cho moi message deserialize - anh huong hieu nang o throughput cao (khong do luong duoc muc do tu source code, chi la quan sat cau truc code) |
| 4 | `CollectionExtensions.CollectionName(this Type)` va `CollectionNameAttribute` nam trong file `KafkaExtensions.cs`, cung class-container voi Kafka serializer/deserializer, nhung khong lien quan gi den Kafka; grep toan repo khong tim thay bat ky call site nao dung `CollectionName()` hoac gan `[CollectionName(...)]` len bat ky Type nao (ke ca cac entity Mongo duoc tai lieu hoa o `Data-MongoDB-CoreMongoDB.md`) | KafkaExtensions.cs:37-49 | Co ve la dead code hoac code dat sai vi tri (co le du dinh dung cho Mongo collection naming nhung chua duoc wire vao `BaseEntityMongoDB` hay repository Mongo nao trong repo hien tai) - gay nham lan ve pham vi module Kafka Extensions |
| 5 | `InjectProducerTraceContext` va `ExtractConsumerTraceContext` khong tim thay call site nao trong toan bo repo (chi co dinh nghia trong KafkaTraceContextExtensions.cs, khong co producer/consumer service nao trong repo goi toi) | KafkaTraceContextExtensions.cs:13, KafkaTraceContextExtensions.cs:31 | Khong the xac nhan tu source code trong repo nay rang 2 ham nay dang duoc dung thuc te trong pipeline Kafka nao - co the chung duoc cac service khac (ngoai repo `sr-core-helper`) tieu thu nhu mot NuGet package, nhung dieu nay **khong xac dinh duoc tu source code** hien co |
| 6 | Hanh vi khong doi xung khi `headers == null` giua 2 ham cua `KafkaTraceContextExtensions`: `InjectProducerTraceContext` throw `ArgumentNullException`, con `ExtractConsumerTraceContext` tra ve `default` (khong throw) | KafkaTraceContextExtensions.cs:15 vs KafkaTraceContextExtensions.cs:33-36 | Caller can nam ro su khac biet nay de tranh nham lan khi xu ly `null` headers giua chieu producer va consumer |
| 7 | `[AttributeUsage(AttributeTargets.All)]` cho `CollectionNameAttribute` cho phep gan attribute len bat ky thanh phan nao (method, parameter, return value, v.v.), rong hon nhieu so voi muc dich "ten collection" thuong chi ap dung cho class/type | KafkaExtensions.cs:45 | Khong co rang buoc compile-time nao ngan viec gan sai cho; day la thiet ke rong bat thuong, khong the xac dinh y do thiet ke tu source code |
| 8 | Doi chieu voi 8 file Knowledge Base hien co (Utilizes-CallApiWithHttp.md, Utilizes-CallApi.md, Data-MongoDB-CoreMongoDB.md, Data-SQL-CoreSQL.md, Data-SQL-CoreSQL-TwoEntity.md, Data-SQL-UnitOfWork-DbContexts.md, Data-SQL-Dapper.md, Data-SQL-Resilience.md): khong file nao trong so nay nhac den `KafkaExtensions`, `KafkaTraceContextExtensions`, `CollectionNameAttribute`, hay `CollectionName()`. Khong phat hien mau thuan/sai lech nao can ghi doi chieu, vi cac kieu du lieu dung chung duoc liet ke trong huong dan (AuditModel, HttpOptionModel, ErrorModel, CustomException, ProjectToExtensions, PrecateBuilderExtensions, MeasureExecutionTimeExtensions.InvokeForHTTP, MongoResiliencePolicyFactory, BaseEntityMongoDB/BaseEntitySQL) **khong xuat hien** trong 2 file source cua module nay | (khong ap dung - ket qua doi chieu la "khong co diem giao") | Khong co hanh dong sua file KB cu can thuc hien tu module nay |
