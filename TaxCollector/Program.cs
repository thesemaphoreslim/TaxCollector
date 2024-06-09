using System.Net;
using System.Text.Json;
using System.Web;
using System.Globalization;
using System.Text;
using System.Configuration;

public class RootObject
{
    public IDictionary<string, Data> data { get; set; }
}

public class Data
{
    public int id { get; set; } 
    public string name { get; set; }
    public string symbol { get; set; }
    public decimal amount { get; set; }
    public Quote quote { get; set; }
}
public class Quote
{
    public USD USD { get; set; }
}

public class USD
{
    public decimal price { get; set; }
    public string last_update { get; set; }

}


static class Program
{
    public static Dictionary<string, decimal> lastamounts = new Dictionary<string, decimal>();
    public static Dictionary<string, string> balanceuri = new Dictionary<string, string>();
    public static Dictionary<string, decimal> globalprices = new Dictionary<string, decimal>();
    public static Dictionary<string, int> cmcids = new Dictionary<string, int>();
    public static Dictionary<string, decimal> multipliers = new Dictionary<string, decimal>();
    public static Dictionary<string, string> keywords = new Dictionary<string, string>();
    public static Dictionary<string, decimal> coinbalance = new Dictionary<string, decimal>();
    public static NumberFormatInfo setPrecision = new NumberFormatInfo();
    
    static void Main(string[] args)
    {
        Task t = CollectTaxes(args);
        t.Wait();
        //Console.ReadKey(true);
    }
    
