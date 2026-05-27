using System.Globalization;
using Telegram.Bot.Types.ReplyMarkups;
using clrhost;

internal sealed class KeyboardFactory
{
    public InlineKeyboardMarkup AdminPanel { get; private set; }
    public InlineKeyboardMarkup Fractions { get; private set; }
    public InlineKeyboardMarkup Servers { get; private set; }
    public ReplyKeyboardMarkup StartMenu { get; private set; }
    public ReplyKeyboardMarkup StartMenuWithoutMainButton { get; private set; }
    public ReplyKeyboardMarkup QuestionsMenu { get; private set; }
    public ReplyKeyboardMarkup FirstQuestionMenu { get; private set; }
    public InlineKeyboardMarkup BackgroundColorHint { get; private set; }
    public InlineKeyboardMarkup ServersUpdateMenu { get; private set; }
    public InlineKeyboardMarkup ServersHintsMenu { get; private set; }
    public InlineKeyboardMarkup ServersBlockedMenu { get; private set; }
    public InlineKeyboardMarkup ConfrimBackToMainMenu { get; private set; }
    public InlineKeyboardMarkup DocumentMenu { get; private set; }
    public ReplyKeyboardMarkup BlacklistMenu { get; private set; }
    public InlineKeyboardMarkup ResultFormatChoiceMenu { get; private set; }
    public ReplyKeyboardMarkup EventsMenu { get; private set; }
    public InlineKeyboardMarkup ReturnToEventMenu { get; private set; }
    public InlineKeyboardMarkup ReportCheckerMenu { get; private set; }
    public InlineKeyboardMarkup ReportCheckerServersMenu { get; private set; }

    public KeyboardFactory()
    {
        AdminPanel = BuildAdminPanel();
        Fractions = BuildFractions();
        Servers = BuildServersOnline();
        StartMenu = BuildStartMenu();
        StartMenuWithoutMainButton = BuildStartMenuWithoutMainButton();
        QuestionsMenu = BuildQuestionsMenu();
        FirstQuestionMenu = BuildFirstQuestionMenu();
        BackgroundColorHint = BuildBackgroundColorHint();
        ServersUpdateMenu = BuildServersUpdateMenu();
        ServersHintsMenu = BuildServersHints();
        ServersBlockedMenu = BuildServersBlocked();
        ConfrimBackToMainMenu = BuildConfirmBackToMainMenu();
        BlacklistMenu = BuildBlacklistReturnMenu();
        ResultFormatChoiceMenu = BuildResultFormatChoiceMenu();
        EventsMenu = BuildEventsMenu();
        ReturnToEventMenu = BuildReturnToEventMenu();
        DocumentMenu = BuildDocumentMenu();
        ReportCheckerMenu = BuildReportCheckerMenu();
        ReportCheckerServersMenu = BuildReportCheckerServersMenu();
    }


    public InlineKeyboardMarkup BuildListOfTrackedForReportsPlayersShowPropertiesMenu(
        List<ReportedAccountRecord> accounts,
        bool addButton = true,
        bool returnButton = false)
    {
        var buttons = new List<InlineKeyboardButton[]>();

        try
        {
            foreach (var acc in accounts)
            {
                buttons.Add(new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        $"{acc.Nickname} ({acc.Server})",
                        $"CheckedAccountProperties@{acc.AccountId},{acc.Nickname},{acc.Server},{acc.NotificationsEnabled}")
                });
            }

            if (addButton)
            {
                buttons.Add(new[]
                {
                    InlineKeyboardButton.WithCallbackData("➕ Добавить", "AddCheckedAccount@None")
                });
            }

            if (returnButton)
            {
                buttons.Add(new[]
                {
                    InlineKeyboardButton.WithCallbackData("Назад", "ReportChecker@BackFromAccounts")
                });
            }

