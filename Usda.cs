using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace MacroTracker {
    public sealed class UsdaPortion {
        public string Label;
        public double Grams;
        public string Display {get{return Label+" ("+FoodPortion.Precise(Grams)+" g)";}}
    }
    public sealed class UsdaFood {
        public string Id, Name, Brand, Type, BasisUnit;
        public double?[] Macros = new double?[4];
        public double? ServingSize;
        public string ServingLabel;
        public List<UsdaPortion> Portions=new List<UsdaPortion>();
        public bool PortionsLoaded;

        public string MacroText(double amount) {
            return string.Join("   ·   ", Macros.Select((v,i) => Store.MacroNames[i]+" "+
                (v.HasValue ? Store.F(v.Value*amount/100)+(i==0?" kcal":"g") : "not supplied")));
        }
        public double BaseAmount(double amount, string unit) {
            if(double.IsNaN(amount)||double.IsInfinity(amount)||amount<=0||amount>100000)
                throw new ArgumentException("Enter a serving amount greater than zero (maximum 100,000).");
            double result;
            var portion=Portions.FirstOrDefault(p=>p.Display==unit);
            if(portion!=null)result=amount*portion.Grams;
            else if(unit=="Label servings" && ServingSize.HasValue) result=amount*ServingSize.Value;
            else if(unit==BasisUnit) result=amount;
            else if(unit=="oz (weight)" && BasisUnit=="g") result=amount*28.349523125;
            else if(unit=="US fl oz" && BasisUnit=="ml") result=amount*29.5735295625;
            else throw new ArgumentException("Select a supported serving unit.");
            if(result>100000)throw new ArgumentException("That serving is too large.");
            return result;
        }
    }

    // A personal key is encrypted for this Windows user and kept outside exports/backups.
    public sealed class UsdaKey {
        readonly string path;
        public UsdaKey(string databasePath) { path=databasePath+".usda-key"; }
        public string Read() {
            if(!File.Exists(path))return "DEMO_KEY";
            try {return Encoding.UTF8.GetString(ProtectedData.Unprotect(File.ReadAllBytes(path),null,DataProtectionScope.CurrentUser));}
            catch(CryptographicException) {throw new InvalidOperationException("Your saved USDA key cannot be unlocked on this Windows account. Open USDA setup and save your key again, or use demo mode.");}
        }
        public bool Personal {get{return File.Exists(path);}}
        public void Save(string key) {
            key=(key??"").Trim();
            if(key.Length==0 || key=="DEMO_KEY") {if(File.Exists(path))File.Delete(path);return;}
            if(key.Length<10 || key.Length>128 || key.Any(c=>!char.IsLetterOrDigit(c)))
                throw new ArgumentException("Paste only the USDA API key, without quotes or spaces.");
            byte[] bytes=ProtectedData.Protect(Encoding.UTF8.GetBytes(key),null,DataProtectionScope.CurrentUser);
            string pending=path+".tmp"; File.WriteAllBytes(pending,bytes);
            if(File.Exists(path)) File.Replace(pending,path,null); else File.Move(pending,path);
        }
    }

    public sealed class UsdaClient {
        readonly Dictionary<string,List<UsdaFood>> cache=new Dictionary<string,List<UsdaFood>>();
        public async Task LoadPortions(UsdaFood food,string key,CancellationToken cancellation) {
            if(food.PortionsLoaded||food.Type=="Branded")return;
            if(food.Id.Any(c=>!char.IsDigit(c)))throw new InvalidOperationException("USDA returned an invalid food ID.");
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            using(var handler=new HttpClientHandler{AllowAutoRedirect=false})
            using(var client=new HttpClient(handler){Timeout=TimeSpan.FromSeconds(20)}) {
                try {
                    using(var response=await client.GetAsync("https://api.nal.usda.gov/fdc/v1/food/"+food.Id+"?api_key="+Uri.EscapeDataString(key),cancellation)) {
                        if(!response.IsSuccessStatusCode)throw new InvalidOperationException(Error((int)response.StatusCode));
                        string json=await response.Content.ReadAsStringAsync();cancellation.ThrowIfCancellationRequested();
                        food.Portions=ParsePortions(json);food.PortionsLoaded=true;
                    }
                }catch(TaskCanceledException){if(cancellation.IsCancellationRequested)throw;throw new InvalidOperationException("Portion lookup timed out. You can still use grams or ounces.");}
                catch(HttpRequestException){throw new InvalidOperationException("Couldn't load USDA portion sizes. You can still use grams or ounces.");}
            }
        }
        public static List<UsdaPortion> ParsePortions(string json) {
            try {
                var data=new JavaScriptSerializer{MaxJsonLength=4000000}.Deserialize<Dictionary<string,object>>(json);
                var portions=new List<UsdaPortion>();object list;
                if(!data.TryGetValue("foodPortions",out list)||!(list is System.Collections.IEnumerable))return portions;
                foreach(var raw in (System.Collections.IEnumerable)list) {
                    var row=raw as Dictionary<string,object>;if(row==null)continue;
                    var grams=Number(row,"gramWeight");if(!grams.HasValue||grams.Value<=0||grams.Value>100000)continue;
                    string modifier=Text(row,"modifier");string description=Text(row,"portionDescription");
                    double amount=Number(row,"amount")??1;if(amount<=0)continue;
                    string label=modifier==""?description:FoodPortion.Precise(amount)+" "+modifier;
                    if(label=="") {
                        object measure; if(row.TryGetValue("measureUnit",out measure)&&measure is Dictionary<string,object>) {
                            string unit=Text((Dictionary<string,object>)measure,"name");
                            if(unit!=""&&unit!="undetermined")label=FoodPortion.Precise(amount)+" "+unit;
                        }
                    }
                    if(label=="")continue;
                    var portion=new UsdaPortion{Label=label,Grams=grams.Value};
                    if(!portions.Any(p=>p.Display==portion.Display))portions.Add(portion);
                }
                return portions;
            }catch{throw new InvalidOperationException("USDA portion sizes weren't readable. You can still use grams or ounces.");}
        }
        public async Task<List<UsdaFood>> Search(string query,string scope,string key,CancellationToken cancellation) {
            query=(query??"").Trim();
            if(query.Length<2 || query.Length>200)throw new ArgumentException("Enter 2–200 characters, such as chicken breast or Greek yogurt.");
            string cacheKey=scope+"|"+query.ToLowerInvariant(); List<UsdaFood> saved;
            if(cache.TryGetValue(cacheKey,out saved))return saved;
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            string[] types=scope=="Branded foods" ? new[]{"Branded"} : new[]{"Foundation","SR Legacy","Survey (FNDDS)"};
            string url="https://api.nal.usda.gov/fdc/v1/foods/search?api_key="+Uri.EscapeDataString(key);
            string payload=new JavaScriptSerializer().Serialize(new{query=query,pageSize=25,dataType=types});
            using(var handler=new HttpClientHandler{AllowAutoRedirect=false})
            using(var client=new HttpClient(handler){Timeout=TimeSpan.FromSeconds(20)}) {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("MacroTracker/1.1");
                try {
                    using(var body=new StringContent(payload,Encoding.UTF8,"application/json"))
                    using(var response=await client.PostAsync(url,body,cancellation)) {
                        if(!response.IsSuccessStatusCode)throw new InvalidOperationException(Error((int)response.StatusCode));
                        string json=await response.Content.ReadAsStringAsync();
                        cancellation.ThrowIfCancellationRequested();
                        var foods=Parse(json); if(cache.Count>=100)cache.Clear(); cache[cacheKey]=foods; return foods;
                    }
                } catch(TaskCanceledException) {
                    if(cancellation.IsCancellationRequested)throw;
                    throw new InvalidOperationException("USDA took too long to respond. Please try again. You can still add food manually.");
                } catch(HttpRequestException) {throw new InvalidOperationException("Couldn't reach USDA. Check your internet connection and try again. Saved foods still work offline.");}
            }
        }
        public static string Error(int status) {
            if(status==401||status==403)return "USDA didn't accept the API key. Open USDA setup to replace it or use demo mode.";
            if(status==429)return "USDA's request limit has been reached. Try later or add your free personal key in USDA setup. Demo mode allows 30 requests/hour and 50/day.";
            return "USDA search is temporarily unavailable (HTTP "+status+"). Please try again later.";
        }
        static string Text(Dictionary<string,object> data,string name) {object value;return data.TryGetValue(name,out value)&&value!=null?Convert.ToString(value,CultureInfo.InvariantCulture):"";}
        static double? Number(Dictionary<string,object> data,string name) {
            double value;return double.TryParse(Text(data,name),NumberStyles.Float,CultureInfo.InvariantCulture,out value)&&!double.IsNaN(value)&&!double.IsInfinity(value)&&value>=0?(double?)value:null;
        }
        public static List<UsdaFood> Parse(string json) {
            try {
                var root=new JavaScriptSerializer{MaxJsonLength=4000000}.Deserialize<Dictionary<string,object>>(json);
                var result=new List<UsdaFood>(); object array;
                if(!root.TryGetValue("foods",out array) || !(array is System.Collections.IEnumerable))throw new FormatException();
                foreach(var raw in (System.Collections.IEnumerable)array) {
                    var row=raw as Dictionary<string,object>;if(row==null)continue;
                    var food=new UsdaFood{Id=Text(row,"fdcId"),Name=Text(row,"description"),Brand=Text(row,"brandOwner"),Type=Text(row,"dataType"),BasisUnit="g",ServingLabel=Text(row,"householdServingFullText")};
                    if(food.Brand=="")food.Brand=Text(row,"brandName");
                    if(food.Id==""||food.Name=="")continue;
                    string servingUnit=Text(row,"servingSizeUnit").ToLowerInvariant();
                    if(food.Type=="Branded") {
                        // Branded values use the metric basis recorded for the label: 100g or 100ml.
                        if(servingUnit=="ml")food.BasisUnit="ml";
                        else if(servingUnit!="g")continue; // Never guess an unknown mass/volume conversion.
                        food.ServingSize=Number(row,"servingSize");
                        if(food.ServingSize<=0)food.ServingSize=null;
                    }
                    var nutrients=new Dictionary<int,double>();object list;
                    if(row.TryGetValue("foodNutrients",out list)&&list is System.Collections.IEnumerable) {
                        foreach(var n in (System.Collections.IEnumerable)list) {
                            var item=n as Dictionary<string,object>; if(item==null)continue;
                            int id; var value=Number(item,"value");
                            if(!int.TryParse(Text(item,"nutrientId"),out id)||!value.HasValue)continue;
                            string unit=Text(item,"unitName").ToUpperInvariant();
                            if(((id==1008||id==2047||id==2048)&&unit=="KCAL") ||
                               ((id==1003||id==1004||id==1005)&&unit=="G"))nutrients[id]=value.Value;
                        }
                    }
                    // Select one energy measure; never add Atwater and legacy energy together.
                    foreach(int id in new[]{1008,2048,2047})if(nutrients.ContainsKey(id)){food.Macros[0]=nutrients[id];break;}
                    int[] ids={1003,1005,1004};for(int i=0;i<3;i++)if(nutrients.ContainsKey(ids[i]))food.Macros[i+1]=nutrients[ids[i]];
                    if(food.Macros.Any(v=>v.HasValue))result.Add(food);
                }
                return result;
            } catch(Exception e) {
                if(e is OutOfMemoryException)throw;
                throw new InvalidOperationException("USDA returned an unexpected response. Please try again later.");
            }
        }
    }
}
