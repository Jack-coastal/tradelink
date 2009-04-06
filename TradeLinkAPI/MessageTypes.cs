﻿

namespace TradeLink.API
{
    /// <summary>
    /// TradeLink Message Types used in TradeLink protocol
    /// http://tradelink.googlecode.com/wiki/TradeLinkProtocol
    /// </summary>
    public enum MessageTypes
    {
        // START TRADELINK STATUS MESSAGES
        TLCLIENT_NOT_FOUND = -111,
        INVALID_ACCOUNT = -110,
        UNKNOWN_ERROR = -109,
        FEATURE_NOT_IMPLEMENTED = -108,
        CLIENTNOTREGISTERED = -107,
        EMPTY_ORDER = -106,
        UNKNOWN_MESSAGE = -105,
        UNKNOWN_SYMBOL = -104,
        BROKERSERVER_NOT_FOUND = -103,
        INVALID_ORDERSIZE = -102,
        DUPLICATE_ORDERID = -101,
        SYMBOL_NOT_LOADED = -100,
        OK = 0,
        // END STATUS MESSAGES
        // START CUSTOM MESSAGES
        CUSTOM1 = 1,
        CUSTOM2,
        CUSTOM3,
        CUSTOM4,
        CUSTOM5,
        CUSTOM6,
        CUSTOM7,
        CUSTOM8,
        CUSTOM9,
        CUSTOM10,
        ISSHORTABLE,
        VWAP,
        LASTTRADESIZE,
        LASTTRADEPRICE,
        LASTBID,
        LASTASK,
        BIDSIZE,
        ASKSIZE,
        DAYLOW,
        DAYHIGH,
        OPENPRICE,
        CLOSEPRICE,
        LRPBUY,
        LRPSELL,
        AMEXLASTTRADE,
        NASDAQLASTTRADE,
        NYSEBID,
        NYSEASK,
        NYSEDAYHIGH,
        NYSEDAYLOW,
        NYSELASTTRADE,
        NASDAQIMBALANCE,
        NASDAQPREVIOUSIMBALANCE,
        NYSEIMBALACE,
        NYSEPREVIOUSIMBALANCE,
        POSPRICEREQUEST,
        POSSIZEREQUEST,
        SENDORDEROCO,
        SENDORDEROSO,
        // END CUSTOM MESSAGES
        // START TRADELINK MESSAGES
        SENDORDER = 5000,
        BROKERNAME,
        VERSION,
        TICKNOTIFY,
        EXECUTENOTIFY,
        REGISTERCLIENT,
        REGISTERSTOCK,
        CLEARSTOCKS,
        CLEARCLIENT,
        HEARTBEAT,
        ORDERNOTIFY,
        ACCOUNTRESPONSE,
        ACCOUNTREQUEST,
        ORDERCANCELREQUEST,
        ORDERCANCELRESPONSE,
        FEATUREREQUEST,
        FEATURERESPONSE,
        POSITIONREQUEST,
        POSITIONRESPONSE,
        IMBALANCEREQUEST,
        IMBALANCERESPONSE,
        SENDORDERMARKET,
        SENDORDERLIMIT,
        SENDORDERSTOP,
        SENDORDERTRAIL,
        SENDORDERMARKETONCLOSE,
        // END TRADELINK MESSAGES

    }
}