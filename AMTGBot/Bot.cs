using NLog;
using NLog.Config;
using System;
using System.IO;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.InputFiles;

namespace AMTGBot
{
    public class Bot
    {
        public ITelegramBotClient BotClient { get; }
        public BotConfig BotConfig { get; }
        public ILatexRenderer LatexRenderer { get; }
        public ILogger Logger { get; }

        public Bot(ITelegramBotClient botClient, BotConfig botConfig, ILatexRenderer latexRenderer, ILogger logger)
        {
            BotClient = botClient;
            BotConfig = botConfig;
            LatexRenderer = latexRenderer;
            Logger = logger;
        }

        public async void SendSingleInlineQueryAnswer(string queryId, InlineQueryResultBase baseResult)
        {
            try
            {
                await BotClient.AnswerInlineQueryAsync(queryId, new[] { baseResult });
            }
            catch (Telegram.Bot.Exceptions.BadRequestException e)
            {
                Logger.Warn(e, "Can't send single inline query answer");
            }
        }

        public async Task<InlineQueryResultBase> TrySendPhoto(Stream stream, string input, string stringFormat)
        {
            var telegramFile = new InputOnlineFile(stream);
            try
            {
                var sendRendered = await BotClient.SendPhotoAsync(BotConfig.PhotoStorageChatId, telegramFile);
                return new InlineQueryResultCachedPhoto(
                    id: "0",
                    photoFileId: sendRendered.Photo[0].FileId
                );
            }
            catch
            {
                InlineQueryResultArticle result = new(
                    id: "0",
                    title: "String result (cannot render to image)",
                    inputMessageContent: new InputTextMessageContent("Input \n" + input + "\n\nOutput \n" + stringFormat)
                )
                { Description = stringFormat };

                return result;
            }
        }
    }
}
