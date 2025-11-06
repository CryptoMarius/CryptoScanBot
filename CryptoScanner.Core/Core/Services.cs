//using CryptoScanner.Core.Exchange;

//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;

//namespace CryptoScanner.Core.Core;

////public abstract class BaseProcessor
////{
////    public abstract string Name { get; }
////    public abstract string Process();
////}

////public class ProcessorA : BaseProcessor
////{
////    public override string Name => "A";
////    public override string Process() => "Processor A draait";
////}

////public class ProcessorB : BaseProcessor
////{
////    public override string Name => "B";
////    public override string Process() => "Processor B draait";
////}

//public interface IExchangeFactory
//{
//    ExchangeBase GetExchange(string key);
//}

//public class ExchangeFactory : IExchangeFactory
//{
//    private readonly Dictionary<string, ExchangeBase> _exchanges;

//    public ExchangeFactory(IEnumerable<ExchangeBase> exchanges)
//    {
//        _exchanges = exchanges.ToDictionary(e => e.ExchangeOptions.ExchangeName, e => e);
//    }

//    public ExchangeBase GetExchange(string key)
//    {
//        if (_exchanges.TryGetValue(key, out var exchange))
//            return exchange;

//        throw new Exception($"Exchange {key} not found.");
//    }
//}

//public class MyService
//{
//    private readonly IExchangeFactory _factory;

//    public MyService(IExchangeFactory factory)
//    {
//        _factory = factory;
//    }

//    public ExchangeBase GetExchange(string key)
//    {
//        var exchange = _factory.GetExchange(key);
//        return exchange;
//    }
//}
//public class Services
//{

//    public void ConfigureServices()
//    {
//        var builder = new HostBuilder().ConfigureServices((hostContext, services) =>
//        {
//            //services.AddSingleton<Form1>();
//            //services.AddLogging(configure => configure.AddConsole());
//            //services.AddScoped<ExchangeBase, BusinessLayer>();
//            //services.AddScoped<IDataAccessLayer, CDataAccessLayer>();
//            services.AddScoped<IExchangeFactory, ExchangeFactory>();
            
//            services.AddScoped<ExchangeBase, Exchange.Binance.Futures.Api>();
//            services.AddScoped<ExchangeBase, Exchange.Binance.Spot.Api>();

//            services.AddScoped<ExchangeBase, Exchange.BybitApi.Futures.Api>();
//            services.AddScoped<ExchangeBase, Exchange.BybitApi.Spot.Api>();
            
//            services.AddScoped<ExchangeBase, Exchange.Kucoin.Futures.Api>();
//            services.AddScoped<ExchangeBase, Exchange.Kucoin.Spot.Api>();

//            services.AddScoped<ExchangeBase, Exchange.BloFin.Futures.Api>();
//            //services.AddScoped<ExchangeBase, Exchange.BloFin.Spot.Api>();
//        });
//    }

//    public void Test()
//    {
//    }

//}
