
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Security.Cryptography;
using System.Globalization;

namespace StockPortfolio.Models
{
	/// <summary>
	/// Summary description for Stock
	/// </summary>
	public class StockDetails : StockSummary
	{
        public float? Ask { get; set; }
        public float? Bid { get; set; }
		public float? DayHigh { get; set; }
		public float? DayLow { get; set; }
		public float? YearHigh { get; set; }
		public float? YearLow { get; set; }
		public double? Volume { get; set; }
		public float? Change { get; set; }
		public string ChangePercent { get; set; }
		public string DateString { get; set; }
        public long? AverageDailyVolume { get; set; }
        public int? AskSize { get; set; }
        public int? BidSize { get; set; }
        public double? PreviousVolume { get; set; }
        public double? Settlement { get; set; }
        public double? PreviousSettlement { get; set; }
	}

	public class StockSummary : Stock
	{
        public float? Open { get; set; }
        public float? Close { get; set; }
		public float? LastSale { get; set; }
		public bool Valid { get; set; }
	}

	public class Stock
	{
		public string Symbol { get; set; }
		public string Name { get; set; }
	}

	public class Stocks
	{
        public static StockSummary GetStockFromBarchart(string symbol)
        {
            StockDetails stock = new StockDetails();
            var fmt = "https://marketdata.websol.barchart.com/getQuote.csv?apikey={0}&symbols={1}&fields={2}&mode=I&jerq=false";
            var token = "a12e94cc748160856f9039fcc6f6177b";
            var fields = "fiftyTwoWkHigh,fiftyTwoWkLow,settlement,previousSettlement,ask,askSize,bid,bidSize,avgVolume";
            // api key 0: the api key from https://www.barchart.com/ondemand
            // symbols 1:A symbol or code that identifies a financial instrument. Multiple symbols separated by a comma may be used.
            // fields 2: the fields requested
            var url = string.Format(fmt, token, symbol, fields);

            // get content
            var sb = new StringBuilder();
            var wc = new WebClient();

            IWebProxy defaultProxy = WebRequest.DefaultWebProxy;
            if (defaultProxy != null)
            {
                defaultProxy.Credentials = CredentialCache.DefaultCredentials;
                wc.Proxy = defaultProxy;
            }

            using (var sr = new StreamReader(wc.OpenRead(url)))
            {
                // skip headers
                var header = sr.ReadLine();
                var headers = new List<string>(header.Split(','));

                var line = sr.ReadLine();
                if (line == null)
                {
                    stock.Valid = false;
                }
                else
                {
                    var items = line.Split(',').Select(item=>item.Trim('"')).ToArray();
                    stock.Open = ParseFloat(items[headers.IndexOf("open")]);
                    stock.Close = ParseFloat(items[headers.IndexOf("close")]);
                    stock.DayHigh = ParseFloat(items[headers.IndexOf("high")]);
                    stock.DayLow = ParseFloat(items[headers.IndexOf("low")]);
                    stock.YearHigh = ParseFloat(items[headers.IndexOf("fiftyTwoWkHigh")]); // as requested
                    stock.YearLow = ParseFloat(items[headers.IndexOf("fiftyTwoWkLow")]); // as requested
                    stock.Volume = ParseDouble(items[headers.IndexOf("volume")]);
                    //stock.PreviousVolume = ParseDouble(items[headers.IndexOf("previousVolume")]);
                    stock.Settlement = ParseDouble(items[headers.IndexOf("settlement")]); // as requested
                    stock.PreviousSettlement = ParseDouble(items[headers.IndexOf("previousSettlement")]); // as requested
                    stock.Symbol = items[headers.IndexOf("symbol")];
                    stock.Name = items[headers.IndexOf("name")];
                    stock.Change = ParseFloat(items[headers.IndexOf("netChange")]);
                    stock.ChangePercent = items[headers.IndexOf("percentChange")];
                    stock.Ask = ParseFloat(items[headers.IndexOf("ask")]); // as requested
                    stock.Bid = ParseFloat(items[headers.IndexOf("bid")]); // as requested
                    stock.AskSize = ParseInt(items[headers.IndexOf("askSize")]); // as requested
                    stock.BidSize = ParseInt(items[headers.IndexOf("bidSize")]); // as requested
                    stock.AverageDailyVolume = ParseLong(items[headers.IndexOf("avgVolume")]); // as requested
                    stock.LastSale = ParseFloat(items[headers.IndexOf("lastPrice")]);
                }
            }

            stock.DateString = DateTime.Now.Date.ToString();
            return stock;
        }

