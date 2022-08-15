using HtmlAgilityPack;

using System.Configuration;
using System.Data;
using System.Data.SQLite;
using System.Reflection;

namespace BipubServer
{
    internal class Program
    {        
        private static Logger logger = new Logger();

        private static string version = "1.0.0";

        // API URL
        private static string APIEndPoint = ConfigurationManager.AppSettings["APIEndPoint"];

        // API 查詢蔬菜
        private static string IMPORTANTCODE = ConfigurationManager.AppSettings["IMPORTANTCODE"];

        // API 指定時間
        private static string Data_Strat_Time = ConfigurationManager.AppSettings["Data_Strat_Time"];

        // API 指定時間
        private static string Data_End_Time = ConfigurationManager.AppSettings["Data_End_Time"];


        private static string connString = ConfigurationManager.ConnectionStrings["connString"].ConnectionString;


        private static string WorkProcess = "BipubServer";

        private static async Task Main(string[] args)
        {
            logger.ExePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            SaveLog($"======開始執行{WorkProcess}程式 version:{version}======");
            SaveLog($"SQL路徑: {connString}");

            // test 
            //string sqlQuery = "select * from product";
            //GetDataTable(sqlQuery);
            
            string a = await GetData();

            SaveLog($"======結束執行{WorkProcess}程式version:{version}======");
        }
        public static void SaveLog(string msg)
        {
            Console.WriteLine(msg);
            logger.Append(msg);
        }

