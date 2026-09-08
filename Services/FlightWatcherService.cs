using Meeting_Project.Data;
using Meeting_Project.Hubs;
using Meeting_Project.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Meeting_Project.Services
{
    public class FlightWatcherService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<FlightHub> _hub;

        private DateTime _lastCall = DateTime.MinValue;

        public FlightWatcherService(
            IServiceScopeFactory scopeFactory,
            IHubContext<FlightHub> hub)
        {
            _scopeFactory = scopeFactory;
            _hub = hub;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("🔥 FlightWatcherService STARTED");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();

                    var flightService =
                        scope.ServiceProvider.GetRequiredService<IFlightApiService>();

                    var db =
                        scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    // ⛔ RATE LIMIT PROTECTION (GLOBAL)
                    if (DateTime.UtcNow - _lastCall < TimeSpan.FromMinutes(2))
                    {
                        Console.WriteLine("⏳ Waiting to avoid API spam...");
                        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                        continue;
                    }

                    _lastCall = DateTime.UtcNow;

                    // 📡 1. OPEN SKY DATA
                    var flights = await flightService.GetFlightsAsync();

                    Console.WriteLine($"📡 API Flights: {flights.Count}");

                    // 📦 2. DB FLIGHTS
                    var trackedFlights = (await db.Flights
                        .AsNoTracking()
                        .Select(x => x.FlightNumber)
                        .ToListAsync(stoppingToken))
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Select(Normalize)
                        .ToHashSet();

                    Console.WriteLine($"📦 DB Flights: {trackedFlights.Count}");

                    // 🎯 3. FILTER MATCHING FLIGHTS
                    var filtered = flights
                        .Where(x =>
                        {
                            var callsign = Normalize(x.FlightNumber);

                            return !string.IsNullOrWhiteSpace(callsign)
                                   && trackedFlights.Contains(callsign);
                        })
                        .ToList();

                    Console.WriteLine($"🎯 MATCHED: {filtered.Count}");

                    foreach (var f in filtered)
                    {
                        Console.WriteLine(
                            $"✈ {f.FlightNumber} | " +
                            $"Lat: {f.Latitude} | Lng: {f.Longitude} | Alt: {f.Altitude}"
                        );
                    }

                    // 📡 4. SIGNALR PUSH (FRONTEND LIVE DATA)
                    await _hub.Clients.All.SendAsync("flightUpdate", filtered, stoppingToken);

                    Console.WriteLine("📡 SignalR pushed");

                    // 💤 5. NORMAL DELAY
                    await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
                }
                catch (HttpRequestException ex) when (ex.Message.Contains("429"))
                {
                    Console.WriteLine("⚠ 429 RATE LIMIT → sleeping 5 minutes");

                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("❌ ERROR: " + ex.Message);
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
            }
        }

        private string Normalize(string input)
        {
            return string.IsNullOrWhiteSpace(input)
                ? null
                : input.Trim().ToUpperInvariant();
        }
    }
}