using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Refit;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;

namespace Telegram_AI_Bot.Core.Services.OpenAi.Tools;

public class TelegramOpenAiGenerationImageTool(ITelegramBotClient botClient, OpenAiConfiguration openAiOptions)
{
    public async Task<string> UploadImageToImgure(MemoryStream stream, CancellationToken cancellationToken)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Client-ID", openAiOptions.ImgurToken);

        var content = new ByteArrayContent(stream.ToArray());

        var response = await client.PostAsync("https://api.imgur.com/3/image", content);
        var responseResult = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonObject>(responseResult);
        return result["data"]["link"].ToString();
    }

    public async Task ResizeImage(MemoryStream stream, CancellationToken cancellationToken)
    {
        stream.Position = 0;
        using var image = await Image.LoadAsync(stream, cancellationToken);
        image.Mutate(x => x.Resize(512, 512));
        stream.SetLength(0);
        await image.SaveAsJpegAsync(stream, cancellationToken);
        stream.Position = 0;
    }

    public string GetPromptFromJson(string json)
    {
        var promptjson = JsonSerializer.Deserialize<JsonObject>(json);
        return promptjson!["prompt"]!.ToString();
    }

    public async Task<string> GenerateAndSendPhotoAsync(Message message, string prompt,
        CancellationToken cancellationToken)
    {
        var photolink = await GenerateFusionbrainImage(prompt, cancellationToken);
        await botClient.SendPhotoAsync(message.Chat.Id, new InputFileUrl(photolink),
            cancellationToken: cancellationToken);
        return photolink;
    }

    private async Task<string> GenerateFusionbrainImage(string prompt, CancellationToken cancellationToken)
    {
        using var httpClient = new HttpClient();
        using var requestContent = new MultipartFormDataContent();

        httpClient.DefaultRequestHeaders.Add("X-Key", $"Key {openAiOptions.FusionbrainKey}");
        httpClient.DefaultRequestHeaders.Add("X-Secret", $"Secret {openAiOptions.FusionbrainSecret}");

        var jsonParams = new StringContent(new JsonObject
        {
            ["type"] = "GENERATE",
            ["numImages"] = 1,
            ["width"] = "1024",
            ["height"] = "1024",
            ["generateParams"] = new JsonObject
            {
                ["query"] = prompt
            },
        }.ToString(), Encoding.UTF8, "application/json");
        requestContent.Add(jsonParams, "params");
        requestContent.Add(new StringContent("4"), "model_id");

        var response =
            await httpClient.PostAsync("https://api-key.fusionbrain.ai/key/api/v1/text2image/run", requestContent);

        if (!response.IsSuccessStatusCode)
            throw new RequestException(response.ReasonPhrase);

        string responseBody = await response.Content.ReadAsStringAsync();
        var responseJson = JsonSerializer.Deserialize<JsonObject>(responseBody);
        var image = await GetGeneratedImageAsync(httpClient, responseJson!["uuid"]!.ToString());

        var imageStream = new MemoryStream();
        await imageStream.WriteAsync(Convert.FromBase64String(image), cancellationToken);
        imageStream.Position = 0;

        string photoLink = await UploadImageToImgure(imageStream, cancellationToken);
        return photoLink;
    }

    private async Task<string> GetGeneratedImageAsync(HttpClient httpClient, string uuid)
    {
        for (int i = 0; i < 100; i++)
        {
            var resultStatus =
                await httpClient.GetAsync("https://api-key.fusionbrain.ai/key/api/v1/text2image/status/" + uuid);
            var resultStatusJson =
                JsonSerializer.Deserialize<JsonObject>(await resultStatus.Content.ReadAsStreamAsync());
            if (resultStatusJson["status"]!.ToString() == "DONE")
            {
                return resultStatusJson["images"]![0]!.ToString();
            }

            await Task.Delay(2000);
        }

        throw new ApiRequestException("Cant find image");
    }
}