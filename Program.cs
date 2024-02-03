using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace SimpleTgBot;

public class Program
{
    private static TelegramBotClient bot = null!;
    private static bool screaming = false;  // should be designed to be specific for each user, otherwise change made by one user affects all the other users.
    private const string nextButton = "Next";  // W: should we keep all of these alike properties private?
    private const string backButton = "Back";
    private const string tutorialButton = "Tutorial";

    const string firstMenu = "<b>Menu 1</b>\n\nA beautiful menu with a shiny inline button.";
    const string secondMenu = "<b>Menu 2</b>\n\nA better menu with even more shiny inline buttons.";

    private static InlineKeyboardMarkup firstMenuMarkup =
        new(InlineKeyboardButton.WithCallbackData(nextButton));
    private static InlineKeyboardMarkup secondMenuMarkup =
        new(
            new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData(backButton) },
                new[]
                {
                    InlineKeyboardButton.WithUrl(
                        tutorialButton,
                        "https://core.telegram.org/bots/tutorial"
                    )
                }
            }
        );

    public static void Main(string[] args)
    {
        Console.WriteLine("Program is now running!");

        bot = new TelegramBotClient(Tg.SecretToken);

        using var cts = new CancellationTokenSource();

        bot.StartReceiving(
            updateHandler: HandleUpdate,
            pollingErrorHandler: HandleError, // W: not sure if 'pollingErrorHandler' is right parameter
            cancellationToken: cts.Token
        );

        Console.WriteLine("Please press ENTER to exit.");
        Console.ReadLine();

        cts.Cancel();
    }

    public static async Task HandleUpdate(
        ITelegramBotClient _,
        Update update,
        CancellationToken token
    )
    {
        switch (update.Type)
        {
            // A message was received
            case UpdateType.Message:
                await HandleMessage(update.Message!);
                break;

            // A button was pressed
            case UpdateType.CallbackQuery:
                await HandleButton(update.CallbackQuery!);
                break;
        }
    }

    public static async Task HandleButton(CallbackQuery query)
    {
        string text = string.Empty;
        InlineKeyboardMarkup markup = new(Array.Empty<InlineKeyboardButton>());  // W: does it differ from 'secondMenuMarkup'?

        if (query.Data == nextButton)
        {
            text = secondMenu;
            markup = secondMenuMarkup;
        }
        else if (query.Data == backButton)
        {
            text = firstMenu;
            markup = firstMenuMarkup;
        }

        // Close the query to end the client-side loading animation
        await bot.AnswerCallbackQueryAsync(query.Id);

        // Replace menu text and keyboard
        await bot.EditMessageTextAsync(
            query.Message!.Chat.Id,
            query.Message.MessageId,
            text,
            ParseMode.Html,
            replyMarkup: markup
        );
    }

    public static async Task HandleMessage(Message message)
    {
        var user = message.From;
        var clientMessage = message.Text ?? string.Empty;

        if (user is null)  // W: how user can ever be null?
            return;

        Console.WriteLine($"User {user.FirstName} sent: {clientMessage}");

        if (clientMessage.StartsWith("/")) // O1: will '.IsCommand()' kind of design be more concise?
        {
            await HandleCommand(user.Id, clientMessage);
        }
        else if (screaming && clientMessage.Length > 0)  // why need check for length? Postman testers?
        {
            // To preserve markdown, we attach entities (bold, italic..) // works without 3rd argument
            await bot.SendTextMessageAsync(user.Id, clientMessage.ToUpper(), entities: message.Entities); // O2: 'Scream(string msg)'
        }
        else
        {
            // This is equivalent to forwarding, without the sender's name
            await bot.CopyMessageAsync(user.Id, user.Id, message.MessageId);
        }
    }

    public static async Task HandleCommand(long userId, string command)
    {
        switch (command)
        {
            case "/scream":
                screaming = true;
                break;
            
            case "/whisper":
                screaming = false;
                break;

            case "/menu":
                await SendMenu(userId);
                break;
        }

        await Task.CompletedTask;
    }

    public static async Task SendMenu(long userId)
    {
        await bot.SendTextMessageAsync(
            userId,
            firstMenu,
            parseMode: ParseMode.Html,
            replyMarkup: firstMenuMarkup
        );
    }

    private static async Task HandleError(
        ITelegramBotClient _,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        await Console.Error.WriteLineAsync(exception.Message);
    }
}
