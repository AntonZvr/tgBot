using Newtonsoft.Json;
using ConsoleApp1;
using ConsoleApp1.Properties;

public class CurrencyService
{
    static string availableCurrenciesMessage = Resources.availableCurrenciesMessage;
    static string apiServiceFailMessage = Resources.apiServiceFailMessage;
    static string exchangeRateAPIFailMessage = Resources.exchangeRateAPIFailMessage;
    static string resultFromCacheMessage = Resources.resultFromCacheMessage;

    private CacheService cacheService = new CacheService();
    private readonly HttpClient _client;

    public CurrencyService(HttpClient client)
    {
        _client = client;
    }

    public async Task<string> GetAvailableCurrencies()
    {
        try
        {
            HttpResponseMessage response = await _client.GetAsync("https://api.privatbank.ua/p24api/exchange_rates?date=01.12.2014");
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            dynamic data = JsonConvert.DeserializeObject(responseBody);

            List<ExchangeRate> exchangeRates = JsonConvert.DeserializeObject<List<ExchangeRate>>(data.exchangeRate.ToString());

            var availableCurrencies = exchangeRates.Select(r => r.Currency).Distinct();

            string message = availableCurrenciesMessage;
            foreach (var currency in availableCurrencies)
            {
                message += "\n" + currency;
            }

            return message;
        }
        catch (HttpRequestException)
        {
            return apiServiceFailMessage;
        }
    }

    public async Task<string> GetExchangeRates(string currencyCode, string date)
    {
        string key = $"{currencyCode}-{date}";
        var cacheResult = cacheService.GetFromCache(key);

        if (cacheResult != null)
        {
            cacheResult = cacheResult + " " + resultFromCacheMessage;
            return (string)cacheResult;
        }

        try
        {
            HttpResponseMessage response = await _client.GetAsync($"https://api.privatbank.ua/p24api/exchange_rates?date={date}");
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            dynamic data = JsonConvert.DeserializeObject(responseBody);

            List<ExchangeRate> exchangeRates = JsonConvert.DeserializeObject<List<ExchangeRate>>(data.exchangeRate.ToString());

            var exchangeRate = exchangeRates.FirstOrDefault(r => r.Currency == currencyCode);

            if (exchangeRate != null)
            {
                cacheService.AddToCache(key, exchangeRate.SaleRateNB, DateTime.Today.AddDays(1));
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
