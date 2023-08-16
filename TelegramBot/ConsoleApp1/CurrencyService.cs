using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using ConsoleApp1;

public class CurrencyService
{
    private static readonly HttpClient client = new HttpClient();

    public static async Task<string> GetAvailableCurrencies()
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
}
