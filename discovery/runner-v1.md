# Runner discovery v1

Wire API v1: бинарный protobuf, схема `proto/RunnerAgent/discovery.proto`, package
`texnokaktus.progolymp.runner.discovery.v1`. В Redis string хранится ровно один
сериализованный RunnerRegistration; Pub/Sub передаёт ровно один RunnerDiscoveryEvent.
Байты записываются напрямую, без JSON, Base64, gRPC framing и length-prefix.
Версия задаётся пространством Redis и proto package.

Решение пользователя от 10 ноября 2026 года заменило локальный, ещё не
опубликованный JSON-вариант. Protobuf — окончательный wire-format v1; JSON
fallback не вводится. При несовместимом изменении опубликованного контракта
потребуется новый namespace. Поля можно добавлять с новыми номерами; прежние
номера нельзя переиспользовать. Потребитель сохраняет неизвестные поля,
проверяет непустые строки регистрации и корректность endpoint.

| Message | Поля |
| --- | --- |
| RunnerRegistration | `string instance_id = 1`, `string language = 2`, `string language_version = 3`, `string endpoint = 4` |
| RunnerDiscoveryEvent | Вложенный enum `RunnerDiscoveryEventType type = 1`, `string instance_id = 2` |

RunnerDiscoveryEvent.RunnerDiscoveryEventType: `UNSPECIFIED=0`, `REGISTERED=1`, `REMOVED=2` (в proto
имена имеют префикс `RUNNER_DISCOVERY_EVENT_TYPE_`). Неизвестный или unspecified
тип не считается успешной регистрацией/удалением; потребитель может перечитать
реестр, не полагаясь на такой hint. Отдельный discovery RPC не добавляется.

Enum объявлен внутри RunnerDiscoveryEvent; C# имя —
`RunnerDiscoveryEvent.Types.RunnerDiscoveryEventType`. Перенос enum внутрь message
не меняет номера полей, значения enum или wire bytes; прежние golden fixtures
сохраняются.

- Ключ: `progolymp:runner-discovery:v1:{environment}:instances:{instance_id}`.
- Значение: ровно `instance_id`, `language`, `language_version`, `endpoint`.
- Environment: 1–64 символа ASCII `[A-Za-z0-9_-]`, отдельные runtime credentials.
- Instance ID: новый случайный UUID в lowercase N-format (32 hex) на процесс.
- TTL: 60 секунд, heartbeat: 20 секунд по умолчанию; допускается настройка.
- Канал: `progolymp:runner-discovery:v1:{environment}:events`.
- События: бинарный RunnerDiscoveryEvent с type REGISTERED или REMOVED и instance_id.

Создание/восстановление записи с TTL и `registered` атомарны. Обычное продление
полной записи с TTL не публикует событие. Удаление существующей собственной
записи и `removed` атомарны; удаление отсутствующей записи не создаёт событие.
Событие — повод перечитать реестр. Pub/Sub не имеет истории и подтверждений;
истечение TTL не обязано публиковать событие. Источник истины — ключи с TTL.
Рекомендация потребителю: SCAN полного namespace при старте/восстановлении
подписки и раз в минуту. Одинаковые endpoint разных ID в одном окружении — ошибка
конфигурации; runner не выполняет глобальную проверку и не блокирует адрес.

Необязательное gRPC metadata `runner-instance-id`: отсутствие сохраняет прямой
вызов, одно точное совпадение допускает обычную обработку. Пустое, чужое или
повторяющееся значение даёт FAILED_PRECONDITION до принятия RunJob и любого IO
задания, в том числе при busy/drain. Работает без Redis и без публикации discovery.
Это идентичность процесса, не аутентификация. Proto RunJob не изменён.

Golden fixtures находятся в `fixtures/`: `.pb` — ожидаемые wire bytes,
`.textproto` — человекочитаемый источник. `.gitattributes` исключает изменение
бинарных fixtures при Git autocrlf. Для воспроизведения (POSIX shell):

```sh
protoc -I proto --encode=texnokaktus.progolymp.runner.discovery.v1.RunnerRegistration proto/RunnerAgent/discovery.proto < discovery/fixtures/registration-v1.textproto > discovery/fixtures/registration-v1.pb
protoc -I proto --encode=texnokaktus.progolymp.runner.discovery.v1.RunnerDiscoveryEvent proto/RunnerAgent/discovery.proto < discovery/fixtures/registered-v1.textproto > discovery/fixtures/registered-v1.pb
protoc -I proto --encode=texnokaktus.progolymp.runner.discovery.v1.RunnerDiscoveryEvent proto/RunnerAgent/discovery.proto < discovery/fixtures/removed-v1.textproto > discovery/fixtures/removed-v1.pb
```

RunnerAgent подключает discovery.proto через MSBuild Protobuf Link с
GrpcServices=None. Constants для namespace/metadata находятся в
`Texnokaktus.ProgOlymp.Common.Contracts/Discovery/RunnerDiscovery.cs` и
подключаются Compile Link, без зависимости от прочих контрактов общей сборки.
Чтение в C#: `RunnerRegistration.Parser.ParseFrom((byte[])value)`;
запись: `registration.ToByteArray()`. RedisValue нельзя преобразовывать в строку
перед protobuf parsing. Внутренняя startup identity не отдаёт изменяемый message
напрямую; экспортируется Clone().
