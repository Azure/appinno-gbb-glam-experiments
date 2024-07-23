using Microsoft.Extensions.DependencyInjection;

namespace ingestion;

class Program
{
    static async Task Main(string[] args)
    {
        IServiceCollection services = new ServiceCollection();
        Startup startup = new Startup();
        startup.ConfigureServices(services);
        IServiceProvider serviceProvider = services.BuildServiceProvider();

        var dataIngestor = serviceProvider.GetService<IDataIngestor>();
        await dataIngestor!.IngestDataAsync();
    }
}
