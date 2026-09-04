# Set up USDA food search

## Try it immediately

1. Exit the older Macro Tracker using its **tray menu → Exit** (closing its window may only hide it).
2. Extract this updated package and open **MacroTracker.exe**. It uses the same local diary database; your existing entries stay available.
3. Choose **Add food → Search USDA foods**.
4. Enter a food name and press **Search**. Start with **Common foods** for ingredients and prepared foods, or **Branded foods** for specific packaged products.
5. Select a matching result, enter the amount you ate, and choose **Use this food**.
6. Review the filled-in macros, select your meal category, and press **Add food** or Enter.

The official `DEMO_KEY` is active automatically until you save a personal key. No registration is needed to test it, but USDA limits the shared demo service to 30 requests per hour and 50 per day per IP address.

## Get your free personal key

1. Open [USDA's API key signup](https://api.data.gov/signup/), or use **USDA setup → Get a free USDA API key** inside the app.
2. Complete USDA's signup form yourself and obtain your key.
3. In the app, open **Settings → Set up USDA search** (or **USDA setup** inside Search).
4. Paste your key into the password field and choose **Save personal key**.
5. Choose **Done**, then search for a food.

Keep the key private; enter it directly in the app. It is encrypted with Windows user protection in a `.usda-key` file next to your database. It is separate from your database, CSV exports, and database backups. On a different Windows account or a new PC, enter the key again. **Use demo key** removes the saved personal key. A saved key is validated by the next uncached search; invalid keys produce a setup message without exposing the key.

The standard USDA limit is currently 1,000 requests per hour per IP address. See the [official API guide](https://fdc.nal.usda.gov/api-guide/) for current limits and terms.

## Portions and data quality

- Common-food nutrients are scaled from 100 grams. You can enter grams or ounces by weight.
- Branded products use their USDA metric basis: 100 grams or 100 milliliters. For products with a label serving size, you can also select **Label servings** and enter a quantity such as `1` or `0.5`.
- Volume-based branded products offer milliliters and US fluid ounces. The app never treats fluid ounces as weight ounces or guesses a density.
- Branded entries with an unknown serving unit are omitted to avoid an unsupported conversion. Up to 25 results are requested; refine your food name or brand if needed.
- Check the description and brand, especially raw versus cooked foods. USDA may have multiple records for the same product.
- Missing nutrients are shown as **not supplied** and left blank when filling the form. Supply those values before saving; a missing value is not silently treated as zero.
- The app uses reported calories independently of the protein/carbs/fat calculation and avoids adding duplicate energy measures.
- Source and FDC ID are recorded in the entry notes. Favorites and recent foods work with imported foods exactly as with manually entered foods.

New searches require internet and send your search query to USDA. Your diary, weight, and targets are not sent. Repeated search results are cached in memory for the current app session; foods you save remain reusable offline. You can always log food manually if the service is unavailable.

If you enabled Windows startup from the old folder, turn that setting off and back on from this updated app so Windows launches the new executable.

## Verification

The updated executable was built and tested with 27 core integration checks, 18 USDA checks including a real USDA demo-key request, and a real-window UI test covering search, selection, 150g scaling, autofill, and saving verified macros to an isolated database. The UI test uses a deterministic USDA-shaped fixture; the network test independently uses the live service.

USDA sources: [API guide](https://fdc.nal.usda.gov/api-guide/), [common-food portions](https://fdc.nal.usda.gov/Foundation_Foods_Documentation/), and [branded-food documentation](https://fdc.nal.usda.gov/GBFPD_Documentation/).
