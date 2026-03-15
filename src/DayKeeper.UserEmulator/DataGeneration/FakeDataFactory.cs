using System.Globalization;

namespace DayKeeper.UserEmulator.DataGeneration;

public sealed class FakeDataFactory
{
    private static readonly string[] SpaceNames = [
        "Family", "Work Team", "Roommates", "Study Group", "Project Alpha",
        "Weekend Warriors", "Book Club", "Travel Crew", "Fitness Buddies", "Home Team",
    ];

    private static readonly string[] WeekStarts = ["SUNDAY", "MONDAY"];

    private static readonly string?[] Locales = [null, "en-US", "en-GB", "fr-FR", "de-DE", "es-ES", "ja-JP"];

    private static readonly string[] TaskStatuses = ["OPEN", "IN_PROGRESS", "COMPLETED", "CANCELLED"];

    private static readonly string[] TaskPriorities = ["NONE", "LOW", "MEDIUM", "HIGH", "URGENT"];

    private static readonly string[] ContactMethodTypes = ["PHONE", "EMAIL", "OTHER"];

    private static readonly string[] ContactMethodLabels = ["Mobile", "Home", "Work", "Personal", "Other"];

    private static readonly string[] AddressLabels = ["Home", "Work", "Billing", "Shipping", "Other"];

    private static readonly string[] ImportantDateLabels = [
        "Birthday", "Anniversary", "Hire Date", "Graduation", "Memorial Day",
        "Wedding", "Due Date", "Founding Date", "First Met",
    ];

    private static readonly string[] DeviceNames = [
        "iPhone 15 Pro", "iPhone 14", "iPhone 13 Mini", "iPhone SE",
        "Galaxy S24", "Galaxy S23 Ultra", "Pixel 8 Pro", "Pixel 7",
        "Chrome Browser", "Firefox Browser", "Safari Browser", "Edge Browser",
        "iPad Pro", "Galaxy Tab S9",
    ];

    private readonly Bogus.Faker _faker;

    public FakeDataFactory(int? seed = null)
    {
        _faker = seed.HasValue
            ? new Bogus.Faker { Random = new Bogus.Randomizer(seed.Value) }
            : new Bogus.Faker();
    }

    public (string DisplayName, string Email, string Timezone, string WeekStart, string? Locale) GenerateUser()
    {
        var firstName = _faker.Name.FirstName();
        var lastName = _faker.Name.LastName();
        var displayName = $"{firstName} {lastName}";
        var email = _faker.Internet.Email(firstName, lastName);
        var timezone = _faker.PickRandom(EventTemplates.Timezones);
        var weekStart = _faker.PickRandom(WeekStarts);
        var locale = _faker.PickRandom(Locales);

        return (displayName, email, timezone, weekStart, locale);
    }

    public string GenerateSpaceName() => _faker.PickRandom(SpaceNames);

    public (string Name, string? Description) GenerateProject()
    {
        string name;
        if (_faker.Random.Bool(0.4f))
        {
            var template = _faker.PickRandom(TaskTemplates.ProjectNameTemplates);
            name = template.Contains("{1}", StringComparison.Ordinal)
                ? string.Format(CultureInfo.InvariantCulture, template, _faker.Hacker.Noun(), _faker.Random.Int(1, 6).ToString(CultureInfo.InvariantCulture))
                : string.Format(CultureInfo.InvariantCulture, template, _faker.Hacker.Noun());
        }
        else
        {
            name = _faker.PickRandom(TaskTemplates.ProjectNames);
        }

        var description = _faker.Random.Bool(0.5f) ? _faker.Lorem.Sentence() : null;
        return (name, description);
    }

    public (string Title, string? Description, string Status, string Priority, DateTime? DueAt, DateOnly? DueDate) GenerateTaskItem()
    {
        string title;
        if (_faker.Random.Bool(0.4f))
        {
            var template = _faker.PickRandom(TaskTemplates.TitleTemplates);
            title = string.Format(CultureInfo.InvariantCulture, template, _faker.Hacker.Noun());
        }
        else
        {
            title = _faker.PickRandom(TaskTemplates.Titles);
        }

        var description = _faker.Random.Bool(0.4f) ? _faker.Lorem.Sentence() : null;
        var status = _faker.PickRandom(TaskStatuses);
        var priority = _faker.PickRandom(TaskPriorities);

        DateTime? dueAt = null;
        DateOnly? dueDate = null;

        if (_faker.Random.Bool(0.6f))
        {
            var future = _faker.Date.Future(90);
            if (_faker.Random.Bool(0.5f))
            {
                dueAt = future;
            }
            else
            {
                dueDate = DateOnly.FromDateTime(future);
            }
        }

        return (title, description, status, priority, dueAt, dueDate);
    }

    public (string Name, string Color) GenerateCalendar()
    {
        var preset = _faker.PickRandom(EventTemplates.CalendarPresets);
        return (preset.Name, preset.Color);
    }

