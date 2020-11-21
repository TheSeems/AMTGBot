using AngouriMath.Extensions;
using System;
using Telegram.Bot;
using Telegram.Bot.Args;
using System.IO;
using Telegram.Bot.Types.InlineQueryResults;
using Newtonsoft.Json;
using System.Threading.Tasks;
using AngouriMath;
using NLog;
using NLog.Config;

namespace AMTGBot
{
    internal sealed class Program
    {
        private static Bot bot;

        private static BotConfig LoadConfig()
        {
            var configPath = Environment.CurrentDirectory + Path.DirectorySeparatorChar + "config.json";
            return JsonConvert.DeserializeObject<BotConfig>(File.ReadAllText(configPath));
        }

        private static void Main(string[] args)
        {
            Console.WriteLine("Loading config...");
            var botConfig = LoadConfig();

            Console.WriteLine("Booting up logging...");
            LogManager.Configuration = new XmlLoggingConfiguration("NLog.config");
            var logger = LogManager.GetCurrentClassLogger();

            Console.WriteLine("Loading bot...");
            var botClient = new TelegramBotClient(botConfig.Token);
            bot = new(botClient, botConfig, new CSharpMathRenderer(), logger);

            var me = botClient.GetMeAsync().Result;
            Console.WriteLine($"Authorized as {me.Username} (#{me.Id}).");
            Console.WriteLine("Booting functional..." + Environment.NewLine);

            botClient.OnInlineQuery += OnInlineHandlerTimeout;

            Console.WriteLine("Listening...");
            botClient.StartReceiving();

            Console.WriteLine("Press any key to stop.");
            Console.ReadKey();

            botClient.StopReceiving();
        }

        private static async void OnInlineHandlerTimeout(object sender, InlineQueryEventArgs e)
        {
            var task = Task.Run(() => OnInlineHandler(sender, e));
            if (await Task.WhenAny(task, Task.Delay(bot.BotConfig.ComputationTimeLimit)) != task)
            {
                InlineQueryResultArticle result = new(
                    id: "0",
                    title: "Computation time exceeded",
                    inputMessageContent: new InputTextMessageContent("Computation time exceeded: " + e.InlineQuery.Query)
                );

                bot.SendSingleInlineQueryAnswer(e.InlineQuery.Id, result);
                bot.Logger.Info($"Computation time exceeded for query '{e.InlineQuery.Query}'");
            }
        }

        private static async void OnInlineHandler(object sender, InlineQueryEventArgs e)
        {
            InlineQueryResultBase baseResult;
            try
            {
                Entity calculated = e.InlineQuery.Query.Solve("x");
                if (calculated.Complexity < bot.BotConfig.SimplifyComplexityThreshold)
                    calculated = calculated.Simplify();

                using var stream = bot.LatexRenderer.Render(@"Input: " + e.InlineQuery.Query.Latexise() + @"\\\\" + calculated.Latexise());
                baseResult = await bot.TrySendPhoto(stream, e.InlineQuery.Query, calculated.Stringize());
            }
            catch (Exception ex)
            {
                baseResult = new InlineQueryResultArticle(
                    id: "0",
                    title: "We can't process your request.",
                    inputMessageContent: new InputTextMessageContent(ex.Message + ": " + e.InlineQuery.Query)
                )
                { Description = ex.Message };

                bot.Logger.Info(ex, $"Can't evaluate query {e.InlineQuery.Query}: {ex.Message}");
            }

            bot.SendSingleInlineQueryAnswer(e.InlineQuery.Id, baseResult);
        }
    }
}
