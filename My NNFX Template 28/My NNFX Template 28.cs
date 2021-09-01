using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;

namespace cAlgo.Robots
{
    //Set time zone to Eastern Standard Time EP9-Best time to trade
    [Robot(TimeZone = TimeZones.EasternStandardTime, AccessRights = AccessRights.None)]
    public class MyNNFXTemplate28 : Robot
    {
        //Create Parameters EP10-Functions and Parameters
        [Parameter("Risk %", Group = "Risk Management", DefaultValue = 0.02)]
        public double RiskPct { get; set; }

        [Parameter("SL Factor", Group = "Risk Management", DefaultValue = 1.5)]
        public double SlFactor { get; set; }

        [Parameter("TP Factor", Group = "Risk Management", DefaultValue = 1)]
        public double TpFactor { get; set; }

        [Parameter("Enable Trailing Stop", Group = "Risk Management", DefaultValue = false)]
        public bool EnableTrailingStop { get; set; }


        //Create indicator variables as seen in EP5-ATR
        private AverageTrueRange _atr;
        private string _botName;

        private Symbol[] TradeList;

        protected override void OnStart()
        {
            //Get Name of Bot and Currency pair of current instance as seen in EP15-Deploy
            _botName = GetType().ToString();
            _botName = _botName.Substring(_botName.LastIndexOf('.') + 1) + "_" + SymbolName;

            //Load ATR indicator on start up as seen in EP5-ATR
            _atr = Indicators.AverageTrueRange(14, MovingAverageType.Exponential);

            //Load Currencies

            foreach (Watchlist watchlist in Watchlists)
            {
                if (watchlist.Name == "28Pairs")
                {
                    TradeList = Symbols.GetSymbols(watchlist.SymbolNames.ToArray());
                }
            }
            foreach (Symbol symbol in TradeList)
            {
                var bars = MarketData.GetBars(TimeFrame, symbol.Name);
                bars.BarOpened += OnBarsBarOpened;
            }

            //Load here the specific indicators for this bot



        }

        private void OnBarsBarOpened(BarOpenedEventArgs obj)
        {
            if (!TimeToTrade())
            {
                return;
            }
            // Put your core logic here, see EP7-MACD and EP8-Custom Indicators for examples


        }

        protected override void OnTick() { }

        protected override void OnBar() { }

        //Function for opening a new trade - EP10-Functions and Parameters
        private void Open(TradeType tradeType, string Label)
        {
            //Check there's no existing position before entering a trade
            if (Positions.Find(Label, SymbolName) != null)
            {
                return;
            }

            //Calculate trade amount based on ATR - EP6-Money Management
            double atr = Math.Round(_atr.Result.Last(0) / Symbol.PipSize, 0);
            double tradeAmount = Account.Equity * RiskPct / (1.5 * atr * Symbol.PipValue);
            tradeAmount = Symbol.NormalizeVolumeInUnits(tradeAmount, RoundingMode.Down);

            ExecuteMarketOrder(tradeType, SymbolName, tradeAmount, Label, SlFactor * atr, TpFactor * atr, null, EnableTrailingStop);
        }

        private void Open(string symbolName, TradeType tradeType, string Label)
        {
            //Check there's no existing position before entering a trade
            if (Positions.Find(Label, symbolName) != null)
            {
                return;
            }

            //Calculate trade amount based on ATR - EP6-Money Management
            double atr = Math.Round(_atr.Result.Last(0) / Symbol.PipSize, 0);
            double tradeAmount = Account.Equity * RiskPct / (1.5 * atr * Symbol.PipValue);
            tradeAmount = Symbol.NormalizeVolumeInUnits(tradeAmount, RoundingMode.Down);

            ExecuteMarketOrder(tradeType, symbolName, tradeAmount, Label, SlFactor * atr, TpFactor * atr, null, EnableTrailingStop);
        }

        //Function for closing trades - EP10-Functions and Parameters
        private void Close(TradeType tradeType, string Label)
        {
            foreach (Position position in Positions.FindAll(Label, SymbolName, tradeType))
                ClosePosition(position);
        }

        private void Close(string symbolName, TradeType tradeType, string Label)
        {
            foreach (Position position in Positions.FindAll(Label, symbolName, tradeType))
                ClosePosition(position);
        }

        private bool TimeToTrade()
        {
            if (Server.Time.Hour == 16 && Server.Time.Minute == 29)
            {
                return true;
            }
            return false;
        }
    }
}
