// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.Configuration;
using TwitchAdDetect;
using TwitchAdDetect.Helper;

IConfiguration config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()
    .Build();

var settings = config.GetRequiredSection("BotSettings").Get<BotSettings>();
var api = new TwitchApiClient(settings);
var bot = new AdBot(settings, api);

var input = Console.ReadLine();
while (input == null || input.ToLower() != "exit")
{
    if (input != null && input.ToLower() != "exit")
    {
        bot.SendMessage(input);
    }
    input = Console.ReadLine();
}