    public (string Title, string? Description, bool IsAllDay, DateTime StartAt, DateTime EndAt, DateOnly? StartDate, DateOnly? EndDate, string Timezone, string? RecurrenceRule, string? Location) GenerateCalendarEvent(bool allowRecurrence = true)
    {
        var title = GenerateEventTitle();
        var description = _faker.Random.Bool(0.35f) ? _faker.Lorem.Sentence() : null;
        var timezone = _faker.PickRandom(EventTemplates.Timezones);
        var location = _faker.Random.Bool(0.45f) ? _faker.PickRandom(EventTemplates.Locations) : null;
        var recurrenceRule = allowRecurrence && _faker.Random.Bool(0.3f)
            ? _faker.PickRandom(EventTemplates.RecurrenceRules)
            : null;

        var isAllDay = _faker.Random.Bool(0.2f);
        DateTime startAt;
        DateTime endAt;
        DateOnly? startDate = null;
        DateOnly? endDate = null;
        var baseDate = _faker.Date.Soon(60);

        if (isAllDay)
        {
            startDate = DateOnly.FromDateTime(baseDate);
            var spanDays = _faker.Random.Int(0, 2);
            endDate = startDate.Value.AddDays(spanDays);
            startAt = baseDate.Date;
            endAt = baseDate.Date.AddDays(spanDays);
        }
        else
        {
            var (startHour, durationMinutes) = GenerateTimedEventSlot();
            startAt = baseDate.Date.AddHours(startHour);
            endAt = startAt.AddMinutes(durationMinutes);
        }

        return (title, description, isAllDay, startAt, endAt, startDate, endDate, timezone, recurrenceRule, location);
    }

    private string GenerateEventTitle()
    {
        if (!_faker.Random.Bool(0.4f))
        {
            return _faker.PickRandom(EventTemplates.Titles);
        }

        var template = _faker.PickRandom(EventTemplates.TitleTemplates);
        var fill = _faker.Random.Bool(0.5f)
            ? _faker.Name.FullName()
            : (_faker.Random.Bool() ? _faker.Company.CompanyName() : _faker.Address.City());
        return string.Format(CultureInfo.InvariantCulture, template, fill);
    }

    private (int StartHour, int DurationMinutes) GenerateTimedEventSlot()
    {
        if (_faker.Random.Bool(0.5f))
        {
            return (_faker.Random.Int(9, 17), PickDuration([30, 60, 90, 120]));
        }

        return (_faker.Random.Int(17, 21), PickDuration([30, 60, 90, 120, 180]));
    }

    public string GenerateShoppingListName() => _faker.PickRandom(GroceryData.ShoppingListNames);

    public (string Name, decimal Quantity, string? Unit) GenerateListItem()
    {
        var name = _faker.PickRandom(GroceryData.Items);
        var quantity = Math.Round((decimal)_faker.Random.Double(0.5, 10), 2);
        var unit = _faker.Random.Bool(0.75f) ? _faker.PickRandom(GroceryData.Units) : null;
        return (name, quantity, unit);
    }

    public (string FirstName, string LastName, string? Notes) GeneratePerson()
    {
        var firstName = _faker.Name.FirstName();
        var lastName = _faker.Name.LastName();
        var notes = _faker.Random.Bool(0.3f) ? _faker.Lorem.Sentence() : null;
        return (firstName, lastName, notes);
    }

    public (string Type, string Value, string? Label, bool IsPrimary) GenerateContactMethod()
    {
        var type = _faker.PickRandom(ContactMethodTypes);
        var value = type switch
        {
            "PHONE" => _faker.Phone.PhoneNumber("(###) ###-####"),
            "EMAIL" => _faker.Internet.Email(),
            _ => _faker.Internet.Url(),
        };
        var label = _faker.Random.Bool(0.6f) ? _faker.PickRandom(ContactMethodLabels) : null;
        var isPrimary = _faker.Random.Bool(0.3f);
        return (type, value, label, isPrimary);
    }

    public (string? Label, string Street1, string? Street2, string City, string? State, string? PostalCode, string Country, bool IsPrimary) GenerateAddress()
    {
        var label = _faker.Random.Bool(0.5f) ? _faker.PickRandom(AddressLabels) : null;
        var street1 = _faker.Address.StreetAddress();
        var street2 = _faker.Random.Bool(0.2f) ? _faker.Address.SecondaryAddress() : null;
        var city = _faker.Address.City();
        var state = _faker.Random.Bool(0.8f) ? _faker.Address.StateAbbr() : null;
        var postalCode = _faker.Random.Bool(0.85f) ? _faker.Address.ZipCode() : null;
        var country = _faker.Address.CountryCode();
        var isPrimary = _faker.Random.Bool(0.3f);
        return (label, street1, street2, city, state, postalCode, country, isPrimary);
    }

    public (string Label, DateOnly DateValue) GenerateImportantDate()
    {
        var label = _faker.PickRandom(ImportantDateLabels);
        var date = DateOnly.FromDateTime(_faker.Date.Past(40));
        return (label, date);
    }

    public (string DeviceName, string Platform, string FcmToken) GenerateDevice()
    {
        var deviceName = _faker.PickRandom(DeviceNames);
        var platform = deviceName switch
        {
            var n when n.Contains("iPhone", StringComparison.Ordinal) || n.Contains("iPad", StringComparison.Ordinal) => "IOS",
            var n when n.Contains("Galaxy", StringComparison.Ordinal) || n.Contains("Pixel", StringComparison.Ordinal) => "ANDROID",
            _ => "WEB",
        };
        var fcmToken = _faker.Random.AlphaNumeric(152);
        return (deviceName, platform, fcmToken);
    }

    public T PickRandom<T>(IList<T> items) => items[_faker.Random.Int(0, items.Count - 1)];

    public int RandomInt(int min, int max) => _faker.Random.Int(min, max);

    public bool RandomBool(float weight = 0.5f) => _faker.Random.Bool(weight);

    public byte[] RandomBytes(int minSize, int maxSize)
    {
        var size = _faker.Random.Int(minSize, maxSize);
        return _faker.Random.Bytes(size);
    }

    private int PickDuration(int[] choices) => choices[_faker.Random.Int(0, choices.Length - 1)];
}
