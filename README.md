<div align="center">

<h1><img src="https://github.com/SP-XD/SP-XD/raw/main/images/hyperkitty.gif?raw=true" width="30"/> ᴛʜᴇᴘʀᴏᴛᴏᴋᴏᴛ</h1>

Многопользовательский Telegram-бот для игры **Amazing Online**, который предоставляет автоматизацию, инструменты, мониторинг и набор вспомагательных функций упрощающих повседневненные игровые задачи.

<img src="media/cat2.gif" width="400"/>

[![Build](https://img.shields.io/github/actions/workflow/status/NZK95/TheProtoKot/build.yml?style=flat-square&label=build)](https://github.com/NZK95/TheProtoKot/actions)
![GitHub Last Commit](https://img.shields.io/github/last-commit/NZK95/TheProtoKot?style=flat-square)
[![Downloads](https://img.shields.io/github/downloads/NZK95/TheProtoKot/total?style=flat-square&color=brightgreen)](https://github.com/NZK95/TheProtoKot/releases)
![GitHub Stars](https://img.shields.io/github/stars/NZK95/TheProtoKot?style=flat-square)
![GitHub Issues](https://img.shields.io/github/issues/NZK95/TheProtoKot?style=flat-square)
![GitHub License](https://img.shields.io/github/license/NZK95/TheProtoKot?style=flat-square)
[![Platform](https://img.shields.io/badge/platform-Windows%2011-0078D4?style=flat-square&logo=windows&logoColor=white)](https://www.microsoft.com/windows)
[![SQLite](https://img.shields.io/badge/SQLite-3-003B57?style=flat-square&logo=sqlite&logoColor=white)](https://www.sqlite.org)
![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet&logoColor=white)
![Telegram](https://img.shields.io/badge/Telegram-Bot-2CA5E0?style=flat-square&logo=telegram&logoColor=white)
</div>

---

## Обзор, демонстрация и скриншоты
## Обзор, демонстрация и скриншоты

<div align="center">

<img src="media/demo/1.png" width="30%"/> <img src="media/demo/2.png" width="30%"/> <img src="media/demo/3.png" width="30%"/>

<img src="media/demo/4.png" width="30%"/> <img src="media/demo/5.png" width="30%"/> <img src="media/demo/6.png" width="30%"/> <img src="media/demo/7.png" width="30%"/>  

 <img src="media/demo/8.png" width="30%"/> <img src="media/demo/9.png" width="30%"/> <img src="media/demo/11.png" width="35%" />

<img src="media/demo/10.png" width="45%" height="45%" /> 

</div>

## Содержание
- [Возможности](#возможности)
- [Требования](#требования)
- [Установка](#установка)
- [Конфигурация](#конфигурация)
- [Команды](#команды)
- [Структура базы данных](#структура-базы-данных)
- [Устранение неполадок / Предложение идеи](#устранение-неполадок--предложение-идеи)
- [Лицензия](#лицензия)

## Требования
- [.NET 8.0+](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- Windows 10/11 *(Linux не поддерживает нужные библиотеки)*
- Установленный [Playwright](https://playwright.dev/dotnet/docs/intro) с браузером **Firefox**
- Telegram-бот, созданный через [@BotFather](https://t.me/BotFather)

## Возможности и функции
- **📄 Документы** - Система автоматического создания документов и протоколов для различных фракций. Пользователю достаточно выбрать нужную фракцию, тип документа и ответить на несколько вопросов — бот самостоятельно сформирует готовый результат в нужном формате (документ, изображение или ссылка на фотохостинг).

- **🖥️ Мониторинг серверов** - Система слежения за состоянием серверов **Amazing Online** в реальном времени. Пользователь выбирает сервер для отслеживания — бот автоматически обновляет информацию и уведомляет об изменениях (сообщение можно закрепить). Доступные данные: онлайн игроков, очередь на вход, текущие акции, IP-адрес сервера и многое другое.

- **✍️ Подписи** - Инструмент для создания подписей со штампами фракций. Пользователь загружает изображение своей подписи, выбирает нужный штамп — после чего бот автоматически объединяет изображения и отправляет готовый результат.

- **🚫 Чёрный список** - Система проверки игроков по базам чёрных списков различных серверов. Пользователь вводит никнейм и сервер, после чего бот выполняет поиск и отображает подробную информацию о блокировке.

- **🔔 События и мероприятия** - Система уведомлений о серверных событиях и мероприятиях. Пользователи могут подписываться на интересующие события и получать уведомления перед их началом и в момент старта.

- **⚠️ Чекер жалоб** - Инструмент для отслеживания жалоб на форуме Amazing Online. Пользователь привязывает аккаунт, указывая игровой ник и маску, после чего бот автоматически уведомляет о новых жалобах и предоставляет подробную информацию.

- **💡 Подсказки и AHK** - Раздел с полезными подсказками и AHK-скриптами для различных серверов и фракций.

## Команды
| Команда | Описание |
|---|---|
| `/start` | Запуск и главное меню. |
| `/report` | Предложить идею или сообщить об проблеме в официальном телеграм канале **ПротоКота**. |
| `/coder` | Показать информацию о кодере и автора бота. |

## Библиотеки

## Структура проекта

<details>
<summary>Нажмите сюда, чтобы посмотреть.</summary>

![Структура проекта](media/images/project-schema.png)

</details>

## Структура базы-данных
<img src="media/database-schema.png" />

## Ответы на частые вопросы (FAQ)

## История версий и изменений 


## Устранение неполадок / Предложение идеи
**Прочие ошибки**  
Открой [Issue](https://github.com/NZK95/TheProtoKot/issues/new?template=bug_report.md) с описанием проблемы и текстом ошибки из консоли — постараюсь помочь.
 
**Есть идея или предложение?**  
Буду рад новым идеям — открывай [Feature Request](https://github.com/NZK95/TheProtoKot/issues/new?template=feature_request.md) и описывай что хочется видеть в боте.
 
## Лицензия
Проект распространяется под лицензией [MIT](LICENSE).

---

[![Telegram](https://img.shields.io/badge/Telegram-канал-2CA5E0?style=flat-square&logo=telegram&logoColor=white)](https://t.me/TheProtoKot#)

