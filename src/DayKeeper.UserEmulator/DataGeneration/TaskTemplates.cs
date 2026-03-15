namespace DayKeeper.UserEmulator.DataGeneration;

public static class TaskTemplates
{
    public static readonly string[] Titles = [
        // Household
        "Take out trash",
        "Do the laundry",
        "Vacuum living room",
        "Clean the bathroom",
        "Mow the lawn",
        "Wash the dishes",
        "Wipe down counters",
        "Change bed sheets",
        "Unclog the drain",
        "Replace smoke detector batteries",
        // Errands
        "Pay electric bill",
        "Renew car registration",
        "Pick up dry cleaning",
        "Drop off library books",
        "Schedule dentist appointment",
        "Refill prescriptions",
        "Return Amazon package",
        "Get car inspected",
        "Buy birthday card",
        "Mail tax documents",
        // Work
        "Prepare quarterly report",
        "Send weekly status update",
        "Review pull request",
        "Update project documentation",
        "Respond to client emails",
        "Prepare slides for presentation",
        "Schedule team retrospective",
        "Submit expense report",
        "Back up project files",
        "Update LinkedIn profile",
    ];

    public static readonly string[] TitleTemplates = [
        "Review {0} implementation",
        "Fix {0} bug",
        "Write tests for {0}",
        "Refactor {0} module",
        "Deploy {0} to staging",
        "Document {0} API",
        "Investigate {0} issue",
        "Set up {0} integration",
        "Migrate {0} to new schema",
        "Performance tune {0}",
        "Buy {0} for the house",
        "Research {0} options",
        "Schedule {0} appointment",
        "Call {0} about the invoice",
    ];

    public static readonly string[] ProjectNames = [
        "Home Renovation",
        "Q2 Launch",
        "Website Redesign",
        "Garage Cleanout",
        "Financial Planning",
        "Fitness Goals",
        "Vacation Planning",
        "Back-to-School Prep",
        "Side Hustle",
        "Learning Spanish",
        "Garden Project",
        "Holiday Planning",
        "Job Search",
        "Book Writing",
        "Meal Planning",
    ];

    public static readonly string[] ProjectNameTemplates = [
        "{0} Sprint {1}",
        "{0} v{1}.0",
        "{0} Phase {1}",
        "Project {0}",
        "{0} Roadmap",
        "{0} Backlog",
    ];
}
