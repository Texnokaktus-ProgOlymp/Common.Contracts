# Runner discovery v1

Wire API v1: UTF-8 JSON, имена полей чувствительны к регистру. Версия задаётся
пространством Redis, а не дополнительным полем регистрации. Порядок JSON-полей
несущественен. Потребитель проверяет обязательные строковые поля и игнорирует
неизвестные поля для совместимости; изменение смысла требует нового namespace.

- Ключ: `progolymp:runner-discovery:v1:{environment}:instances:{instance_id}`.
- Значение: ровно `instance_id`, `language`, `language_version`, `endpoint`.
- Environment: 1–64 символа ASCII `[A-Za-z0-9_-]`, отдельные runtime credentials.
- Instance ID: новый случайный UUID в lowercase N-format (32 hex) на процесс.
- TTL: 60 секунд, heartbeat: 20 секунд по умолчанию; допускается настройка.
- Канал: `progolymp:runner-discovery:v1:{environment}:events`.
- События: `{"type":"registered|removed","instance_id":"..."}`.

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

Эталонные JSON находятся в `fixtures/`. C# определения —
`Texnokaktus.ProgOlymp.Common.Contracts/Discovery/RunnerDiscovery.cs`.
RunnerAgent подключает этот исходник через Compile Link, как runner.proto через
Protobuf Link, без зависимости от прочих контрактов общей сборки.
