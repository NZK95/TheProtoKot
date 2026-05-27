using System.ComponentModel;
using System.Text;
using OfficeOpenXml;
using OfficeOpenXml.Configuration;

namespace AmazingBot
{
    internal sealed class Program
    {
        static void Main(string[] args)
        {
            ExcelPackage.License.SetNonCommercialPersonal("TheProtoKot"); 
            SQLitePCL.Batteries.Init();

            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;

            FileService.ClearDataAtStart();

            Host.CreateDefaultBuilder(args).ConfigureServices((context, services) =>
                {
                    var token = context.Configuration["Telegram:BotToken"];

                    if (string.IsNullOrWhiteSpace(token))
                        throw new InvalidOperationException(
                            "Telegram BotToken is missing in appsettings.json");

                    services.Configure<TelegramOptions>(
                        context.Configuration.GetSection("Telegram"));

                    services.Configure<ImageServiceOptions>(
                        context.Configuration.GetSection("Image-Hosting"));

                    services.Configure<ServersServiceOptions>(
                        context.Configuration.GetSection("ServersApi"));

                    services.Configure<BlacklistTablesOptions>(context.Configuration);

                    services.AddHostedService<Worker>();

                }).Build().Run();
        }
    }
}
