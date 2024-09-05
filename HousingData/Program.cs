using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HousingData
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            
            // Add logging
            builder.Logging.AddConsole();
            builder.Services.AddCors();

            var app = builder.Build();
            
            app.UseCors(builder => builder
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader());

            app.MapGet("/", () => "Welcome to the Housing Data Analysis Service");

            app.MapGet("/analyze", AnalyzeHousingData);
            
            app.MapGet("/api/analysis", GetAnalysisResults);

            app.Run();
        }

        private static async Task<IResult> AnalyzeHousingData(HttpContext context)
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

            try
            {
                var results = await Task.Run(() => GetMonthlyIncreaseResults(logger));

                var outputFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "output", "analysis_results.csv");
                Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));

                await File.WriteAllTextAsync(outputFilePath, "Month,Region,Increase(%)\n" + string.Join("\n", results), Encoding.UTF8);

                return Results.File(outputFilePath, "text/csv", "analysis_results.csv");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while analyzing housing data");
                return Results.Problem("An error occurred while processing your request. Please try again later.");
            }
        }
        
        
        private static async Task<IResult> GetAnalysisResults(HttpContext context)
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

            try
            {
                var results = await Task.Run(() => GetMonthlyIncreaseResults(logger));

                var analysisResults = results.Select(r => 
                {
                    var parts = r.Split(',');
                    return new 
                    {
                        Month = parts[0],
                        Region = parts[1],
                        Increase = double.Parse(parts[2], CultureInfo.InvariantCulture)
                    };
                }).ToList();

                return Results.Json(analysisResults);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while retrieving analysis results");
                return Results.Problem("An error occurred while processing your request. Please try again later.");
            }
        }
        
        private static List<string> GetMonthlyIncreaseResults(ILogger logger)
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "Zip_zhvi_bdrmcnt_4_uc_sfrcondo_tier_0.33_0.67_sm_sa_month.csv");
            
            if (!File.Exists(filePath))
            {
                logger.LogError("CSV file not found at path: {FilePath}", filePath);
                throw new FileNotFoundException("Required CSV file not found", filePath);
            }

            var records = HousingAnalyzer.ReadCsvFile(filePath);
            var monthlyIncreases = HousingAnalyzer.CalculateMonthlyIncreases(records);

            return monthlyIncreases.Values
                .SelectMany(v => v)
                .GroupBy(mi => mi.Date)
                .Select(g => 
                {
                    var maxIncrease = g.OrderByDescending(mi => mi.Increase).First();
                    var region = monthlyIncreases.First(kv => kv.Value.Contains(maxIncrease)).Key;
                    return $"{maxIncrease.Date},{region},{maxIncrease.Increase:F2}";
                })
                .ToList();
        }
    }
}