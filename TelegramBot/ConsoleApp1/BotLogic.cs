using System.Globalization;
using Telegram.Bot;
using Telegram.Bot.Args;
using Newtonsoft.Json;
using ConsoleApp1;
using ConsoleApp1.Properties;

public class BotLogic
{
    static string getCurrenciesCommandString = Resources.getCurrenciesCommandString;
    static string startCommandString = Resources.startCommandString;
    static string startMessage = Resources.startMessage;
    static string dateInvalidMessage = Resources.dateInvalidMessage;
    static string dateInvalidPeriodMessage = Resources.dateInvalidPeriodMessage;
    static string invalidCurrencyCodeMessage = Resources.invalidCurrencyCodeMessage;
    static string invalidFormatMessage = Resources.invalidFormatMessage;

    private HttpClient httpClient;
    private static readonly HttpClient client = new HttpClient();
    private static readonly TelegramBotClient bot = new TelegramBotClient(Configuration.LoadConfiguration().BotToken);
    private CurrencyService currencyService;

    public BotLogic(HttpClient httpClient)
    {
        this.httpClient = httpClient;
        this.currencyService = new CurrencyService(httpClient);
    }

    public BotLogic()
    {
        this.currencyService = new CurrencyService(client);
    }

    public void InitializeBot()
    {
        bot.OnMessage += Bot_OnMessage;
        bot.StartReceiving();
        Console.ReadLine();
        bot.StopReceiving();
    }

    private async void Bot_OnMessage(object sender, MessageEventArgs e)
    {
        if (e.Message.Text != null)
        {
            Console.WriteLine($"Received a text message in chat {e.Message.Chat.Id}.");

            if (e.Message.Text == getCurrenciesCommandString)
            {
                string availableCurrencies = await currencyService.GetAvailableCurrencies();

                await bot.SendTextMessageAsync(chatId: e.Message.Chat, text: availableCurrencies);
                return;
            }

            if (e.Message.Text == startCommandString)
            {
                await bot.SendTextMessageAsync(chatId: e.Message.Chat,
                                               text: startMessage);
                return;
            }

            string[] messageParts = e.Message.Text.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (messageParts.Length == 2)
            {
                string currencyCode = messageParts[0].ToUpper();
                string date = messageParts[1];

                // validate the date format
                if (!DateTime.TryParseExact(date, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
                {
                    await bot.SendTextMessageAsync(chatId: e.Message.Chat,
                                                   text: dateInvalidMessage);
                    return;
                }

                // Check if the date is earlier than 4 years from now
                var fourYearsAgo = DateTime.Now.AddYears(-4);
                if (parsedDate.CompareTo(fourYearsAgo) < 0)
                {
                    await bot.SendTextMessageAsync(chatId: e.Message.Chat,
                                                   text: dateInvalidPeriodMessage);
                    return;
                }

                string exchangeRate = await currencyService.GetExchangeRates(currencyCode, date);

                if (exchangeRate == null)
                {
                    await bot.SendTextMessageAsync(chatId: e.Message.Chat,
                                                   text: invalidCurrencyCodeMessage);
                }
                else
                {
                    await bot.SendTextMessageAsync(chatId: e.Message.Chat,
                                                   text: $"The exchange rate for {currencyCode} on {date} was {exchangeRate}");
                }
            }
            else
            {
                await bot.SendTextMessageAsync(chatId: e.Message.Chat,
                                               text: invalidFormatMessage);
            }
        }
    }
}