    static async Task CollectTaxes(string[] args)
    {
        bool newamounts = false;
        setPrecision.NumberDecimalDigits = 2;
        decimal lastamount = 0;
        try
        {
            foreach (string id in ConfigurationManager.AppSettings.Get("cmcids").Split(","))
            {
                cmcids.Add(id.Split(":")[0], Convert.ToInt32(id.Split(":")[1]));
            }
        }
        catch (Exception ex)
        {
            LogData("Error getting cmcids: " + ex.Message);
        }

        try
        {
            foreach (string multiplier in ConfigurationManager.AppSettings.Get("balance_multiplier").Split(","))
            {
                multipliers.Add(multiplier.Split(":")[0], Convert.ToDecimal(multiplier.Split(":")[1]));
            }
        }
        catch (Exception ex)
        {
            LogData("Error getting multipliers: " + ex.Message);
        }

        try
        {
            foreach (string keyword in ConfigurationManager.AppSettings.Get("balance_keywords").Split(","))
            {
                keywords.Add(keyword.Split(":")[0], keyword.Split(":")[1]);
            }
        }
        catch (Exception ex)
        {
            LogData("Error getting keywords: " + ex.Message);
        }

        try
        {
            foreach (string uri in ConfigurationManager.AppSettings.Get("balanceuri").Split(","))
            {
                balanceuri.Add(uri.Split("|")[0], uri.Split("|")[1]);
            }
        }
        catch (Exception ex)
        {
            LogData("Error getting balanceuri: " + ex.Message);
        }

        try
        {
            if (File.Exists(ConfigurationManager.AppSettings.Get("lastamountfile")))
            {
                foreach(string line in File.ReadAllLines(ConfigurationManager.AppSettings.Get("lastamountfile")))
                {
                    lastamounts.Add(line.Split('|')[0], Convert.ToDecimal(line.Split("|")[1]));    
                }
            }
        }
        catch (Exception ex)
        {
            LogData("Error getting last amount from file: " + ex.Message);
        }

        //string coins = null;
        StringBuilder coins = new StringBuilder();
        try
        {
            foreach (string coin in ConfigurationManager.AppSettings.Get("coins").Split(","))
            {
                if (coins.Length == 0)
                {
                    if (cmcids.ContainsKey(coin))
                    {
                        coins.Append(cmcids[coin]);
                    }
                }
                else if (coins.Length > 0)
                {
                    coins.Append(",").Append(cmcids[coin]);
                }
            }
        }
        catch (Exception ex)
        {
            LogData("Error getting CoinMarketCap ids from config file: " + ex.Message);
            return;
        }
        if (coins.Length == 0)
        {
            LogData("No coin ids found.");
            return;
        }
        Console.WriteLine("Fetching prices for the following coin ids: " + coins.ToString());
        string pricedata = "none";
        try
        {
            pricedata = await getPrice(coins.ToString());
        }
        catch (Exception ex)
        {
            LogData("Error getting price" + ex.Message);
        }
        //Console.WriteLine(pricedata);
        try
        {
            if (pricedata != "none")
            {
                RootObject? root = JsonSerializer.Deserialize<RootObject>(pricedata);
                if (root != null)
                {
                    foreach (var data in root.data)
                    {
                        Console.WriteLine(data.Value.symbol.ToLower() + "," + data.Value.quote.USD.price);
                        globalprices.Add(data.Value.symbol.ToLower(), data.Value.quote.USD.price);
                    }
                    //Console.WriteLine("USD Price: " + root.data.quote.USD.price);
                    //globalprices.Add(coin, root.data.quote.USD.price);
                }
                else
                {
                    LogData("Price api returned no data");
                    return;
                }
            }
            else
            {
                LogData("Price api returned no data");
                return;
            }
        }
        catch (Exception ex)
        {
            LogData("Error getting price: " + ex.Message);
            return;
        }

        foreach (string coin in ConfigurationManager.AppSettings.Get("coins").Split(","))
        {
            if (lastamounts.ContainsKey(coin))
            {
                lastamount = lastamounts[coin];
            }
            else
            {
                lastamount = 0;
            }
            decimal totalcoins = 0;
            bool getbalanceerror = false;
            try
            {
                string balancedata = await getBalance(balanceuri[coin], coin);
                if (!string.IsNullOrEmpty(balancedata) && balancedata != "none")
                {
                    totalcoins = (Convert.ToDecimal(balancedata.Substring(balancedata.IndexOf(keywords[coin])).Split(":")[1].Split(",")[0].Replace("\"", "").Replace("}", "")) / Convert.ToDecimal(multipliers[coin].ToString("N", setPrecision)));
                    coinbalance.Add(coin, totalcoins);
                }
                else
                {
                    getbalanceerror = true;
                }
            }
            catch (Exception ex)
            {
                LogData("Error getting " + coin + " balance data: " + ex.Message);
                getbalanceerror = true;
            }
            try
            {
                if (lastamount.ToString("N", setPrecision) != totalcoins.ToString("N", setPrecision) && !getbalanceerror)
                {
                    newamounts = true;
                    if (globalprices.ContainsKey(coin))
                    {
                        if (lastamount < totalcoins)
                        {
                            File.AppendAllText(ConfigurationManager.AppSettings.Get("historyfile"), string.Concat(coin, "|ADDED", "|", (totalcoins - lastamount).ToString("N", setPrecision), "|$", globalprices[coin].ToString("N", setPrecision), "|", totalcoins.ToString("N", setPrecision), "|", DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss tt"), Environment.NewLine));
                        }
                        else if (lastamount > totalcoins)
                        {
                            File.AppendAllText(ConfigurationManager.AppSettings.Get("historyfile"), string.Concat(coin, "|SPENT", "|", (lastamount - totalcoins).ToString("N", setPrecision), "|$", globalprices[coin].ToString("N", setPrecision), "|", totalcoins.ToString("N", setPrecision), "|", DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss tt"), Environment.NewLine));
                        }
                    }
                    else
                    {
                        LogData("New amount found but missing pricing data for " + coin);
                    }
                }
            }
            catch (Exception ex)
            {
                LogData("Error writing history file: " + ex.Message);
            }

        }
        if (newamounts)
        {
            try
            {
                if (File.Exists(ConfigurationManager.AppSettings.Get("lastamountfile")))
                {
                    File.Delete(ConfigurationManager.AppSettings.Get("lastamountfile"));
                }
                foreach (string coin in ConfigurationManager.AppSettings.Get("coins").Split(","))
                {
                    if (coinbalance.ContainsKey(coin))
                    {
                        File.AppendAllText(ConfigurationManager.AppSettings.Get("lastamountfile"), coin + "|" + coinbalance[coin].ToString("N", setPrecision) + Environment.NewLine);
                    }
                }
            }
            catch (Exception ex)
            {
                LogData("Error appending data to lastamountfile: " + ex.Message);
            }
        }
    }

    static async Task<string> getPrice(string coin)
    {
        try
        {
            using (HttpClient httpClient = new HttpClient())
            {
                UriBuilder requestUri = new UriBuilder(ConfigurationManager.AppSettings.Get("priceuri"));
                var queryString = HttpUtility.ParseQueryString(string.Empty);
                queryString["id"] = coin;
                queryString["convert"] = "USD";
                requestUri.Query = queryString.ToString();
                HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri.ToString());
                requestMessage.Headers.Add("X-CMC_PRO_API_KEY", ConfigurationManager.AppSettings.Get("cmcapikey"));
                requestMessage.Headers.Add("Accepts", "application/json");
                HttpResponseMessage response = await httpClient.SendAsync(requestMessage);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    LogData("Error getting " + coin + " price info: " + response.StatusCode);
                    return "none";
                }
            }
        }
        catch (Exception ex)
        {
            LogData("Error calling price api for " + coin + ": " + ex.Message);
            return "none";
        }
    }

    static async Task<string> getBalance(string address, string coin)
    {
        try
        {
            Uri requestUri = new Uri(address);
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri);
            foreach (string header in ConfigurationManager.AppSettings.Get("headers").Split(","))
            {
                if (header.Split(":")[0] == coin)
                {
                    requestMessage.Headers.Add(header.Split(":")[1].Split("|")[0], header.Split(":")[1].Split("|")[1]);
                }
            }
            using (HttpClient httpClient = new HttpClient())
            {
                HttpResponseMessage response = await httpClient.SendAsync(requestMessage);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    LogData("Error getting " + coin + " balance info: " + response.StatusCode);
                    return "none";
                }
            }
        }
        catch (Exception ex)
        {
            LogData("Error calling " + coin + " balance api: " + ex.Message);
            return "none";
        }
    }

    static void LogData(string data)
    {
        try
        {
            File.AppendAllText(ConfigurationManager.AppSettings.Get("logfile"), string.Concat(DateTime.Now,", ", data, Environment.NewLine));
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error writing log file: " + ex.Message);
        }
    }
}

