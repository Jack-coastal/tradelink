using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.ComponentModel;

namespace TradeLib
{
    public delegate void DebugDelegate(string msg);
    [Serializable]
    public class Box
    {
        NewsService news = null;
        protected event IndexDelegate GotIndex;
        private string name = "Unnamed";
        private string ver = "$Rev: 1036 $";
        private string symbol;
        private int tDir = 0;
        private decimal avgprice = 0;
        private int trades = 0;
        private int adjusts = 0;
        private bool DEBUG = false;
        private bool SHUTDOWN = false;
        private bool USELIMITS = false;
        private int MAXTRADES = Int32.MaxValue;
        private int MAXADJUSTS = Int32.MaxValue;
        private int MAXSIZE = 100;
        private int MINSIZE = 100;
        private int date = 0;
        private int time = 0;
        private int sday = 930;
        private int eday = 1600;
        private string loadstr = "";
        private bool _email = false;
        private bool _quickorder = true;
        public int DayEndBuff = 2;


        
        public Box () : this(null) { }
        public Box(NewsService ns) // constructor
        {
            this.news = ns;
        }
        
        public void NewIndex(Index i)
        {
        	if (GotIndex!=null) GotIndex(i);
        }

        public Order Trade(Tick tick,BarList bl, Position pos,BoxInfo bi)
        {
            Order o = new Order();
            if (Symbol == null)
            {
                if (tick.sym != "") symbol = tick.sym;
                else throw new Exception("No symbol specified");
            }
            if (!pos.isValid) throw new Exception("Invalid Position Provided to Box" + pos.ToString());
            if (tick.sym != Symbol) return o;
            time = tick.time;
            date = tick.date;

           
            if ((Time < DayStart) || (Time>DayEnd)) return o; // is market open?
            if (Off) return o; // don't trade if shutdown
            if (UseLimits &&  pos.Flat && ((this.trades >= MAXTRADES) || (this.adjusts >= MAXADJUSTS)))
            {
                this.Shutdown("Trade limit reached.");
                return o;
            }
            if ((pos.AvgPrice*pos.Size!=0) && ((pos.AvgPrice != AvgPrice) || (pos.Size != PosSize)))
            {
                avgprice = pos.AvgPrice;
                tDir = pos.Size;
                decimal pl = pos.Size * (tick.trade - AvgPrice);
                D("Position: "+PosSize + "@" + AvgPrice + "\t"+pl.ToString("c"));
            }

            if (QuickOrder) // user providing only size adjustment
            {
                // get our adjustment
                int adjust = this.Read(tick, bl,bi);  

                // convert adjustment to an order
                o = this.Adjust(adjust); 
            }
            else // user providing a complete order, so get it
            {
                o = ReadOrder(tick, bl,bi);
            }

            //flat us at the close
            if ((Time >= (DayEnd - DayEndBuff))) 
                o = this.Adjust(Flat);
            if (o.isValid)
            {
                // send our order
                o.time = Time;
                o.date = Date;
                this.D("Sent order: " + o);
            }
            return o;
        }


        protected Order Adjust(int asize)
        {
            if (asize == 0) return new Order();
            int size = asize;
            int ts = PosSize;
            if (Math.Abs(size) < 11) size = Norm2Min(ts * size);
            Boolean side = size > 0;
            size = NoCrossingFlat(size);
            if (Math.Abs(size + ts) > MAXSIZE) size = (MAXSIZE - Math.Abs(ts)) * (side ? 1 : -1);
            Order o = new Order(Symbol, side, size,Name);
            if (o.isValid)
            {
                adjusts++;
                // if we're going to flat from non-flat, this is a "trade"
                if ((Math.Abs(size+ts)==0) && (ts!=0)) trades++;
            }
            return o;
        }

        protected int Norm2Min(int size)
        {
            int wmult = (int)Math.Ceiling((decimal)size/MINSIZE);
            return wmult * MINSIZE;
        }

        private int NoCrossingFlat(int size) 
        {
            if ((PosSize!=0) &&
                ((PosSize+size)!=0) &&
                (((PosSize+size)*PosSize)<0))
                size = -1 * PosSize;
            return size;
        }

