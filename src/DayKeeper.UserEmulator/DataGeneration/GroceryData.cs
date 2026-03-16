namespace DayKeeper.UserEmulator.DataGeneration;

public static class GroceryData
{
    // ~80 items across categories
    public static readonly string[] Items = [
        // Produce
        "Bananas", "Apples", "Avocados", "Tomatoes", "Onions", "Potatoes", "Garlic",
        "Lettuce", "Spinach", "Bell Peppers", "Carrots", "Broccoli", "Mushrooms",
        "Lemons", "Limes", "Strawberries", "Blueberries", "Grapes", "Cucumbers", "Celery",
        // Dairy
        "Whole Milk", "2% Milk", "Heavy Cream", "Butter", "Eggs", "Greek Yogurt",
        "Cheddar Cheese", "Mozzarella", "Cream Cheese", "Sour Cream", "Parmesan",
        // Meat/Protein
        "Chicken Breast", "Ground Beef", "Salmon Fillet", "Bacon", "Italian Sausage",
        "Pork Chops", "Shrimp", "Tofu", "Ground Turkey", "Chicken Thighs",
        // Pantry
        "Olive Oil", "Rice", "Pasta", "Bread", "Flour", "Sugar", "Salt", "Pepper",
        "Black Beans", "Chickpeas", "Canned Tomatoes", "Chicken Broth", "Soy Sauce",
        "Peanut Butter", "Honey", "Coffee", "Tea Bags", "Cereal", "Oatmeal",
        // Frozen
        "Frozen Pizza", "Ice Cream", "Frozen Vegetables", "Frozen Berries", "Frozen Waffles",
        // Beverages
        "Orange Juice", "Sparkling Water", "Almond Milk", "Apple Juice",
        // Household
        "Paper Towels", "Dish Soap", "Trash Bags", "Laundry Detergent", "Sponges",
        "Aluminum Foil", "Plastic Wrap", "Napkins", "Hand Soap",
        // Snacks
        "Tortilla Chips", "Salsa", "Hummus", "Crackers", "Granola Bars", "Trail Mix",
    ];

    public static readonly string[] Units = ["each", "lbs", "oz", "gallons", "ct", "bags", "boxes", "cans", "bottles", "bunches"];

    public static readonly string[] ShoppingListNames = [
        "Weekly Groceries", "Costco Run", "Party Supplies", "Home Depot List",
        "Target Run", "Trader Joe's List", "Meal Prep", "BBQ Supplies",
        "Holiday Baking", "Camping Supplies", "School Lunches", "Snack Run",
        "Thanksgiving Shopping", "Game Day Food", "Brunch Ingredients",
    ];
}
