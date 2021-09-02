﻿using System;
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
        [Parameter("Risk %", Group = "Risk Management", DefaultValue = 0.02)]
        public double RiskPct { get; set; }

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

        //Parameters for the Imported indicators



        //indicator variables for the Template
        private List<AverageTrueRange> _atr = new List<AverageTrueRange>();
        private List<Symbol> _symbolList = new List<Symbol>();
        private string _botName;


        //indicator variables for the Imported indicators



        protected override void OnStart()
        {
            _botName = GetType().ToString();
            _botName = _botName.Substring(_botName.LastIndexOf('.') + 1);

            _atr = new List<AverageTrueRange>();

            if (TradeMultipleInstruments)
            {
                _symbolList = Symbols.GetSymbols(Watchlists.FirstOrDefault(w => w.Name == WatchListName).SymbolNames.ToArray()).ToList();


                foreach (Symbol symbol in _symbolList)
                {
                    var bars = MarketData.GetBars(TimeFrame, symbol.Name);
                    if (TradeOnTime)
                    {
                        bars.BarOpened += OnBarsBarOpened;
                    }
                    else
                    {
                        bars.Tick += OnBarTick;
                    }
                    _atr.Add(Indicators.AverageTrueRange(bars, 14, MovingAverageType.Exponential));

                    //Load here the specific indicators for this bot for multiple Instruments

                }
            }

            //Load here the specific indicators for this bot for a single instrument

        }

        private void OnBarTick(BarsTickEventArgs obj)
        {
            if (TradeMultipleInstruments && TradeOnTime && TimeToTrade())
            {
                MakeTrades(obj.Bars.SymbolName);
            }
        }

        private void OnBarsBarOpened(BarOpenedEventArgs obj)
        {
            if (TradeMultipleInstruments && !TradeOnTime)
            {
                MakeTrades(obj.Bars.SymbolName);
            }
        }

        protected override void OnTick()
        {
            if (!TradeMultipleInstruments && TradeOnTime && TimeToTrade())
            {
                MakeTrades(SymbolName);
            }
        }

        protected override void OnBar()
        {
            if (!TradeMultipleInstruments && !TradeOnTime)
            {
                MakeTrades(SymbolName);
            }
        }

        private void MakeTrades(string symbolname)
        {
            if (OpenTrade(symbolname) != null)
            {
                Open(OpenTrade(symbolname).Item1, symbolname);
                Close(OpenTrade(symbolname).Item2, symbolname);
            }
        }

        private Tuple<TradeType, TradeType> OpenTrade(string symbolname)
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
        private void Open(TradeType tradeType, string instrument)
        {
            string label = _botName + " _ " + instrument;
            int symbolIndex = _symbolList.FindIndex(s => s.Name == instrument);
            Symbol sym = Symbols.GetSymbol(instrument);

            //Check there's no existing position before entering a trade, label contains the Indicatorname and the currency
            if (Positions.Find(label) != null)
            {
                return;
            }

            //Calculate trade amount based on ATR
            double atr = Math.Round(_atr[symbolIndex].Result.Last(0) / sym.PipSize, 0);
            double tradeAmount = Account.Equity * RiskPct / (SlFactor * atr * sym.PipValue);
            tradeAmount = sym.NormalizeVolumeInUnits(tradeAmount, RoundingMode.Down);

            ExecuteMarketOrder(tradeType, instrument, tradeAmount, label, SlFactor * atr, TpFactor * atr);
        }

        //Function for closing trades
        private void Close(TradeType tradeType, string instrument)
        {
            string label = _botName + " _ " + instrument;
            foreach (Position position in Positions.FindAll(label, SymbolName, tradeType))
                ClosePosition(position);
        }

        private bool TimeToTrade()
        {
            if (Server.Time.Hour == TradeHour && Server.Time.Minute == TradeMinute)
            {
                return true;
            }
            return false;
        }
    }
}
