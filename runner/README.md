# Вложенные типы RunJob v1

Схема: `proto/RunnerAgent/runner.proto`. Тип, который используется только одним
родительским message и описывает его данные, объявляется внутри него.
Вложенные message и enum размещаются после полей родителя, в том числе
при нескольких уровнях вложенности. Этот порядок принят и для discovery.proto.

| Родитель | Вложенные типы |
| --- | --- |
| RunJobRequest | JobTest, ResourceLimits |
| RunJobResponse | TestResult, JobStatus |
| RunJobResponse.TestResult | TestStatus |
| StageResult | TerminationReason |

JobTest и ResourceLimits описывают вход задания. TestResult и JobStatus принадлежат
ответу, TestStatus — результату отдельного теста, TerminationReason — результату
стадии. ObjectReference, Diagnostic и StageResult остаются общими: ими пользуются
разные сообщения. Не требуется вкладывать общий тип в ближайшего общего предка
только потому, что он достижим из одного response.

Имена, номера и типы полей, optional/repeated, числовые значения enum, proto package
и RPC RunJob сохранены. Бинарный wire-format совместим с прежней невложенной
схемой. Полные protobuf-имена дочерних типов и сгенерированный API изменились;
исходных C# потребителей нужно перегенерировать/пересобрать. Например:

```text
RunJobRequest.Types.JobTest
RunJobRequest.Types.ResourceLimits
RunJobResponse.Types.JobStatus
RunJobResponse.Types.TestResult.Types.TestStatus
StageResult.Types.TerminationReason
```

`fixtures/request-v1.pb` и `response-v1.pb` созданы protoc --encode **до** вложения
типов, по runner.proto из commit `95a8166804ffaa8cf20bcd46a28a16a9a0a6877f`.
`.textproto` — их человекочитаемый источник. RunnerContractTests проверяет чтение
старых байтов новой схемой и точное совпадение новой сериализации со старыми
fixtures, включая optional presence, статусы и причины завершения. Golden bytes
не перегенерируются при переносе типов. `.gitattributes` сохраняет их бинарными.

Для воспроизведения с текущей схемой (POSIX shell, из корня Common.Contracts):

```sh
protoc -I proto --encode=texnokaktus.progolymp.runner.v1.RunJobRequest proto/RunnerAgent/runner.proto < runner/fixtures/request-v1.textproto > /tmp/runner-request.pb
cmp /tmp/runner-request.pb runner/fixtures/request-v1.pb
protoc -I proto --encode=texnokaktus.progolymp.runner.v1.RunJobResponse proto/RunnerAgent/runner.proto < runner/fixtures/response-v1.textproto > /tmp/runner-response.pb
cmp /tmp/runner-response.pb runner/fixtures/response-v1.pb
```
