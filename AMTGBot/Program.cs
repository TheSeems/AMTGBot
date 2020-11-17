using AngouriMath.Extensions;
using System;
using Telegram.Bot;
using Telegram.Bot.Args;
using System.IO;
using Telegram.Bot.Types.InputFiles;
using AngouriMath;
using PeterO.Numbers;
using Telegram.Bot.Types.InlineQueryResults;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace AMTGBot
{
    class Program
    {
        static ITelegramBotClient botClient;
        public static ILatexRenderer Renderer { get; private set; }
        public static BotConfig Config { get; private set; }

        private static void LoadConfig()
        {
            var configPath = Environment.CurrentDirectory + Path.DirectorySeparatorChar + "config.json";
            Config = JsonConvert.DeserializeObject<BotConfig>(File.ReadAllText(configPath));
            Renderer = new CSharpMathRenderer();
        }

        private static void Main(string[] args)
        {
            Console.WriteLine("Loading config...");
            LoadConfig();

            Console.WriteLine("Loading bot...");
            botClient = new TelegramBotClient(Config.Token);

            var me = botClient.GetMeAsync().Result;
            Console.WriteLine($"Authorized as {me.Username} (#{me.Id}).");
            Console.WriteLine("Booting functional...");

            botClient.OnInlineQuery += OnInlineHandlerTimeout;
            MathS.Settings.DecimalPrecisionContext.Global(new(10, ERounding.HalfUp, -10, 10, false));

            Console.WriteLine("Booted up.");
            botClient.StartReceiving();

            Console.WriteLine("Press any key to exit");
            Console.ReadKey();

            botClient.StopReceiving();
        }

        private static async Task<InlineQueryResultBase> TrySendPhoto(Stream stream, string stringFormat)
        {
            var telegramFile = new InputOnlineFile(stream);
            try
            {
                var sendRendered = await botClient.SendPhotoAsync(Config.PhotoStorageChatId, telegramFile);
                return new InlineQueryResultCachedPhoto(
                    id: "0",
                    photoFileId: sendRendered.Photo[0].FileId
                );
            }
            catch (Exception)
            {
                return new InlineQueryResultArticle(
                    id: "0",
                    title: "String result (cannot render to image)",
                    inputMessageContent: new InputTextMessageContent(stringFormat)
                );
            }
        }

        static async void OnInlineHandlerTimeout(object sender, InlineQueryEventArgs e)
        {
            var task = Task.Run(() => OnInlineHandler(sender, e));
            if (await Task.WhenAny(task, Task.Delay(Config.ComputationTimeLimit)) != task)
            {
                var result = new InlineQueryResultArticle(
                    id: "0",
                    title: "Computation time exceeded",
                    inputMessageContent: new InputTextMessageContent("Computation time exceeded: " + e.InlineQuery.Query)
                );

                await botClient.AnswerInlineQueryAsync(e.InlineQuery.Id, new[] { result });
            }
        }

        static async void OnInlineHandler(object sender, InlineQueryEventArgs e)
        {
            InlineQueryResultBase baseResult;
            try
            {
                var calculated = e.InlineQuery.Query.Solve("x").Simplify();
                using var stream = Renderer.Render(calculated.Latexise());
                baseResult = await TrySendPhoto(stream, calculated.Stringize());
            }
            catch (Exception ex)
            {
                baseResult = new InlineQueryResultArticle(
                    id: "0",
                    title: ex.Message,
                    inputMessageContent: new InputTextMessageContent(ex.Message)
                );
            }

            await botClient.AnswerInlineQueryAsync(e.InlineQuery.Id, new[] { baseResult });
        }
    }
}
