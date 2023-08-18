using Moq.Protected;
using Moq;
using System.Net;

namespace ConsoleApp1
{
    [TestClass]
    public class BotLogicTest
    {
        [TestMethod]
        public async Task GetExchangeRate_ValidResponse_ValidOutput()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.IsAny<HttpRequestMessage>(),
                  ItExpr.IsAny<CancellationToken>()
               )
               .ReturnsAsync(new HttpResponseMessage()
               {
                   StatusCode = HttpStatusCode.OK,
                   Content = new StringContent("{\"date\":\"01.12.2014\",\"bank\":\"PB\",\"baseCurrency\":980,\"baseCurrencyLit\":\"UAH\",\"exchangeRate\":[{\"baseCurrency\":\"UAH\",\"currency\":\"USD\",\"saleRateNB\":15.056413,\"purchaseRateNB\":15.056413,\"saleRate\":15.7000000,\"purchaseRate\":15.3500000}]}"),
               })
               .Verifiable();

            var httpClient = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri("http://test.com/"),
            };

            var currencyService = new CurrencyService(httpClient);

            // Act
            string result = await currencyService.GetExchangeRates("USD", "01.12.2014");

            // Assert
            Assert.AreEqual(result, "15.056413");
        }
    }
}
