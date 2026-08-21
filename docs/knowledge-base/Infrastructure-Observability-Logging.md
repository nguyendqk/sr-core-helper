# Infrastructure - Observability & Logging Pipeline (Serilog/Kafka/OTel/DI)

> Nguon: FTELSRCore.Shared/Infrastructure/Extensions/Helpers/LoggerConfigurationExtensions/ExcludingLogByFilter.cs; FTELSRCore.Shared/Infrastructure/Extensions/Helpers/LoggerConfigurationExtensions/SRKafkaSinks/SRKafkaSinkExtensions.cs; FTELSRCore.Shared/Infrastructure/Extensions/Helpers/LoggerConfigurationExtensions/SRKafkaSinks/TenantSRKafkaSinkExtensions.cs; FTELSRCore.Shared/Infrastructure/Extensions/Helpers/LoggerConfigurationExtensions/SRLoggerConfigurationExtensions.cs; FTELSRCore.Shared/Infrastructure/Extensions/Helpers/LoggerConfigurationExtensions/SRProducerConfigExtensions.cs; FTELSRCore.Shared/Infrastructure/Extensions/Helpers/SerilogProviderExtensions/Enrichers/SRLogEventEnricherExtensions.cs; FTELSRCore.Shared/Infrastructure/Extensions/Helpers/SerilogProviderExtensions/Formatters/SRKafkaLogFormatter.cs; FTELSRCore.Shared/Infrastructure/Extensions/Helpers/OpenTelemetryExtensions/OpenTelemetryExtensions.cs; FTELSRCore.Shared/Infrastructure/Extensions/Helpers/RegisterImplementationExtensions/RegisterImplementationExtensions.cs
> Loai: static class (ExcludingLoggerExtensions, SRLoggerConfigurationExtensions x2 partial, SRProducerConfigExtensions, OpenTelemetryExtensions x2 partial, RegisterImplementationExtensions) | sealed record implementing interface Serilog (SRKafkaSinkExtensions, TenantSRKafkaSinkExtensions) | class implementing interface Serilog (SRLogEventEnricherExtensions, SRKafkaLogFormatter) | record du lieu (TenantSRKafkaSinkModel) | class cau hinh don gian (TracingFTELSRModel, MetricFTELSRModel)
> Cap nhat theo commit: 89c1ce9

## 1. Tong quan

Module nay la tang ha tang **observability** dung chung cho toan he thong SR: cau hinh Serilog ghi log co cau truc, day log qua Kafka (mot topic co dinh hoac phan tuyen theo `LogEventLevel`), dinh dang message JSON gui Kafka (`SRKafkaLogFormatter`), bo sung (enrich) cac truong ngu canh HTTP/nguoi dung vao moi `LogEvent`, loc bo log health-check khoi pipeline, cau hinh OpenTelemetry tracing/metrics, va mot bo extension dung `Reflection` de tu dong dang ky cac lop implementation vao `IServiceCollection`. Day la tang ha tang (Infrastructure), khong chua logic nghiep vu SR, duoc goi tu tang khoi tao ung dung (Program.cs/Startup) va duoc cac tang goi HTTP/CQRS khac (da tai lieu hoa o cac file KB khac) tao du lieu de module nay dinh dang/gui di.

**Diem can lam ro ngay:** despite ten class va ten kieu du lieu co chua chu "Tenant" (`TenantSRKafkaSinkExtensions`, `TenantSRKafkaSinkModel`), **source code KHONG co bat ky truong/logic nao dinh danh tenant** (khong co `TenantId`, `TenantCode`, `TenantName`, khong doc claim/header tenant nao). Co che thuc su cua lop nay la **phan tuyen theo `LogEventLevel`** — moi cap do log (`Information`, `Warning`, `Error`...) duoc gan mot producer/topic Kafka rieng thong qua khoa `Dictionary<LogEventLevel, (Producer, TopicPartition)>` (`TenantSRKafkaSinkExtensions.cs:20,41,86,101`). Vi vay, **day KHONG phai la noi co tenant-awareness thuc su trong repo** — ten goi "Tenant" trong lop nay la nham lan/gay hieu lam, khong phan anh dung hanh vi code. Xem chi tiet o muc 3, van de #1.

### 1.1 Pham vi chuc nang

