#  TimeSeriesUploader

**WebAPI для загрузки и анализа time‑series данных из CSV**  
*Чистая архитектура · 100% покрытие критических сценариев · Готовность к продакшену*

---

##  О проекте

Сервис принимает CSV-файлы с временными рядами, выполняет валидацию, сохраняет данные и рассчитывает агрегированные показатели.  
Проект выполнен в рамках тестового задания и демонстрирует глубокое понимание **ASP.NET Core, EF Core, чистой архитектуры и модульного тестирования**.

---

##  Быстрый старт

```bash
git clone https://github.com/fairwix/TimeSeriesUploader.git
cd TimeSeriesUploader
# настройте строку подключения в appsettings.json
dotnet ef database update
dotnet run
# Swagger: http://localhost:5131/swagger
```

---

##  Основные возможности

###  Загрузка и обработка CSV  
`POST /api/files/upload`

**Формат файла (строго по ТЗ):**  
```csv
Date;ExecutionTime;Value
2023-01-01T10-00-00.0000Z;1.5;10.5
2023-01-01T10-05-00.0000Z;2.0;20.5
```

✔️ Валидация каждой строки  
✔️ Транзакционность – при ошибке данные не сохраняются  
✔️ Перезапись существующего файла  

###  Получение агрегатов с фильтрацией  
`GET /api/results`

Фильтры: `fileName`, `minDateFrom`, `minDateTo`, `avgValueFrom`, `avgValueTo`, `avgExecutionTimeFrom`, `avgExecutionTimeTo`.  

✔️ Без пересчёта – данные берутся из заранее посчитанной таблицы `Results`

###  Последние 10 значений по файлу  
`GET /api/files/{fileName}/last10`

✔️ Быстрый запрос благодаря индексу `(FileName, Date)`

---

##  Архитектура

```
TimeSeriesUploader/
├── Domain/          # сущности (POCO)
├── Application/     # DTO, интерфейсы, сервисы, валидация, маппинг
├── Infrastructure/  # EF Core, PostgreSQL, миграции, конфигурации
├── WebAPI/          # контроллеры, middleware, DI
└── Tests/           # unit-тесты (xUnit, Moq, FluentAssertions)
```

**Ключевые архитектурные решения:**  
- **Строгое следование ТЗ** – кастомный конвертер для формата даты `yyyy-MM-ddTHH-mm-ss.ffffZ`  
- **Чистая абстракция** – `IAppDbContext` для лёгкого тестирования  
- **Централизованная обработка ошибок** – middleware с RFC 7807 Problem Details  
- **Валидация через FluentValidation** – декларативно и расширяемо  

---

##  Валидация 

| Проверка               | Условие                                   |
|------------------------|-------------------------------------------|
| Формат даты            | `yyyy-MM-ddTHH-mm-ss.ffffZ`               |
| Диапазон дат           | от `2000-01-01` до текущего UTC           |
| ExecutionTime          | ≥ 0                                       |
| Value                  | ≥ 0                                       |
| Количество строк       | от 1 до 10 000                            |
| Отсутствие полей       | недопустимо                               |

 При любой ошибке – rollback транзакции + понятное сообщение клиенту.

---

##  Тестирование и качество кода

 **88.5% line coverage**, **75% branch coverage**  
 Отдельные тесты на:  
- парсинг даты с дефисами  
- валидацию полей  
- транзакционность и rollback  
- отмену запроса (`CancellationToken`)  
- корректную работу фильтров  

Тесты запускаются на изолированном `TestAppDbContext` – никакого влияния на реальную БД.

---

##  Стек технологий

- **.NET 8** + ASP.NET Core WebAPI  
- **Entity Framework Core** + **PostgreSQL**  
- **CsvHelper** – парсинг CSV  
- **FluentValidation** – валидация  
- **AutoMapper** – маппинг DTO  
- **Swagger / OpenAPI** – документация  
- **xUnit**, **Moq**, **FluentAssertions** – тестирование  

---

##  Структура БД

### `Values` (исходные записи)

| Колонка        | Тип          | Индекс               |
|----------------|--------------|----------------------|
| Id             | Guid         | PK                   |
| FileName       | string(255)  |                      |
| Date           | timestamptz  | (FileName, Date)     |
| ExecutionTime  | double       |                      |
| Value          | double       |                      |

### `Results` (агрегаты по файлам)

| Колонка             | Тип          |
|---------------------|--------------|
| FileName            | string(255) PK |
| TimeDeltaSeconds    | double       |
| FirstExecutionDate  | timestamptz  |
| AvgExecutionTime    | double       |
| AvgValue            | double       |
| MedianValue         | double       |
| MaxValue            | double       |
| MinValue            | double       |



