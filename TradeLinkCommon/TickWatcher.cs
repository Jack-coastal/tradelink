using System;
using System.Collections.Generic;
using System.Collections;
using TradeLink.API;
using System.Threading;

namespace TradeLink.Common
{
    /// <summary>
    /// Used to watch a stream of ticks, and send alerts when the stream goes idle for a specified time.
    /// </summary>
    public class TickWatcher : TickIndicator
    {
        public bool isValid { get { return _continue; } }
        private bool _alertonfirst = true;
        /// <summary>
        ///  returns count of symbols that have ticked at least once
        /// </summary>
        public int Count { get { return _last.Count; } }
        /// <summary>
        /// gets last time a tick was received for symbol
        /// </summary>
        /// <param name="sym"></param>
        /// <returns></returns>
        public int this[string sym]
        {
            get
            {
                int lasttime = 0;
                if (_last.TryGetValue(sym, out lasttime))
                    return lasttime;
                return 0;
            }
        }

        bool _ismassalertcleared = false;
        /// <summary>
        /// will be true if mass alerted existed previously and was cleared.
        /// this value can only be checked once as it will reset to false once read
        /// </summary>
        public bool isMassAlertCleared { get { bool v = _ismassalertcleared; if (v) _ismassalertcleared = !v; return v; } }

        public IEnumerator GetEnumerator()
        {
            foreach (string s in _last.Keys)
                yield return s;
        }
        private Dictionary<string, int> _last = new Dictionary<string, int>();
        /// <summary>
        /// alert thrown when no ticks have arrived since AlertThreshold.
        /// Time of last tick is provided.
        /// </summary>
        public event Int32Delegate GotMassAlert;
        /// <summary>
        /// alert thrown when AlertThreshold is exceeded for a symbol
        /// </summary>
        public event SymDelegate GotAlert;
        /// <summary>
        /// alert thrown when first tick arrives for symbol
        /// </summary>
        public event SymDelegate GotFirstTick;
        public bool FireFirstTick { get { return _alertonfirst; } set { _alertonfirst = value; } }
        private int _defaultwait = 180;
        /// <summary>
        /// minimum threshold in seconds when no tick updates have been received for a single symbol, alerts can be thrown.
        /// </summary>
        public int AlertThreshold { get { return _defaultwait; } set { _defaultwait = value; } }

        int _defaultmass = 60;
        /// <summary>
        /// minimum threshold when no ticks have been received for many symbols
        /// </summary>
        public int MassAlertThreshold { get { return _defaultmass; } set { _defaultmass = value; } }

        System.ComponentModel.BackgroundWorker _bw = null;
        volatile int _lasttime = 0;
        /// <summary>
        /// most recent time received
        /// </summary>
        public int RecentTime { get { return _lasttime; } }
        /// <summary>
        /// Watches the specified tick.
        /// Alerts if wait time exceeded.
        /// </summary>
        /// <param name="tick">The tick.</param>
        /// <returns></returns>
        public bool newTick(Tick tick) 
        {
            _lasttime = tick.time;
            if ((AllsymbolsTicking != null) || (GotAlert != null) || (GotFirstTick != null))
            {
                int last = tick.time;
                // ensure we are storing per-symbol times
                if (!_last.TryGetValue(tick.symbol, out last))
                {
                    _last.Add(tick.symbol, tick.time);
                    if (_alertonfirst) // if we're notifying when first tick arrives, do it.
                        if (GotFirstTick != null)
                            GotFirstTick(tick.symbol);
                    if (_ast != null)
                    {
                        if (!alltrading && (_ast.Count == Count))
                        {
                            alltrading = true;
                            if (AllsymbolsTicking != null)
                                AllsymbolsTicking(Util.ToTLTime());
                        }
                    }
                    last = tick.time;
                    return false;
                }
                // if alerts requested, check for idle symbol
                if (GotAlert != null)
                {
                    int span = Util.FTDIFF(last, tick.time);
                    bool alert = span > _defaultwait;
                    if (alert)
                        GotAlert(tick.symbol);
                    return alert;
                }
                // store time
                _last[tick.symbol] = tick.time;
            }
            return false; 
        }
        /// <summary>
        /// send alerts for idle symbols using current time as comparison point
        /// </summary>
        /// <returns></returns>
        public int SendAlerts() { return SendAlerts(DateTime.Now); }
        /// <summary>
        /// Sends the alerts for tickstreams who have gone idle based on the provided datetime.
        /// </summary>
        /// <param name="date">The current datetime.</param>
        public int SendAlerts(DateTime time)
        {
            return SendAlerts(Util.DT2FT(time), _defaultwait);
        }
        /// <summary>
        /// sends alerts for i
        /// </summary>
        /// <param name="date"></param>
        public int SendAlerts(int time)
        {
            return SendAlerts(time, _defaultwait);
        }

