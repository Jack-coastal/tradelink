using System;
using System.Collections.Generic;
using System.Text;
using TradeLink.Common;
using NUnit.Framework;
using TradeLink.API;

namespace TestTradeLink
{
    [TestFixture]
    public class TestBarList
    {

        const string sym = "TST";
        const int d = 20070517;
        const int t = 935;
        const string x = "NYSE";
        TickImpl[] ticklist = new TickImpl[] { 
                TickImpl.NewTrade(sym,d,t,0,10,100,x), // new on all intervals
                TickImpl.NewTrade(sym,d,t+1,0,10,100,x), //new on 1min
                TickImpl.NewTrade(sym,d,t+2,0,10,100,x),
                TickImpl.NewTrade(sym,d,t+3,0,10,100,x),
                TickImpl.NewTrade(sym,d,t+4,0,15,100,x), 
                TickImpl.NewTrade(sym,d,t+5,0,16,100,x), //new on 5min
                TickImpl.NewTrade(sym,d,t+6,0,16,100,x),
                TickImpl.NewTrade(sym,d,t+7,0,10,100,x), 
                TickImpl.NewTrade(sym,d,t+7,10,10,100,x), 
            };

        [Test]
        public void NewBars()
        {
            BarListImpl bl = new BarListImpl(BarInterval.FiveMin);
            int newbars = 0;
            foreach (TickImpl k in ticklist)
            {
                bl.newTick(k);
                if (bl.NewBar)
                    newbars++;
            }

            Assert.That(newbars == 2, newbars.ToString());


            bl = new BarListImpl(BarInterval.Minute);
            newbars = 0;
            foreach (TickImpl k in ticklist)
            {
                bl.newTick(k);
                if (bl.NewBar)
                    newbars++;
            }

            Assert.That(newbars == 8, newbars.ToString());

        }
        [Test]
        public void HourTest()
        {
            int t = 1915;
            TickImpl[] tape = new TickImpl[] { 
                TickImpl.NewTrade(sym,d,t,0,10,100,x), // new on all intervals
                TickImpl.NewTrade(sym,d,t+1,0,10,100,x), 
                TickImpl.NewTrade(sym,d,t+2,0,10,100,x),
                TickImpl.NewTrade(sym,d,t+3,0,10,100,x),
                TickImpl.NewTrade(sym,d,t+4,0,15,100,x), 
                TickImpl.NewTrade(sym,d,t+5,0,16,100,x), 
                TickImpl.NewTrade(sym,d,t+6,0,16,100,x),
                TickImpl.NewTrade(sym,d,t+7,0,10,100,x), 
                TickImpl.NewTrade(sym,d,t+7,10,10,100,x), 
                TickImpl.NewTrade(sym,d,t+100,0,10,100,x), // new on hour interval
            };

            int newbars = 0;
            BarListImpl bl = new BarListImpl(BarInterval.Hour, sym);
            foreach (TickImpl k in tape)
            {
                bl.newTick(k);
                if (bl.NewBar)
                    newbars++;
            }

            Assert.That(newbars == 2, newbars.ToString());
        }


    }
}
