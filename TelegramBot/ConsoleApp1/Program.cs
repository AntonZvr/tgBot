using System;
using System.Net.Http;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Newtonsoft.Json;

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

            string[] messageParts = e.Message.Text.Split(' ');
            if (messageParts.Length == 2)
            {
                string currencyCode = messageParts[0];
                string date = messageParts[1];

                string exchangeRate = await GetExchangeRate(currencyCode, date);

                if (exchangeRate != null)
                {
                    await bot.SendTextMessageAsync(
                        chatId: e.Message.Chat,
                        text: $"The exchange rate for {currencyCode} on {date} was {exchangeRate}"
                    );
                }
                else
                {
                    await bot.SendTextMessageAsync(
                        chatId: e.Message.Chat,
                        text: "No data available for this currency and date. Please enter a valid currency code and date."
                    );
                }
            }
            else
            {
                await bot.SendTextMessageAsync(
                    chatId: e.Message.Chat,
                    text: "Please send a message in the format: <currency code> <date>"
                );
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
            foreach (var rate in data.exchangeRate)
            {
                if (rate.currency == currencyCode)
                {
                    return rate.saleRateNB;
                }
            }
        }
        catch (HttpRequestException)
        {
            // Handle the exception and return an error message
            return "An error occurred while retrieving the exchange rate. Check wtitten currency code and date and try again";
        }

        return null;
    }

}
