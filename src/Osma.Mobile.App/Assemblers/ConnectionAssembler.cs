using Autofac;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Features.DidExchange;
using Osma.Mobile.App.Utilities;
using Osma.Mobile.App.ViewModels.Connections;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Osma.Mobile.App.Assemblers
{
    public class ConnectionAssembler : IConnectionAssembler
    {
        private readonly IAgentProvider _agentContextProvider;

        private readonly IConnectionService _connectionService;

        private readonly ILifetimeScope _scope;

        public ConnectionAssembler(IAgentProvider agentContextProvider, IConnectionService connectionService, ILifetimeScope scope)
        {
            _agentContextProvider = agentContextProvider;
            _connectionService = connectionService;
            _scope = scope;
        }

        public ConnectionViewModel Assemble(ConnectionRecord connectionRecord)
        {
            if (connectionRecord == null) return null;

            ConnectionViewModel connection = _scope.Resolve<ConnectionViewModel>(new NamedParameter("record", connectionRecord));

            if (string.IsNullOrWhiteSpace(connection.ConnectionName)) connection.ConnectionName = "Agent Médiateur";

            connection.ConnectionSubtitle = ConnectionStateTranslator.Translate(connectionRecord.State);

            DateTime datetime = DateTime.Now;

            if (connectionRecord.CreatedAtUtc.HasValue)
            {
                datetime = connectionRecord.CreatedAtUtc.Value.ToLocalTime();
                connection.DateTime = datetime;
            }

            return connection;
        }

        public IList<ConnectionViewModel> AssembleMany(IList<ConnectionRecord> connectionRecords)
        {
            if (connectionRecords == null) return null;

            IList<ConnectionViewModel> connections = new List<ConnectionViewModel>();

            foreach (ConnectionRecord connectionRecord in connectionRecords.OrderByDescending(pr => pr.CreatedAtUtc))
            {
                connections.Add(Assemble(connectionRecord));
            }

            return connections;
        }
    }
}