        /// <summary>
        /// Sends the alerts for tickstreams who have gone idle based on the provided datetime.
        /// </summary>
        /// <param name="date">The datetime.</param>
        /// <param name="AlertSecondsWithoutTick">The alert seconds without tick.</param>
        public int SendAlerts(int time, int AlertSecondsWithoutTick) 
        {
            int c = 0;
            foreach (string sym in _last.Keys)
                if (GotAlert != null)
                    if (Util.FTDIFF(_last[sym], time) > AlertSecondsWithoutTick)
                    {
                        c++;
                        GotAlert(sym);
                    }
            return c;
        }

        bool _continue = true;

        public event Int32Delegate PollProcess;
        long _pollint = 0;
        public const int DEFAULTPOLLINT = 1000;
        public int BackgroundPollInterval { get { return (int)_pollint; } set { _pollint = (long)Math.Abs(value); if (_pollint == 0) Stop(); } }
        public TickWatcher(int BackgroundPollIntervalms) : this(BackgroundPollIntervalms,null) { }
        public TickWatcher() : this(DEFAULTPOLLINT,null) { }
        /// <summary>
        /// creates a tickwatcher and polls specificed millseconds
        /// if timer has expired, sends alert.
        /// Background polling occurs in addition to tick-induced time checks.
        /// </summary>
        /// <param name="BackgroundPollIntervalms">Value in millseconds to wait between checks.  0 = disable background checks</param>
        public TickWatcher(int BackgroundPollIntervalms, GenericTrackerI symboltracker)
        {
            _ast = symboltracker;
            _pollint = (long)Math.Abs(BackgroundPollIntervalms);
            if (_pollint != 0)
                Start();
        }

        public void Start()
        {
            if ((_bw ==null) && (_pollint!=0))
            {
                _bw = new System.ComponentModel.BackgroundWorker();
                _bw.DoWork += new System.ComponentModel.DoWorkEventHandler(_bw_DoWork);
                _bw.WorkerSupportsCancellation = true;
                _bw.RunWorkerAsync();
            }
            else if ((_bw!=null) && !_continue)
            {
                _continue = true;
                _bw.RunWorkerAsync();
            }
        }

        bool _alerting = false;
        /// <summary>
        /// whether mass alert is firing or not
        /// </summary>
        public bool isMassAlerting { get { return _alerting; } }

        bool sentmissingfirstticks = false;
        bool _stop = true;
        int _stoptime = 0;
        int _starttime = 0;
        bool alltrading = false;
        bool massalert = false;
        int _lastmass = 0;
        GenericTrackerI _ast = null;
        /// <summary>
        /// gets reference to active symbol tracker
        /// </summary>
        public GenericTrackerI ActiveSymbolTracker { get { return _ast; } }

        public event Int32Delegate StarttimeAndMissingTicks;
        public event Int32Delegate AllsymbolsTicking;
        public event Int32Delegate StopTime;

        public bool useStartAndStop { get { return _starttime * _stoptime != 0; } }

        public int StartAlertTime { get { return _starttime; } set { _starttime = value; } }
        public int StopAlertTime { get { return _stoptime; } set { _stoptime = value; } }
        void _bw_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            while (_continue)
            {
                if (_bw.CancellationPending || e.Cancel)
                    break;
                if (PollProcess != null)
                    PollProcess(_lasttime);
                if ((GotMassAlert != null) && (_defaultmass != 0) && (_lasttime!=0))
                {
                    int span = Util.FTDIFF(_lasttime, Util.DT2FT(DateTime.Now));
                    bool alert = (span > _defaultmass) && 
                        (!useStartAndStop 
                        || ((_lasttime>=_starttime) && (_lasttime<=_stoptime)));
                    if (alert && !_alerting)
                    {
                        _alerting = true;
                        GotMassAlert(_lasttime);
                    }
                    else if (!alert && _alerting)
                    {
                        _alerting = false;
                        _ismassalertcleared = true;
                    }
                }
                if (!alltrading && !sentmissingfirstticks 
                    && (_starttime!=0) && (_lasttime > _starttime))
                {
                    sentmissingfirstticks = true;
                    if (StarttimeAndMissingTicks != null)
                        StarttimeAndMissingTicks(Util.ToTLTime());
                }

                if (_stop && (_stoptime!=0) && (_lasttime >= _stoptime))
                {
                    _stop = false;
                    Stop();
                    if (StopTime != null)
                        StopTime(_lasttime);
                    return;
                }
                System.Threading.Thread.Sleep((int)_pollint);
            }
        }

        public void Stop()
        {
            // flag to stop
            _continue = false;
            _bw.CancelAsync();
        }

    }
}
