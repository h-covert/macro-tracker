using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace MacroTracker {
public static class FoodPortion {
    public const string ItemMeasure="Item / serving",GramMeasure="Grams",OunceMeasure="Ounces";
    public static readonly string[] Measures={ItemMeasure,GramMeasure,OunceMeasure};
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
    public static string Measure(string serving) {
        string value=(serving??"").Trim().ToLowerInvariant();
        if(value=="g"||value=="gram"||value=="grams"||value=="1 g"||value=="1 gram"||value=="1 grams")return GramMeasure;
        if(value=="oz"||value=="ounce"||value=="ounces"||value=="1 oz"||value=="1 ounce"||value=="1 ounces"||value=="1 oz (weight)")return OunceMeasure;
        return ItemMeasure;
    }
    public static string Definition(string measure,string item) {
        if(measure==GramMeasure)return "1 gram";
        if(measure==OunceMeasure)return "1 ounce";
        if(string.IsNullOrWhiteSpace(item))throw new ArgumentException("Describe one item / serving, such as 1 whole egg, 1 slice, or 1 bottle.");
        return item.Trim();
    }
    public static string BasisLabel(string measure) {return measure==GramMeasure?"1 gram":measure==OunceMeasure?"1 ounce":"item / serving";}
    public static string AmountDescription(double quantity,string measure,string serving) {
        return measure==GramMeasure?Precise(quantity)+" g":measure==OunceMeasure?Precise(quantity)+" oz":Precise(quantity)+" × "+Definition(measure,serving);
    }
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
