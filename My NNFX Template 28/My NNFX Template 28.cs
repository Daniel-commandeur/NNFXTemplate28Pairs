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
        //Parameters for the Template Risk Management
        [Parameter("Risk %", Group = "Risk Management", DefaultValue = 0.02)]
        public double RiskPct { get; set; }

        [Parameter("SL Factor", Group = "Risk Management", DefaultValue = 1.5)]
        public double SlFactor { get; set; }

        [Parameter("TP Factor", Group = "Risk Management", DefaultValue = 1)]
        public double TpFactor { get; set; }

        //Parameters for the Template General Settings
        [Parameter("Trade Method", Group = "General Settings", DefaultValue = TradeMethod.MultipleInstuments)]
        public TradeMethod TradeMethodSelected { get; set; }

        [Parameter("Name of WatchList", Group = "General Settings", DefaultValue = "28Pairs")]
        public string WatchListName { get; set; }

        [Parameter("Trade Hour", Group = "General Settings", DefaultValue = "55")]
        public string TradeHour { get; set; }

        [Parameter("Trade Minute", Group = "General Settings", DefaultValue = "16")]
        public string TradeMinute { get; set; }

        //Parameters for the Imported indicators



        //indicator variables for the Template
        private AverageTrueRange _atr;
        private string _botName;
        private Symbol[] _tradeList;

        public enum TradeMethod
        {
            MultipleInstuments,
            OnSetTime,
            OnBar
        }


        //indicator variables for the Imported indicators



        protected override void OnStart()
        {
            //Get Name of Bot and Currency pair of current instance
            _botName = GetType().ToString();
            _botName = _botName.Substring(_botName.LastIndexOf('.') + 1);

            //ATR indicator for Money Management system
            _atr = Indicators.AverageTrueRange(14, MovingAverageType.Exponential);

            //Load Currencies if necessary
            if (TradeMethodSelected.Equals(TradeMethod.MultipleInstuments))
            {
                foreach (Watchlist watchlist in Watchlists)
                {
                    if (watchlist.Name == WatchListName)
                    {
                        _tradeList = Symbols.GetSymbols(watchlist.SymbolNames.ToArray());
                    }
                }

                foreach (Symbol symbol in _tradeList)
                {
                    var bars = MarketData.GetBars(TimeFrame, symbol.Name);
                    bars.BarOpened += OnBarsBarOpened;
                }
            }

            //Load here the specific indicators for this bot



        }

        private void OnBarsBarOpened(BarOpenedEventArgs obj)
        {
            //Check if its time to make a trade
            if (!TimeToTrade() && !TradeMethodSelected.Equals(TradeMethod.MultipleInstuments))
            {
                return;
            }
            string label = _botName + " _ " + obj.Bars.SymbolName;
            //Put your core logic in the private methods OpenBuyTrade and OpenSellTrade
            if (OpenBuyTrade(obj))
            {
                Open(TradeType.Buy, label);
                Close(TradeType.Sell, label);
            }
            if (OpenSellTrade(obj))
            {
                Open(TradeType.Sell, label);
                Close(TradeType.Buy, label);
            }

        }

        protected override void OnTick()
        {
            //Check if its time to make a trade
            if (!TimeToTrade() && !TradeMethodSelected.Equals(TradeMethod.OnSetTime))
            {
                return;
            }
            string label = _botName + " _ " + SymbolName;
            //Put your core logic in the private methods OpenBuyTrade and OpenSellTrade
            if (OpenBuyTrade())
            {
                Open(TradeType.Buy, label);
                Close(TradeType.Sell, label);
            }
            if (OpenSellTrade())
            {
                Open(TradeType.Sell, label);
                Close(TradeType.Buy, label);
            }
        }

        protected override void OnBar()
        {
            if (!TradeMethodSelected.Equals(TradeMethod.OnBar))
            {
                return;
            }
            string label = _botName + " _ " + SymbolName;
            //Put your core logic in the private methods OpenBuyTrade and OpenSellTrade
            if (OpenBuyTrade())
            {
                Open(TradeType.Buy, label);
                Close(TradeType.Sell, label);
            }
            if (OpenSellTrade())
            {
                Open(TradeType.Sell, label);
                Close(TradeType.Buy, label);
            }
        }

        //Conditions for opening a Buy Trade
        private bool OpenBuyTrade(BarOpenedEventArgs obj = null)
        {
            return false;
        }

        //Conditions for opening a Sell Trade
        private bool OpenSellTrade(BarOpenedEventArgs obj = null)
        {
            return false;
        }

        //Function for opening a new trade
        private void Open(TradeType tradeType, string label)
        {
            //Check there's no existing position before entering a trade, label contains the Indicatorname and the currency
            if (Positions.Find(label) != null)
            {
                return;
            }

            //Calculate trade amount based on ATR
            double atr = Math.Round(_atr.Result.Last(0) / Symbol.PipSize, 0);
            double tradeAmount = Account.Equity * RiskPct / (1.5 * atr * Symbol.PipValue);
            tradeAmount = Symbol.NormalizeVolumeInUnits(tradeAmount, RoundingMode.Down);

            ExecuteMarketOrder(tradeType, SymbolName, tradeAmount, label, SlFactor * atr, TpFactor * atr, null);
        }

        //Function for closing trades
        private void Close(TradeType tradeType, string label)
        {
            foreach (Position position in Positions.FindAll(label, SymbolName, tradeType))
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
