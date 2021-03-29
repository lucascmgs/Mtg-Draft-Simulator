using System;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace ConsoleApp1
{
    public static class TelegramCommunication
    {
        private static ITelegramBotClient botClient;


        public static void Initialize()
        {
            botClient = new TelegramBotClient("1715918494:AAHpauQ1dU5AL3FdmPKT8fUgkkHjU6onMYY");

            botClient.OnMessage += OnMessage;
            botClient.OnCallbackQuery += OnCallbackQuery;
            botClient.StartReceiving();
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();

            botClient.StopReceiving();
        }


        static async void OnCallbackQuery(object sender, CallbackQueryEventArgs e)
        {
            Console.WriteLine(e.CallbackQuery.Data);
            var data = e.CallbackQuery.Data;
            var chat = e.CallbackQuery.Message.Chat;
            var user = e.CallbackQuery.From;
            var messageId = e.CallbackQuery.Message.MessageId;

            var markup = e.CallbackQuery.Message.ReplyMarkup;
            
            string[] dataParts;
            try
            {
                dataParts = data.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
            }
            catch (Exception ex)
            {
                return;
            }

            switch (dataParts[0])
            {
                case "/joindraft":
                    await DraftManagement.JoinDraft(chat, user, messageId, markup);
                    break;
                case "/startdraft":
                    await DraftManagement.StartDraft(chat, messageId);
                    break;
                case "/pick":
                    int cardNumber;
                    try
                    {
                        cardNumber = Int32.Parse(dataParts[1]);
                    }
                    catch
                    {
                        cardNumber = 0;
                    }

                    await DraftManagement.PickCard(user, messageId, cardNumber);
                    break;
            }
        }

        static async void OnMessage(object sender, MessageEventArgs e)
        {
            var user = e.Message.From;
            var chat = e.Message.Chat;
            var text = e.Message.Text.Replace("@MtgDraftBot", "");
            var messageId = e.Message.MessageId;

            Console.WriteLine(text);
            string[] messageParts;
            try
            {
                messageParts = text.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
            }
            catch (Exception ex)
            {
                return;
            }

            try
            {
                switch (messageParts[0])
                {
                    case "/createdraft":
                        AssertChatContext(chat, ChatType.Group);
                        string set = "";
                        try
                        {
                            set = messageParts[1];
                        }
                        catch
                        {
                            set = "war";
                            await SendTextMessageAsync(chat,
                                $"Couldn't parse the set. Creating draft with the default set: {set}");
                        }

                        await DraftManagement.CreateDraft(chat, messageId, set);
                        break;

                    case "/startdraft":
                        AssertChatContext(chat, ChatType.Group);


                        await DraftManagement.StartDraft(chat, messageId);
                        break;

                    case "/stopdraft":
                        AssertChatContext(chat, ChatType.Group);
                        await DraftManagement.StopDraft(chat);
                        break;

                    case "/joindraft":
                        AssertChatContext(chat, ChatType.Group);
                        await DraftManagement.JoinDraft(chat, user, messageId);
                        break;

                    case "/listplayers":
                        await DraftManagement.ListPlayers(chat);
                        break;

                    case "/pick":
                        AssertChatContext(chat, ChatType.Private);
                        AssertUserJoined(user);
                        int pickIndex;
                        try
                        {
                            pickIndex = Int32.Parse(messageParts[1]);
                        }
                        catch
                        {
                            pickIndex = 0;
                        }

                        await DraftManagement.PickCard(user, pickIndex, 0);
                        break;

                    case "/listpack":
                        AssertChatContext(chat, ChatType.Private);
                        AssertUserJoined(user);

                        await DraftManagement.ListCurrentPack(chat, user);
                        break;

                    case "/listpool":
                        AssertChatContext(chat, ChatType.Private);
                        AssertUserJoined(user);

                        await DraftManagement.ListCurrentPool(user);
                        break;
                }
            }
            catch (ChatTypeDivergentException ex)
            {
                if (chat.Type == ChatType.Private)
                {
                    await SendTextMessageAsync(chat.Id, $"Please send this command in the draft group.", messageId);
                }

                if (chat.Type == ChatType.Group)
                {
                    await SendTextMessageAsync(chat.Id, $"Please send this command in a private chat with the bot.",
                        messageId);
                }
            }
            catch (UserNotJoinedException ex)
            {
                await SendTextMessageAsync(user.Id, $"Please join a draft in a group first", messageId);
            }
            catch (DraftNotStartedException ex)
            {
                await SendTextMessageAsync(user.Id, $"Please start a draft first", messageId);
            }
        }

        private static void AssertChatContext(Chat chat, ChatType chatType)
        {
            if (chat.Type != chatType)
            {
                throw new ChatTypeDivergentException();
            }
        }

        private static void AssertUserJoined(User user)
        {
            if (!DraftManagement.HasUser(user))
            {
                throw new UserNotJoinedException();
            }
        }

        


        public static async Task SendTextMessageAsync(ChatId chatId, string text, int messageId = 0,
            InlineKeyboardMarkup keyboardMarkup = null)
        {
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: text,
                replyToMessageId: messageId,
                replyMarkup: keyboardMarkup,
                parseMode: ParseMode.Markdown
            );
            
        }
        
        public static async Task EditMessageAsync(ChatId chatId, string text, int messageId = 0,
            InlineKeyboardMarkup keyboardMarkup = null)
        {
            await botClient.EditMessageTextAsync(
                chatId: chatId,
                messageId: messageId,
                text: text,
                replyMarkup: keyboardMarkup
            );
        }
    }

    internal class DraftNotStartedException : Exception
    {
    }

    public class ChatTypeDivergentException : Exception
    {
    }

    public class UserNotJoinedException : Exception
    {
    }
}