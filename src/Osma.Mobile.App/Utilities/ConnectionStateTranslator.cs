using Hyperledger.Aries.Features.DidExchange;
using System;

namespace Osma.Mobile.App.Utilities
{
    public class ConnectionStateTranslator
    {
        public static string Translate(ConnectionState value)
        {
            switch(value)
            {
                case ConnectionState.Invited:
                    return AppResources.ConnectionStateInvited;
                case ConnectionState.Negotiating:
                    return AppResources.ConnectionStateNegotiating;
                case ConnectionState.Connected:
                    return AppResources.ConnectionStateConnected;
            }
            throw new Exception();
        }
    }
}
