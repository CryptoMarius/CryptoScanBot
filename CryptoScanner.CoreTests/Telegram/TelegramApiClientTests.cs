using CryptoScanner.Core.Telegram;

using System.Text.Json;

namespace CryptoScanner.CoreTests.Telegram;

/// <summary>
/// The scanner talks to the Telegram Bot API over http instead of through a package. What can break
/// silently in that arrangement is the mapping of the json field names onto our properties: a typo
/// in "update_id" does not fail to compile, it simply hands out an offset of zero and every command
/// in the chat is answered again on the next poll. These tests pin the field names down with the
/// answers Telegram actually sends.
/// </summary>
[TestClass]
public class TelegramApiClientTests
{
    private static readonly JsonSerializerOptions jsonOptions = new() { PropertyNameCaseInsensitive = true };


    [TestMethod]
    public void TextCommandIsUnpackedFromGetUpdates()
    {
        // arrange - the answer to getUpdates when someone types "status" in the chat
        string json = """
        {
          "ok": true,
          "result": [
            {
              "update_id": 874301912,
              "message": {
                "message_id": 4321,
                "from": { "id": 710219603, "is_bot": false, "first_name": "Marius" },
                "chat": { "id": 710219603, "first_name": "Marius", "type": "private" },
                "date": 1756400000,
                "text": "status"
              }
            }
          ]
        }
        """;

        // act
        TelegramApiResponse<TelegramUpdate[]>? answer =
            JsonSerializer.Deserialize<TelegramApiResponse<TelegramUpdate[]>>(json, jsonOptions);

        // assert
        Assert.IsNotNull(answer);
        Assert.IsTrue(answer.Ok, "ok should be read from the envelope");
        Assert.IsNotNull(answer.Result);
        Assert.AreEqual(1, answer.Result.Length);

        TelegramUpdate update = answer.Result[0];
        Assert.AreEqual(874301912, update.Id, "update_id feeds the offset, without it commands repeat forever");
        Assert.IsNotNull(update.Message);
        Assert.AreEqual("status", update.Message.Text);
        Assert.AreEqual(710219603L, update.Message.Chat.Id, "the answer is sent back to this chat");
        Assert.AreEqual(TelegramMessageType.Text, update.Message.Type);
    }


    [TestMethod]
    public void MessageWithAPhotoIsNotATextMessage()
    {
        // arrange - only the photo array is filled in, there is no text
        string json = """
        {
          "message_id": 12,
          "chat": { "id": 42, "type": "private" },
          "date": 1756400000,
          "photo": [ { "file_id": "AgACAgQAA", "width": 90, "height": 51 } ]
        }
        """;

        // act
        TelegramMessage? message = JsonSerializer.Deserialize<TelegramMessage>(json, jsonOptions);

        // assert
        Assert.IsNotNull(message);
        Assert.AreEqual(TelegramMessageType.Photo, message.Type);
        Assert.IsNull(message.Text);
    }


    [TestMethod]
    public void RejectionCarriesTheWaitingTimeTelegramAsksFor()
    {
        // arrange - the 429 the polling loop backs off on
        string json = """
        {
          "ok": false,
          "error_code": 429,
          "description": "Too Many Requests: retry after 5",
          "parameters": { "retry_after": 5 }
        }
        """;

        // act
        TelegramApiResponse<TelegramUpdate[]>? answer =
            JsonSerializer.Deserialize<TelegramApiResponse<TelegramUpdate[]>>(json, jsonOptions);

        // assert
        Assert.IsNotNull(answer);
        Assert.IsFalse(answer.Ok);
        Assert.AreEqual(429, answer.ErrorCode);
        Assert.AreEqual("Too Many Requests: retry after 5", answer.Description, "this text is what reaches the log");
        Assert.IsNotNull(answer.Parameters);
        Assert.AreEqual(5, answer.Parameters.RetryAfter, "the loop waits this many seconds instead of guessing");
    }


    [TestMethod]
    public void UnknownFieldsAreIgnored()
    {
        // arrange - Telegram keeps adding fields, an update we have never seen may not throw
        string json = """
        {
          "update_id": 7,
          "message": {
            "message_id": 1,
            "chat": { "id": 42, "type": "private" },
            "date": 1756400000,
            "text": "help",
            "link_preview_options": { "is_disabled": true },
            "business_connection_id": "something_new"
          },
          "some_update_type_from_the_future": { "a": 1 }
        }
        """;

        // act
        TelegramUpdate? update = JsonSerializer.Deserialize<TelegramUpdate>(json, jsonOptions);

        // assert
        Assert.IsNotNull(update);
        Assert.AreEqual(7, update.Id);
        Assert.IsNotNull(update.Message);
        Assert.AreEqual("help", update.Message.Text);
    }


    [TestMethod]
    public void EmptyUpdateListIsAnAnswerAndNotAFailure()
    {
        // arrange - the usual answer, nobody typed anything
        string json = """{ "ok": true, "result": [] }""";

        // act
        TelegramApiResponse<TelegramUpdate[]>? answer =
            JsonSerializer.Deserialize<TelegramApiResponse<TelegramUpdate[]>>(json, jsonOptions);

        // assert
        Assert.IsNotNull(answer);
        Assert.IsTrue(answer.Ok);
        Assert.IsNotNull(answer.Result, "an empty array is not the same as no result, it must not raise");
        Assert.AreEqual(0, answer.Result.Length);
    }
}