        protected List<string> _iname = new List<string>(0);
        protected List<object> _indicators = new List<object>(0);
        protected int _indicount = 0;
        public void ResetIndicators(int IndicatorCount)
        {
            _indicators = new List<object>(IndicatorCount);
            _indicount = IndicatorCount;
            _iname = new List<string>(IndicatorCount);
            for (int i = 0; i < IndicatorCount; i++)
            {
                _indicators.Add(0);
                _iname.Add("i" + i);
            }
        }
        [BrowsableAttribute(false)]
        public bool hasIndicators { get { return _indicators.Count != 0; } }
        [BrowsableAttribute(false)]
        public object[] Indicators
        {
            get { return _indicators.ToArray(); }
            set
            {
                if (value.Length != _indicators.Capacity)
                    throw new Exception("Must provide all indicator values when specifying array.");
                for (int i = 0; i < value.Length; i++)
                    _indicators[i] = value[i];
            }
        }
        [BrowsableAttribute(false)]
        public string[] Inames
        {
            get { return _iname.ToArray(); }
            set
            {
                if (value.Length != _indicators.Capacity)
                    throw new Exception("Must provide all indicator values when specifying array.");
                for (int i = 0; i < value.Length; i++)
                    _iname[i] = value[i];
            }
        }
        protected string GetIname(int i)
        {
            if ((i >= _indicators.Capacity) || (i < 0))
                throw new IndexOutOfRangeException("Cannot access an index beyond what was defined with ResetIndicators(int IndicatorCount)");
            return _iname[i];
        }
        protected void SetIname(int i, string value)
        {
            if ((i >= _indicators.Capacity) || (i < 0))
                throw new IndexOutOfRangeException("Cannot access an index beyond what was defined with ResetIndicators(int IndicatorCount)");
            _iname[i] = value;
        }

        protected object GetIndicator(int i)
        {
            if ((i >= _indicators.Capacity) || (i < 0))
                throw new IndexOutOfRangeException("Cannot access an index beyond what was defined with ResetIndicators(int IndicatorCount)");
            return _indicators[i];
        }
        protected void SetIndicator(int i, object value)
        {
            if ((i >= _indicators.Capacity) || (i < 0))
                throw new IndexOutOfRangeException("Cannot access an index beyond what was defined with ResetIndicators(int IndicatorCount)");
            _indicators[i] = value;
        }

        public virtual void Reset() 
        { 
            symbol = null; SHUTDOWN = false; trades = 0; adjusts = 0; 
            DayStart = 930; DayEnd = 1600;
            ResetIndicators(_indicount);
        }


        public bool Debug { get { return DEBUG; } set { DEBUG = value; } }
        public virtual void Shutdown(string message)
        {
            this.SHUTDOWN = true;
            this.D(message);
        }

        public virtual void Activate(string message)
        {
            this.SHUTDOWN = false;
            this.D(message);
        }
        public virtual void E(string to, string from, string subject, string msg)
        {
            if (!_email) return;
            Email.Send(to, from, subject, msg);
        }

        public virtual void D(string debug)
        {
            if (!this.DEBUG) return;
            if (this.news != null) this.news.newNews("["+Name+"] "+Symbol+" "+Date+":"+Time+" "+debug);
            else Console.WriteLine(debug);
            return;
        }
        public virtual void d(string deb) { if (this.news != null) this.news.newNews("[" + Name + "] " + Symbol+ " " + deb); }


        protected virtual int Read(Tick tick, BarList bl,BoxInfo boxinfo) { D("No Read function provided"); return 0; }
        protected virtual Order ReadOrder(Tick tick, BarList bl,BoxInfo boxinfo) { D("No ReadOrder function provided.");  return new Order(); }

