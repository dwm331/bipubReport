# bipubReport
https://dwm331.github.io/bipubReport/

整合平臺上面的沒辦法年份比較
所以實作一版練習

## bipubServer 爬蟲
農糧產銷資訊整合平臺 URL: https://bipub.afa.gov.tw/
### 1.先設定爬蟲bipubServer.dll.config設定檔
 ```sh
connectionString: SQLite檔案位置
APIEndPoint: 爬蟲位址
IMPORTANTCODE: 需要爬的產品CODE
Data_Strat_Time: 可以指定資料起始時間，預設前一天
Data_End_Time: 可以指定資料結束時間，預設今天
```
### 2.執行bipubServer.exe