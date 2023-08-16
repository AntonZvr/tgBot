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
    static string exchangeRateAPIFailMessage = Resources.exchangeRateAPIFailMessage;

    private HttpClient httpClient;
    private static readonly HttpClient client = new HttpClient();
    private static readonly TelegramBotClient bot = new TelegramBotClient(Configuration.LoadConfiguration().BotToken);

    public BotLogic(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public BotLogic()
    {
       
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
        CurrencyService currencyService = new CurrencyService(httpClient);
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

                string exchangeRate = await GetExchangeRates(currencyCode, date);

                if (exchangeRate == null)
                {
                    await bot.SendTextMessageAsync(chatId: e.Message.Chat,
                                                   text: invalidCurrencyCodeMessage);
                }
                else if (exchangeRate != null)
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

    public async Task<string> GetExchangeRates(string currencyCode, string date)
    {
        try
        {
            HttpResponseMessage response = await client.GetAsync($"https://api.privatbank.ua/p24api/exchange_rates?date={date}");
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            // Deserialize JSON response to dynamic object
            dynamic data = JsonConvert.DeserializeObject(responseBody);

            // Deserialize exchangeRate to List<ExchangeRate>
            List<ExchangeRate> exchangeRates = JsonConvert.DeserializeObject<List<ExchangeRate>>(data.exchangeRate.ToString());

            // Get the exchange rate for the specified currency code
            var exchangeRate = exchangeRates.FirstOrDefault(r => r.Currency == currencyCode);

            if (exchangeRate != null)
            {
                return exchangeRate.SaleRateNB;
            }
            else
            {
                return null;
            }
        }
        catch (HttpRequestException)
        {
            return exchangeRateAPIFailMessage;
        }
    }
}