| Lam duoc | Khong lam duoc |
|---|---|
| Dinh nghia sink Kafka cho Serilog gui log theo 1 topic co dinh hoac topic tinh dong qua `topicDecider` (`SRKafkaSinkExtensions`) | Khong co retry/backoff cho Kafka producer khi loi — chi log console va tiep tuc (`SRKafkaSinkExtensions.cs:77-80`) |
| Dinh nghia sink Kafka phan tuyen theo `LogEventLevel` toi nhieu producer/topic khac nhau (`TenantSRKafkaSinkExtensions`) | Khong phan tuyen theo tenant/khach hang du ten lop goi la "Tenant" (xem muc 3, #1) |
| Nap cau hinh `ProducerConfig` cua Confluent.Kafka tu bien moi truong co tien to `SERILOG__KAFKA__` bang Reflection (`SRProducerConfigExtensions`) | Khong validate gia tri bien moi truong truoc khi ep kieu — loi parse duoc bat va **bo qua im lang** (chi log console), khong dung tien trinh (`SRProducerConfigExtensions.cs:24-33`) |
| Dinh dang `LogEvent` thanh JSON co cau truc co dinh de gui Kafka (`SRKafkaLogFormatter`) | Khong ho tro dinh dang khac ngoai JSON; khong co schema versioning |
| Bo sung `ServiceName`, `ClientIp`, `CorrelationId`, `UserInfo` vao moi `LogEvent` co HTTP context (`SRLogEventEnricherExtensions`) | Khong bo sung duoc thong tin permission chi tiet (code lien quan bi comment, xem muc 3) |
| Loc bo log cua duong dan healthcheck khoi mot pipeline log cu the (`ExcludingLoggerExtensions`) | Chi loai theo dieu kien `RequestPath` khop tuyet doi voi 1 `subDirectory` truyen vao — khong ho tro wildcard/regex |
| Dang ky OpenTelemetry tracing/metrics cho ASP.NET Core + HttpClient + cac ActivitySource/Meter noi bo cua repo (`OpenTelemetryExtensions`) | Khong cau hinh exporter (OTLP/Console/...) — chi cau hinh instrumentation/source, viec export nam ngoai pham vi 2 method nay |
| Tu dong quet Assembly va dang ky class theo hau to ten + kieu co so vao DI (`RegisterImplementationExtensions`) | Khong loc theo namespace; chi loc theo hau to chuoi ten class + kiem tra assignability, de nham voi class trung hau to nhung khac nghiep vu |

### 1.2 Dependency

| Thanh phan | Muc dich su dung |
|---|---|
| `Serilog` / `Serilog.Events` / `Serilog.Configuration` / `Serilog.Filters` | Nen tang logging: `LoggerConfiguration`, `LogEvent`, `LoggerSinkConfiguration`, `Matching.FromSource` |
| `Serilog.Sinks.PeriodicBatching` | `IBatchedLogEventSink`, `PeriodicBatchingSink`, `PeriodicBatchingSinkOptions` - co che gui log theo lo (batch) dinh ky |
| `Serilog.Formatting` / `Serilog.Formatting.Json` | `ITextFormatter`, `JsonFormatter` (formatter mac dinh khi khong truyen `formatter` tuy chinh) |
| `Confluent.Kafka` | `IProducer<,>`, `ProducerBuilder<,>`, `ProducerConfig`, `TopicPartition`, `SaslMechanism`, `SecurityProtocol`, `Partitioner`, `Acks`,... |
| `OpenTelemetry.Trace` / `OpenTelemetry.Metrics` / `OpenTelemetry.Resources` | `TracerProviderBuilder`, `MeterProviderBuilder`, cac extension `AddAspNetCoreInstrumentation`, `AddHttpClientInstrumentation`, `AddFusionCacheInstrumentation`, `ConfigureResource` |
| `Microsoft.Extensions.DependencyInjection` | `IServiceCollection`, `AddTransient/AddScoped/AddSingleton` (dang ky dong bang Reflection) |
| `Microsoft.AspNetCore.Http` | `IHttpContextAccessor`, `HttpContext` - lay ngu canh request de enrich log |
| `System.Diagnostics` | `Activity.Current`, `ActivityTraceId`, `ActivitySpanId` - lien ket voi tracing/OTel |
| `System.Text.Json` | `JsonSerializer`, `JsonSerializerOptions`, `JsonIgnoreCondition` - serialize JSON gui Kafka |
| `System.Reflection` | `PropertyInfo`, `Assembly.GetExecutingAssembly()`, `Assembly.GetExportedTypes()` - set property dong (Kafka config) va quet type dong (DI registration) |
| `FTELSRCore.Constants.CommonBaseConstant` | `ConfigLoggerExceptionByConsole`/`ConfigLoggerInformationByConsole` (log console fallback khi Kafka sink loi), `UserAgentCore`, `Anonymous`, `System` |
| `FTELSRCore.Constants.SerilogConstant` | Toan bo ten property chuan hoa dung trong `Enrich`/`Format`/filter (vi du `RequestPathPropertyName`, `ClientIpPropertyName`,...) |
| `FTELSRCore.Constants.OpenTelemetryConstant` | Ten cac `ActivitySource`/`Meter` noi bo cua repo (`CoreCacheActivitySource`, `LoggingBehaviorActivitySource`) |
| `FTELSRCore.Exceptions.CustomException` | Nem loi cau hinh khong hop le trong `TenantSRKafkaSinkExtensions`/`SRProducerConfigExtensions` (xem doi chieu muc 3, #8) |
| `FTELSRCore.Helpers.ConvertHelpers.GetClientIpAddress` | Lay IP client uu tien theo header `Forwarded`/`X-Forwarded-For` truoc khi enrich |
| `FTELSRCore.Constants.ClaimTypesConstant`, `HeaderConstant`, `DelimiterConstant`, `RoleData.RoleDataConstant`, `Enum.RoleSR` | Doc claim vai tro nguoi dung, header correlation id, ky tu phan tach, suy ra `RoleSR` de dua vao `UserInfo` |

### 1.3 Danh muc API

| API | Nhom | Mo ta ngan |
|---|---|---|
| `ExcludingLoggerExtensions.ExcludeRemoveLogHealthCheck` | Filter | Tra `true` neu `LogEvent` la log healthcheck can loai bo |
| `SRKafkaSinkExtensions(...)` (ctor) | Kafka sink | Tao Kafka producer + xac dinh topic co dinh hoac dong |
| `SRKafkaSinkExtensions.OnEmptyBatchAsync` | Kafka sink | No-op khi batch rong |
| `SRKafkaSinkExtensions.EmitBatchAsync` | Kafka sink | Format + gui tung `LogEvent` trong batch len Kafka |
| `TenantSRKafkaSinkExtensions(...)` (ctor) | Kafka sink (theo level) | Tao nhieu producer, moi producer gan voi 1 nhom `LogEventLevel` |
| `TenantSRKafkaSinkExtensions.OnEmptyBatchAsync` | Kafka sink (theo level) | No-op khi batch rong |
| `TenantSRKafkaSinkExtensions.EmitBatchAsync` | Kafka sink (theo level) | Gui tung `LogEvent` toi producer ung voi `Level` cua no |
| `TenantSRKafkaSinkModel` (record) | Model | Khai bao 1 nhom `LogEventLevel` + thong tin ket noi Kafka rieng |
| `SRLoggerConfigurationExtensions.SRKafkaSink` | DI/Config | Dang ky sink Kafka topic co dinh vao `LoggerConfiguration` |
| `SRLoggerConfigurationExtensions.TenantSRKafkaSinks` | DI/Config | Dang ky sink Kafka phan tuyen theo level vao `LoggerConfiguration` |
| `SRProducerConfigExtensions.LoadFromEnvironmentVariables` | Config | Doc bien moi truong `SERILOG__KAFKA__*` gan vao `ProducerConfig` |
| `SRProducerConfigExtensions.SetValue` | Config | Gan 1 property cua `ProducerConfig` theo ten (string) bang Reflection |
| `SRLogEventEnricherExtensions(...)` (2 ctor) | Enricher | Khoi tao enricher voi `IHttpContextAccessor` + ten service |
| `SRLogEventEnricherExtensions.Enrich` | Enricher | Gan `ServiceName`/`ClientIp`/`CorrelationId`/`UserInfo` vao `LogEvent` |
| `SRKafkaLogFormatter()` (ctor) | Formatter | Constructor rong, khong logic |
| `SRKafkaLogFormatter.Format` | Formatter | Chuyen `LogEvent` -> JSON co cau truc co dinh (36 truong) |
| `SRKafkaLogFormatter.GetLogEventPropertyValue` (internal) | Formatter (dung chung) | Lay gia tri string tho tu 1 property cua `LogEvent`, dung ca trong `ExcludingLoggerExtensions` |
| `SRKafkaLogFormatter.DirectionType` (enum public) | Formatter (dung chung) | `Outbound = 0`, `Inbound = 1` - duoc `FTELSRCore.Shared/Utilizes/CallApiWithHttp.cs` import truc tiep qua `using static` |
| `OpenTelemetryExtensions.AddFTELSRTracing` | OTel | Dang ky AspNetCore/HttpClient/FusionCache instrumentation + ActivitySource noi bo |
| `OpenTelemetryExtensions.AddFTELSRMetrics` | OTel | Dang ky AspNetCore/HttpClient instrumentation + Meter noi bo |
| `RegisterImplementationExtensions.AddTransientImplementationsOnly` | DI Reflection | Quet + dang ky Transient theo hau to ten, khong dang ky interface |
| `RegisterImplementationExtensions.AddScopedImplementationsOnly` | DI Reflection | Nhu tren, lifetime Scoped |
| `RegisterImplementationExtensions.AddSingletonImplementationsOnly` | DI Reflection | Nhu tren, lifetime Singleton |
| `RegisterImplementationExtensions.AddSingletonImplementationsWithInterface` | DI Reflection | Quet + dang ky Singleton, anh xa qua interface `I{ClassName}` |
| `RegisterImplementationExtensions.AddScopedImplementationsWithInterface` | DI Reflection | Nhu tren, lifetime Scoped |
| `RegisterImplementationExtensions.AddTransientImplementationsWithInterface` | DI Reflection | Nhu tren, lifetime Transient |

## 2. Chi tiet API

### 2.1 ExcludingLoggerExtensions.ExcludeRemoveLogHealthCheck

**Signature**
```csharp
public static bool ExcludeRemoveLogHealthCheck(this LogEvent e, string subDirectory)
```

**Muc dich** - Xac dinh xem mot `LogEvent` co phai la log cua request healthcheck (dinh danh boi mot duong dan con cu the) phat sinh tu `UserAgent = FTELSRCore` hay khong, de dung lam predicate loai bo (`Filter.ByExcluding`) trong cau hinh Serilog.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `e` | `LogEvent` (extension `this`) | Co | Khong check null truc tiep; `e.Properties` duoc check `IsNullOrEmpty()` | Khong co |
| `subDirectory` | `string` | Co | So sanh `Equals` (case-sensitive, khong trim) voi gia tri `RequestPath` cua log | Khong co |

**Output** - `bool`: `true` neu log nay can bi loai (healthcheck cua `FTELSRCore`); `false` cho moi truong hop khac (bao gom UserAgent khong khop, properties rong, hoac `RequestPath` khong khop `subDirectory`).

**Dieu kien xu ly** (theo thu tu thuc thi):
1. `Matching.FromSource(CommonBaseConstant.UserAgentCore)(e)` (`ExcludingLogByFilter.cs:11`) - neu log KHONG co `SourceContext` bat dau bang `"FTELSRCore"`, tra ve `false` ngay (khong loai).
2. Neu `e.Properties` rong/null (`IsNullOrEmpty()`), tra ve `false`.
3. Lay `value = SRKafkaLogFormatter.GetLogEventPropertyValue(e.Properties, SerilogConstant.RequestPathPropertyName)`.
4. Neu `value` khong rong VA `value.Equals(subDirectory)` -> tra ve `true`.
5. Moi truong hop con lai -> tra ve `false`.

**Side effect** - Khong co. Ham thuan doc, khong ghi/log/goi ngoai.

**Error handling** - Khong co try/catch. Khong nem exception ro rang trong logic hien co (cac ham duoc goi deu tra `null`/`false` an toan khi thieu du lieu).

**Khi nao NEN dung** - Khi can loai bo hang loat log healthcheck cua chinh SR (co `SourceContext` bat dau `FTELSRCore`) khoi mot sink cu the, truyen vao `Filter.ByExcluding(e => e.ExcludeRemoveLogHealthCheck(healthcheckPath))`.

**Khi nao KHONG dung** - Khi can loai theo nhieu duong dan/pattern (wildcard, regex) - ham chi so sanh tuyet doi 1 chuoi; khong dung cho log tu cac `SourceContext` khac `FTELSRCore`.

**Gioi han** - So sanh `Equals` mac dinh la case-sensitive va khong `Trim()`; neu `RequestPath` co khac hoa/thuong hoac khoang trang thua so voi `subDirectory` truyen vao, dieu kien loai se khong khop (khong duoc `StringComparison` chi dinh ro).

---

### 2.2 SRKafkaSinkExtensions (constructor)

**Signature**
```csharp
public SRKafkaSinkExtensions(string bootstrapServers,
                             SecurityProtocol securityProtocol,
                             SaslMechanism saslMechanism,
                             string saslUsername,
                             string saslPassword,
                             string sslCaLocation,
                             string topic = null,
                             Func<LogEvent, string> topicDecider = null,
                             ITextFormatter formatter = null)
```

**Muc dich** - Khoi tao mot Kafka producer song (`IProducer<string, byte[]>`) va xac dinh cach chon `TopicPartition` cho moi `LogEvent` se duoc gui (co dinh qua `topic`, hoac dong qua `topicDecider`).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `bootstrapServers` | `string` | Co (truyen qua) | Khong validate rong/null truc tiep o day | Khong co |
| `securityProtocol` | `SecurityProtocol` | Co | Khong validate | Khong co |
| `saslMechanism` | `SaslMechanism` | Co | Khong validate | Khong co |
| `saslUsername`, `saslPassword` | `string` | Co (co the rong) | Khong validate | Khong co |
| `sslCaLocation` | `string` | Khong | Neu rong -> khong set (giu nguyen mac dinh Confluent.Kafka) | Khong co (tham so bat buoc vi tri, khong co `= null`) |
| `topic` | `string` | Khong | Neu khac null -> tao `_globalTopicPartition` | `null` |
| `topicDecider` | `Func<LogEvent, string>` | Khong | Neu khac null -> gan `_topicDecider` | `null` |
| `formatter` | `ITextFormatter` | Khong | Neu null -> dung `JsonFormatter(null, renderMessage: true)` | `null` |

**Output** - Khong co (constructor). Sau khi khoi tao, instance da san sang nhan `EmitBatchAsync`.

**Dieu kien xu ly**:
1. Goi `ConfigureKafkaConnection(...)` de tao `_producer` (luon chay, khong dieu kien).
2. `_formatter = formatter ?? new JsonFormatter(null, renderMessage: true)`.
3. Neu `topic is not null` -> `_globalTopicPartition = new TopicPartition(topic, Partition.Any)`.
4. Neu `topicDecider is not null` -> `_topicDecider = topicDecider`.
5. **Neu ca `topic` VA `topicDecider` deu `null`**: khong nhanh nao o buoc 3/4 duoc thuc hien -> `_globalTopicPartition` khong duoc gan gia tri nao (giu gia tri mac dinh cua field, khong co gia tri thay the) va sau nay `EmitBatchAsync` van se dung no lam `topicPartition` khi `_topicDecider is null` (`SRKafkaSinkExtensions.cs:57-59`) - khong co guard rieng cho truong hop nay trong source code.

**Side effect** - Tao mot ket noi Kafka producer thuc su (`ProducerBuilder<string, byte[]>(config).Build()`) ngay tai thoi diem khoi tao constructor - **khong lazy**.

**Error handling** - Khong co try/catch trong constructor; loi ket noi Kafka (vi du sai `bootstrapServers`) se nem thang ra ngoai cho caller (thuong la code cau hinh `LoggerConfiguration` khi khoi dong app).

**Khi nao NEN dung** - Khi can 1 sink Kafka don gian, 1 topic co dinh hoac topic quyet dinh boi noi dung `LogEvent` (vi du theo `SourceContext`).

**Khi nao KHONG dung** - Khi can phan tuyen rieng theo `LogEventLevel` toi nhieu cum Kafka/topic khac nhau - dung `TenantSRKafkaSinkExtensions` (muc 2.5) thay vi lop nay.

**Gioi han** - Xem muc 3, van de #2 (thu tu ap dung bien moi truong bi de len boi gia tri tham so mac dinh) va van de #3 (thieu guard khi ca `topic`/`topicDecider` null). Neu `sslCaLocation` khac rong, gia tri thuc te gan vao `ProducerConfig.SslCaLocation` la `Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), sslCaLocation)` (`SRKafkaSinkExtensions.cs:101`) - `Assembly.GetExecutingAssembly()` tra ve assembly **chua chinh class nay** (`FTELSRCore.Shared`), khong phai assembly cua ung dung goi ham; duong dan tuong doi truyen vao se duoc ghep voi thu muc chua `FTELSRCore.Shared.dll`, khong phai thu muc lam viec/thu muc chay cua service (thuong giong nhau trong build/publish thong thuong, nhung co the khac neu deploy dang single-file hoac tach thu muc assembly).

---

### 2.3 SRKafkaSinkExtensions.EmitBatchAsync

**Signature**
```csharp
public Task EmitBatchAsync(IEnumerable<LogEvent> batch)
```

**Muc dich** - Dinh dang va gui toan bo cac `LogEvent` trong 1 batch (do `PeriodicBatchingSink` gom lai theo `BatchSizeLimit`/`Period`) len Kafka, roi flush producer.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `batch` | `IEnumerable<LogEvent>` | Co | Khong check null (foreach se throw `NullReferenceException` neu `null` duoc truyen, nhung trong thuc te `PeriodicBatchingSink` luon truyen collection hop le) | Khong co |

**Output** - `Task` da hoan tat (`Task.CompletedTask`) - phuong thuc khong bao gio tra ve `Task` loi (loi duoc nuot noi bo, xem Error handling).

**Dieu kien xu ly**:
1. Voi moi `item` trong `batch`: chon `topicPartition` = `_globalTopicPartition` neu `_topicDecider is null`, nguoc lai `new TopicPartition(_topicDecider(item), Partition.Any)`.
2. Format `item` qua `_formatter.Format(item, stringWriter)`, encode UTF-8 thanh `Message<string, byte[]>.Value` (`Key = null`).
3. `_producer!.Produce(topicPartition, message)` - **fire-and-forget**, khong cho callback delivery report (xem `EnableDeliveryReports = false` trong `ConfigureKafkaConnection`).
4. Sau khi (hoac du) loop hoan tat/loi, luon goi `_producer!.Flush(TimeSpan.FromSeconds(10))` (dong o ngoai `try/catch`).

**Side effect** - Ghi message len Kafka broker (I/O ngoai). Chan (block) thread goi toi da 10 giay o buoc `Flush` cuoi cung.

**Error handling** - Toan bo vong `foreach` nam trong 1 `try/catch (Exception exception)`; khi co loi (vi du producer mat ket noi), log ra console qua `CommonBaseConstant.ConfigLoggerExceptionByConsole` va **dung han vong lap ngay tai item gay loi** (cac item con lai trong batch **khong duoc gui**, vi `catch` nam ngoai `foreach`, khong phai trong tung iteration). Exception **khong bi nem lai**; `Flush` van duoc goi va `Task.CompletedTask` van duoc tra ve - **khong co dau hieu bao loi cho caller** (`PeriodicBatchingSink`).

**Khi nao NEN dung** - Duoc goi tu ha tang Serilog (`PeriodicBatchingSink`), khong danh de goi truc tiep tu code nghiep vu.

**Khi nao KHONG dung** - Khong dung khi can bao dam khong mat log (at-least-once): loi giua batch se lam mat cac message con lai cua batch do ma khong co canh bao/retry.

**Gioi han** - `Produce` khong cho phep quan sat ket qua gui (khong dang ky delivery report callback); mot loi o item dau batch se lam "mat tham lang" cac item con lai cung batch do catch nam ngoai vong for.

---

### 2.4 SRKafkaSinkExtensions.OnEmptyBatchAsync

**Signature**
```csharp
public Task OnEmptyBatchAsync()
```

**Muc dich** - Hook cua `IBatchedLogEventSink` duoc `PeriodicBatchingSink` goi khi den ky nhung khong co `LogEvent` nao trong hang doi.

**Input hop le** - Khong co tham so.

**Output** - `Task.CompletedTask` - luon thanh cong, khong lam gi.

**Dieu kien xu ly** - Khong co, tra ve ngay.

**Side effect** - Khong co.

**Error handling** - Khong ap dung (khong the loi).

**Khi nao NEN/KHONG dung** - Khong duoc goi truc tiep tu code ung dung; day la implementation bat buoc cua interface `IBatchedLogEventSink`.

**Gioi han** - Khong co.

---

### 2.5 TenantSRKafkaSinkExtensions (constructor + SetupConfiguration)

**Signature**
```csharp
public TenantSRKafkaSinkExtensions(
    List<TenantSRKafkaSinkModel> tenantSRKafkaSinkModels,
    ITextFormatter formatter = null)

private void SetupConfiguration(List<TenantSRKafkaSinkModel> tenantSRKafkaSinkModels)
```

**Muc dich** - Xay dung mot bang tra cuu `Dictionary<LogEventLevel, (IProducer<string, byte[]> Producer, TopicPartition TopicPartition)>`: moi cap do log duoc gan 1 producer + topic rieng, dua tren danh sach cau hinh `tenantSRKafkaSinkModels` truyen vao.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `tenantSRKafkaSinkModels` | `List<TenantSRKafkaSinkModel>` | Co | `throw new CustomException(...)` neu rong/null (`TenantSRKafkaSinkExtensions.cs:36-39`) | Khong co |
| `formatter` | `ITextFormatter` | Khong | Neu null -> `JsonFormatter(null, renderMessage: true)` | `null` |

**Output** - Khong co (constructor). Neu khong nem exception, `_keyValuePairs` da duoc dien day theo cac nhom `LogEventLevel` hop le.

**Dieu kien xu ly** (trong `SetupConfiguration`):
1. Neu `tenantSRKafkaSinkModels` rong/null -> `throw new CustomException("Tenant Kafka Sink Models cannot be null or empty.")`.
2. `GroupBy(x => x.LogEventLevels)` - **nhom theo chinh danh sach `LogEventLevels`**, **khong nhom theo tenant nao** (mo hinh `TenantSRKafkaSinkModel` khong co truong tenant - xem muc 3, #1). `List<LogEventLevel>` (kieu cua `LogEventLevels`) **khong override** `Equals`/`GetHashCode`, nen `EqualityComparer<List<LogEventLevel>>.Default` dung so sanh **tham chieu (reference equality)**, khong phai so sanh gia tri phan tu. Trong thuc te, moi `TenantSRKafkaSinkModel` thuong duoc tao voi 1 instance `List<LogEventLevel>` rieng (ke ca khi noi dung giong nhau ve gia tri) nen `GroupBy` hau nhu luon tra ve **1 nhom cho moi model dau vao** (khong gop 2 model co danh sach level "giong nhau ve gia tri" nhung khac instance vao 1 nhom) - buoc `GroupBy` nay tren thuc te gan nhu khong co tac dung gop nhom, tru khi caller chu dong truyen chung 1 instance `List<LogEventLevel>` cho nhieu model.
3. Voi moi nhom: lay `value = item.FirstOrDefault()`; neu `value.Topic` rong/trang hoac `item.Key` (danh sach level) rong -> `continue` (bo qua ca nhom, **khong log canh bao**).
4. Voi moi `logEventLevel` trong `item.Key`: neu `_keyValuePairs.ContainsKey(logEventLevel)` (level da duoc cau hinh boi 1 nhom truoc) -> log thong tin console "Duplicate configuration..." va `continue` (bo qua, **khong ghi de**).
5. Nguoc lai, tao 1 producer moi (`ProducerConfig(...)`, ham private static, cung logic voi `ConfigureKafkaConnection` cua muc 2.2) va them vao `_keyValuePairs[logEventLevel] = (Producer, TopicPartition)`.

**Side effect** - Tao **nhieu** ket noi Kafka producer thuc su, moi ket noi tuong ung 1 nhom cau hinh hop le (khong phai 1 ket noi cho toan bo danh sach).

**Error handling** - Chi 1 diem nem exception ro rang (`tenantSRKafkaSinkModels` rong/null). Cac truong hop cau hinh khong hop le khac (topic rong, level trung) duoc **xu ly mem** bang cach bo qua + log console, khong nem loi.

**Khi nao NEN dung** - Khi can gui cac cap do log khac nhau (vi du `Error` rieng, `Information` rieng) toi cac topic/cluster Kafka khac nhau.

**Khi nao KHONG dung** - Khi hieu lam day la co che multi-tenant (phan tach theo khach hang/tenant) - source code **khong** ho tro dieu nay (xem muc 3, #1). Cung khong dung khi can 1 `LogEventLevel` duoc gui toi nhieu topic dong thoi - moi level chi anh xa toi **dung 1** producer/topic (cau hinh sau se bi bo qua nhu buoc 4).

**Gioi han** - Neu 2 phan tu trong `tenantSRKafkaSinkModels` co `LogEventLevels` giao nhau mot phan (vi du `[Info, Warning]` va `[Warning, Error]`), hanh vi phu thuoc thu tu: nhom xu ly truoc chiem `Warning`, nhom sau bi bo qua toan bo `Warning` (khong bo qua rieng phan tu trung) va van dang ky duoc `Error` (vi kiem tra `ContainsKey` la per-level, o trong loop `foreach (LogEventLevel logEventLevel in item.Key)`).

---

### 2.6 TenantSRKafkaSinkExtensions.EmitBatchAsync

**Signature**
```csharp
public Task EmitBatchAsync(IEnumerable<LogEvent> batch)
```

**Muc dich** - Voi moi `LogEvent`, tra cuu producer/topic tuong ung `Level` cua no trong `_keyValuePairs` va gui message JSON toi Kafka.

**Input hop le** - Nhu muc 2.3 (`batch: IEnumerable<LogEvent>`, khong validate null).

**Output** - `Task.CompletedTask` (giong muc 2.3, khong bao gio phan anh loi qua `Task`).

**Dieu kien xu ly**:
1. Voi moi `item`: `logEventLevel = item.Level`; format thanh `Message<string, byte[]>` (giong muc 2.3).
2. `_keyValuePairs.TryGetValue(logEventLevel, out value)`; neu khong co cau hinh cho level nay -> log console "configuration for log event level {level} is empty" va **`continue`** (log bi **bo qua hoan toan**, khong gui di dau).
3. Nguoc lai, `Producer.Produce(TopicPartition, message)`.
4. Sau vong lap (hoac du loi), lap qua **tat ca** producer trong `_keyValuePairs.Values` va `Flush(TimeSpan.FromSeconds(10))` cho moi producer (khac muc 2.3 - o day co nhieu producer can flush).

**Side effect** - Ghi message Kafka cho cac level co cau hinh; **im lang bo qua** (khong gui, khong loi) cac level khong nam trong danh sach cau hinh ban dau.

**Error handling** - Giong muc 2.3: 1 `try/catch` boc toan bo `foreach` chinh; loi giua batch cat ngang cac item con lai. Rieng vong `Flush` cuoi (qua tung producer) **nam ngoai** `try/catch` - neu 1 producer `Flush` loi, exception se nem thang ra ngoai `EmitBatchAsync` (khac hanh vi muc 2.3 vi o day co vong `foreach` flush, khong phai 1 lenh flush don).

**Khi nao NEN dung** - Khi da chac chan can phan log theo cap do nghiem trong toi cac topic/cluster Kafka rieng.

**Khi nao KHONG dung** - Khi ky vong "khong mat log level chua cau hinh" - cac level ngoai danh sach se bi loai bo hoan toan, khong co fallback/topic mac dinh.

**Gioi han** - Xem Error handling: rui ro exception khong duoc bat trong vong `Flush` cuoi (`TenantSRKafkaSinkExtensions.cs:119-129`), khac voi hanh vi "luon nuot loi" cua `SRKafkaSinkExtensions.EmitBatchAsync`.

---

### 2.7 TenantSRKafkaSinkExtensions.OnEmptyBatchAsync

**Signature**
```csharp
public Task OnEmptyBatchAsync()
```

**Muc dich** - Giong muc 2.4, hook rong bat buoc cua `IBatchedLogEventSink`.

**Input hop le** - Khong co tham so. **Output** - `Task.CompletedTask`. **Dieu kien xu ly** - Khong co. **Side effect** - Khong co. **Error handling** - Khong ap dung. **Khi nao dung** - Framework tu goi, khong goi truc tiep. **Gioi han** - Khong co.

---

### 2.8 TenantSRKafkaSinkModel (record)

**Signature**
```csharp
public record TenantSRKafkaSinkModel(
    List<LogEventLevel> LogEventLevels,
    string Topic,
    string BootstrapServers,
    SaslMechanism SaslMechanism,
    SecurityProtocol SecurityProtocol,
    string SaslUsername,
    string SaslPassword,
    string SslCaLocation = null);
```

**Muc dich** - Mo hinh du lieu bat buoc de cau hinh 1 nhom "level -> Kafka connection" cho `TenantSRKafkaSinkExtensions`. Day la **positional record**, tat ca thuoc tinh la `init`-only qua constructor vi tri.

**Input hop le** - Xem bang tham so trong signature; **khong co truong tenant nao** (khong `TenantId`/`TenantCode`) du ten kieu la `TenantSRKafkaSinkModel`.

**Output** - Khong ap dung (kieu du lieu).

**Dieu kien xu ly / Side effect / Error handling** - Khong co (record du lieu thuan, khong logic).

**Khi nao NEN dung** - Khai bao danh sach cau hinh truyen vao `TenantSRKafkaSinks(...)` (muc 2.10).

**Khi nao KHONG dung** - Khong dung ten kieu nay de suy luan co tenant-scoping - xem muc 3, #1.

**Gioi han** - Ten goi gay hieu lam (xem muc 1 va muc 3, #1).

---

### 2.9 SRLoggerConfigurationExtensions.SRKafkaSink

**Signature**
```csharp
public static LoggerConfiguration SRKafkaSink(this LoggerSinkConfiguration loggerConfiguration,
                                              int period = 5,
                                              string topic = "logs",
                                              int batchSizeLimit = 50,
                                              int queueLimit = 10_000,
                                              string saslUsername = null,
                                              string saslPassword = null,
                                              string sslCaLocation = null,
                                              ITextFormatter formatter = null,
                                              string bootstrapServers = "localhost:9092",
                                              SaslMechanism saslMechanism = SaslMechanism.Plain,
                                              SecurityProtocol securityProtocol = SecurityProtocol.Plaintext)
```

**Muc dich** - Extension method cong khai de dang ky sink Kafka (topic co dinh) vao pipeline cau hinh Serilog (`.WriteTo.SRKafkaSink(...)`), boc `PeriodicBatchingSink` quanh `SRKafkaSinkExtensions`.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `period` | `int` | Khong | Khong validate (dung truc tiep `TimeSpan.FromSeconds`) | `5` |
| `topic` | `string` | Khong | Khong validate | `"logs"` |
| `batchSizeLimit` | `int` | Khong | Khong validate | `50` |
| `queueLimit` | `int` | Khong | Khong validate | `10_000` |
| `saslUsername`, `saslPassword`, `sslCaLocation` | `string` | Khong | Khong validate | `null` |
| `formatter` | `ITextFormatter` | Khong | Neu null, `SRKafkaSinkExtensions` tu dung `JsonFormatter` | `null` |
| `bootstrapServers` | `string` | Khong | Khong validate | `"localhost:9092"` |
| `saslMechanism` | `SaslMechanism` | Khong | Khong validate | `SaslMechanism.Plain` |
| `securityProtocol` | `SecurityProtocol` | Khong | Khong validate | `SecurityProtocol.Plaintext` |

**Output** - `LoggerConfiguration` (cho phep fluent chain tiep `.WriteTo...`) sau khi `Sink(logEventSink)` da duoc goi.

**Dieu kien xu ly** - Uy quyen thang cho `SRKafka` (private, cung file, cung tham so) -> tao `SRKafkaSinkExtensions` -> boc trong `PeriodicBatchingSink` voi `BatchSizeLimit`/`Period`/`QueueLimit` -> `loggerConfiguration.Sink(...)`. Khong co nhanh dieu kien nao khac trong method public nay.

**Side effect** - Tao Kafka producer ngay (qua constructor `SRKafkaSinkExtensions`, xem muc 2.2) - xay ra tai thoi diem **cau hinh** logger (thuong luc khoi dong app), khong phai lazy khi log dau tien duoc ghi.

**Error handling** - Khong co try/catch o tang nay; loi tao producer nem thang len code cau hinh `Log.Logger = new LoggerConfiguration()...CreateLogger()`.

**Khi nao NEN dung** - Cau hinh sink Kafka mac dinh (topic tinh) cho Serilog trong `Program.cs`/`appsettings` cua service SR.

**Khi nao KHONG dung** - Khi can phan tuyen theo `LogEventLevel` - dung `TenantSRKafkaSinks` (muc 2.10) thay vi method nay.

**Gioi han** - `queueLimit` (mac dinh 10.000) la co che **chan tang truong khong gioi han bo nho** khi Kafka broker gian doan keo dai (ghi chu tieng Viet trong code, `SRLoggerConfigurationExtensions.cs:71-72`) - neu hang doi day, `PeriodicBatchingSink` (thu vien ben ngoai) se quyet dinh hanh vi drop, **khong duoc kiem soat boi code trong module nay**.

---

### 2.10 SRLoggerConfigurationExtensions.TenantSRKafkaSinks

**Signature**
```csharp
public static LoggerConfiguration TenantSRKafkaSinks(
    this LoggerSinkConfiguration loggerConfiguration,
    List<TenantSRKafkaSinkModel> kafkaSinkModels,
    ITextFormatter formatter = null,
    int period = 5, int batchSizeLimit = 50, int queueLimit = 10_000)
```

**Muc dich** - Extension method cong khai de dang ky sink Kafka phan tuyen theo `LogEventLevel` (`TenantSRKafkaSinkExtensions`) vao `LoggerConfiguration`.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `kafkaSinkModels` | `List<TenantSRKafkaSinkModel>` | Co | Validate that su nam trong constructor `TenantSRKafkaSinkExtensions` (`throw CustomException` neu rong/null) - **khong** validate o tang method nay | Khong co |
| `formatter` | `ITextFormatter` | Khong | Nhu muc 2.9 | `null` |
| `period`, `batchSizeLimit`, `queueLimit` | `int` | Khong | Khong validate | `5` / `50` / `10_000` |

**Output** - `LoggerConfiguration` sau khi `Sink(...)` duoc goi.

**Dieu kien xu ly** - Uy quyen cho `TenantSRKafkas` (private) -> tao `TenantSRKafkaSinkExtensions(kafkaSinkModels, formatter)` (co the nem `CustomException` ngay tai day, xem muc 2.5) -> boc `PeriodicBatchingSink` -> `Sink(...)`.

**Side effect** - Tao **nhieu** Kafka producer (moi nhom level hop le 1 producer) ngay tai thoi diem cau hinh logger.

**Error handling** - Khong tu bat exception; `CustomException` tu constructor `TenantSRKafkaSinkExtensions` (khi `kafkaSinkModels` rong/null) se nem thang ra ngoai method nay.

**Khi nao NEN dung** - Khi ha tang can gui `Error`/`Warning`/`Information`... toi cac topic/cluster Kafka khac nhau theo cap do.

**Khi nao KHONG dung** - Khi ky vong day la cau hinh multi-tenant (xem muc 3, #1) - se khong dat duoc muc tieu do.

**Gioi han** - Nhu muc 2.9 ve `queueLimit`; ngoai ra neu `kafkaSinkModels` co it hon so `LogEventLevel` thuc te duoc log, cac level thieu cau hinh se bi **im lang bo qua** tai `EmitBatchAsync` (muc 2.6).

---

### 2.11 SRProducerConfigExtensions.LoadFromEnvironmentVariables

**Signature**
```csharp
public static ProducerConfig LoadFromEnvironmentVariables(this ProducerConfig config)
```

**Muc dich** - Doc toan bo bien moi truong cua process, loc cac bien co tien to `SERILOG__KAFKA__`, chuyen ten bien thanh ten property cua `ProducerConfig` (bo tien to, bo `_`, ve chu thuong) va gan gia tri qua Reflection.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `config` | `ProducerConfig` (extension `this`) | Co | Khong check null (NullReferenceException neu `config` la `null` khi goi `config.SetValue(...)` ben trong) | Khong co |

**Output** - `ProducerConfig` - chinh tham so `config` da duoc mutate tai cho (khong tao instance moi), tra ve de fluent-chain tiep.

**Dieu kien xu ly**:
1. Lay `Environment.GetEnvironmentVariables()` (toan bo bien cua process, khong loc theo prefix truoc).
2. Voi moi entry: neu `Key` bat dau bang `"SERILOG__KAFKA__"` -> tinh `key = text.Replace("SERILOG__KAFKA__", "").Replace("_", "").ToLower()` (vi du `SERILOG__KAFKA__BOOTSTRAP_SERVERS` -> `bootstrapservers`).
3. Goi `config.SetValue(key, value)` trong `try/catch` **rieng cho moi bien**.

**Side effect** - Doc toan bo bien moi truong he thong (khong chi cua Kafka) o moi lan goi - khong cache ket qua.

**Error handling** - Loi tu `SetValue` (property khong ton tai, kieu khong ho tro, parse loi) duoc bat **tung bien mot** va chi log console qua `ConfigLoggerExceptionByConsole`, **khong dung tien trinh, khong nem lai** - bien loi bi bo qua, cac bien khac van duoc xu ly tiep.

**Khi nao NEN dung** - Khi can override cau hinh Kafka producer bang bien moi truong o moi truong container/K8s ma khong sua code.

**Khi nao KHONG dung** - Khi can bao dam 100% bien cau hinh duoc ap dung dung - loi âm thầm (silent) neu ten bien sai hoac gia tri khong parse duoc, chi thay duoc qua log console.

**Gioi han** - Xem muc 3, van de #2: cac property duoc `SetValue` mot cach ro rang **sau** loi goi ham nay (trong `ConfigureKafkaConnection`/`ProducerConfig`) se de len (override) gia tri da nap tu bien moi truong.

---

### 2.12 SRProducerConfigExtensions.SetValue (+ SetValues private)

**Signature**
```csharp
public static ProducerConfig SetValue(this ProducerConfig config, string key, object value)

private static void SetValues(object obj, string propertyName, string stringValue)
```

**Muc dich** - Gan gia tri cho 1 property cua `ProducerConfig` (hoac bat ky object) bang ten property (chuoi, khong phan biet hoa/thuong) va gia tri dang `object`, tu dong ep kieu ve 1 trong 10 kieu duoc ho tro cung.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `config` | `ProducerConfig` | Co | Khong check null | Khong co |
| `key` | `string` | Co | So khop `PropertyInfo.Name` qua `Equals(..., StringComparison.CurrentCultureIgnoreCase)`; neu khong tim thay -> `throw CustomException` | Khong co |
| `value` | `object` | Khong | Chuyen ve string qua `value?.ToString() ?? string.Empty`; neu ket qua rong/trang -> **bo qua hoan toan, khong gan** (`SetValues` return som) | Khong co |

**Output** - `ProducerConfig` (chinh `config` da mutate, hoac khong doi gi neu `value` rong/trang).

**Dieu kien xu ly** (trong `SetValues`):
1. Neu `stringValue` rong/trang -> return ngay, khong lam gi (khong loi, khong log).
2. Tim `PropertyInfo` tren `obj` co `Name` khop `propertyName` (khong phan biet hoa/thuong, van hoa hien tai). Neu khong tim thay -> `throw new CustomException("A property (...) could not be found in Confluent.Kafka)")`.
3. Tra cuu kieu cua property trong 1 `Dictionary<Type, Action>` noi bo (chi ho tro: `string`, `int?`, `bool?`, `Partitioner?`, `CompressionType?`, `SecurityProtocol?`, `SaslMechanism?`, `BrokerAddressFamily?`, `Acks?`, `SslEndpointIdentificationAlgorithm?`). Neu kieu property khong nam trong danh sach nay -> `throw new CustomException("Kieu (...) chua duoc ho tro...")`.
4. Goi `Action` tuong ung de parse `stringValue` (`int.Parse`, `bool.Parse`, `System.Enum.Parse`,...) - **khong bat exception parse rieng tai day** (neu chuoi khong parse duoc, exception goc cua `int.Parse`/`Enum.Parse` se nem thang ra, **khong duoc boc thanh `CustomException`**).
5. Neu sau khi parse `objValue` van la `null` (ly thuyet khong xay ra voi cac delegate hien co, vi moi delegate deu gan gia tri) -> `throw new CustomException("... could not be assigned to ...")`.
6. `propertyInfo.SetValue(obj, objValue)`.

**Side effect** - Mutate truc tiep property cua `obj` (thuong la `ProducerConfig`) qua Reflection.

**Error handling** - 3 diem nem `CustomException` ro rang (khong tim thay property, kieu khong ho tro, gia tri null sau parse); rieng loi parse gia tri (`FormatException` cua `int.Parse`/`bool.Parse`/`Enum.Parse` khi chuoi khong hop le) **khong duoc bat** boi `SetValues` - no se nem thang ra ngoai. Trong `LoadFromEnvironmentVariables` (muc 2.11), loi nay duoc `catch (Exception)` boc lai va chi log console; nhung trong cac noi goi truc tiep `.SetValue(...)` (ConfigureKafkaConnection/ProducerConfig, khong co try/catch quanh chuoi `.SetValue`) thi loi se nem thang len caller.

**Khi nao NEN dung** - Gan 1 gia tri don le, khi ten property va kieu du lieu da biet truoc va thuoc danh sach 10 kieu ho tro.

**Khi nao KHONG dung** - Khi property co kieu khac 10 kieu liet ke (vi du `TimeSpan?`, kieu tuy chinh khac cua `ProducerConfig`) - se luon nem `CustomException` "chua duoc ho tro".

**Gioi han** - Danh sach kieu ho tro la **hardcode** (10 kieu) va gan chat voi cac property hien co cua `Confluent.Kafka.ProducerConfig`; them property kieu moi vao `ProducerConfig` (qua ban cap nhat thu vien) se lam ham nay nem loi "chua duoc ho tro" ma khong co canh bao truoc luc build.

---

### 2.13 SRLogEventEnricherExtensions (constructors + Enrich)

**Signature**
```csharp
public class SRLogEventEnricherExtensions(IHttpContextAccessor contextAccessors, string serviceName) : ILogEventEnricher
{
    public SRLogEventEnricherExtensions()
        : this(new HttpContextAccessor(), CommonBaseConstant.System)
    { }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
}
```

**Muc dich** - Bo sung (khong ghi de neu da co) cac property `ServiceName`, `ClientIp`, `CorrelationId`, `UserInfo` vao moi `LogEvent` di qua pipeline Serilog, dua tren `HttpContext` hien tai (neu co).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `contextAccessors` | `IHttpContextAccessor` | Co (ctor chinh) hoac tu tao (ctor rong) | Khong check null | Ctor rong: `new HttpContextAccessor()` |
| `serviceName` | `string` | Co (ctor chinh) hoac co dinh | Khong validate | Ctor rong: `CommonBaseConstant.System` (`"FTEL-SERVICEREQUEST-API"`) |
| `logEvent`, `propertyFactory` (cua `Enrich`) | `LogEvent`, `ILogEventPropertyFactory` | Co (do Serilog goi) | Khong check null | Khong co |

**Output** - `void` (`Enrich` mutate truc tiep `logEvent` qua `AddPropertyIfAbsent`).

**Dieu kien xu ly** (trong `Enrich`, theo thu tu):
1. Luon gan `ServiceName = serviceName` (neu chua co property nay tren `logEvent`).
2. Neu `context (HttpContext) is null` -> gan `UserInfo = CommonBaseConstant.Anonymous` va **return ngay** (khong xu ly gi them - khong co `ClientIp`/`CorrelationId` trong truong hop nay).
3. Neu co `context`: tinh `ipAddress` = `ConvertHelpers.GetClientIpAddress(context)`, neu rong thi fallback `context.Connection.RemoteIpAddress?.ToString()`. Gan `ClientIp`.
4. Tinh `correlationId`: **chi khi header `X-Correlation-Id` (`HeaderConstant.CorrelationIdHeaderKey`) KHONG co trong request** (`TryGetValue` tra `false`) moi fallback sang `Activity.Current?.TraceId`, roi `Guid.NewGuid().ToString("N")` neu van rong. **Neu header co ton tai nhung gia tri la chuoi rong/whitespace, code KHONG fallback** - `correlationId` se nhan nguyen gia tri rong do (`correlationIds.FirstOrDefault()`), khac voi mo ta "neu khong co/rong deu fallback" o ban nay truoc do (`SRLogEventEnricherExtensions.cs:56-69`). Gan `CorrelationId`.
5. Neu `context.User?.Identity` la `null` hoac chua `IsAuthenticated` -> gan `UserInfo = Anonymous` va return.
6. Nguoc lai: `roleDataName` duoc khoi tao truoc bang gia tri mac dinh `RoleSR.ONLY_CREATE` (`SRLogEventEnricherExtensions.cs:97`) - **khong phai** gia tri rong/Anonymous; doc claim `ClaimTypesConstant.SRRoles` (`"SR.SRRoles"`) -> neu co danh sach role, goi `RoleDataConstant.GetRoleData(roles)` (ham ngoai pham vi 9 file cua module nay - khong tai lieu hoa chi tiet o day) de **ghi de** `RoleSR` mac dinh do; neu KHONG co claim role nao, `roleDataName` giu nguyen `RoleSR.ONLY_CREATE` va xuat hien trong `UserInfo` (`RoleData: ONLY_CREATE`) du nguoi dung khong co role nao duoc gan - xem Gioi han. Doc `ClaimTypes.Name` lam `username`; ghep chuoi `UserInfo = "[User: {username} - Roles: {roleName} - RoleData: {roleDataName} - IP: {ipAddress}]"` va gan.

**Side effect** - Doc `HttpContext`/claims hien tai (khong ghi ngoai). Tao `Guid` moi neu thieu correlation id (co the sinh nhieu ID khac nhau cho cung 1 request neu enricher duoc goi nhieu lan ma khong co header/Activity - xem muc 3).

**Error handling** - Khong co try/catch trong `Enrich`; loi truy cap `context.User`/claims (hiem, thuong khong nem) se nem thang ra pipeline Serilog.

**Khi nao NEN dung** - Dang ky qua `.Enrich.With<SRLogEventEnricherExtensions>()` hoac DI (`IHttpContextAccessor` tu container) trong cau hinh Serilog cua web app.

**Khi nao KHONG dung** - Trong ngu canh khong co HTTP pipeline (worker/console) - `contextAccessors.HttpContext` luon `null`, chi co `ServiceName` + `UserInfo = Anonymous` duoc enrich, `ClientIp`/`CorrelationId` se khong xuat hien.

**Gioi han** - `AddPropertyIfAbsentInSerilog` dung `AddPropertyIfAbsent` (khong `AddOrUpdateProperty`) - neu `LogEvent` da co san property cung ten (vi du do `LogContext.PushProperty` hoac message template dat truoc), gia tri cua enricher **se khong duoc ap dung** (giu gia tri co san). Phan doc claim `SRPermissions` bi **comment het** trong code (`SRLogEventEnricherExtensions.cs:88-95`) - permissions **khong** duoc dua vao `UserInfo` du bien `permissionsName` van con ket qua comment, chi co `RoleData`/`Roles` duoc dua vao. Fallback `correlationId` (`Activity.Current?.TraceId` roi `Guid.NewGuid()`) **chi kich hoat khi header `X-Correlation-Id` hoan toan khong co trong request** - neu header co mat nhung gia tri rong/whitespace, `correlationId` van nhan gia tri rong do, khong fallback (`SRLogEventEnricherExtensions.cs:56-69`). `roleDataName` mac dinh la `RoleSR.ONLY_CREATE` (khong phai rong/Anonymous) khi nguoi dung da authenticate nhung khong co claim `SR.SRRoles` nao - `UserInfo` sinh ra van chua `RoleData: ONLY_CREATE` trong truong hop nay, co the gay hieu lam la nguoi dung "chi duoc tao" trong khi thuc te chi la khong co claim role (`SRLogEventEnricherExtensions.cs:97-106`).

---

### 2.14 SRKafkaLogFormatter.Format

**Signature**
```csharp
public void Format(LogEvent logEvent, TextWriter output)
```

**Muc dich** - Chuyen 1 `LogEvent` cua Serilog thanh 1 dong JSON co cau truc co dinh (36 truong) de gui qua Kafka sink, doc toan bo gia tri tu `logEvent.Properties` bang ten property chuan hoa trong `SerilogConstant`.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `logEvent` | `LogEvent` | Co | Khong check null (se `NullReferenceException` neu `null`) | Khong co |
| `output` | `TextWriter` | Co | Khong check null | Khong co |

**Output** - Khong co gia tri tra ve (`void`); ket qua duoc `output.Write(...)` ghi truc tiep chuoi JSON da serialize.

**Dieu kien xu ly** - Khong co nhanh re (if/switch) trong `Format` chinh; toan bo la gan gia tri tuyen tinh cho 36 truong cua `LogTemplateModel` roi serialize 1 lan. Truong hop dac biet duy nhat: `ResponseTimeMs = long.TryParse(GetLogEventPropertyValue(..., ResponseTimeMsPropertyName), out var latency) ? latency : null` - neu property khong ton tai hoac khong parse duoc thanh `long` -> `null`.

**Thu tu cac truong trong JSON dau ra** (theo thu tu khai bao cua `LogTemplateModel`, la thu tu System.Text.Json xuat ra khi khong co `[JsonPropertyOrder]`):

`Level, Message, LocalTimeStamp, ActionId, ActionName, ClassName, ClientIp, CorrelationId, Environment, EventId, Pod, MethodName, Parameters, RequestId, RequestName, RequestPath, ServiceName, SourceContext, UserInfo, TraceId, SpanId, User, IPAddress, UserAgent, DynamicRule, ErrorCategory, Endpoint, HttpMethod, ResponseTimeMs, HttpStatusCode, SystemOwner, Direction, LatencyRating, Topic, ErrorMessage, StackTrace`

(`SRKafkaLogFormatter.cs:106-298`).

**Side effect** - Khong co (thuan format, khong I/O ngoai ngoai `output.Write`).

**Error handling** - Khong co try/catch trong `Format`; loi serialize (hiem, vi cac truong deu la `string`/`long?`/`ActivityTraceId?`) se nem thang ra `EmitBatchAsync` cua sink dang goi no (roi bi bat/log console o do, xem muc 2.3/2.6).

**Khi nao NEN dung** - Truyen lam `formatter` tuy chinh cho `SRKafkaSink`/`TenantSRKafkaSinks` khi can dinh dang JSON co cau truc rieng (thay cho `JsonFormatter` mac dinh cua Serilog).

**Khi nao KHONG dung** - Khi can giu nguyen toan bo property tuy y cua `LogEvent` - formatter nay **chi xuat 36 truong co dinh**, moi property khac tren `LogEvent` khong nam trong danh sach nay se **bi bo qua hoan toan**, khong xuat hien trong JSON.

**Gioi han**:
- `JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault` (`SRKafkaLogFormatter.cs:11-14`) khien moi truong `string` co gia tri `null` (mac dinh cua `string`) **bi loai khoi JSON dau ra** (khong xuat hien key), tru `ResponseTimeMs` duoc ghi de bang `[JsonIgnore(Condition = JsonIgnoreCondition.Never)]` (`SRKafkaLogFormatter.cs:255`) nen **luon** xuat hien (ke ca khi `null`).
- Truong `IPAddress` doc tu Serilog property ten `"Forwarded"` (`SerilogConstant.ForwardedPropertyName`), **khong phai** tu property `"ClientIp"` (do la truong `ClientIp` rieng, doc tu enricher muc 2.13). Trong pham vi 9 file cua module nay, **khong co file nao gan property `"Forwarded"`** cho `LogEvent` - property nay duoc gan boi `FTELSRCore.Shared/Infrastructure/MiddleWares/SerilogHandlerMiddleWare.cs:75` (**nam ngoai pham vi duoc giao cua tai lieu nay**). Neu middleware do khong duoc dang ky trong pipeline cua mot service cu the, truong `IPAddress` trong JSON gui Kafka se luon rong/vang mat, doc lap voi `ClientIp` (van duoc dien binh thuong boi `SRLogEventEnricherExtensions`).
- Truong `Direction` (mac dinh CLR `= nameof(DirectionType.Inbound)` trong khai bao property) **duoc ghi de** boi `GetLogEventPropertyValue(..., DirectionPropertyName)` trong `Format` (`SRKafkaLogFormatter.cs:86`) - nghia la gia tri mac dinh "Inbound" chi ap dung cho instance moi tao truoc khi object initializer chay; sau khi `Format` gan gia tri thuc te (hoac `null` neu property khong ton tai tren `LogEvent`), gia tri cuoi cung phan anh dung du lieu tu `LogEvent`, khong con la default "Inbound" co dinh.

---

### 2.15 SRKafkaLogFormatter.GetLogEventPropertyValue (internal)

**Signature**
```csharp
internal static string GetLogEventPropertyValue(IReadOnlyDictionary<string, LogEventPropertyValue> value, string type)
```

**Muc dich** - Ham tien ich noi bo, doc gia tri "tho" (dang `string`) cua 1 property tu `LogEvent.Properties`, uu tien unwrap `ScalarValue` truoc khi fallback ve `ToString()` chung. Duoc dung lai boi `SRKafkaLogFormatter.Format` (muc 2.14) **va** boi `ExcludingLoggerExtensions.ExcludeRemoveLogHealthCheck` (muc 2.1) - day la diem dung chung duy nhat giua 2 file khac nhau trong module.

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `value` | `IReadOnlyDictionary<string, LogEventPropertyValue>` | Co | Neu `null` -> tra `null` ngay | Khong co |
| `type` | `string` | Co | Neu rong/trang -> tra `null` ngay | Khong co |

**Output** - `string`: gia tri cua property (uu tien `ScalarValue.Value.ToString()`, fallback `LogEventPropertyValue.ToString()`), hoac `null` neu: dictionary null/rong, `type` rong, khong tim thay key, hoac gia tri render ra dung chuoi `"null"`.

**Dieu kien xu ly**:
1. `value is null || string.IsNullOrWhiteSpace(type)` -> `null`.
2. `value.TryGetValue(type, out sourceContextValue) is false` **HOAC** `sourceContextValue?.ToString() == "null"` -> `null`.
3. Neu la `ScalarValue { Value: not null }` va `scalarValue.Value.ToString() != "null"` -> tra `scalarValue.Value.ToString()`.
4. Nguoc lai -> tra `sourceContextValue?.ToString()`.

**Side effect** - Khong co.

**Error handling** - Khong co try/catch; khong nem exception trong logic hien co.

**Khi nao NEN dung** - Doc 1 property duoi dang chuoi tho tu `LogEvent.Properties` khi khong can biet kieu du lieu goc.

**Khi nao KHONG dung** - Khi can phan biet "property khong ton tai" voi "property co gia tri chuoi dung la `null`" - ca hai deu tra ve `null` tu ham nay (xem Gioi han).

**Gioi han** - So sanh `sourceContextValue?.ToString() == "null"` (buoc 2) la so sanh **chuoi van ban**, khong phai kiem tra `is null`. Neu mot property thuc su co gia tri la chuoi 4 ky tu `"null"` (vi du du lieu nghiep vu nhap chuoi "null"), ham nay se coi nhu property khong ton tai va tra ve `null` — mat thong tin, khong the phan biet 2 truong hop.

---

### 2.16 OpenTelemetryExtensions.AddFTELSRTracing

**Signature**
```csharp
public static TracerProviderBuilder AddFTELSRTracing(
    this TracerProviderBuilder builder, TracingFTELSRModel model)
```

**Muc dich** - Dang ky bo instrumentation tracing tieu chuan cua service SR: FusionCache, 2 `ActivitySource` noi bo cua repo, `ActivitySource` cua chinh service, AspNetCore instrumentation (co ghi nhan exception) va HttpClient instrumentation (co ghi nhan exception + tuy chinh `DisplayName` cua Activity).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `builder` | `TracerProviderBuilder` (extension `this`) | Co | Khong check null | Khong co |
| `model` | `TracingFTELSRModel` | Co | Khong check null (`model.ServiceName` se `NullReferenceException` neu `model` la `null`) | Khong co |
| `model.ServiceName` | `string` | Co (thuoc tinh cua model) | Khong validate rong/null | Khong co (property `{ get; set; }` khong gia tri mac dinh) |

**Output** - `TracerProviderBuilder` (chinh `builder` sau khi fluent-chain toan bo cau hinh) - de tiep tuc `.Build()` o noi goi.

**Dieu kien xu ly** - Khong co nhanh re; luon thuc hien tuan tu: `AddFusionCacheInstrumentation()` -> `AddSource(model.ServiceName)` -> `AddSource(CoreCacheActivitySource)` -> `AddSource(LoggingBehaviorActivitySource)` -> `ConfigureResource(r => r.AddService(model.ServiceName))` -> `AddAspNetCoreInstrumentation(o => o.RecordException = true)` -> `AddHttpClientInstrumentation(o => { RecordException = true; FilterHttpRequestMessage = _ => true; EnrichWithHttpRequestMessage = (activity, request) => activity.DisplayName = $"{request.Method} {request.RequestUri?.AbsolutePath}"; })`.

**Side effect** - Cau hinh `TracerProviderBuilder` (mutate builder, khong tao ket noi ngoai tai thoi diem goi - viec export xay ra khi `Build()`/co exporter duoc cau hinh o noi khac, ngoai pham vi ham nay).

**Error handling** - Khong co try/catch; khong ap dung (cau hinh builder, khong I/O truc tiep).

**Khi nao NEN dung** - Goi 1 lan trong `Program.cs` khi cau hinh `.WithTracing(b => b.AddFTELSRTracing(model))`.

**Khi nao KHONG dung** - Khong dung lai nhieu lan tren cung 1 builder voi model khac nhau trong cung 1 provider - se dang ky trung `AddSource`/`ConfigureResource` nhieu lan.

**Gioi han** - `FilterHttpRequestMessage = _ => true` (`OpenTelemetryExtensions.cs:28`) nghia la **khong loc bat ky HttpClient request nao** - moi request HttpClient deu duoc trace, khong co co che loai tru (vi du goi noi bo/healthcheck) o tang nay. `AddFusionCacheInstrumentation()` phu thuoc thu vien FusionCache duoc cau hinh dung noi khac trong repo (ngoai pham vi 9 file); source code cua module nay khong the xac nhan FusionCache co duoc dang ky DI hay khong.

---

### 2.17 OpenTelemetryExtensions.AddFTELSRMetrics

**Signature**
```csharp
public static MeterProviderBuilder AddFTELSRMetrics(
    this MeterProviderBuilder builder, MetricFTELSRModel model)
```

**Muc dich** - Dang ky instrumentation metrics tuong tu muc 2.16 nhung cho `MeterProviderBuilder`: AspNetCore, HttpClient, Meter cua chinh service va 2 Meter noi bo cua repo.

**Input hop le** - Tuong tu muc 2.16 (`builder`, `model.ServiceName`), khong co validate null.

**Output** - `MeterProviderBuilder` sau khi cau hinh.

**Dieu kien xu ly** - Tuan tu, khong nhanh re: `AddAspNetCoreInstrumentation()` -> `AddHttpClientInstrumentation()` -> `AddMeter(model.ServiceName)` -> `AddMeter(CoreCacheActivitySource)` -> `AddMeter(LoggingBehaviorActivitySource)` -> `ConfigureResource(r => r.AddService(model.ServiceName))`.

**Side effect** - Cau hinh builder (khong I/O ngoai truc tiep).

**Error handling** - Khong ap dung.

**Khi nao NEN dung** - Cau hinh `.WithMetrics(b => b.AddFTELSRMetrics(model))` cung luc voi `AddFTELSRTracing`.

**Khi nao KHONG dung** - Khong ap dung rieng ma thieu `AddFTELSRTracing` neu can ca hai loai du lieu quan sat (metrics + tracing dung 2 pipeline OTel doc lap, phai cau hinh ca hai).

**Gioi han** - Su dung `AddMeter`/`AddSource` voi **cung ten hang so** `OpenTelemetryConstant.CoreCacheActivitySource`/`LoggingBehaviorActivitySource` cho ca tracing va metrics (`OpenTelemetryExtensions.cs:15-16,53-54`) - dung 1 ten `ActivitySource`/`Meter` cho ca 2 muc dich; source code khong xac nhan duoc lieu ma nguon phat metrics thuc su co ton tai duoi ten nay (ngoai pham vi 9 file cua module nay).

---

### 2.18 RegisterImplementationExtensions - nhom `*ImplementationsOnly`

**Signature**
```csharp
public static IServiceCollection AddTransientImplementationsOnly(this IServiceCollection services, string suffixFilter, Type typeofImplementation)
public static IServiceCollection AddScopedImplementationsOnly(this IServiceCollection services, string suffixFilter, Type typeofImplementation)
public static IServiceCollection AddSingletonImplementationsOnly(this IServiceCollection services, string suffixFilter, Type typeofImplementation)
```

**Muc dich** - Quet toan bo type cong khai (exported) trong **assembly chua `typeofImplementation`** (khong phai assembly goi ham), loc ra cac class (khong abstract) co ten ket thuc bang `suffixFilter` VA co the gan (`IsAssignableFrom`) tu `typeofImplementation`, roi dang ky **chinh class do** (khong qua interface) vao DI voi lifetime tuong ung (Transient/Scoped/Singleton).

**Input hop le**

| Tham so | Kieu | Bat buoc | Rang buoc/Validate trong code | Gia tri mac dinh |
|---|---|---|---|---|
| `services` | `IServiceCollection` | Co | Khong check null | Khong co |
| `suffixFilter` | `string` | Co | So sanh `t.Name.EndsWith(suffixFilter)` - phan biet hoa/thuong theo van hoa hien tai (khong chi dinh `StringComparison`) | Khong co |
| `typeofImplementation` | `Type` | Co | Dung ca de lay `Assembly` quet (`typeofImplementation.Assembly`) VA de kiem tra `IsAssignableFrom` - neu `null` se `NullReferenceException` | Khong co |

**Output** - `IServiceCollection` (chinh `services` sau khi dang ky, cho fluent chain). Neu khong co type nao khop dieu kien, tra ve `services` khong thay doi (khong loi, khong canh bao).

**Dieu kien xu ly**:
1. `typeofImplementation.Assembly.GetExportedTypes()` - **chi lay type public cua ca assembly** chua `typeofImplementation`, khong phai assembly goi.
2. `Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith(suffixFilter))`.
3. `Where(t => typeofImplementation.IsAssignableFrom(t))` (thuc hien qua LINQ query syntax `where managers.IsAssignableFrom(type.Implementation)`).
4. Voi tung type con lai: `services.AddTransient(t)` / `AddScoped(t)` / `AddSingleton(t)` (dang ky **khong co interface**, service type = implementation type).

**Side effect** - Mutate `services` (them ServiceDescriptor cho moi type khop). Dùng Reflection quét toàn bộ exported type của 1 assembly - có thể chậm nếu assembly lớn (khong do luong trong source code).

**Error handling** - Khong co try/catch; `GetExportedTypes()` co the nem `ReflectionTypeLoadException` (hanh vi chuan cua .NET khi assembly co dependency thieu) - **khong duoc bat rieng**, se nem thang ra caller.

**Khi nao NEN dung** - Khi co quy uoc dat ten nhat quan (hau to co dinh, vi du `...Manager`, `...Service`) va tat ca implementation nam trong **cung 1 assembly** voi kieu co so `typeofImplementation`.

**Khi nao KHONG dung** - Khi implementation nam rai o nhieu assembly khac nhau - ham chi quet **1 assembly duy nhat** (cua `typeofImplementation`); khong dung khi can dang ky qua interface (dung nhom `*ImplementationsWithInterface`, muc 2.19).

**Gioi han** - `suffixFilter` chi so khop hau to ten class dang chuoi - de nham voi class khac nghiep vu nhung trung hau to; khong loc theo namespace nen co the vo tinh dang ky nham class trong cung assembly nhung khac module logic.

---

### 2.19 RegisterImplementationExtensions - nhom `*ImplementationsWithInterface`

**Signature**
```csharp
public static IServiceCollection AddSingletonImplementationsWithInterface(this IServiceCollection services, string suffixFilter, Type typeofImplementation)
public static IServiceCollection AddScopedImplementationsWithInterface(this IServiceCollection services, string suffixFilter, Type typeofImplementation)
public static IServiceCollection AddTransientImplementationsWithInterface(this IServiceCollection services, string suffixFilter, Type typeofImplementation)
```

**Muc dich** - Giong nhom muc 2.18 nhung dang ky theo cap `(Service: I{ClassName}, Implementation: class)` - tim interface co ten dung format `I` + ten class, va dang ky no lam service type trong DI thay vi dang ky truc tiep class.

**Input hop le** - Giong muc 2.18 ve `services`/`suffixFilter`/`typeofImplementation`, nhung **y nghia cua `typeofImplementation` khac**: dieu kien loc o day la `typeofImplementation.IsAssignableFrom(t.GetInterface($"I{t.Name}"))` - tuc `typeofImplementation` phai tuong hop voi **INTERFACE** `I{ClassName}` duoc tim thay, khong phai voi class implementation nhu muc 2.18.

**Output** - `IServiceCollection` sau khi dang ky (co the khong thay doi gi neu khong co type nao khop).

**Dieu kien xu ly**:
1. `GetExportedTypes()` cua assembly chua `typeofImplementation` (giong muc 2.18).
2. `Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith(suffixFilter))`.
3. Voi moi type con lai, tinh `Service = t.GetInterface($"I{t.Name}")` (tim interface **dung ten** `"I" + tenClass`, vi du class `FooManager` -> tim interface `IFooManager`).
4. `Where(t => t.Service != null)` - loai bo class khong co interface dung dinh dang nay.
5. `Where(t => typeofImplementation.IsAssignableFrom(t.Service))` - kiem tra assignability tren **interface** `t.Service`, khong phai tren class `t.Implementation`.
6. Dang ky `services.AddSingleton(t.Service, t.Implementation)` / `AddScoped(...)` / `AddTransient(...)`.

**Side effect** - Giong muc 2.18 (mutate `services`).

**Error handling** - Giong muc 2.18 (khong try/catch, `GetExportedTypes()` co the nem `ReflectionTypeLoadException`).

**Khi nao NEN dung** - Khi implementation tuan thu chat quy uoc `I{ClassName}` (vi du `OrderManager` : `IOrderManager`) va can DI resolve qua interface.

**Khi nao KHONG dung** - Khi interface khong dung dung format `I` + ten class hoan chinh (vi du interface ten khac, hoac generic) - `GetInterface($"I{t.Name}")` se khong tim thay va class do bi loai am tham (khong loi, khong canh bao).

**Gioi han** - Vi dieu kien loc kiem tra `typeofImplementation.IsAssignableFrom(t.Service)` tren **interface**, tham so `typeofImplementation` truyen vao cac ham nay **phai la mot type ma cac interface `I{ClassName}` co the gan duoc toi** (thuong ban than cung phai la 1 interface "goc" ma cac interface cu the ke thua, hoac `typeof(object)`/tuong tu) - khac han quy uoc cua nhom `*ImplementationsOnly` (muc 2.18, kiem tra tren class). Truyen nham mot `Type` la class cu the (khong phai interface) vao day rat de dan den ket qua **0 type nao duoc dang ky ma khong co bat ky exception/canh bao nao** - loi cau hinh sai se im lang, chi phat hien duoc khi runtime thieu service can resolve.

## 3. Van de da biet

| # | Van de | Vi tri | Anh huong |
|---|---|---|---|
| 1 | `TenantSRKafkaSinkExtensions`/`TenantSRKafkaSinkModel` khong co bat ky truong dinh danh tenant nao (`TenantId`/`TenantCode`/...); co che thuc su la **phan tuyen theo `LogEventLevel`** (`Dictionary<LogEventLevel, (Producer, TopicPartition)>`), khong lien quan gi den tenant/khach hang | `TenantSRKafkaSinkExtensions.cs:20-21,41,86,101`; `TenantSRKafkaSinkExtensions.cs:159-167` (dinh nghia `TenantSRKafkaSinkModel`) | Cao. Ten class/model gay hieu lam nghiem trong cho nguoi doc/AI agent tuong day la co che multi-tenant; day **khong phai** noi duy nhat co tenant-awareness trong repo theo huong nguoc lai - **khong co tenant-awareness thuc su o day**, tuong tu ket luan da biet truoc ve `CoreSQLTenant.cs` (khong co logic tenant) |
| 2 | Trong ca `ConfigureKafkaConnection` (`SRKafkaSinkExtensions.cs:87-106`) va `ProducerConfig` private static (`TenantSRKafkaSinkExtensions.cs:134-156`), `LoadFromEnvironmentVariables()` duoc goi **truoc** cac `.SetValue("SecurityProtocol",...)`/`.SetValue("SaslMechanism",...)` tuong ung tham so co gia tri mac dinh (`SecurityProtocol.Plaintext`, `SaslMechanism.Plain` trong `SRKafkaSink`, `SRLoggerConfigurationExtensions.cs:25-26`) | `SRKafkaSinkExtensions.cs:94-103`; `TenantSRKafkaSinkExtensions.cs:142-153`; `SRLoggerConfigurationExtensions.cs:25-26` | Cao. Vi tham so `securityProtocol`/`saslMechanism` luon co gia tri (khong `null`, kieu enum non-nullable), buoc `.SetValue` sau `LoadFromEnvironmentVariables()` **luon de len** bat ky gia tri nap tu bien moi truong `SERILOG__KAFKA__SECURITYPROTOCOL`/`SERILOG__KAFKA__SASLMECHANISM` - 2 bien moi truong nay tren thuc te **khong bao gio co hieu luc** tru khi caller chu dong truyen tham so tuong ung khac mac dinh |
| 3 | Neu goi constructor `SRKafkaSinkExtensions` voi ca `topic` VA `topicDecider` deu `null`, `_globalTopicPartition` khong duoc gan gia tri nao; nhanh `_topicDecider is null` trong `EmitBatchAsync` van su dung field nay ma khong co guard | `SRKafkaSinkExtensions.cs:35-43,57-59` | Trung binh. Khong the xay ra qua API cong khai `SRLoggerConfigurationExtensions.SRKafkaSink` (vi `topic` co gia tri mac dinh `"logs"`, luon khac null) nhung co the xay ra neu ai do goi truc tiep constructor `SRKafkaSinkExtensions` voi `topic: null, topicDecider: null` |
| 4 | 2 lop `SRKafkaSinkExtensions` va `TenantSRKafkaSinkExtensions` dung hau to `*Extensions` giong quy uoc dat ten cho static class chua extension method (nhu `SRLoggerConfigurationExtensions`, `SRProducerConfigExtensions`, `RegisterImplementationExtensions` cung module), nhung ban chat la `sealed record` implement `IBatchedLogEventSink`, khong phai static class chua extension method | `SRKafkaSinkExtensions.cs:12`; `TenantSRKafkaSinkExtensions.cs:12` | Thap. Khong anh huong hanh vi runtime, nhung de gay nham lan khi doc code/tra cuu theo quy uoc dat ten cua repo |
| 5 | `SRKafkaLogFormatter.GetLogEventPropertyValue` coi property co gia tri render la dung chuoi `"null"` (4 ky tu) giong nhu property khong ton tai - ca hai deu tra `null` | `SRKafkaLogFormatter.cs:320-324` | Thap-Trung binh. Chi anh huong trong truong hop hiem gia tri du lieu thuc su la chuoi `"null"`; khi do JSON gui Kafka se thieu truong tuong ung do quy tac `WhenWritingDefault` (xem #6) |
| 6 | Truong `IPAddress` trong JSON cua `SRKafkaLogFormatter` doc tu Serilog property ten `"Forwarded"`, duoc gan boi `SerilogHandlerMiddleWare.cs:75` - **mot file nam ngoai pham vi 9 file duoc giao cua tai lieu nay** - khong co file nao trong module set property nay | `SRKafkaLogFormatter.cs:64`; doi chieu: `FTELSRCore.Shared/Infrastructure/MiddleWares/SerilogHandlerMiddleWare.cs:75` | Trung binh. Neu middleware do khong duoc dang ky trong pipeline cua mot service cu the su dung module nay, truong `IPAddress` se luon vang mat trong JSON gui Kafka (do `WhenWritingDefault` loai bo gia tri `null`), doc lap voi truong `ClientIp` (van duoc dien binh thuong boi enricher trong module nay) |
| 7 | Nhom `RegisterImplementationExtensions.*ImplementationsWithInterface` (muc 2.19) kiem tra assignability tren **interface tim duoc** (`t.Service`), khac voi nhom `*ImplementationsOnly` (muc 2.18) kiem tra tren **class implementation**; truyen nham mot `Type` khong phu hop (vi du 1 class cu the) vao `typeofImplementation` cua nhom WithInterface se cho ket qua **0 dang ky, khong co exception/canh bao** | `RegisterImplementationExtensions.cs:119-141,152-174,185-207` | Trung binh. Loi cau hinh DI se im lang, chi phat hien duoc khi runtime bao thieu service (`InvalidOperationException` cua DI container o noi resolve, khong phai tai diem dang ky) |
| 8 | Doi chieu voi 8 file Knowledge Base hien co (Utilizes-CallApiWithHttp.md, Utilizes-CallApi.md, Data-MongoDB-CoreMongoDB.md, Data-SQL-CoreSQL.md, Data-SQL-CoreSQL-TwoEntity.md, Data-SQL-UnitOfWork-DbContexts.md, Data-SQL-Dapper.md, Data-SQL-Resilience.md): trong 9 kieu du lieu can doi chieu, chi `CustomException` xuat hien trong source cua module nay (`TenantSRKafkaSinkExtensions.cs:38`; `SRProducerConfigExtensions.cs:65,116,124`). Cach goi `new CustomException(message: "...")` (chi truyen `message`, khong truyen `statusCode`) khop dung voi constructor `CustomException(string message, int statusCode = 500)` ma `Utilizes-CallApi.md:45` va `Utilizes-CallApiWithHttp.md:56` da mo ta (Code mac dinh 500) - **khong phat hien mo ta sai/thieu** trong 2 file KB nay lien quan den module nay. Enum `SRKafkaLogFormatter.DirectionType` (`Outbound = 0, Inbound = 1`, `SRKafkaLogFormatter.cs:303-307`) cung khop dung voi mo ta tai `Utilizes-CallApiWithHttp.md:57` | `Utilizes-CallApi.md:45`; `Utilizes-CallApiWithHttp.md:56-57` | Thong tin. Ghi nhan da hoan thanh buoc doi chieu nguoc bat buoc; khong can hanh dong sua file KB cu tai buoc nay |
| 9 | `SRLogEventEnricherExtensions.Enrich` doc claim `SRPermissions` bi comment toan bo (`SRLogEventEnricherExtensions.cs:88-95`) - bien `permissionsName` duoc tinh trong comment nhung khong bao gio duoc su dung trong chuoi `UserInfo` thuc te duoc gan | `SRLogEventEnricherExtensions.cs:88-95,116` | Thap. Neu XML doc/comment cu con sot lai gia dinh `UserInfo` co chua thong tin permission, gia dinh do la **sai** so voi than ham thuc te - `UserInfo` chi chua `username`, `Roles`, `RoleData`, `IP` |
| 10 | `Enrich` chi fallback `correlationId` sang `Activity.Current?.TraceId`/`Guid.NewGuid()` khi header `X-Correlation-Id` **hoan toan khong co** trong request (`TryGetValue` tra `false`); neu header co mat nhung gia tri la chuoi rong/whitespace, `correlationId` van nhan gia tri rong do, **khong fallback** | `SRLogEventEnricherExtensions.cs:56-69` | Trung binh. Client tu gui header rong (vi du proxy forward header thieu gia tri) se khien `CorrelationId` trong log bi rong thay vi co gia tri hop le, gay kho khan khi truy vet log lien he thong |
| 11 | `roleDataName` trong `Enrich` duoc khoi tao gia tri mac dinh la `RoleSR.ONLY_CREATE` (khong phai rong/Anonymous); neu nguoi dung da authenticate nhung khong co claim `SR.SRRoles` nao, `UserInfo` sinh ra van the hien `RoleData: ONLY_CREATE` | `SRLogEventEnricherExtensions.cs:97-106` | Thap-Trung binh. De gay hieu lam khi doc log: `RoleData: ONLY_CREATE` co the bi hieu la nguoi dung duoc gan quyen "chi tao", trong khi thuc te la khong co claim role nao duoc doc duoc |
| 12 | `GroupBy(x => x.LogEventLevels)` trong `TenantSRKafkaSinkExtensions.SetupConfiguration` dua tren so sanh **tham chieu** cua `List<LogEventLevel>` (kieu nay khong override `Equals`/`GetHashCode`) - tren thuc te hau nhu luon tao ra 1 nhom cho moi model dau vao, khong gop duoc cac model co danh sach level "giong nhau ve gia tri" nhung khac instance | `TenantSRKafkaSinkExtensions.cs:41` | Thap. Khong gay loi runtime (logic per-level `ContainsKey` ben trong van hoat dong dung), nhung ten goi "GroupBy" trong code de gay hieu lam ve muc dich thuc su cua buoc nay |