        public static async Task<string> GetData()
        {
            string startDate = "";
            string endDate = "";
            string querytime = DateTime.Now.ToString("yyyyMMddHHmmssffff");
            if (Data_Strat_Time == "" && Data_End_Time == "")
            {
                startDate = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd");
                endDate = DateTime.Today.ToString("yyyy-MM-dd");
            }
            else {
                startDate = Data_Strat_Time;
                endDate = Data_End_Time;
            }

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.BaseAddress = new Uri(APIEndPoint);
                    client.DefaultRequestHeaders.Clear();
                    //?startdate=2019-05-09&enddate=2019-08-09&importantcode=00201002004000000,00204002001000000,00205002002000000&querytime=1660092678050
                    string param_url = "";
                    param_url = string.Format("?startdate={0}", startDate);
                    param_url += string.Format("&enddate={0}", endDate);
                    param_url += string.Format("&importantcode={0}", IMPORTANTCODE);
                    param_url += string.Format("&querytime={0}", querytime);

                    SaveLog($"[GetData] 取得資料位址: URL:{APIEndPoint + param_url}");

                    var r = await client.GetAsync(param_url);

                    if (r.IsSuccessStatusCode)
                    {
                        string response = r.Content.ReadAsStringAsync().Result.ToString();
                        //Console.WriteLine(response);
                        SaveLog($"[GetData] 讀取資料中");
                        HtmlDocument htmlSnippet = new HtmlDocument();
                        htmlSnippet.LoadHtml(response);
                        List<string> hrefTags = new List<string>();
                        //Console.WriteLine(htmlSnippet.DocumentNode.ToString());

                        List<List<string>> table = htmlSnippet.DocumentNode.SelectNodes("//table")
                                                .Descendants("tr")
                                                .Skip(1)
                                                .Where(tr => tr.Elements("td").Count() > 1)
                                                .Select(tr => tr.Elements("td").Select(td => td.InnerText.Trim()).ToList())
                                                .ToList();
                        //string reqString = System.Text.Json.JsonSerializer.Serialize(table);
                        //Console.WriteLine("===>>" + reqString);

                        List<string> codes = IMPORTANTCODE.Split(",").ToList();
                        int itemIdx = -1;
                        string itemcode =  "";

                        List<Product> products = new List<Product>();
                        List<string> countrys = new List<string>();
                        foreach (List<string> row in table) {
                            int rowlong = row.Count();
                            DateTime dateTime = DateTime.Now;
                            for (int idx = 0; idx < rowlong; idx++)
                            {
                                if (!row.Contains("各國合計(公噸)")) {
                                    // 整理表頭
                                    if (row.Contains("國家"))
                                    {
                                        if (row[idx] == "國家")
                                        {
                                            countrys.Clear();
                                        }

                                        if (row[idx] == "每日小計" && itemIdx < (codes.Count() - 1))
                                        {
                                            itemIdx++;
                                            itemcode = codes[itemIdx];
                                        }

                                        countrys.Add(row[idx]);
                                    }
                                    else 
                                    {
                                        // 整理tabel body
                                        if (idx == 0)
                                        {
                                            dateTime = (DateTime)StringFormatToDate(row[idx]);
                                        }
                                        else if (idx > 0 && idx < rowlong - 1)
                                        {
                                            //Console.WriteLine($" =====> {idx} {dateTime} {itemcode} {countrys[idx]} {row[idx]}");
                                            if (CheckFormat(row[idx])) {
                                                products.Add(new Product
                                                {
                                                    Date = dateTime.ToString("yyyy-MM-dd"),
                                                    Itemcode = itemcode,
                                                    Country = countrys[idx],
                                                    Weights = row[idx]
                                                });
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        //string reqString = System.Text.Json.JsonSerializer.Serialize(products);
                        //Console.WriteLine("===products>>" + reqString);
                        SaveLog($"[GetData] 讀取完畢");
                        insertData(products);
                        return response;
                    }
                    else
                    {
                        SaveLog($"[GetData] 取得資料位址: StatusCode:{r.StatusCode}");

                        string err = r.Content.ReadAsStringAsync().Result.ToString();
                        string repString = System.Text.Json.JsonSerializer.Serialize(err);
                        SaveLog($"[GetData] 取得資料發生錯誤: {repString}");
                    }
                }
            }
            catch (Exception ex)
            {
                SaveLog($"[GetData] 發生不可預期錯誤: {ex.ToString()}");
            }

            return "";
        }

        /// <summary>建立資料庫連線</summary>
        /// <param name="database">資料庫名稱</param>
        /// <returns></returns>
        private static SQLiteConnection OpenConnection(string database)
        {
            var conntion = new SQLiteConnection()
            {
                ConnectionString = $"Data Source={database};"
            };
            if (conntion.State == ConnectionState.Open) conntion.Close();
            conntion.Open();
            return conntion;
        }

        /// <summary>新增</summary>
        /// <param name="datas">資料</param>
        private static void insertData(List<Product> datas)
        {
            SaveLog($"[insertData] 更新資料中");
            var connection = OpenConnection(connString);
            using (SQLiteTransaction tran = connection.BeginTransaction())
            {
                try
                {
                    foreach (Product p in datas)
                    {
                        var cmd = connection.CreateCommand();
                        //cmd.CommandText = $"INSERT INTO quantum VALUES('{p.Date}', '{p.Itemcode}', '{p.Country}', '{p.Weights}')";
                        cmd.CommandText = $@"INSERT INTO quantum(date,itemcode,country,weights) 
                                            SELECT '{p.Date}', '{p.Itemcode}', '{p.Country}', '{p.Weights}'
                                            WHERE NOT EXISTS(SELECT 1 FROM quantum WHERE date = '{p.Date}' AND itemcode = '{p.Itemcode}' AND country = '{p.Country}');";
                        cmd.ExecuteNonQuery();
                    }
                    tran.Commit();
                    SaveLog($"[insertData] 更新完畢");
                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    SaveLog($"[insertData] 資料錯誤  將關閉程式 {ex.ToString()}");
                    Environment.Exit(0);
                }
                finally
                {
                    if (connection.State == ConnectionState.Open) connection.Close();
                }
            }
        }

        /// <summary>讀取資料</summary>
        /// <param name="sqlQuery">資料查詢的 SQL 語句</param>
        /// <returns></returns>
        private static DataTable GetDataTable(string sqlQuery)
        {
            var connection = OpenConnection(connString);
            DataTable dt = new DataTable();
            try
            {
                SQLiteCommand command = new SQLiteCommand(sqlQuery, connection);
                SQLiteDataReader reader = command.ExecuteReader();
                dt.Load(reader);
                //while (reader.Read())
                //    Console.WriteLine("code: " + reader["code"] + "\tname: " + reader["name"]);
                foreach (DataRow r in dt.Rows)
                {
                    Console.WriteLine("code: " + r["code"] + "\tname: " + r["name"]);
                }
            }
            catch (Exception ex)
            {
                SaveLog($"[GetDataTable] 資料錯誤  將關閉程式 {ex.ToString()}");
                Environment.Exit(0);
            }
            finally
            {
                if (connection.State == ConnectionState.Open) connection.Close();
            }
            return dt;
        }


        public static bool CheckFormat(string value)
        {
            decimal number = 0;
            return decimal.TryParse(value, out number);
        }

        public static DateTime? StringFormatToDate(string date)
        {
            // 20220809
            if (date.ToString() != "" && date.ToString().Length == 8)
            {
                string dateStr = "";
                string year = date.ToString().Substring(0, 4);
                string month = date.ToString().Substring(4, 2);
                string day = date.ToString().Substring(6, 2);
                dateStr = year + "-" + month + "-" + day;

                DateTime tmpBirthday;
                if (DateTime.TryParse(dateStr, out tmpBirthday))
                {
                    // OK
                    return tmpBirthday;
                }
            }
            return null;
        }
    }

    public class Product
    {
        public string Date { get; set; }
        public string Itemcode { get; set; }
        public string Country { get; set; }
        public string Weights { get; set; }
    }

}