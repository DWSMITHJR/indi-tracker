using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;

namespace Tracker.API.Data.Seeders;

public interface ICsvDataReader
{
    Task<List<T>> ReadCsvAsync<T>(string filePath);
}

public class CsvDataReader : ICsvDataReader
{
    private readonly ILogger<CsvDataReader> _logger;

    public CsvDataReader(ILogger<CsvDataReader> logger)
    {
        _logger = logger;
    }

    public async Task<List<T>> ReadCsvAsync<T>(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            throw new FileNotFoundException($"CSV file not found: {filePath}");

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                HeaderValidated = null,
                MissingFieldFound = null,
                BadDataFound = context =>
                {
                    try
                    {
                        // The context parameter is a non-nullable value type, so we don't need to check for null
                        if (context.Context?.Parser != null && context.RawRecord != null)
                        {
                            _logger?.LogWarning("Bad data found in {FilePath} at row {Row}: {RawRecord}", 
                                filePath, context.Context.Parser.Row, context.RawRecord);
                        }
                        else
                        {
                            _logger?.LogWarning("Bad data found in {FilePath}", filePath);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error processing bad data in {FilePath}", filePath);
                    }
                }
            };

            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, config);
            
            var records = new List<T>();
            await foreach (var record in csv.GetRecordsAsync<T>()) 
            {
                records.Add(record);
            }
            
            _logger?.LogInformation("Successfully read {RecordCount} records from {FilePath}", records.Count, filePath);
            return records;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error reading CSV file: {FilePath}", filePath);
            throw;
        }
    }
}
