# Quantity and time eaten

## Install the update

Exit the previous app with **tray menu → Exit**, then open `MacroTracker.exe` from this updated package. It uses the same diary and USDA key. If Windows startup was enabled, turn that setting off and on from the new app to update the executable path.

On the first open, the app makes `macro-tracker.db.before-quantity-upgrade.bak` before upgrading an older database. Existing entries keep their original calories, macros, times, and history. Use the updated app after upgrading.

## Log two whole eggs in one entry

1. Open **Add food** or choose **Edit** on an existing entry.
2. Set **Measure** to **Item / serving** and describe it as `1 whole egg`.
3. Enter the macros for one egg and set **Amount** to `2`.
4. Check the total preview, then save.

Amount scales all four macros. Choose **Item / serving** for countable foods such as an egg, slice, scoop, bottle, or package. Choose **Grams** or **Ounces** for weighed foods; the macro boxes then show values per one gram or one ounce. The preview and diary show the full amount's totals. You can change the amount on an existing entry without adding duplicate entries. Fractional amounts such as `0.5` are supported. Favorites and recent foods retain the serving definition and amount so they can be reused and adjusted.

With USDA search, select the matching egg food, wait for portion sizes to load, and choose a portion such as **1 large (50 g)** in the unit menu. Set its amount to **2** and choose **Use this food**. The Add Food form receives the per-egg macros and quantity 2 automatically. USDA supplies the portion weights; the app does not guess them. Available portions vary by food. If a food has no household portions, grams/ounces still work. Loading portion sizes makes one additional USDA request per selected common food; the result is cached for that food in the current search cache.

Older entries have a quantity of **1**, representing the **entire portion originally logged**. For example, an older entry whose serving text says `2 eggs` keeps its original totals with quantity 1. To convert it to individual eggs, change the serving definition to `1 whole egg`, enter the per-egg macros, and set quantity to 2. The app deliberately does not guess what old free-text portions meant.

## Set the time you ate

**Time eaten** is available for Add Food, Edit, Copy Food, and saved meals. Choose a clock time in 15-minute intervals, then choose AM or PM. New and reused entries start at the current quarter-hour rather than the original meal's time. Editing an entry preserves its saved time until you change it; older non-quarter-hour times remain available for that entry.

The date shown above the form is the diary date. Use the Food Log date picker to log or edit food on another day. Changing the time reorders the day's food list chronologically. Invalid times, zero quantities, and negative quantities are rejected. Edited times, quantities, and total macros persist after restarting and in database backups. Food CSV export includes a `quantity` column; its calories/macros are still totals for the entire entry.

The updated build passed the quantity/time, portion, migration, restore, core integration, and real-window tests, including logging two eggs at 8:15 AM and editing the entry to three eggs at 7:15 PM.
