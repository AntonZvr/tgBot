using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Globalization;
using Telegram.Bot;
using Telegram.Bot.Args;
using Newtonsoft.Json;
using System.Linq;

class Program
{
    private static readonly HttpClient client = new HttpClient();
    private static readonly TelegramBotClient bot = new TelegramBotClient("6500634051:AAFU_2CnX8GQIDTsnUYwe5QLC_LPsZBxC6o");

    static void Main()
    {
        bot.OnMessage += Bot_OnMessage;
        bot.StartReceiving();
        Console.ReadLine();
        bot.StopReceiving();
    }

    private static async void Bot_OnMessage(object sender, MessageEventArgs e)
    {
        if (e.Message.Text != null)
        {
            Console.WriteLine($"Received a text message in chat {e.Message.Chat.Id}.");

            if (e.Message.Text == "/get_currencies")
            {
                string availableCurrencies = await GetAvailableCurrencies();

                await bot.SendTextMessageAsync(chatId: e.Message.Chat, text: availableCurrencies);
                return;
            }

            if (e.Message.Text == "/start")
            {
                await bot.SendTextMessageAsync(chatId: e.Message.Chat,
                                                   text: "Hello! Please send a message in the format: <currency code> <date>. For example: USD 01.01.2022. The command to see a list of all currencies - /get_currencies");
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
                                                   text: "The date format is incorrect. Correct format is dd.MM.yyyy (e.g., 01.01.2020)");
                    return;
                }

                // Check if the date is earlier than 4 years from now
                var fourYearsAgo = DateTime.Now.AddYears(-4);
                if (parsedDate.CompareTo(fourYearsAgo) < 0)
                {
                    await bot.SendTextMessageAsync(chatId: e.Message.Chat,
                                                   text: "You can't request a date earlier than 4 years from now.");
                    return;
                }

                string exchangeRate = await GetExchangeRate(currencyCode, date);

                if (exchangeRate == null)
                {
                    await bot.SendTextMessageAsync(chatId: e.Message.Chat,
                                                   text: "No data available for this currency. Please enter a valid currency code. For example: USD.");
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
                                               text: "Please send a message in the format: <currency code> <date>");
            }
        }
    }

    private static async Task<string> GetExchangeRate(string currencyCode, string date)
    {
        try
        {
            HttpResponseMessage response = await client.GetAsync($"https://api.privatbank.ua/p24api/exchange_rates?json&date={date}");
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            dynamic data = JsonConvert.DeserializeObject(responseBody);

            // Deserializing dynamic object to list of ExchangeRate
            List<ExchangeRate> exchangeRates = JsonConvert.DeserializeObject<List<ExchangeRate>>(JsonConvert.SerializeObject(data.exchangeRate));

            // 2. Validate the currency code
            var targerCurrency = currencyCode;
            Console.WriteLine(targerCurrency);
            List<ExchangeRate> matchingCurrency = exchangeRates.FindAll(currency => currency.Currency == targerCurrency);

            Console.WriteLine(matchingCurrency);
            foreach (ExchangeRate c in matchingCurrency)
            {
                Console.WriteLine(c.SaleRateNB, c.Currency);
                return c.SaleRateNB;
            }

            return null;
        }
        catch (HttpRequestException)
        {
            return "An error occurred while retrieving the exchange rate. Check the written currency code and date, then try again";
        }
    }

    private static async Task<string> GetAvailableCurrencies()
    {
        try
        {
            HttpResponseMessage response = await client.GetAsync("https://api.privatbank.ua/p24api/exchange_rates?date=01.12.2014");
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            // Deserialize JSON response to dynamic object
            dynamic data = JsonConvert.DeserializeObject(responseBody);

            // Deserialize exchangeRate to List<ExchangeRate>
            List<ExchangeRate> exchangeRates = JsonConvert.DeserializeObject<List<ExchangeRate>>(data.exchangeRate.ToString());

            // Get all available currencies with LINQ
            var availableCurrencies = exchangeRates.Select(r => r.Currency).Distinct();

            // Construct response message
            string message = "Available currencies are:";
            foreach (var currency in availableCurrencies)
            {
                message += "\n" + currency;
            }

            return message;
        }
        catch (HttpRequestException)
        {
            return "An error occurred while retrieving the available currencies. Please try again later.";
        }
    }


    public class ExchangeRate
    {
        public string Currency { get; set; }
        public string SaleRateNB { get; set; }
    }
}
