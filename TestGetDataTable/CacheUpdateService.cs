using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using TestGetDataTable.Context;

public class CacheUpdateService : IHostedService, IDisposable
{
    private readonly IDistributedCache _cache;
    private readonly ApplicationDbContext _context;
    private Timer _timer;

    public CacheUpdateService(IDistributedCache cache, ApplicationDbContext context)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new Timer(UpdateCache, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));
        return Task.CompletedTask;
    }

    private async void UpdateCache(object state)
    {
        var data = await _context.Employees.ToListAsync();
        var serializedData = JsonConvert.SerializeObject(data);

        await _cache.SetStringAsync("DataEmployee", serializedData, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        });
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
