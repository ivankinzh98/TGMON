using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Timers;
using System.Globalization;

var bot = new TelegramBotClient("8562357082:AAH4snlvUek5yX9O1rubnfCGwAbSedWVu0g");

// 🔹 Налаштування
long CHAT_ID = 468147291; // твій chat id
decimal baseHours = 8;
decimal normalRate = 199;
decimal overtimeRate = 249;
decimal lastDaySalary = 0;

// *Кнопки
ReplyKeyboardMarkup GetMainKeyboard()
{
    return new ReplyKeyboardMarkup(new[]
    {
        new KeyboardButton[] { "▶ Запустити бота" },
        new KeyboardButton[] { "💰 За сьогодні", "📅 За тиждень" },
        new KeyboardButton[] { "🗓 За місяць" }
    })
    {
        ResizeKeyboard = true
    };
}

// 🔹 Статистика
decimal weekTotal = 0;
decimal monthTotal = 0;

int currentWeek = DateTime.Now.DayOfYear / 7;
int currentMonth = DateTime.Now.Month;

DateTime lastReminder = DateTime.MinValue;

// 🔹 Таймер перевіряє кожну хвилину
var timer = new System.Timers.Timer(60000);
timer.Elapsed += CheckTime;
timer.Start();

// 🔹 Запуск отримання повідомлень
using var cts = new CancellationTokenSource();

bot.StartReceiving(
    HandleUpdateAsync,
    HandleErrorAsync,
    cancellationToken: cts.Token
);

Console.WriteLine("Бот запущено...");

// 🔹 Метод нагадування
async void CheckTime(object? sender, ElapsedEventArgs e)
{
    var now = DateTime.Now;

    if (now.Hour == 20 && now.Minute == 0 && lastReminder.Date != now.Date)
    {
        lastReminder = now;

        await bot.SendMessage(
            CHAT_ID,
            "⏰ Скільки годин ти сьогодні відпрацював?"
        );
    }
}

// 🔹 Обробка повідомлень
async Task HandleUpdateAsync(
    ITelegramBotClient client,
    Update update,
    CancellationToken token)
{   


    if (update.Type != UpdateType.Message) return;

    var msg = update.Message;

    if (msg?.Text == null) return;

    if (msg.Text == "▶ Запустити бота")
    {
        await client.SendMessage(
            msg.Chat.Id,
            "Бот активований ✅\nВведи кількість годин за сьогодні."
        );
        return;
    }

    if (msg.Text == "💰 За сьогодні")
    {
        await client.SendMessage(
            msg.Chat.Id,
            $"Сьогодні: {lastDaySalary} грн"
        );
        return;
    }

    if (msg.Text == "📅 За тиждень")
    {
        await client.SendMessage(
            msg.Chat.Id,
            $"За тиждень: {weekTotal} грн"
        );
        return;
    }

    if (msg.Text == "🗓 За місяць")
    {
        await client.SendMessage(
            msg.Chat.Id,
            $"За місяць: {monthTotal} грн"
        );
        return;
    }

    if (msg.Text == "/start")
    {
        await client.SendMessage(
            msg.Chat.Id,
            "Я рахую твою зарплату 💰\nОбери дію:",
            replyMarkup: GetMainKeyboard()
        );
        return;
    }

    // 🔹 Ввод годин
    if (decimal.TryParse(
        msg.Text.Replace(",", "."),
        NumberStyles.Any,
        CultureInfo.InvariantCulture,
        out decimal hours))
    {
        // новий тиждень
        int newWeek = DateTime.Now.DayOfYear / 7;
        if (newWeek != currentWeek)
        {
            weekTotal = 0;
            currentWeek = newWeek;
        }

        // новий місяць
        int newMonth = DateTime.Now.Month;
        if (newMonth != currentMonth)
        {
            monthTotal = 0;
            currentMonth = newMonth;
        }

        // 🔹 Підрахунок
        decimal salary;

        if (hours <= baseHours)
        {
            salary = hours * normalRate;
        }
        else
        {
            decimal overtime = hours - baseHours;
            salary = baseHours * normalRate +
                     overtime * overtimeRate;
        }

        lastDaySalary = salary;
        weekTotal += salary;
        monthTotal += salary;

        await client.SendMessage(
            msg.Chat.Id,
$@"💰 За день: {salary} грн
📅 За тиждень: {weekTotal} грн
🗓 За місяць: {monthTotal} грн"
        );
    }
}

// 🔹 Обробка помилок
Task HandleErrorAsync(
    ITelegramBotClient client,
    Exception ex,
    CancellationToken token)
{
    Console.WriteLine(ex);
    return Task.CompletedTask;
}

// 🔹 Щоб не завершувався
await Task.Delay(-1);