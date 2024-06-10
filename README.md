# TaxCollector

This application monitors a farmer reward address (Chia) and/or a staking address (Cardano) for changes in balance.  When a wallet balance changes, the delta is recorded along with the current market price (obtained from CMC api).  This information is stored in a pipe-separated (|) text file for use during tax season to calculate income from farming/mining/staking (example below).

What you need:
 - The application is writeen in C# so you will need to run it on a Windows PC with at least dotnet version 6.0.
 - A CoinMarketCap API key.  You can obtain a Basic api key for free from https://coinmarketcap.com/api/. This will provide you with a limited number of API calls per 24 hours (more on that later).
 - Your XCH farmer reward address and/or ADA staking address.
 - You will need to configure a Windows Scheduled Task that runs the TaxCollector app periodically (no more frequently than 5 minutes due to API restrictions).
 - (Optional) A Blockfrost API key.  This is only required if you want to track Cardano staking rewards. This can be obtained for free from blockfrost.io.
 - When you wish to calculate your total income, you will need a spreadsheet application like MS Excel or OpenOffice Calc to import the data. You will import it as a pipe-delimited file and then you can sort by coin, add/spend, and/or datetime.

What you DON'T need:
 - The application obtains wallet balances from external sources (xchscan and blockfrost), so you do NOT need a full node/wallet installed on the PC where TaxCollector will run.
 - Capturing balance changes at least once per day is recommended. The PC with TaxCollector installed does NOT need to be operational/online 24/7.  Running it on your daily-use work/gaming PC or even your leisure Surface Pro is fine as long as you typically boot up that device daily.

Limitations:
 - The application does NOT obtain historical reward information. It captures rewards on a current/go-forward basis.
 - The application treats any negative delta (coins leaving the wallet) as a spend, even you are simply moving your balance to a different wallet (cold/hardware/etc). However, you can updated the "SPENT" value in the log file with clarifying text (like "TRANSFERRED TO HARDWARE WALLET").
 - When you first run the application (and ONLY on first run), your total wallet balance will be logged to the history file as "ADDED". You can update this text in the log file with something like "INITIAL BALANCE" to avoid confusion at tax time. After the initial run, only deltas will be logged.

TaxCollector.dll.config.
 - balance_keywords: used to determine the coin denomination returned by the api (DO NOT MODIFY)
 - balance_multiplier: used in conjunction with "balance_keywords" to calculate coin balance in XCH/ADA (DO NOT MODIFY)
 - balanceuri: a comma-separated and pipe-delimited value used to identify the api for each respective coin (XCH/ADA). Modify this value with your farmer reward address (Chia) or staking address (Cardano).
 - cmcapikey: your CoinMarketCap API key. Modify this value with the API key you receive from https://coinmarketcap.com/api/.  DO NOT share API keys with other individuals as this can cause overutilization of CMC's Basic api allowance and result in failed api calls.
 - cmcids: used by the CoinMarketCap API to correlate coins using their respective CMC-assigned IDs (DO NOT MODIFY)
 - coins: a comma-separated list of the coins you wish to track rewards for. Remove any coins you do not wish to track. Acceptable values are:
     xch
     ada
     xch,ada
 - headers: the Blockfrost API key used to obtain Cardano balance. If you wish to track Cardano rewards, obtain a free API key from Blockfrost.io and enter it as shown "ada:project_id|<YOUR API KEY HERE>". DO NOT share API keys with other individuals as this can cause overutilization of Blockfrost's free api allowance and result in failed api calls.
 - historyfile: a pipe-separated log file where all balance changes will be logged. Modify this value to be the location where you want your balance changes recorded. The application will update this file automatically when it runs and finds delta. You may also modify this file and your changes will NOT be overwritten.
 - lastamountfile: location of temp file used by the application to track balance changes. Modify this value to match the location where you install the application but do not modify the lastamount.txt file itself to avoid balance calculation errors.
 - logfile: location of the log file where errors/warnings are captured. Modify this value to match the location where you install the application.
 - priceuri: the CoinMarketCap api URI. (DO NOT MODIFY)

History File Example:
COIN|DIRECTION|DELTA|CURRENT PRICE|WALLET BALANCE|DATE/TIME

xch|ADDED|0.46|$32.77|11.82|05/11/2024 07:05:02 AM
xch|ADDED|0.12|$32.24|11.94|05/12/2024 04:30:02 AM
xch|ADDED|0.46|$32.62|12.40|05/12/2024 07:05:03 AM
xch|ADDED|0.33|$31.89|12.73|05/13/2024 07:05:02 AM
xch|ADDED|0.12|$31.28|12.85|05/13/2024 15:10:04 PM
xch|ADDED|0.42|$30.98|13.27|05/14/2024 07:05:03 AM
ada|ADDED|8.45|$0.43|20,316.82|05/14/2024 16:50:03 PM
xch|ADDED|0.38|$30.21|13.65|05/15/2024 07:05:03 AM
xch|SPENT|12.97|$31.19|0.68|05/15/2024 08:20:03 AM
xch|ADDED|0.32|$30.70|1.00|05/16/2024 07:05:02 AM
xch|ADDED|0.54|$30.72|1.54|05/17/2024 07:05:03 AM
xch|ADDED|0.13|$30.74|1.67|05/17/2024 08:20:04 AM

NOTE: In the above example, if your "spend" on 5/15 was actually transferring 12.97 XCH to your hardware wallet, you can replace "SPENT" with "TRANSFERRED TO HARDWARE WALLET". This notation will NOT be overwritten by the TaxCollector app.

It is likely possible to capture rewards from other coins using this application as long as they have APIs available to collect wallet balances; however, the app has only been tested and verified working with Chia and Cardano.
