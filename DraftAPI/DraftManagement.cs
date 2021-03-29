using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DraftSimulator;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace ConsoleApp1
{
    public static class DraftManagement
    {
        private static Dictionary<User, Draft> _userDrafts;
        private static List<Draft> _drafts;

        public static bool HasUser(User user)
        {
            return _userDrafts.ContainsKey(user);
        }

        public static void Initialize()
        {
            _userDrafts = new Dictionary<User, Draft>();
            _drafts = new List<Draft>();
        }

        private static void AssertDraftStarted(User user)
        {
            var draft = _userDrafts[user];
            if (draft == null || !draft.Started)
            {
                throw new DraftNotStartedException();
            }
        }
        
        private static void AssertDraftStarted(Chat chat)
        {
            var draft = _drafts.Find(d => d.GroupId == chat.Id);
            if (draft == null || !draft.Started)
            {
                throw new DraftNotStartedException();
            }
        }
        

        #region GroupCommands

        public static async Task ListPlayers(Chat chat, InlineKeyboardMarkup keyboardMarkup = null, int messageId = 0)
        {
            var draft = _drafts.FirstOrDefault(d => d.GroupId == chat.Id);
            if (draft != null)
            {
                var listedPlayers = draft.ListPlayers();
                if (!String.IsNullOrEmpty(listedPlayers))
                {
                    if (messageId == 0 || keyboardMarkup == null)
                    {
                        await TelegramCommunication.SendTextMessageAsync(chat.Id,
                            $"The players that have joined are:\n{listedPlayers}");
                    }
                    else
                    {
                        await TelegramCommunication.EditMessageAsync(chat.Id,
                            $"The players that have joined are:\n{listedPlayers}", messageId, keyboardMarkup);
                    }

                    return;
                }
            }

            await TelegramCommunication.SendTextMessageAsync(chat.Id, $"There are currently no players.");
        }

        public static async Task CreateDraft(Chat chat, int messageId, string set)
        {
            try
            {
                if (_drafts.Find(d => d.GroupId == chat.Id) == null)
                {
                    var draft = new Draft(chat.Id, set);

                    _drafts.Add(draft);
                    var buttons = new List<InlineKeyboardButton>
                    {
                        InlineKeyboardButton.WithCallbackData("Join", "/joindraft"),
                        InlineKeyboardButton.WithCallbackData("Start", "/startdraft")
                    };

                    var replyMarkup = new InlineKeyboardMarkup(buttons);

                    await TelegramCommunication.SendTextMessageAsync(chat.Id,
                        $"Draft Created", messageId, keyboardMarkup: replyMarkup);
                }
                else
                {
                    await TelegramCommunication.SendTextMessageAsync(chat.Id, "There's already a draft for this chat.");
                }
            }
            catch (InexistentSetException)
            {
                await TelegramCommunication.SendTextMessageAsync(chat.Id, "Couldn't find the specified set or fetch it with the API.");
            }
        }

        public static async Task StartDraft(Chat chat, int messageId)
        {
            var draft = _drafts.Find(d => d.GroupId == chat.Id);

            if (draft != null)
            {
                draft.StartDraft();

                await TelegramCommunication.SendTextMessageAsync(chat.Id,
                    $"Draft Started", messageId);
            }
            else
            {
                await TelegramCommunication.SendTextMessageAsync(chat.Id, "There's no draft for this chat yet.");
            }
        }

        public static async Task StopDraft(Chat chat)
        {
            AssertDraftStarted(chat);
            if (_drafts.Remove(_drafts.FirstOrDefault(d => d.GroupId == chat.Id)))
            {
                await TelegramCommunication.SendTextMessageAsync(chat.Id, "Draft Removed.");
            }
            else
                await TelegramCommunication.SendTextMessageAsync(chat.Id, "There is currently no draft for this chat.");
        }

        public static async Task JoinDraft(Chat chat, User user, int messageId,
            InlineKeyboardMarkup keyboardMarkup = null)
        {
            var draft = _drafts.FirstOrDefault(d => d.GroupId == chat.Id);
            if (draft != null)
            {
                try
                {
                    draft.AddPlayer($"{user.FirstName} {user.LastName}", user.Id);
                    await TelegramCommunication.EditMessageAsync(chat.Id,
                        $"User {user.FirstName} {user.LastName} joined the draft!",
                        messageId);
                    await TelegramCommunication.SendTextMessageAsync(user.Id,
                        $"You joined a draft at {chat.Title}!");
                    _userDrafts.Add(user, draft);
                    await ListPlayers(chat, keyboardMarkup, messageId);
                }
                catch
                {
                    await TelegramCommunication.SendTextMessageAsync(chat.Id, $"Could not join any draft", messageId);
                }
            }
        }

        #endregion

        #region PrivateCommands

        public static async Task ListCurrentPool(User user)
        {
            AssertDraftStarted(user);
            var draft = _userDrafts[user];
            if (draft != null)
            {
                var pool = draft.ListPool(user.Id);
                await TelegramCommunication.SendTextMessageAsync(user.Id, $"{pool}");
                return;
            }

            await TelegramCommunication.SendTextMessageAsync(user.Id, $"Could not list your pool");
        }


        public static async Task PickCard(User user, int messageId, int cardIndex)
        {
            AssertDraftStarted(user);
            var draft = _userDrafts[user];
            if (draft != null)
            {
                try
                {
                    var pickResult = draft.Pick(user.Id, cardIndex);
                    var pool = draft.ListPool(user.Id);
                    if (messageId == 0)
                    {
                        await TelegramCommunication.SendTextMessageAsync(user.Id, $"{pickResult}\n\n{pool}");
                    }
                    else
                    {
                        await TelegramCommunication.EditMessageAsync(user.Id, $"{pickResult}\n\n{pool}", messageId);
                    }

                    return;
                }
                catch (Exception)
                {
                    return;
                }
            }

            await TelegramCommunication.SendTextMessageAsync(user.Id, $"Could not make a pick.");
        }

        public static async Task ListCurrentPack(Chat chat, User user)
        {
            AssertDraftStarted(user);
            var draft = _userDrafts[user];
            if (draft != null)
            {
                var pack = draft.ListPack(user.Id);
                if (pack.Contains("You cannot see the pack yet."))
                {
                    await TelegramCommunication.SendTextMessageAsync(chat.Id, $"{pack}");
                }

                var cardNames = pack.Split(";;", StringSplitOptions.RemoveEmptyEntries);
                pack = pack.Replace(";;", "");
                var buttonList = new List<List<InlineKeyboardButton>>
                {
                    new List<InlineKeyboardButton>()
                };
                int cardIndex = 0;
                int buttonListIndex = 0;
                foreach (var name in cardNames)
                {
                    buttonList[buttonListIndex]
                        .Add(InlineKeyboardButton.WithCallbackData($"{cardIndex}", $"/pick {cardIndex}"));
                    cardIndex++;
                    if (cardIndex % 7 == 0)
                    {
                        buttonListIndex++;
                        buttonList.Add(new List<InlineKeyboardButton>());
                    }
                }

                InlineKeyboardMarkup replyMarkup = new InlineKeyboardMarkup(buttonList);
                await TelegramCommunication.SendTextMessageAsync(chat.Id, $"{pack}", keyboardMarkup: replyMarkup);
                return;
            }

            await TelegramCommunication.SendTextMessageAsync(chat.Id, $"Could not list your pack.");
        }

        #endregion
    }
}