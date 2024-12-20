namespace ExchangeTest.Exchange.Mexc;

internal class FileName
{
    //private static string baseUrl = "https://api.mexc.com";

    //private async Task<string> SendAsync(HttpClient httpClient, string requestUri, HttpMethod httpMethod, object content = null)
    //{
    //    Console.WriteLine(requestUri);
    //    using (var request = new HttpRequestMessage(httpMethod, baseUrl + requestUri))
    //    {
    //        request.Headers.Add("X-MEXC-APIKEY", apiKey);

    //        if (content is not null)
    //        {
    //            //request.Content = new StringContent(JsonConvert.SerializeObject(content), Encoding.UTF8, "application/json");
    //            string text = System.Text.Json.JsonSerializer.Serialize(content, new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true });
    //            //File.WriteAllText(filename, text);
    //            request.Content = new StringContent(text);
    //            //request.Content = new StringContent(JsonConvert.SerializeObject(content), Encoding.UTF8, "application/json");
    //        }

    //        HttpResponseMessage response = await httpClient.SendAsync(request);

    //        using (HttpContent responseContent = response.Content)
    //        {
    //            string jsonString = await responseContent.ReadAsStringAsync();

    //            return jsonString;
    //        }
    //    }
    //}


    //private async Task MexcTest()
    //{
    //try
    //{
    //string text;
    //string apiKey = "your apikey";
    //string apiSecret = "your secret";
    //string BaseUrl = "https://api.mexc.com";

    //HttpClient httpClient = new();
    //MexcService service = new(apiKey, apiSecret, BaseUrl, httpClient);


    //text = await SendAsync(httpClient, "/api/v3/exchangeInfo?symbol=BTCUSDT", HttpMethod.Get);
    //GlobalData.AddTextToLogTab(text);

    //text = await SendAsync(httpClient, "/api/v3/exchangeInfo?symbols=BTCUSDT,ETHUSDT", HttpMethod.Get);
    //GlobalData.AddTextToLogTab(text);

    //text = await SendAsync(httpClient, "/api/v3/exchangeInfo", HttpMethod.Get);
    //GlobalData.AddTextToLogTab(text);


    ///// Exchange Information
    //using (var response = service.SendPublicAsync("/api/v3/exchangeInfo", HttpMethod.Get, new Dictionary<string, object> {{"symbol", "BTCUSDT"}}))
    //{
    //    string text = await response;
    //    GlobalData.AddTextToLogTab(text);
    //};

    //using (var response = service.SendPublicAsync("/api/v3/exchangeInfo", HttpMethod.Get, new Dictionary<string, object> { }))
    //{
    //    string text = await response;
    //    //GlobalData.AddTextToLogTab(text);
    //};
    //}
    //catch (Exception error)
    //{
    //    ScannerLog.Logger.Error(error, "");
    //    GlobalData.AddTextToLogTab("error back testing " + error.ToString()); // symbol.Text + " " + 
    //}
    //}
}