        public static List<StockSummary> GetStocksFromBarchart(string Symbols)
        {
            List<StockSummary> lst = new List<StockSummary>();
            var fmt = "https://marketdata.websol.barchart.com/getQuote.csv?apikey={0}&symbols={1}&fields={2}&mode=I&jerq=false";
            var token = "a12e94cc748160856f9039fcc6f6177b";
            var fields = "ask,bid";
            // api key 0: the api key from https://www.barchart.com/ondemand
            // symbols 1:A symbol or code that identifies a financial instrument. Multiple symbols separated by a comma may be used.
            // fields 2: the fields requested
            var url = string.Format(fmt, token, Symbols, fields);

            // get content
            var sb = new StringBuilder();
            var wc = new WebClient();

            IWebProxy defaultProxy = WebRequest.DefaultWebProxy;
            if (defaultProxy != null)
            {
                defaultProxy.Credentials = CredentialCache.DefaultCredentials;
                wc.Proxy = defaultProxy;
            }

            using (var sr = new StreamReader(wc.OpenRead(url)))
            {
                // skip headers
                var header = sr.ReadLine();
                var headers = new List<string>(header.Split(','));

                int i = 0;
                // read each line
                for (var line = sr.ReadLine(); line != null; line = sr.ReadLine())
                {
                    var items = line.Split(',').Select(item=>item.Trim('"')).ToArray();

                    var stock = new StockSummary();
                    stock.Symbol = items[headers.IndexOf("symbol")];
                    stock.Name = items[headers.IndexOf("name")];
                    stock.Open = ParseFloat(items[headers.IndexOf("open")]); // as requested
                    stock.Close = ParseFloat(items[headers.IndexOf("close")]); // as requested
                    stock.LastSale = ParseFloat(items[headers.IndexOf("lastPrice")]);

                    lst.Add(stock);
                }
            }

            return lst;
        }

        public static List<StockSummary> GetStockHistoryFromQuandl(string symbol, DateTime startDate, DateTime endDate, string interval)
        {
            List<StockSummary> list = new List<StockSummary>();

            var fmt = "https://www.quandl.com/api/v3/datasets/WIKI/{0}.csv?auth_token={1}&start_date={2}&end_date={3}";
            // 0: stock symbol
            // auth_token 1: token
            // start_date 2: date yyyy-MM-dd
            // auth_token 3: date yyyy-MM-dd
            var token = "n5zwD2oCokRjh96x_yxV";
            var url = string.Format(fmt, symbol, token, startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));

            // get content
            var sb = new StringBuilder();
            var wc = new WebClient();

            IWebProxy defaultProxy = WebRequest.DefaultWebProxy;
            if (defaultProxy != null)
            {
                defaultProxy.Credentials = CredentialCache.DefaultCredentials;
                wc.Proxy = defaultProxy;
            }

            using (var sr = new StreamReader(wc.OpenRead(url)))
            {
                // skip headers
                sr.ReadLine();

                int i = 0;
                // read each line
                for (var line = sr.ReadLine(); line != null; line = sr.ReadLine())
                {
                    var items = line.Split(',');

                    var stock = new StockDetails();
                    stock.Open = float.Parse(items[1], CultureInfo.InvariantCulture);
                    stock.Close = float.Parse(items[4], CultureInfo.InvariantCulture);
                    stock.DayHigh = float.Parse(items[2], CultureInfo.InvariantCulture);
                    stock.DayLow = float.Parse(items[3], CultureInfo.InvariantCulture);
                    stock.Volume = double.Parse(items[5], CultureInfo.InvariantCulture);
                    stock.Valid = true;
                    stock.Symbol = symbol;
                    stock.DateString = DateTime.Parse(items[0], CultureInfo.InvariantCulture).ToString(@"MM'/'dd'/'yyyy");

                    list.Add(stock);
                }
            }

