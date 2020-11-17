using AngouriMath.Extensions;
using CSharpMath.Rendering.FrontEnd;
using System;
using Telegram.Bot;
using Telegram.Bot.Args;
using CSharpMath.SkiaSharp;
using System.IO;
using Telegram.Bot.Types.InputFiles;
using AngouriMath;
using PeterO.Numbers;
using SkiaSharp;
using System.Linq;
using Telegram.Bot.Types.InlineQueryResults;

namespace AMTGBot
{
    class Program
    {
        static ITelegramBotClient botClient;

        static void Main(string[] args)
        {
            Console.WriteLine("Loading bot...");
            botClient = new TelegramBotClient("1407004510:AAHdTLlFUWB1CByXMHu6YHnodMKCcg4PA1k");

            var me = botClient.GetMeAsync().Result;
            Console.WriteLine($"Authorized as {me.Username} (#{me.Id}). Booting functional.");

            botClient.OnInlineQuery += Bot_OnMessage;
            MathS.Settings.DecimalPrecisionContext.Global(new(10, ERounding.HalfUp, -10, 10, false));

            Console.WriteLine("Booted up.");
            botClient.StartReceiving();

            Console.WriteLine("Press any key to exit");
            Console.ReadKey();

            botClient.StopReceiving();
        }

        static void WriteToFile(Stream stream, string destinationfile, bool append = true, int bufferSize = 4096)
        {
            using (var destinationFileStream = new FileStream(destinationfile, FileMode.OpenOrCreate))
            {
                while (stream.Position < stream.Length)
                {
                    destinationFileStream.WriteByte((byte)stream.ReadByte());
                }
            }
        }

        static async void Bot_OnMessage(object sender, InlineQueryEventArgs e)
        {
            try
            {
                var calculated = e.InlineQuery.Query.Solve("x");
                var path = System.IO.Path.GetTempFileName();
                var painter = new MathPainter { LaTeX = calculated.Simplify().Latexise() };

                var measures = painter.Measure();
                int border = 50;

                SKBitmap bitMap = new SKBitmap(
                    width: ((int)measures.Width) + 2 * border,
                    height: ((int)measures.Height) + 2 * border
                );

                var canvas = new SKCanvas(bitMap);

                painter.Draw(canvas, new SKPoint(border, measures.Height / 2.0f + border));
                canvas.Flush();

                using (var image = SKImage.FromBitmap(bitMap))
                using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
                {
                    // save the data to a stream
                    using (var streamf = File.OpenWrite(path))
                    {
                        data.SaveTo(streamf);
                    }
                }
                Console.WriteLine(path);


                using var stream = File.Open(path, FileMode.Open);
                var telegramFile = new InputOnlineFile(stream);

                var result = await botClient.SendPhotoAsync(-480623756, telegramFile);

                try
                {
                    var res = new InlineQueryResultCachedPhoto("0", result.Photo[0].FileId);
                    await botClient.AnswerInlineQueryAsync(e.InlineQuery.Id, new[] { res });
                }
                catch (Exception ignored)
                {
                    var res = new InlineQueryResultArticle("0", "String result (cannot Latexise to image)", new InputTextMessageContent(calculated.Stringize()));
                    await botClient.AnswerInlineQueryAsync(e.InlineQuery.Id, new[] { res });
                }

                stream.Close();

                File.Delete(path);

            }
            catch (Exception ex)
            {
                var res = new InlineQueryResultArticle("0", "Error handling request", new InputTextMessageContent(ex.Message));
                try
                {
                    await botClient.AnswerInlineQueryAsync(e.InlineQuery.Id, new[] { res });
                }
                catch (Exception ee) { }
            }
        }
    }
}
