using Askmethat.Aspnet.JsonLocalizer.Localizer;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;
using Telegram_AI_Bot.Core.Models.Users;

namespace Telegram_AI_Bot.Core.Services.Telegram.Payments;

public class CryptoTonClient
{
    private string Token { get; }
    
    public CryptoTonClient(string token)
    {
        Token = token;
    }

    public async Task<CreateInvoiceResponse?> CreateInvoice(IJsonStringLocalizer _l, TelegramUser user, string token, decimal amount, string botUrl, CancellationToken cancellationToken)
    {
        var httpClient = new HttpClient();
        var baseAddress = "https://pay.crypt.bot/api/createInvoice";

        var queryParams = new Dictionary<string, string>
        {
            { "asset", "TON" },
            { "amount", amount.ToString() },
            { "description", _l.GetString("TonCoin.InvoiceDescription") },
            // { "hidden_message", "Спасибо за оплату :)" },
            { "allow_anonymous", "false" },
            { "allow_comments", "false" },
            { "payload", user.UserId.ToString() },
            { "paid_btn_name", "openBot" },
            { "paid_btn_url", botUrl },
        };
        var queryString = new FormUrlEncodedContent(queryParams).ReadAsStringAsync().Result;

        var requestUri = $"{baseAddress}?{queryString}";
        var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

        request.Headers.Add("Crypto-Pay-API-Token", Token);

        HttpResponseMessage response = await httpClient.SendAsync(request);
        
        string responseBody = await response.Content.ReadAsStringAsync();
        CreateInvoiceResponse? obj = JsonConvert.DeserializeObject<CreateInvoiceResponse>(responseBody);

        return obj;

        // if (response.IsSuccessStatusCode)
        // {
        //     string responseBody = await response.Content.ReadAsStringAsync();
        //     CreateInvoiceResponse? obj = JsonConvert.DeserializeObject<CreateInvoiceResponse>(responseBody);
        //     Console.WriteLine($"Response: {responseBody}");
        // }
        // else
        // {
        //     Console.WriteLine($"Error: {response.ReasonPhrase}");
        // }
    }

    public record CreateInvoiceResponse(bool Ok, CreateInvoiceResponseResult? Result, CreateInvoiceResponseErrorResult? Error);

    public record CreateInvoiceResponseResult(
        [JsonProperty("invoice_id")] int InvoiceId,
        [JsonProperty("status")] string Status,
        [JsonProperty("hash")] string Hash,
        [JsonProperty("asset")] string Asset,
        [JsonProperty("amount")] decimal Amount,
        [JsonProperty("pay_url")] string PayUrl,
        [JsonProperty("description")] string Description,
        [JsonProperty("created_at")] DateTimeOffset CreatedAt,
        [JsonProperty("allow_comments")] bool AllowComments,
        [JsonProperty("allow_anonymous")] bool AllowAnonymous,
        [JsonProperty("hidden_message")] string HiddenMessage,
        [JsonProperty("paid_btn_name")] string PaidBtnName,
        [JsonProperty("paid_btn_url")] string PaidBtnUrl
    );

    public record CreateInvoiceResponseErrorResult(
        int Code,
        string Name
    );
}