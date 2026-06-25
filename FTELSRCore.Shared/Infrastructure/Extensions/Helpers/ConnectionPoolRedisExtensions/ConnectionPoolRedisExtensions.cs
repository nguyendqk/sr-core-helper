using StackExchange.Redis;

namespace FTELSRCore.Infrastructure.Extensions.Helpers.ConnectionPoolRedisExtensions
{
    public sealed class ConnectionPoolRedisExtensions : IDisposable
    {
        private bool _disposed;
        private int _index = -1;
        private readonly Lazy<IConnectionMultiplexer>[] _pool;

        public ConnectionPoolRedisExtensions(ConfigurationOptions configurationOptions, int poolSize)
        {
            _pool = new Lazy<IConnectionMultiplexer>[Math.Max(1, poolSize)];

            for (int i = 0; i < _pool.Length; i++)
            {
                _pool[i] = new Lazy<IConnectionMultiplexer>(
                    () => ConnectionMultiplexer.Connect(configurationOptions));
            }
        }

        public IConnectionMultiplexer GetConnection()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            int next = Interlocked.Increment(ref _index);

            return _pool[(next & 0x7FFFFFFF) % _pool.Length].Value;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            foreach (var lazy in _pool)
            {
                if (lazy.IsValueCreated)
                {
                    lazy.Value.Dispose();
                }
            }
        }
    }
}