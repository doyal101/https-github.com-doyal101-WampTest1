using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using WampSharp.Binding;
using WampSharp.Logging;
using WampSharp.V2.Authentication;
using WampSharp.V2.Client;
using WampSharp.V2.Core.Contracts;
using WampSharp.V2;
using WampSharp.V2.Rpc;
using WampSharp.V2.Binding;
using WampSharp.V2.Fluent;
using WampSharp.V2.Transports;

namespace WampTest1
{
    public class WampInterface
    {
        #region Member Data
        private string WampServerURL = "";
        private string WampServerRealm = "";
        private string WampServerAuthMethod = "";
        private string WampLoginName = "";
        private string WampLoginPwd = "";

        /// <summary>
        /// A WampChannel is an object that represents a WAMP client session to a remote router.
        /// </summary>
        private IWampChannel WampChannel;

        /// <summary>
        /// The "Callee" proxy is the easiest way for the "Caller" to access rpc methods of a WAMP router
        /// </summary>
        private IWampRealmProxy WampRealmProxy;

        private IArgumentsService WampArgsProxy;

        private IWampClientAuthenticator authenticator;

        private string lastErrorMessage = "";

        #endregion

        #region Accessors

        public bool IsConnected { get; private set; } = false;
        public string LastErrorDetails { get => lastErrorMessage; }

        #endregion

        public WampInterface()
        {
           // GetConnectionConfigParams(ref WampServerURL, ref WampServerRealm, ref WampServerAuthMethod, ref WampLoginName, ref WampLoginPwd);
            WampServerURL = "wss://localhost:8080/ws";
            WampServerRealm = "realm1";
            WampServerAuthMethod = "ticket";
            WampLoginName = "admin";
            WampLoginPwd = "123456";
        }

        #region WAMP Server - Instrument - Connection

        public async Task ConnectToLocalServer()
        {
            WampServerURL = "ws://localhost:443/ws";
            WampServerRealm = "realm1";

            try
            {

                DefaultWampChannelFactory channelFactory = new DefaultWampChannelFactory();

                IWampChannel channel = channelFactory.CreateJsonChannel(WampServerURL, WampServerRealm);

                await channel.Open().ConfigureAwait(false);

                Program.ReportAndLog($"Wamp session initialized successfully. ");
                IsConnected = true;
            }
            catch (Exception ex)
            {
                Program.ReportAndLog(ex.Message);
            }
        }

        public async Task ConnectToRemoteServer()
        {
            string stepDescription = "";


            try
            {
                Program.ReportAndLog($"Creating Wamp Channel factory and channel. URL:{WampServerURL}, Realm:{WampServerRealm}");

                DefaultWampChannelFactory channelFactory = new DefaultWampChannelFactory();

                authenticator = new TicketAuthenticator(WampLoginName, WampLoginPwd);
                IWampChannel channel = channelFactory.CreateJsonChannel(WampServerURL, WampServerRealm);  //, authenticator);

                await channel.Open().ConfigureAwait(false);

                Program.ReportAndLog($"Wamp session initialized successfully. ");
                IsConnected = true;

            }
            catch (Exception ex)
            {
                lastErrorMessage = $"Exception attempting WAMP server connection while {stepDescription}. Ex: {ex.Message}";
                Program.ReportAndLog(lastErrorMessage);
                IsConnected = false;
            }
        }

        /// <summary>
        /// If the key fields are present in the app.config file, then update the ref parameters.
        /// If any fields are absent, don't change the associated parameter values.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="realm"></param>
        /// <param name="authentMode"></param>
        /// <param name="user"></param>
        /// <param name="pwd"></param>
        /// <returns></returns>
        private bool GetConnectionConfigParams(ref string url, ref string realm, ref string authentMode, ref string user, ref string pwd)
        {
            bool success = true;
            string s = ConfigurationManager.AppSettings["InstrumentURL"];
            if (success)
                url = string.Copy(s);
            s = ConfigurationManager.AppSettings["InstrumentRealm"];
            if (success)
                realm = string.Copy(s);
            s = ConfigurationManager.AppSettings["AuthorizationID"];
            if (success)
                user = string.Copy(s);
            s = ConfigurationManager.AppSettings["AuthorizationPwd"];
            if (success)
                pwd = string.Copy(s);
            s = ConfigurationManager.AppSettings["AuthorizationMethod"];
            if (success)
                authentMode = string.Copy(s);

            return success;
        }


        //////private async Task ProcessInstrumentStatusUpdate()
        //////{

        //////}

        private bool SubscribeToTopics()
        {
            int received = 0;
            IDisposable subscription = null;

            subscription =
                WampRealmProxy.Services.GetSubject<int>("com.dropworks.mw.private.machine_state")
                          .Subscribe(x =>
                          {
                              Program.ReportAndLog($"Received Event: {x}");

                              received++;

                              if (received > 5)
                              {
                                  Program.ReportAndLog("Closing ..");
                                  subscription.Dispose();
                              }
                          });

            return true;
        }
        #endregion  Connection

        #region Public Commands / Queries


        public bool CancelRun()
        { return true; }

        #endregion Commands / Queries

    }  // End Class QXCInterface



    #region  Helper Classes TicketAuthenticator, IArgumentsService
    public class TicketAuthenticator : IWampClientAuthenticator
    {
        private static readonly string[] mAuthenticationMethods = { "ticket" };
        private readonly IDictionary<string, string> mTickets;
        private const string User = "admin";

        public TicketAuthenticator()
        {
            mTickets = new Dictionary<string, string>() { { "admin", "12345" }, { "invalid", "nonono" } };
        }
        public TicketAuthenticator(string wampLoginName, string wampLoginPwd)
        {
            mTickets = new Dictionary<string, string>();
            mTickets.Add(wampLoginName, wampLoginPwd);
            mTickets.Add("invalid", "nonono");
        }
        public AuthenticationResponse Authenticate(string authmethod, ChallengeDetails extra)
        {
            if (authmethod == "ticket")
            {
                AuthenticationResponse result = new AuthenticationResponse { Signature = mTickets[User] };
                Program.ReportAndLog($"Authenticate called with method {authmethod}  Result = {result.Signature} ");
                return result;
            }
            else
            {
                throw new WampAuthenticationException("Authenticate: don't know how to authenticate using '" + authmethod + "'");
            }
        }

        public string[] AuthenticationMethods
        {
            get
            {
                Program.ReportAndLog($"TicketAuthenticator AuthenticationMethods accessor called. Returning {mAuthenticationMethods[0]}");
                return mAuthenticationMethods;
            }
        }

        public string AuthenticationId
        {
            get
            {
                Program.ReportAndLog($"TicketAuthenticator AuthenticationId accessor called. Returning {User}. ");
                return User;
            }
        }
    }



    /// <summary>
    /// Used for calls to proxy
    /// </summary>
    public interface IArgumentsService
    {
        [WampProcedure("com.arguments.ping")]
        Task PingAsync();

        [WampProcedure("com.arguments.add2")]
        Task<int> Add2Async(int a, int b);

        [WampProcedure("com.arguments.stars")]
        Task<string> StarsAsync(string nick = "somebody", int stars = 0);

        [WampProcedure("com.arguments.orders")]
        Task<string[]> OrdersAsync(string product, int limit = 5);

    }  // end class IArgumentsService

    #endregion
}