        [CategoryAttribute("TradeLink Box"), DescriptionAttribute("User-Supplied version number.")]
        public virtual string Version { get { return ver; } set { ver = value; } }
        [CategoryAttribute("TradeLink Box"), DescriptionAttribute("Toggle enforcing of MaxTrades and MaxAdjusts")]
        public bool UseLimits { get { return USELIMITS; } set { USELIMITS = value; } }
        [CategoryAttribute("TradeLink Box"), DescriptionAttribute("Allow emails from the box to be sent.")]
        public bool Emails { get { return _email; } set { _email = value; } }
        [CategoryAttribute("TradeLink Box"), DescriptionAttribute("Sets inclusive start time for box each day.")]
        public int DayStart { get { return sday; } set { sday = value; } }
        [CategoryAttribute("TradeLink Box"), DescriptionAttribute("Sets inclusive stop time for box each day.")]
        public int DayEnd { get { if ((eday % 100) != 0) return eday - DayEndBuff; return eday - 40 - DayEndBuff; } set { eday = value; } }
        [CategoryAttribute("TradeLink BoxInfo"), DescriptionAttribute("Date")]
        public int Date { get { return date; } }
        [CategoryAttribute("TradeLink BoxInfo"), DescriptionAttribute("Time")]
        public int Time { get { return time; } }
        [CategoryAttribute("TradeLink Box"), DescriptionAttribute("If UseLimits is true, Shutdown box when this many trades is reached.")]
        public int MaxTrades { get { return MAXTRADES; } set { MAXTRADES = value; } }
        [CategoryAttribute("TradeLink Box"), DescriptionAttribute("If UseLimits is true, Shutdown box after this many adjustments.")]
        public int MaxAdjusts { get { return MAXADJUSTS; } set { MAXADJUSTS = value; } }
        [CategoryAttribute("TradeLink Box"), DescriptionAttribute("Maximum size of a single position.")]
        public int MaxSize { get { return MAXSIZE; } set { MAXSIZE = value; } }
        [CategoryAttribute("TradeLink Box"), DescriptionAttribute("Minimum size of a single position.")]
        public int MinSize { get { return MINSIZE; } set { MINSIZE = value; } }
        [CategoryAttribute("TradeLink Box"), DescriptionAttribute("Whether this box returns full orders or just adjustments to position size.")]
        public bool QuickOrder { get { return _quickorder; } set { _quickorder = value; } }
        [CategoryAttribute("TradeLink BoxInfo"), DescriptionAttribute("Average Position Price")]
        public decimal AvgPrice { get { return avgprice; } }
        [CategoryAttribute("TradeLink BoxInfo"), DescriptionAttribute("Fully qualified box class name."), ReadOnlyAttribute(true)]
        public string FullName { get { return loadstr; } set { loadstr = value; } }
        [CategoryAttribute("TradeLink Box"), DescriptionAttribute("Name of this box.")]
        public string Name { get { return name; } set { name = value; } }
        [CategoryAttribute("TradeLink BoxInfo"), DescriptionAttribute("Current symbol")]
        public string Symbol { get { return symbol; } }
        [CategoryAttribute("TradeLink BoxInfo"), Description("True if the box is Shutdown.")]
        public bool Off { get { return SHUTDOWN; } }
        [CategoryAttribute("TradeLink BoxInfo"), DescriptionAttribute("Number of trades (long/short) this box has made.")]
        public int Trades { get { return trades; } }
        [CategoryAttribute("TradeLink BoxInfo"), DescriptionAttribute("Number of position adjustments this box has made.")]
        public int Adjusts { get { return adjusts; } }

        [BrowsableAttribute(false)]
        public NewsService NewsHandler { get { return news; } set { news = value; } }
        [BrowsableAttribute(false)]
        protected string NameVersion { get { return Name + CleanVersion; } }
        [BrowsableAttribute(false)]
        protected string CleanVersion { get { return Util.CleanVer(Version); } }
        [BrowsableAttribute(false)]
        public int PosSize { get { return tDir; } }
        [BrowsableAttribute(false)]
        protected int Flat { get { return PosSize * -1; } }
        [BrowsableAttribute(false)]
        protected int Half { get { return -1 * Norm2Min(PosSize / 2); } }
        [BrowsableAttribute(false)]
        protected virtual int TakeProfitSize { get { return Half; } }


        public static Box FromDLL(string boxname, string dllname,NewsService ns)
        {
            System.Reflection.Assembly a;
            try
            {
                a = System.Reflection.Assembly.LoadFrom(dllname);
            }
            catch (Exception ex) { ns.newNews(ex.Message); return new Box(ns); }
            return FromAssembly(a, boxname, ns);
        }
        public static Box FromAssembly(System.Reflection.Assembly a, string boxname, NewsService ns)
        {
            Type type;
            object[] args;
            Box mybox = new Box(ns);
            try
            {
                type = a.GetType(boxname, true, true);
            }
            catch (Exception ex) { ns.newNews(ex.Message); return mybox; }
            args = new object[] { ns };
            try
            {
                mybox = (Box)Activator.CreateInstance(type, args);
            }
            catch (Exception ex) 
            { 
                ns.newNews(ex.Message); 
                if (ex.InnerException != null) 
                    ns.newNews(ex.InnerException.Message); 
                return mybox; 
            }
            mybox.FullName = boxname;
            return mybox;
        }
    }
}