            return new InlineKeyboardMarkup(buttons);
        }
        catch
        {
            return new InlineKeyboardMarkup(buttons);
        }
    }

    public InlineKeyboardMarkup BuildCheckedForReportsAccountMenu(
        ReportedAccountRecord account,
        bool addOnlyBackButton = false)
    {
        var buttons = new List<InlineKeyboardButton[]>();

        try
        {
            if (addOnlyBackButton)
            {
                buttons.Add(new[]
                {
                    InlineKeyboardButton.WithCallbackData("Назад", "AccountMenuBack@None")
                });

                return new InlineKeyboardMarkup(buttons);
            }

            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    "🔔 Уведомления",
                    $"AccountMenuNotifications@{account.AccountId},{account.Nickname},{account.Server},{account.NotificationsEnabled}")
            });

            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    "🚪 Выйти из аккаунта",
                    $"AccountMenuLogout@{account.AccountId},{account.Nickname},{account.Server},{account.NotificationsEnabled}")
            });

            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData("Назад", "AccountMenuBack@None")
            });

            return new InlineKeyboardMarkup(buttons);
        }
        catch
        {
            return new InlineKeyboardMarkup(buttons);
        }
    }

    public InlineKeyboardMarkup BuildListOfTrackedForReportsPlayersCheckNowMenu(
        List<ReportedAccountRecord> accounts)
    {
        var buttons = new List<InlineKeyboardButton[]>();

        try
        {
            foreach (var acc in accounts)
            {
                buttons.Add(new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        $"{acc.Nickname} ({acc.Server})",
                        $"CheckAccount@{acc.AccountId},{acc.Nickname},{acc.Server},{acc.NotificationsEnabled}")
                });
            }

            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData("Назад", "ReportChecker@BackFromAccounts")
            });

            return new InlineKeyboardMarkup(buttons);
        }
        catch
        {
            return new InlineKeyboardMarkup(buttons);
        }
    }

    public InlineKeyboardMarkup BuildDateHintsMenu(string question)
    {
        var questionTemp = question.Trim().RemoveLeadingNumber();

        if (QuestionsConstants.DayQuestionsPatterns.Any(x => questionTemp.Contains(x)) ||
            QuestionsConstants.MonthQuestionsPatterns.Any(x => questionTemp.Contains(x)) ||
            QuestionsConstants.YearQuestionsPatterns.Any(x => questionTemp.Contains(x)))
        {
            return new InlineKeyboardMarkup(new[]
            {
                InlineKeyboardButton.WithCallbackData("Выбрать", "CurrentDate@Сегодня")
            });
        }

        return null;
    }

    public InlineKeyboardMarkup BuildEventButtons()
    {
        var buttons = new List<InlineKeyboardButton[]>();

        foreach (var ev in EventService.Events)
        {
            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData(ev.Name, $"Event@{ev.Id}")
            });
        }

        buttons.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData("🔙 Вернуться", "EventsMenu@SubscribeToEvent")
        });

        return new InlineKeyboardMarkup(buttons);
    }

    public InlineKeyboardMarkup BuildDocumentTypes(string fraction)
    {
        var path = Path.Combine(PathService.TEMPLATES_PATH, fraction);
        var documents = FileService.GetFileNames(path)
            .Select(x => x.Replace(".docx", string.Empty))
            .ToArray();

        var buttons = new List<InlineKeyboardButton[]>();

        foreach (var doc in documents)
        {
            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    doc.Replace($"_{fraction}", ""),
                    $"DocumentType@{doc}")
            });
        }

        return new InlineKeyboardMarkup(buttons);
    }

    public InlineKeyboardMarkup BuildStamps()
    {
        var stamps = FileService.GetFileNames(PathService.STAMPS_PATH)
            .Select(x => x.Replace(".PNG", string.Empty))
            .ToArray();

        var buttons = new List<InlineKeyboardButton[]>();

        foreach (var doc in stamps)
        {
            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData(doc, $"Stamp@{doc}")
            });
        }

        buttons.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData("🔙 Вернуться", "FirmsMenu@none")
        });

        return new InlineKeyboardMarkup(buttons);
    }

    public InlineKeyboardMarkup BuildDefaultHints(string server)
    {
        if (!Directory.Exists(Path.Combine(PathService.HINTS_PATH, server)))
            return null;

        var documents = FileService.GetFileNames(Path.Combine(PathService.HINTS_PATH, server))
            .Select(x => Path.GetFileNameWithoutExtension(x))
            .ToArray<string>();

        var buttons = new List<InlineKeyboardButton[]>();
        var seen = new HashSet<string>();

        foreach (var doc in documents)
        {
            var parts = doc.Split('_');

            if (seen.Contains(parts[0]))
                continue;

            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData(parts[0], $"Hints@{parts[0]}")
            });

            seen.Add(parts[0]);
        }

        return new InlineKeyboardMarkup(buttons);
    }

    public InlineKeyboardMarkup BuildPagesMenu(int currentPage, int totalPages)
    {
        var buttons = new List<InlineKeyboardButton[]>();

        var navButtons = new List<InlineKeyboardButton>();

        if (currentPage > 0)
            navButtons.Add(InlineKeyboardButton.WithCallbackData("⬅️", $"prev@{currentPage - 1}"));

        if (currentPage < totalPages - 1)
            navButtons.Add(InlineKeyboardButton.WithCallbackData("➡️", $"next@{currentPage + 1}"));

        if (navButtons.Count > 0)
            buttons.Add(navButtons.ToArray());

        buttons.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData("↩️ Вернуться", "BlacklistMenu@None")
        });

        return new InlineKeyboardMarkup(buttons);
    }

    public async Task<InlineKeyboardMarkup> BuildRemoveEvents(EventsDatabase eventsDb, long chatId)
    {
        var record = await eventsDb.GetUserAsync(chatId);
        var buttons = new List<InlineKeyboardButton[]>();

        if (string.IsNullOrEmpty(record.EventsIds))
            return new InlineKeyboardMarkup(buttons);

        var ids = record.EventsIds
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => int.TryParse(x.Trim(), out var id) ? id : (int?)null)
            .Where(id => id.HasValue)
            .Select(id => id.Value)
            .ToList();

        var userEvents = EventService.Events
            .Where(e => ids.Contains(e.Id))
            .ToList();

        foreach (var ev in userEvents)
        {
            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData(ev.Name, $"EventRemove@{ev.Id}")
            });
        }

        buttons.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData("🔙 Вернуться", "EventsMenu@UnscribeFromEvent")
        });

        return new InlineKeyboardMarkup(buttons);
    }


    private InlineKeyboardMarkup BuildAdminPanel()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("📝 Документы", "Admin@CreatingDocument"),
                InlineKeyboardButton.WithCallbackData("✍️ Подписи",   "Admin@Firm"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🚫 ЧС",        "Admin@Blacklist"),
                InlineKeyboardButton.WithCallbackData("📜 События",   "Admin@Events"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🗄 Главная БД", "Admin@MainDb"),
                InlineKeyboardButton.WithCallbackData("📢 Рассылка",  "Admin@Newsletter"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🥾 Кик",       "Admin@Kick"),
                InlineKeyboardButton.WithCallbackData("🔨 Бан",       "Admin@Ban"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("⏱ За 5 мин",  "Admin@Users5m"),
                InlineKeyboardButton.WithCallbackData("📅 За день",   "Admin@UsersDay"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Лимиты", "Admin@Limits"),
            }
        });
    }

    private InlineKeyboardMarkup BuildFractions()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("ТРК",           "Fractions@ТРК"),
                InlineKeyboardButton.WithCallbackData("МЧС",           "Fractions@МЧС"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Больница",      "Fractions@Больница"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Суд",           "Fractions@Суд"),
                InlineKeyboardButton.WithCallbackData("ВЧ",            "Fractions@ВЧ"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("ФСИН",          "Fractions@ФСИН"),
                InlineKeyboardButton.WithCallbackData("ФСБ",           "Fractions@ФСБ"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Правительство", "Fractions@Прав"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("ДПС",           "Fractions@ДПС"),
                InlineKeyboardButton.WithCallbackData("ППС",           "Fractions@ППС"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🔙 Вернуться",  "MainMenu@Fractions"),
            }
        });
    }

    private InlineKeyboardMarkup BuildServersOnline()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("🌐 Все серверы (без автообновления)", "Servers@ВСЕ") },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("RED ❤️",    "Servers@RED"),
                InlineKeyboardButton.WithCallbackData("YELLOW 💛", "Servers@YELLOW"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("GREEN 💚",  "Servers@GREEN"),
                InlineKeyboardButton.WithCallbackData("AZURE 💙",  "Servers@AZURE"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("SILVER 🩶", "Servers@SILVER"),
                InlineKeyboardButton.WithCallbackData("ROSE 🩷",   "Servers@ROSE"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("BLACK 🖤",  "Servers@BLACK"),
                InlineKeyboardButton.WithCallbackData("SKY 🩵",    "Servers@SKY"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("TITAN 💜",  "Servers@TITAN"),
                InlineKeyboardButton.WithCallbackData("X 💗",      "Servers@X"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("FIRE 🧡",   "Servers@FIRE"),
                InlineKeyboardButton.WithCallbackData("LIME 🍋‍",    "Servers@LIME"),
            },
            new[] { InlineKeyboardButton.WithCallbackData("🔙 Вернуться", "MainMenu@ServersOnline") }
        });
    }

    private InlineKeyboardMarkup BuildReportCheckerServersMenu()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("RED ❤️",    "Servers-RC@RED"),
                InlineKeyboardButton.WithCallbackData("YELLOW 💛", "Servers-RC@YELLOW"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("GREEN 💚",  "Servers-RC@GREEN"),
                InlineKeyboardButton.WithCallbackData("AZURE 💙",  "Servers-RC@AZURE"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("SILVER 🩶", "Servers-RC@SILVER"),
                InlineKeyboardButton.WithCallbackData("ROSE 🩷",   "Servers-RC@ROSE"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("BLACK 🖤",  "Servers-RC@BLACK"),
                InlineKeyboardButton.WithCallbackData("SKY 🩵",    "Servers-RC@SKY"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("TITAN 💜",  "Servers-RC@TITAN"),
                InlineKeyboardButton.WithCallbackData("X 💗",      "Servers-RC@X"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("FIRE 🧡",   "Servers-RC@FIRE"),
                InlineKeyboardButton.WithCallbackData("LIME 🍋‍",    "Servers-RC@LIME"),
            }
        });
    }

    private InlineKeyboardMarkup BuildServersHints()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("RED ❤️",    "Servers-Hints@RED"),
                InlineKeyboardButton.WithCallbackData("YELLOW 💛", "Servers-Hints@YELLOW"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("GREEN 💚",  "Servers-Hints@GREEN"),
                InlineKeyboardButton.WithCallbackData("AZURE 💙",  "Servers-Hints@AZURE"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("SILVER 🩶", "Servers-Hints@SILVER"),
                InlineKeyboardButton.WithCallbackData("ROSE 🩷",   "Servers-Hints@ROSE"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("BLACK 🖤",  "Servers-Hints@BLACK"),
                InlineKeyboardButton.WithCallbackData("SKY 🩵",    "Servers-Hints@SKY"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("TITAN 💜",  "Servers-Hints@TITAN"),
                InlineKeyboardButton.WithCallbackData("X 💗",      "Servers-Hints@X"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("FIRE 🧡",   "Servers-Hints@FIRE"),
                InlineKeyboardButton.WithCallbackData("LIME 🍋‍",    "Servers-Hints@LIME"),
            },
            new[] { InlineKeyboardButton.WithCallbackData("🔙 Вернуться", "ServersHintsMenu@empty") }
        });
    }

    private InlineKeyboardMarkup BuildServersBlocked()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("RED ❤️",    "Servers-Blocked@RED"),
                InlineKeyboardButton.WithCallbackData("YELLOW 💛", "Servers-Blocked@YELLOW"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("SILVER 🩶", "Servers-Blocked@SILVER"),
                InlineKeyboardButton.WithCallbackData("ROSE 🩷",   "Servers-Blocked@ROSE"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("BLACK 🖤",  "Servers-Blocked@BLACK"),
                InlineKeyboardButton.WithCallbackData("SKY 🩵",    "Servers-Blocked@SKY"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("TITAN 💜",  "Servers-Blocked@TITAN"),
                InlineKeyboardButton.WithCallbackData("X 💗",      "Servers-Blocked@X"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("FIRE 🧡",   "Servers-Blocked@FIRE"),
                InlineKeyboardButton.WithCallbackData("AZURE 💙",  "Servers-Blocked@AZURE"),
            },
            new[] { InlineKeyboardButton.WithCallbackData("LIME 🍋‍",       "Servers-Blocked@LIME") },
            new[] { InlineKeyboardButton.WithCallbackData("🔙 Вернуться", "BlacklistMenu@None") }
        });
    }

    private InlineKeyboardMarkup BuildReportCheckerMenu()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("👥 Аккаунты",       "ReportChecker@Accounts"),
                InlineKeyboardButton.WithCallbackData("📜 Проверить жалобы", "ReportChecker@Check"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🔙 Вернуться", "ReportChecker@BackToMainMenu"),
            }
        });
    }

    private InlineKeyboardMarkup BuildDocumentMenu()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🖊️ Подписи и штампы", "DocumentMenu@Firms"),
                InlineKeyboardButton.WithCallbackData("📃 Документы",         "DocumentMenu@Documents"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🔙 Вернуться", "DocumentMenu@BackToMainMenu"),
            }
        });
    }

    private InlineKeyboardMarkup BuildConfirmBackToMainMenu()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Да",  "BackToMainMenu@Да"),
                InlineKeyboardButton.WithCallbackData("Нет", "BackToMainMenu@Нет"),
            }
        });
    }

    private InlineKeyboardMarkup BuildResultFormatChoiceMenu()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("Изображение PNG (водяной знак)", "ResultFormat@Image") },
            new[] { InlineKeyboardButton.WithCallbackData("Документ DOCX",                  "ResultFormat@Document") },
            new[] { InlineKeyboardButton.WithCallbackData("Ссылка",                          "ResultFormat@Links") },
        });
    }

    private InlineKeyboardMarkup BuildServersUpdateMenu()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("⛔ Остановить", "ServersUpdate@Stop") }
        });
    }

    private InlineKeyboardMarkup BuildBackgroundColorHint()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("Прозрачный", "HintsColorBackground@Прозрачный") },
            new[] { InlineKeyboardButton.WithCallbackData("Чёрный",     "HintsColorBackground@Чёрный") },
        });
    }

    private InlineKeyboardMarkup BuildReturnToEventMenu()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("🔙 Вернуться", "EventsMenu@empty") }
        });
    }

    private ReplyKeyboardMarkup BuildStartMenu()
    {
        return new ReplyKeyboardMarkup(new[]
        {
            new KeyboardButton[] { new("🏠 Главное меню") },
            new KeyboardButton[]
            {
                new("📄 Документы"),
                new("🌐 Мониторинг серверов"),
            },
            new KeyboardButton[]
            {
                new("🔔 Чекер жалоб (NEW)"),
                new("🚫 Чёрный список"),
            },
            new KeyboardButton[]
            {
                new("📅 События и мероприятия"),
                new("💡 Подсказки"),
            },
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = false,
            IsPersistent = true
        };
    }

    private ReplyKeyboardMarkup BuildStartMenuWithoutMainButton()
    {
        return new ReplyKeyboardMarkup(new[]
        {
            new KeyboardButton[]
            {
                new("📄 Документы"),
                new("🌐 Мониторинг серверов"),
            },
            new KeyboardButton[]
            {
                new("🔔 Чекер жалоб (NEW)"),
                new("🚫 Чёрный список"),
            },
            new KeyboardButton[]
            {
                new("📅 События и мероприятия"),
                new("💡 Подсказки"),
            },
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = false,
            IsPersistent = true
        };
    }

    private ReplyKeyboardMarkup BuildQuestionsMenu()
    {
        return new ReplyKeyboardMarkup(new[]
        {
            new KeyboardButton[] { new("🏠 Главное меню") },
            new KeyboardButton[] { new("⏪ Вернуться к предыдущему вопросу") },
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = false,
            IsPersistent = true
        };
    }

    private ReplyKeyboardMarkup BuildFirstQuestionMenu()
    {
        return new ReplyKeyboardMarkup(new[]
        {
            new KeyboardButton[] { new("🏠 Главное меню") },
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = false,
            IsPersistent = true
        };
    }

    private ReplyKeyboardMarkup BuildBlacklistReturnMenu()
    {
        return new ReplyKeyboardMarkup(new[]
        {
            new KeyboardButton[] { new("🏠 Домой") },
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = false,
            IsPersistent = true
        };
    }

    private ReplyKeyboardMarkup BuildEventsMenu()
    {
        return new ReplyKeyboardMarkup(new[]
        {
            new KeyboardButton[] { new("🏠 Главное меню") },
            new KeyboardButton[] { new("📋 Показать подписанные") },
            new KeyboardButton[] { new("➕ Подписаться") },
            new KeyboardButton[] { new("❌ Отписаться") },
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = false,
            IsPersistent = true
        };
    }
}