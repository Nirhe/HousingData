using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Linq;

namespace HousingData
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var app = builder.Build();

            app.MapGet("/", () => "Hello World!");

            app.MapGet("/analyze", () =>
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "Metro_zhvi_bdrmcnt_4_uc_sfrcondo_tier_0.33_0.67_sm_sa_month.csv");
                var records = HousingAnalyzer.ReadCsvFile(filePath);
                var monthlyIncreases = HousingAnalyzer.CalculateMonthlyIncreases(records);

                var results = new List<string>();
                foreach (var month in monthlyIncreases.Values.First().Select(mi => mi.Date))
                {
                    var maxIncrease = monthlyIncreases
                        .SelectMany(kv => kv.Value)
                        .Where(mi => mi.Date == month)
                        .OrderByDescending(mi => mi.Increase)
                        .First();

                    var region = monthlyIncreases.First(kv => kv.Value.Any(mi => mi.Date == month && mi.Increase == maxIncrease.Increase)).Key;
                    results.Add($"Region with the largest increase in {month}: {region} with an increase of {maxIncrease.Increase:F2}%");

                }

                return results;
            });

            app.Run();
        }
    }
}