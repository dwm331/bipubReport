using HtmlAgilityPack;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using Microsoft.AspNetCore.StaticFiles;
using Newtonsoft.Json;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Reflection;
using System.Text;
using System.Text.Json;

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

        // GIT 位置
        private static string GIT_REPOSITORY = ConfigurationManager.AppSettings["GIT_REPOSITORY"];
        private static string GIT_USERNAME = ConfigurationManager.AppSettings["GIT_USERNAME"];
        private static string GIT_PASSWORD = ConfigurationManager.AppSettings["GIT_PASSWORD"];

        // 圖片轉檔
        private static string IMAGES_JSON_DATA_PATH = ConfigurationManager.AppSettings["IMAGES_JSON_DATA_PATH"];
        private static string IMAGES_PATH = ConfigurationManager.AppSettings["IMAGES_PATH"];
        private static string SQL_FILE_PATH = ConfigurationManager.AppSettings["SQL_FILE_PATH"];
        //private static string SQL_FILE_PATH2 = ConfigurationManager.AppSettings["SQL_FILE_PATH2"];

        // JS更新資料庫檔案
        private static string JS_DATAFILE_PATH = ConfigurationManager.AppSettings["JS_DATAFILE_PATH"];

        private static string connString = ConfigurationManager.ConnectionStrings["connString"].ConnectionString;


        private static string WorkProcess = "BipubServer";

        private static async Task Main(string[] args)
        {
            logger.ExePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            SaveLog($"======開始執行{WorkProcess}程式 version:{version}======");
            SaveLog($"SQL路徑: {connString}");

            // test 
            //string sqlQuery = "select * from quantum";
            //GetDataTable(sqlQuery);

            // 圖片轉sql
            //await getImgage();

            int nums = await GetData(); // 爬蟲網頁資料
            if (nums > 0)
            {
                TransWeekData(); // 轉換Rowdata to week
                await UpdateDBDataJS(); // Write To JS DB file
                UpdateSqlitDB(); // Deploy to GitHub
            }
            else {
                SaveLog($"======Week 沒有資料可以更新======");
            }

            SaveLog($"======結束執行{WorkProcess}程式version:{version}======");
        }

        public static async Task getImgage()
        {
            string[] folderPaths = Directory.GetDirectories(Path.Combine(IMAGES_PATH), "*", SearchOption.TopDirectoryOnly);

            List<string> SimageLists = new List<string>();
            List<string> SpotMapLists = new List<string>();
            foreach (string folderPath in folderPaths)
            {
                //Console.WriteLine($"folderPath: {folderPath} ");
                List<spotData> spotDatas = new List<spotData>();
                string[] filePaths = Directory.GetFiles(folderPath);
                SpotMapJson sd = getSubSpots(Path.GetFileName(folderPath));
                foreach (string filePath in filePaths)
                {
                    string fileName = Path.GetFileName(filePath);
                    Console.WriteLine($" folderName: {Path.GetFileName(folderPath)} -  filePath: {fileName}");
                    if (Path.GetExtension(filePath) == ".txt")
                        break;

                    string mimeType = GetMimeTypeForFileExtension(filePath);
                    string content = @$"data:{mimeType};base64,{GetBase64StringForImage(filePath)}";
                    string uuid = Guid.NewGuid().ToString();
                    string sql = @$"INSERT INTO [dbo].[CheckIn_FileInfo] ([fileId],[type],[content],[create_time]) VALUES('{uuid}', '{mimeType}', '{content}', '{DateTime.Now.ToString("yyyy-MM-dd")}');";
                    SimageLists.Add(sql);

                    //SaveLog(System.Text.Json.JsonSerializer.Serialize(sd));
                    if (sd != null && sd.spots.Count > 0) {
                        SpotJson tmpSpot = getSpot(sd.spots, fileName);
                        if (tmpSpot != null)
                        {
                            // 縮圖
                            Bitmap bmp = (Bitmap)Bitmap.FromFile(filePath);
                            //Console.WriteLine($" Height {bmp.Height} Width {bmp.Width}");
                            Bitmap newImage = ResizeImage(bmp, 52, 52);
                            //SaveLog(Convert.ToBase64String(ImageToByte(newImage))); // Get Base64);

                            // 產生spotAddGMap資料
                            spotDatas.Add(new spotData
                            {
                                seq = getFileNameIndex(fileName),
                                mapName = tmpSpot.mapName,
                                mapPath = tmpSpot.mapPath,
                                mapPic_L = uuid,
                                mapPic_S = Convert.ToBase64String(ImageToByte(newImage))
                            });
                        }
                        else {
                            SaveLog($"===========> fileName not found getSpot {fileName}");
                        }
                    }
                }

                if (sd != null && spotDatas.Count > 0)
                {
                    string up_sql = @$"UPDATE [TPass_WebView_CST].[dbo].[CheckIn_ActivitySpot] SET [spotAddGMap]='{System.Text.Json.JsonSerializer.Serialize(spotDatas)}' WHERE [activitySpotId] = {sd.spot_id} ;";
                    SpotMapLists.Add(up_sql);
                }
            }

            using StreamWriter file = new(SQL_FILE_PATH);
            foreach (string line in SimageLists)
            {
                await file.WriteLineAsync(line);
            }

            //using StreamWriter file2 = new(SQL_FILE_PATH2);
            //foreach (string line in SpotMapLists)
            //{
            //    await file2.WriteLineAsync(line);
            //}
        }
        public static SpotMapJson getSubSpots(string folderName)
        {
            string jsonString = System.IO.File.ReadAllText(IMAGES_JSON_DATA_PATH);
            List<SpotMapJson> entities = JsonConvert.DeserializeObject<List<SpotMapJson>>(jsonString);
            return entities.Where(a => a.folderName == folderName).SingleOrDefault();
        }
        public static SpotJson getSpot(List<SpotJson> spots, string fileName)
        {
            return spots.Where(a => a.fileName == fileName).SingleOrDefault();
        }

        public static int getFileNameIndex(string fileName)
        {
            int fIdx = fileName.IndexOf('(');
            return fIdx > -1 ? int.Parse(fileName.Substring(fIdx + 1, 1)) : 0;
        }


        public static byte[] ImageToByte(Image img)
        {
            ImageConverter converter = new ImageConverter();
            return (byte[])converter.ConvertTo(img, typeof(byte[]));
        }

        public static string GetMimeTypeForFileExtension(string filePath)
        {
            const string DefaultContentType = "application/octet-stream";

            var provider = new FileExtensionContentTypeProvider();

            if (!provider.TryGetContentType(filePath, out string contentType))
            {
                contentType = DefaultContentType;
            }

            return contentType;
        }

        /// <summary>
        /// Resize the image to the specified width and height.
        /// </summary>
        /// <param name="image">The image to resize.</param>
        /// <param name="width">The width to resize to.</param>
        /// <param name="height">The height to resize to.</param>
        /// <returns>The resized image.</returns>
        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        protected static string GetBase64StringForImage(string imgPath)
        {
            byte[] imageBytes = System.IO.File.ReadAllBytes(imgPath);
            string base64String = Convert.ToBase64String(imageBytes);
            return base64String;
        }

        public static void TestPush()
        {
            SaveLog($"======開始Deploy To GitHub======");
            // https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/creating-a-personal-access-token
            var creds = new UsernamePasswordCredentials()
            {
                Username = GIT_USERNAME,
                Password = GIT_PASSWORD
            };
            CredentialsHandler credHandler = (_url, _user, _cred) => creds;
            var fetchOpts = new PushOptions { CredentialsProvider = credHandler };
            using (var repo = new Repository(GIT_REPOSITORY))
            {
           
                Remote remote = repo.Network.Remotes["origin"];
                SaveLog(remote.PushUrl);
                repo.Network.Push(remote, @"refs/heads/main", fetchOpts);

                SaveLog($"======完成Deploy To GitHub======");
            }
        }

        public static void UpdateSqlitDB()
        {
            SaveLog($"======開始Deploy To GitHub======");
            var options = new PushOptions();
            options.CredentialsProvider = (_url, _user, _cred) =>
                new UsernamePasswordCredentials { Username = GIT_USERNAME, Password = GIT_PASSWORD };

            // https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/creating-a-personal-access-token
            using (var repo = new Repository(GIT_REPOSITORY))
            {
                // Stage the file
                repo.Index.Add("bipubServer/SQLite/20220811.db");
                repo.Index.Add("DBdata.js");
                repo.Index.Write();

                // Create the committer's signature and commit
                Signature author = new Signature("Auto Robot", "t9618006@ntut.org.tw", DateTime.Now);
                Signature committer = author;

                // Commit to the repository
                Commit commit = repo.Commit($"Update SQLite! ({DateTime.Today.ToString("yyyy-MM-dd")})", author, committer);

                Remote remote = repo.Network.Remotes["origin"];
                
                //var masterBranch = repo.Branches["master"];
                repo.Network.Push(remote, @"refs/heads/main", options);

                SaveLog($"======完成Deploy To GitHub====== commit:{commit.Id}");
            }
        }

        public static void SaveLog(string msg)
        {
            Console.WriteLine(msg);
            logger.Append(msg);
        }
        public static async Task UpdateDBDataJS()
        {
            SaveLog($"======開始更新JS 資料庫文件======" + JS_DATAFILE_PATH);
            DataTable products = GetDataTable("select * from product");
            List<DB_Product> productList = new List<DB_Product>();
            foreach (DataRow r in products.Rows)
            {
                //Console.WriteLine(JsonSerializer.Serialize(r.ItemArray)); ;
                productList.Add(new DB_Product
                {
                    id = r["id"].ToString(),
                    code = r["code"].ToString(),
                    name = r["name"].ToString()
                });
            }

            DataTable quantums = GetDataTable("select * from quantum");
            List<DB_Quantum> quantumList = new List<DB_Quantum>();
            foreach (DataRow r in quantums.Rows)
            {
                //Console.WriteLine(JsonSerializer.Serialize(r.ItemArray)); ;
                quantumList.Add(new DB_Quantum
                {
                    Date = r["Date"].ToString(),
                    Itemcode = r["Itemcode"].ToString(),
                    Country = r["Country"].ToString(),
                    Weights = r["Weights"].ToString()
                });
            }

            DataTable quantumWeeks = GetDataTable("select * from quantrunWeek");
            List<DB_QuantrunWeek> quantumWeeksList = new List<DB_QuantrunWeek>();
            foreach (DataRow r in quantumWeeks.Rows)
            {
                //Console.WriteLine(JsonSerializer.Serialize(r.ItemArray)); ;
                quantumWeeksList.Add(new DB_QuantrunWeek
                {
                    yyyyww = r["yyyyww"].ToString(),
                    total = r["total"].ToString(),
                    country = r["country"].ToString(),
                    itemcode = r["itemcode"].ToString()
                });
            }
            string[] lines = {
                "let DB_Products = [];",
                "let DB_Quantums = [];",
                "let DB_QuantumWeek = [];",
                $"let DB_ProductsStr = '{System.Text.Json.JsonSerializer.Serialize(productList)}'",
                $"let DB_QuantumsStr = '{System.Text.Json.JsonSerializer.Serialize(quantumList)}'",
                $"let DB_QuantumWeekStr = '{System.Text.Json.JsonSerializer.Serialize(quantumWeeksList)}'",
                $"let DB_UpdateTime = '{DateTime.Today.ToString("yyyy-MM-dd")}'"
            };

            using StreamWriter file = new(JS_DATAFILE_PATH);
            foreach (string line in lines)
            {
                await file.WriteLineAsync(line);
            }
            SaveLog($"======完成更新JS 資料庫文件======");
        }

        public static async Task<int> GetData()
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
                        if(products.Count > 0)
                            insertData(products);
                        return products.Count;
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

            return 0;
        }

        /// <summary>更新每周數量</summary>
        /// <returns></returns>
        private static void TransWeekData()
        {
            SaveLog($"[TransWeekData] 更新資料中");

            string sqlQuery = $@"SELECT strftime('%Y-%W', date ) as yyyyww , sum(weights) as total, country, itemcode
                                            from quantum
                                            GROUP BY yyyyww, itemcode, country;";
            DataTable weeks = GetDataTable(sqlQuery);
            if (weeks.Rows.Count > 0)
            {
                TruncateData("quantrunWeek");
                insertWeekData(weeks);
            }
            else
            {
                SaveLog($"[TransWeekData] 沒有資料需要更新");
            }

            SaveLog($"[TransWeekData] 更新完成");
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

        private static void insertWeekData(DataTable datas)
        {
            SaveLog($"[insertWeekData] 更新資料中");
            var connection = OpenConnection(connString);
            using (SQLiteTransaction tran = connection.BeginTransaction())
            {
                try
                {
                    foreach (DataRow r in datas.Rows)
                    {
                        var cmd = connection.CreateCommand();
                        //cmd.CommandText = $"INSERT INTO quantum VALUES('{p.Date}', '{p.Itemcode}', '{p.Country}', '{p.Weights}')";
                        cmd.CommandText = $@"INSERT INTO quantrunWeek VALUES ('{r["yyyyww"]}', '{r["total"]}', '{r["country"]}', '{r["itemcode"]}');";
                        //Console.WriteLine($@"INSERT INTO quantrunWeek VALUES ('{r["yyyyww"]}', '{r["total"]}', '{r["country"]}', '{r["itemcode"]}');");
                        cmd.ExecuteNonQuery();
                    }
                    tran.Commit();
                    SaveLog($"[insertWeekData] 更新完畢");
                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    SaveLog($"[insertWeekData] 資料錯誤  將關閉程式 {ex.ToString()}");
                    Environment.Exit(0);
                }
                finally
                {
                    if (connection.State == ConnectionState.Open) connection.Close();
                }
            }
        }

        /// <summary>刪除Table</summary>
        /// <param name="datas">資料</param>
        private static void TruncateData(string tableName)
        {
            SaveLog($"[TruncateData] 清除{tableName}舊資料中");
            var connection = OpenConnection(connString);
            using (SQLiteTransaction tran = connection.BeginTransaction())
            {
                try
                {
                    var cmd = connection.CreateCommand();
                    cmd.CommandText = $@"Delete FROM  {tableName};";
                    cmd.ExecuteNonQuery();
                    tran.Commit();
                    SaveLog($"[TruncateData] 刪除完畢");
                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    SaveLog($"[TruncateData] 資料錯誤  將關閉程式 {ex.ToString()}");
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
            ///定義DataTable
            DataTable datatable = new DataTable();
            var connection = OpenConnection(connString);
            try
            {
                using (SQLiteCommand command = new SQLiteCommand(sqlQuery, connection))
                {
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            ///動態添加表的數據列
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                DataColumn myDataColumn = new DataColumn();
                                myDataColumn.DataType = reader.GetFieldType(i);
                                myDataColumn.ColumnName = reader.GetName(i);
                                datatable.Columns.Add(myDataColumn);
                            }

                            ///添加表的數據
                            while (reader.Read())
                            {
                                //Console.WriteLine($"'{reader["yyyyww"]}', '{reader["total"]}', '{reader["country"]}', '{reader["itemcode"]}'");
                                DataRow myDataRow = datatable.NewRow();
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    myDataRow[i] = reader[i].ToString();
                                }
                                datatable.Rows.Add(myDataRow);
                                myDataRow = null;
                            }
                        }
                        reader.Close();
                    }
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
            return datatable;
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

    public class DB_Product
    {
        public string id { get; set; }
        public string code { get; set; }
        public string name { get; set; }
    }

    public class DB_Quantum
    {
        public string Date { get; set; }
        public string Itemcode { get; set; }
        public string Country { get; set; }
        public string Weights { get; set; }
    }

    public class DB_QuantrunWeek
    {
        public string yyyyww { get; set; }
        public string total { get; set; }
        public string country { get; set; }
        public string itemcode { get; set; }
    }
    public class spotData
    {
        public int seq { get; set; }
        public string mapName { get; set; }
        public string mapPath { get; set; }
        public string mapPic_S { get; set; }
        public string mapPic_L { get; set; }
    }
    public class SpotMapJson
    {
        public string folderName { get; set; }
        public string spot_id { get; set; }
        public List<SpotJson> spots { get; set; }
    }

    public class SpotJson
    {
        public string fileName { get; set; }
        public string mapName { get; set; }
        public string mapPath { get; set; }
    }
}