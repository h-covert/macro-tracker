# Quantity and time eaten

## Install the update

Exit the previous app with **tray menu → Exit**, then open `MacroTracker.exe` from this updated package. It uses the same diary and USDA key. If Windows startup was enabled, turn that setting off and on from the new app to update the executable path.

On the first open, the app makes `macro-tracker.db.before-quantity-upgrade.bak` before upgrading an older database. Existing entries keep their original calories, macros, times, and history. Use the updated app after upgrading.

## Log two whole eggs in one entry

1. Open **Add food** or choose **Edit** on an existing entry.
2. Set **One item / serving** to `1 whole egg` and use the macros for one egg.
3. Set **Quantity** to `2`.
4. Check the total preview, then save.

Quantity scales all four macros. The editable macro boxes always show values for **one** item / serving; the preview and diary show the full quantity's totals. You can change the quantity on an existing entry without adding duplicate entries. Fractional quantities such as `0.5` are supported. Favorites and recent foods retain the serving definition and quantity so they can be reused and adjusted.

With USDA search, select the matching egg food, wait for portion sizes to load, and choose a portion such as **1 large (50 g)** in the unit menu. Set its amount to **2** and choose **Use this food**. The Add Food form receives the per-egg macros and quantity 2 automatically. USDA supplies the portion weights; the app does not guess them. Available portions vary by food. If a food has no household portions, grams/ounces still work. Loading portion sizes makes one additional USDA request per selected common food; the result is cached for that food in the current search cache.

Older entries have a quantity of **1**, representing the **entire portion originally logged**. For example, an older entry whose serving text says `2 eggs` keeps its original totals with quantity 1. To convert it to individual eggs, change the serving definition to `1 whole egg`, enter the per-egg macros, and set quantity to 2. The app deliberately does not guess what old free-text portions meant.

## Set the time you ate

**Time eaten** is available for both Add Food and Edit. Enter a time such as `8:15 AM`, `7:10 PM`, or `19:10`. New entries start with the current time, but you can change it before saving. Reused favorites/recent foods start with the current time, not the original meal's time. Editing an entry preserves its saved time until you change it.

The date shown above the form is the diary date. Use the Food Log date picker to log or edit food on another day. Changing the time reorders the day's food list chronologically. Invalid times, zero quantities, and negative quantities are rejected. Edited times, quantities, and total macros persist after restarting and in database backups. Food CSV export includes a `quantity` column; its calories/macros are still totals for the entire entry.

The updated build passed 20 quantity/time/portion/migration/restore checks, the 27 core integration checks, and a real-window test that logged two eggs at 8:15 AM, edited the entry to three eggs at 7:10 PM, and verified the stored result.
