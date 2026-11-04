#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace FrontierDispatch.Core
{
    /// <summary>
    /// Straightforward queries over the supplied WorldData.
    ///
    /// The starter project intentionally performs these queries directly over the stored
    /// collections. The implementation is correct, but students should analyze
    /// how its work scales as the world becomes larger and queries are repeated.
    ///
    /// Do not assume that every method must be replaced. Identify the operations
    /// that matter first, then justify any representation changes.
    /// </summary>
    public sealed class WorldQueries
    {
        private readonly WorldData _world;
        
        private readonly Dictionary<int, Location?> _locations = new Dictionary<int, Location?>();
        private readonly Dictionary<int, List<Connection>> _connections = new Dictionary<int, List<Connection>>();

        public WorldQueries(WorldData world)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            CreateDictionary();
        }

        private void CreateDictionary()
        {
            foreach (var location in _world.Locations)
            {
                _locations.Add(location.Id, location);
            }

            foreach (var connection in _world.Connections)
            {
                if (!_connections.ContainsKey(connection.FromLocationId))
                {
                    _connections.Add(connection.FromLocationId, new List<Connection>());
                    
                }

                _connections[connection.FromLocationId].Add(connection);
            }
           
        }

        public Location? FindLocationById(int id)
        {
            return _locations.GetValueOrDefault(id);
        }

        public IReadOnlyList<Connection> FindOutgoingConnections(int fromLocationId)
        {
            return _connections.GetValueOrDefault(fromLocationId);
        }
    }
}
