using System;
using System.Collections.Generic;
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
        [Parameter("Risk %", Group = "Risk Management", DefaultValue = 2)]
        public int RiskPct { get; set; }

        [Parameter("SL Factor", Group = "Risk Management", DefaultValue = 1.5)]
        public double SlFactor { get; set; }

        [Parameter("TP Factor", Group = "Risk Management", DefaultValue = 1)]
        public double TpFactor { get; set; }

        //Parameters for the Template General Settings
        [Parameter("Trade On Time", Group = "General Settings", DefaultValue = true)]
        public bool TradeOnTime { get; set; }

        [Parameter("Trade On Multiple Instruments", Group = "General Settings", DefaultValue = true)]
        public bool TradeMultipleInstruments { get; set; }

        [Parameter("Name of WatchList", Group = "General Settings", DefaultValue = "28Pairs")]
        public string WatchListName { get; set; }

        [Parameter("Trade Hour", Group = "General Settings", DefaultValue = "16")]
        public int TradeHour { get; set; }

        [Parameter("Trade Minute", Group = "General Settings", DefaultValue = "55")]
        public int TradeMinute { get; set; }


        //indicator variables for the Template for multi symbols
        private readonly Dictionary<string, AverageTrueRange> _atrList = new Dictionary<string, AverageTrueRange>();
        private readonly Dictionary<string, Symbol> _symbolList = new Dictionary<string, Symbol>();

        //indicator variables for the Template for single symbol
        private string _botName;
        private AverageTrueRange _atr;
        private int _barToCheck;
        private double riskPercentage;
        private bool _hadBigBar = false;



        //indicator variables for the Imported indicators


        protected override void OnStart()
        {
            _botName = GetType().ToString();
            _botName = _botName.Substring(_botName.LastIndexOf('.') + 1);

            _barToCheck = TradeOnTime ? 0 : 1;
            riskPercentage = (double)RiskPct / 100;

            if (TradeMultipleInstruments)
            {
                foreach (string symbolName in Watchlists.FirstOrDefault(w => w.Name == WatchListName).SymbolNames.ToArray())
                {
                    _symbolList.Add(symbolName, Symbols.GetSymbol(symbolName));
                }

                foreach (KeyValuePair<string, Symbol> symbol in _symbolList)
                {
                    var bars = MarketData.GetBars(TimeFrame.Daily, symbol.Key);
                    if (!TradeOnTime)
                    {
                        bars.BarOpened += OnBarsBarOpened;
                    }
                    else
                    {
                        bars.Tick += OnBarTick;
                    }
                    _atrList.Add(symbol.Key, Indicators.AverageTrueRange(bars, 14, MovingAverageType.Exponential));

                    //Load here the specific indicators for this bot for multiple Instruments
                }
            }
            else
            {
                _atr = Indicators.AverageTrueRange(14, MovingAverageType.Exponential);
                //Load here the specific indicators for this bot for a single instrument

            }
            Positions.Closed += PositionsOnClosed;
        }

        private void PositionsOnClosed(PositionClosedEventArgs obj)
        {
            if (obj.Reason == PositionCloseReason.TakeProfit)
            {
                Position position = Positions.Find(obj.Position.Label);
                ModifyPosition(position, position.EntryPrice, null, false);
            }

        }

        private void OnBarTick(BarsTickEventArgs obj)
        {
            if (TradeMultipleInstruments && TradeOnTime && TimeToTrade())
            {
                MakeTrades(obj.Bars, _symbolList[obj.Bars.SymbolName], _atrList[obj.Bars.SymbolName]);
            }
        }

        private void OnBarsBarOpened(BarOpenedEventArgs obj)
        {
            if (TradeMultipleInstruments && !TradeOnTime)
            {
                MakeTrades(obj.Bars, _symbolList[obj.Bars.SymbolName], _atrList[obj.Bars.SymbolName]);
            }
        }

        protected override void OnTick()
        {
            if (!TradeMultipleInstruments && TradeOnTime && TimeToTrade())
            {
                MakeTrades(Bars, Symbol, _atr);
            }
        }

        protected override void OnBar()
        {
            if (!TradeMultipleInstruments && !TradeOnTime)
            {
                MakeTrades(Bars, Symbol, _atr);
            }
        }

        private void MakeTrades(Bars bars, Symbol symbol, AverageTrueRange atr)
        {
            string label = string.Format("{0}_{1}", _botName, symbol.Name);

            double barSize = Math.Round(Math.Abs((bars.ClosePrices.Last(_barToCheck + 1) - bars.OpenPrices.Last(_barToCheck + 1)) / symbol.PipSize), 0);
            double atrSize = Math.Round(atr.Result.Last(_barToCheck) / symbol.PipSize, 0);

            if (barSize > atrSize && !_hadBigBar)
            {
                Print(string.Format("barSize = {0} --- atrSize = {1} --- for Symbol: {2}", barSize, atrSize, symbol.Name));
                Print("Bar to large for ATR at " + Server.Time.Date);
                if (OpenTrade(bars) != null)
                {
                    Close(OpenTrade(bars).Item2, symbol, label);
                    Close(OpenTrade(bars).Item1, symbol, label);
                }
                _barToCheck++;
                _hadBigBar = true;
                return;
            }

            if (OpenTrade(bars) != null)
            {
                Close(OpenTrade(bars).Item2, symbol, label);
                Open(OpenTrade(bars).Item1, symbol, atr, label);
            }

            if (_hadBigBar)
            {
                _barToCheck = TradeOnTime ? 0 : 1;
                _hadBigBar = false;
            }
        }

        private Tuple<TradeType, TradeType> OpenTrade(Bars bars)
        {

            if (false)
            {
                return new Tuple<TradeType, TradeType>(TradeType.Buy, TradeType.Sell);
            }
            else if (false)
            {
                return new Tuple<TradeType, TradeType>(TradeType.Sell, TradeType.Buy);
            }

            return null;
        }

        //Function for opening a new trade
        private void Open(TradeType tradeType, Symbol symbol, AverageTrueRange atr, string label)
        {
            //Check there's no existing position before entering a trade, label contains the Indicatorname and the currency
            if (Positions.Find(label, symbol.Name, tradeType) != null)
            {
                return;
            }
            //Calculate trade amount based on ATR
            double atrSize = Math.Round(atr.Result.Last(_barToCheck) / symbol.PipSize, 0);
            double tradeAmount = Account.Equity * riskPercentage / (SlFactor * atrSize * symbol.PipValue);
            tradeAmount = symbol.NormalizeVolumeInUnits(tradeAmount, RoundingMode.Down);
            tradeAmount = (int)tradeAmount / 2000;
            tradeAmount *= 1000;

            ExecuteMarketOrder(tradeType, symbol.Name, tradeAmount, label, SlFactor * atrSize, TpFactor * atrSize);
            ExecuteMarketOrder(tradeType, symbol.Name, tradeAmount, label, SlFactor * atrSize, null);
        }

        //Function for closing trades
        private void Close(TradeType tradeType, Symbol symbol, string label)
        {
            if (Positions.FindAll(label, symbol.Name, tradeType) == null)
            {
                return;
            }
            foreach (Position position in Positions.FindAll(label, symbol.Name, tradeType))
            {
                ClosePosition(position);
            }
        }

        private bool TimeToTrade()
        {
            return Server.Time.Hour == TradeHour && Server.Time.Minute == TradeMinute;
        }
    }
}
