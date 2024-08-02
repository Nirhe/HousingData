using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CsvHelper;
using System.Linq;

namespace HousingData
{
    public static class HousingAnalyzer
    {
        public static List<ZHVIRecord> ReadCsvFile(string filePath)
        {
            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            csv.Read();
            csv.ReadHeader();
            var headers = csv.HeaderRecord;

            var records = new List<ZHVIRecord>();

            while (csv.Read())
            {
                var record = new ZHVIRecord
                {
                    RegionName = csv.GetField("RegionName")
                };

                foreach (var header in headers)
                {
                    if (header != "RegionName" && decimal.TryParse(csv.GetField(header), out decimal value))
                    {
                        record.DateValues[header] = value;
                    }
                }

                records.Add(record);
            }

            return records;
        }

        public static Dictionary<string, List<MonthlyIncrease>> CalculateMonthlyIncreases(List<ZHVIRecord> records)
        {
            var monthlyIncreases = new Dictionary<string, List<MonthlyIncrease>>();

            foreach (var record in records)
            {
                var increases = new List<MonthlyIncrease>();
                var dateKeys = record.DateValues.Keys.OrderBy(k => k).ToList();

                for (int i = 1; i < dateKeys.Count; i++)
                {
                    var previousValue = record.DateValues[dateKeys[i - 1]];
                    var currentValue = record.DateValues[dateKeys[i]];
                    var increase = new MonthlyIncrease
                    {
                        Date = dateKeys[i],
                        //Increase = currentValue - previousValue
                        Increase = previousValue != 0 ? ((currentValue - previousValue) / previousValue) * 100 : 0
                    };
                    increases.Add(increase);
                }
                monthlyIncreases.Add(record.RegionName, increases);
            }

            return monthlyIncreases;
        }
    }
}
