using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace MacroTracker {
public static class FoodPortion {
    public static void ValidateQuantity(double quantity) {
        if(double.IsNaN(quantity)||double.IsInfinity(quantity)||quantity<=0||quantity>100000)
            throw new ArgumentException("Quantity must be greater than zero and at most 100,000. Fractions such as 0.5 are allowed.");
    }
    public static double Quantity(Dictionary<string,string> entry) {
        if(entry==null||!entry.ContainsKey("quantity"))return 1;
        double q=Store.Num(entry["quantity"]);ValidateQuantity(q);return q;
    }
    public static double[] Scale(double[] one,double quantity) {
        ValidateQuantity(quantity);Store.Validate(one,false);
        var total=one.Select(v=>v*quantity).ToArray();Store.Validate(total,false);return total;
    }
    public static string Precise(double value) {return value.ToString("0.########",CultureInfo.CurrentCulture);}
    public static string NormalizeTime(string input) {
        DateTime parsed;
        string[] formats={"H:mm","HH:mm","H:mm:ss","HH:mm:ss","h:mm tt","hh:mm tt","h:mm:ss tt","hh:mm:ss tt"};
        if(!DateTime.TryParseExact((input??"").Trim().ToUpperInvariant(),formats,CultureInfo.InvariantCulture,DateTimeStyles.AllowWhiteSpaces,out parsed))
            throw new ArgumentException("Enter a time such as 8:30 AM, 6:15 PM, or 18:15.");
        return parsed.ToString("HH:mm:ss",CultureInfo.InvariantCulture);
    }
    public static string Description(Dictionary<string,string> entry) {
        return Precise(Quantity(entry))+" × "+(entry["serving"]==""?"serving":entry["serving"]);
    }
}
}