            return list;
        }
		
		/// <summary>
		/// Get stock summary by symbol name.
		/// </summary>
		/// <param name="symbol">symbol name</param>
		/// <returns></returns>
		public static StockSummary GetStock(string symbol)
		{
			StockSummary stock = null ;
			try
			{
                stock = GetStockFromBarchart(symbol);
			}
			catch
			{
				try
				{
					CacheData cacheData = new CacheData();
					stock = cacheData.GetCachedStock(symbol);
				}
				catch { }
			}
			return stock;
		}
		/// <summary>
		/// Get stock list by a list of symbol 
		/// </summary>
		/// <param name="Symbols">symbol list (separator ',')</param>
		/// <returns></returns>
		public static List<StockSummary> GetStocks(string Symbols) {
			List<StockSummary> stocks = null;
			try {
				stocks = GetStocksFromBarchart(Symbols);
			}
			catch {
				CacheData cacheData = new CacheData();
				stocks = cacheData.GetCachedStocks();
			}
			return stocks;
		}
		/// <summary>
		/// Get stock history by symbol name and the date range. 
		/// </summary>
		/// <param name="Symbol"></param>
		/// <param name="startDate"></param>
		/// <param name="endDate"></param>
		/// <param name="interval"></param>
		/// <returns></returns>
		public static List<StockSummary> GetStockHistory(string Symbol, DateTime startDate, DateTime endDate, string interval) {
			List<StockSummary> lst = new List<StockSummary>();
			try
			{
				lst = GetStockHistoryFromQuandl(Symbol, startDate, endDate, interval);
			}
			catch {
				try {
					CacheData cacheData = new CacheData();
					List<StockDetails> details = cacheData.GetCachedStockHistory(Symbol, startDate, endDate);
					foreach (StockDetails s in details) 
					{
						lst.Add(s);
					}
				}
				catch { }
			}
			return lst;
		}

		/// <summary>
		/// Get stocks by words start with the name and exchange Id.
		/// </summary>
		/// <param name="name">The words start with name</param>
		/// <param name="exchangeId">exchange Id
		/// possible value: 
		///		1.ASX
		///		2.NYSE
		///		3.NASDAQ
		///		4.AMSE
		/// </param>
		/// <param name="count">set the count of the items</param>
		/// <returns></returns>
		public static IList<Stock> GetStockSymbols(string name, int exchangeId, int count)
		{
			using (StockPortfolio.Models.EntityClass.StockContext context = new EntityClass.StockContext())
			{
				var caseInfo = from info in context.StockEntitys
							   where (info.StockCode.StartsWith(name) || info.StockName.Contains(name)) 
							   && info.ExchangeId == exchangeId
							   orderby info.StockCode
							   select new Stock
							   {
								   Name = info.StockName,
								   Symbol = info.StockCode
							   };
				return caseInfo.Take(count).ToList<Stock>();
			}
		}

		private static XDocument GetYQLXDoc(string url)
		{
			XDocument doc = XDocument.Load(url);
			return doc;
		}

		private static float? ParseFloat(string from)
		{
			float to;
			if (float.TryParse(from, out to))
			{
				return to;
			}
			return null;
		}

        private static int? ParseInt(string from)
        {
            int to;
            if (int.TryParse(from, out to))
            {
                return to;
            }
            return null;
        }

        private static double? ParseDouble(string from)
        {
            double to;
            if (double.TryParse(from, out to))
            {
                return to;
            }
            return null;
        }

        private static long? ParseLong(string from)
        {
            long to;
            if (long.TryParse(from, out to))
            {
                return to;
            }
            return null;
        }
	}
}