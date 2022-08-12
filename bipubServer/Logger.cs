using System;
using System.Data.SqlClient;
using System.IO;
using System.Text;

internal class Logger
{
    private static string thisMonth;

    private string exePath;
    //TODO:
    public string ExePath
    {
        set
        {
            this.exePath = value;
        }
        get
        {
            return this.exePath;
        }
    }

    private string logText;
    private bool timestamp = true;

    public bool Timestamp
    {
        set
        {
            this.timestamp = value;
        }
    }


    //celine update
    private string connString;
    private string pgname;

    public string ConnString
    {
        set
        {
            this.connString = value;
        }
        get
        {
            return this.connString;
        }
    }

    public string Pgname
    {
        set
        {
            this.pgname = value;
        }
        get
        {
            return this.pgname;
        }
    }

    private static void CheckPathExist(string exePath)
    {
        thisMonth = DateTime.Now.ToString("yyyyMM");
        //如果此路徑沒有資料夾
        if (!Directory.Exists(Path.Combine(exePath, thisMonth)))
        {
            //新增資料夾
            Directory.CreateDirectory(Path.Combine(exePath, thisMonth));
        }
    }

    /// <summary>
    /// 寫入檔案
    /// </summary>
    public void WriteFile(string appendFileName)
    {
        CheckPathExist(exePath);

        string thisDate = DateTime.Now.ToString("yyyyMMdd");

        string filePath = Path.Combine(exePath, thisMonth, $"{thisDate}{appendFileName}");
        //把內容寫到目的檔案，若檔案存在則附加在原本內容之後(換行)
        using (FileStream fs = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.None))
        {
            StringBuilder _sb = new StringBuilder();
            if (timestamp)
            {
                string todayMillisecond = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff");
                _sb.Append($"{todayMillisecond}  {logText}").AppendLine();
                //_sb.AppendLine($"{todayMillisecond}  {logText}");
            }
            else
                _sb.Append($"{logText}").AppendLine();
            //_sb.AppendLine($"{logText}");

            using (StreamWriter srOutFile = new StreamWriter(fs, Encoding.Unicode))
            {
                srOutFile.Write(_sb.ToString());
                srOutFile.Flush();
                srOutFile.Close();
            }
        }
    }

    /// <summary>
    /// 附加訊息
    /// </summary>
    /// <param name="_logText">要寫入的訊息</param>
    /// <param name="_timestamp">時間戳記有無</param>
    public void Append(string _logText, bool _timestamp = true)
    {
        timestamp = _timestamp;
        logText = _logText;
        WriteFile(".txt");
    }

    /// <summary>
    /// 附加訊息(_err檔案)
    /// </summary>
    /// <param name="_logText">要寫入的錯誤訊息</param>
    /// <param name="_timestamp">時間戳記有無</param>
    public void AppendErr(string _logText, bool _timestamp = true)
    {
        timestamp = _timestamp;
        logText = _logText;
        WriteFile("_err.txt");
    }
}