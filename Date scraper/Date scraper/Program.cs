using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace HamroPatroScraper
{
    class Program
    {
        private static readonly HttpClient Http = new()
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        private const int StartYear = 2001;
        private const int EndYear = 2100;

        private static readonly Regex BsIdPattern = new(@"(\d{4})-(\d{1,2})-(\d{1,2})-usn", RegexOptions.Compiled);

        static async Task Main()
        {
            Console.WriteLine($"Scraping data for BS {StartYear}–{EndYear} ...");
            var data = new Dictionary<int, int[]>(); // year → 12 month lengths

            for (int bsYear = StartYear; bsYear <= EndYear; bsYear++)
            {
                var monthLengths = new int[12];

                for (int month = 1; month <= 12; month++)
                {
                    try
                    {
                        var url = $"https://www.example.com/calendar/{bsYear}/{month}";
                        Console.WriteLine($"Fetching {url}");
                        var html = await Http.GetStringAsync(url);

                        var doc = new HtmlDocument();
                        doc.LoadHtml(html);

                        // Find spans like <span id="2082-10-1-usn">
                        var spans = doc.DocumentNode
                            .SelectNodes("//span[starts-with(@id, '" + bsYear + "-" + month + "-')]");
                        if (spans == null)
                        {
                            Console.WriteLine($"  ⚠️ No data found for {bsYear}-{month}");
                            await Task.Delay(400);
                            continue;
                        }

                        var maxDay = spans
                            .Select(s => BsIdPattern.Match(s.Id))
                            .Where(m => m.Success)
                            .Select(m => int.Parse(m.Groups[3].Value))
                            .DefaultIfEmpty(0)
                            .Max();

                        monthLengths[month - 1] = maxDay;
                        Console.WriteLine($"  ✅ {bsYear}-{month:D2} → {maxDay} days");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  ⚠️ {bsYear}-{month:D2}: {ex.Message}");
                    }

                    await Task.Delay(400); // be polite
                }

                data[bsYear] = monthLengths;
            }

            WriteCSharp(data);
            Console.WriteLine("\n✅ Done! BsData.generated.cs created successfully.");
        }

        private static void WriteCSharp(Dictionary<int, int[]> data)
        {
            var lines = new List<string>
            {
                "// Auto-generated Nepali calendar data scraped from HamroPatro",
                "using System.Collections.Generic;",
                "",
                "namespace NepaliCalendar",
                "{",
                "    public static partial class BsDataProvider",
                "    {",
                "        public static readonly Dictionary<int, int[]> BsData = new()",
                "        {"
            };

            foreach (var kv in data.OrderBy(k => k.Key))
            {
                lines.Add($"            {{{kv.Key}, new[]{{{string.Join(",", kv.Value)}}}}},");
            }

            lines.Add("        };");
            lines.Add("    }");
            lines.Add("}");

            File.WriteAllLines("BsData.generated.cs", lines);
        }
    }
}
