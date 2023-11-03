# bipubReport
https://dwm331.github.io/bipubReport/

整合平臺上面的沒辦法年份比較
所以實作一版練習

## bipubServer 爬蟲
農糧產銷資訊整合平臺 URL: https://pbi.afa.gov.tw/afabi_open/default/index

### 1.先設定爬蟲bipubServer.dll.config設定檔

#### 基本設定

 ```sh
connectionString: SQLite檔案位置(ex: .\bipubServer\SQLite\20220811.db)
APIEndPoint: 爬蟲位址
IMPORTANTCODE: 需要爬的產品CODE
Data_Strat_Time: 可以指定資料起始時間，預設前一天
Data_End_Time: 可以指定資料結束時間，預設今天
```

#### 自動commit, push (筆記，不然換電腦就忘記了)
```sh
GIT_REPOSITORY: 要推扣的Folder
GIT_USERNAME: git account
GIT_PASSWORD: 要使用 PersonalAccessToken
```

設定 [PersonalAccessToken]，設定 Scopes 
a. repo 全開
b. admin:org => read:org
c. user 全開

### 2.執行bipubServer.exe

[PersonalAccessToken]: <https://github.com/settings/tokens>