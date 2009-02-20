using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using TradeLink.Common;

namespace TestTradeLink
{
    [TestFixture]
    public class TestHistSim
    {
        public TestHistSim() { }

        const double EXPECT2SYMTIME = 1.6;

        [Test]
        public void PlaybackTime()
        {
            HistSim h = new HistSim(Environment.CurrentDirectory+"\\");
            h.GotTick += new TradeLink.API.TickDelegate(h_GotTick);

            Assert.AreEqual(0, tickcount);
            Assert.AreEqual(0, syms.Count);
            Assert.AreEqual(0, lasttime);

            DateTime start = DateTime.Now;

            h.PlayTo(HistSim.ENDSIM);

            double time = DateTime.Now.Subtract(start).TotalSeconds;

            // make sure ticks arrived in order
            //Assert.IsTrue(GOODTIME,"Tick arrived out-of-order.");
            // check running time
            Assert.LessOrEqual(time, EXPECT2SYMTIME,"may fail on slow machines");
            Assert.AreEqual(2,syms.Count);
            // 42614 (FTI) + 5001 (SPX)
            Assert.AreEqual(42614+4991, tickcount);
            // last time is 1649 on SPX
            //Assert.AreEqual(1649, lasttime);
            // printout simulation runtime
            Console.WriteLine("2SYM RunTime (Expect): " + time.ToString("N2") + "sec (" + EXPECT2SYMTIME + ")");
            Console.WriteLine("Performance: " + ((double)tickcount / time).ToString("N0") + " ticks/sec");
        }
        int tickcount = 0;
        List<string> syms = new List<string>();
        int lasttime = 0;
        bool GOODTIME = true;
        void h_GotTick(TradeLink.API.Tick t)
        {
            if (!syms.Contains(t.symbol))
                syms.Add(t.symbol);
            tickcount++;
            GOODTIME &= (t.time>=lasttime);
            lasttime = t.time;
        }
    }
}