namespace DayKeeper.UserEmulator.DataGeneration;

public static class EventTemplates
{
    public static readonly string[] Titles = [
        "Team Standup",
        "Sprint Review",
        "Sprint Planning",
        "Retrospective",
        "1:1 with Manager",
        "Dentist Appointment",
        "Doctor Checkup",
        "Date Night",
        "Book Club",
        "Soccer Practice",
        "Piano Lesson",
        "Oil Change",
        "Parent-Teacher Conference",
        "Gym Session",
        "Yoga Class",
        "Haircut",
        "Car Registration",
        "Eye Exam",
        "Tax Appointment",
        "Neighborhood Watch Meeting",
        "HOA Meeting",
        "Therapy Session",
        "Dog Grooming",
        "Kids' Playdate",
        "Pool Party",
        "Movie Night",
        "Game Night",
        "Family Dinner",
        "Church Service",
        "Volunteer Shift",
    ];

    public static readonly string[] TitleTemplates = [
        "Meeting with {0}",
        "Lunch at {0}",
        "Call with {0}",
        "Interview: {0}",
        "Coffee with {0}",
        "Dinner at {0}",
        "Workshop: {0}",
        "Training: {0}",
        "Review: {0}",
        "Catch-up with {0}",
        "Onboarding: {0}",
        "Happy Hour at {0}",
    ];

    public static readonly string[] RecurrenceRules = [
        "FREQ=DAILY;COUNT=5",
        "FREQ=WEEKLY;BYDAY=MO,WE,FR",
        "FREQ=WEEKLY;BYDAY=TU,TH",
        "FREQ=WEEKLY;BYDAY=MO",
        "FREQ=WEEKLY;INTERVAL=2;BYDAY=MO",
        "FREQ=MONTHLY;BYMONTHDAY=1",
        "FREQ=MONTHLY;BYMONTHDAY=15",
        "FREQ=MONTHLY;BYDAY=1MO",
        "FREQ=YEARLY",
        "FREQ=DAILY;COUNT=10",
    ];

    public static readonly string[] Locations = [
        "Conference Room A",
        "Conference Room B",
        "Main Boardroom",
        "Zoom",
        "Google Meet",
        "Microsoft Teams",
        "Office Lobby",
        "Break Room",
        "Parking Lot",
        "Coffee Shop",
        "Local Park",
        "Community Center",
        "School Gymnasium",
        "Doctor's Office",
        "Downtown Branch",
    ];

    public static readonly (string Name, string Color)[] CalendarPresets = [
        ("Personal", "#3B82F6"),
        ("Work", "#EF4444"),
        ("Family", "#10B981"),
        ("Birthdays", "#F59E0B"),
        ("Holidays", "#8B5CF6"),
        ("Fitness", "#EC4899"),
        ("School", "#06B6D4"),
        ("Medical", "#14B8A6"),
    ];

    public static readonly string[] Timezones = [
        "America/New_York",
        "America/Chicago",
        "America/Denver",
        "America/Los_Angeles",
        "Europe/London",
        "Europe/Berlin",
        "Asia/Tokyo",
        "Australia/Sydney",
        "America/Toronto",
        "Pacific/Auckland",
    ];
}
