using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedLibrary.Models;

namespace ServerLibrary.HubsProvider
{
    public class ConnectionMapping<T>
    {
        private readonly List<Connections<T>> _connections = new();

        public int Count
        {
            get
            {
                return _connections.Count;
            }
        }

        public void Add(T key, string connectionId, bool isAuthorize)
        {
            lock (_connections)
            {
                var conn = _connections.FirstOrDefault(x => x.Key?.Equals(key) ?? false);

                if (conn == null)
                {
                    conn = new(key);
                    _connections.Add(conn);
                }
                lock (conn)
                {
                    if (!conn.ConnectionsItem.Any(x => x.ConnectionId == connectionId))
                    {
                        conn.ConnectionsItem.Add(new ConnectionsInfo(connectionId, isAuthorize));
                    }
                }

            }
        }

        public IEnumerable<string> GetConnectionsIdForKey(T key)
        {
            if (key != null)
            {
                if (_connections.Any(x => key.Equals(x.Key)))
                {
                    return _connections.First(x => key.Equals(x.Key)).ConnectionsItem.Where(x => x.IsAuthorize).Select(x => x.ConnectionId);
                }
            }
            return Enumerable.Empty<string>();
        }

        public T GetUrlForConnectionId(string connectionId)
        {
            if (_connections.Any(x => x.ConnectionsItem.Any(x => x.ConnectionId == connectionId)))
            {
                return _connections.First(x => x.ConnectionsItem.Any(x => x.ConnectionId == connectionId)).Key;
            }
            return default!;

        }

        public void Remove(T key, string connectionId)
        {
            lock (_connections)
            {

                var conn = _connections.FirstOrDefault(x => x.Key?.Equals(key) ?? false);

                if (conn == null)
                {
                    return;
                }

                lock (conn)
                {
                    conn.ConnectionsItem.RemoveAll(x => x.ConnectionId == connectionId);

                    if (conn.ConnectionsItem.Count == 0)
                    {
                        _connections.Remove(conn);
                    }
                }
            }
        }
    }
}
