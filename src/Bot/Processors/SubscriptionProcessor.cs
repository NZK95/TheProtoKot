using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

internal sealed partial class TelegramBot
{
    private async Task<bool> IsUserSubscribed(long chatId)
    {
        var member = await _bot.GetChatMember(TelegramConstants.BotChannelName, chatId);

        return member.Status == ChatMemberStatus.Member ||
                      member.Status == ChatMemberStatus.Administrator ||
                      member.Status == ChatMemberStatus.Creator;
    }

    private async Task<bool> CheckSubscribe(UserSession session)
    {
        if (await IsUserSubscribed(session.ChatId) == false)
        {
            var keyboardSubscribe = new InlineKeyboardButton[][]
            {
                        new InlineKeyboardButton[]
                        {
                            InlineKeyboardButton.WithUrl("Подписаться на канал", "https://t.me/TheProtoKot")
                        }
            };

            await SendMessageAsync("<b>❗️ Для использования бота необходимо подписаться на наш канал.</b>\n\nПосле подписки, повторите попытку.", session.ChatId, keyboardSubscribe);
            return false;
        }

        return true;
    }
}
