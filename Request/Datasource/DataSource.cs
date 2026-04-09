using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;

namespace RockwellPlexServiceLibrary.Request.Datasource
{
    public static class DataSource
    {

        [Display(Name = "Workcenter_Status_Get")]
        public static DataSourceDefinition WorkcenterStatusGet(string dateFrom,string dateTo,string workcenter,string status)
        {
            var parameters = new[]
            {
                new DataSourceParameter { Name = "Date_From", OriginalValue = dateFrom },
                new DataSourceParameter { Name = "Date_To", OriginalValue = dateTo },
                new DataSourceParameter { Name = "Workcenter", OriginalValue = workcenter },
                new DataSourceParameter { Name = "Status", OriginalValue = status }
            };

            return new DataSourceDefinition(
                "Workcenter_Status_Get",
                "12345",
                parameters);                                                                                                                                                                                                           
        }

        [Display(Name = "GetMonthlyScraps")]
        public static DataSourceDefinition GetMonthlyScraps(string dateFrom,string dateTo,string workcenter,string status)
        {
            var parameters = new[]
            {
                new DataSourceParameter { Name = "Date_From", OriginalValue = dateFrom },
                new DataSourceParameter { Name = "Date_To", OriginalValue = dateTo },
                new DataSourceParameter { Name = "Workcenter", OriginalValue = workcenter },
                new DataSourceParameter { Name = "Status", OriginalValue = status }
            };
            return new DataSourceDefinition("GetMonthlyScraps","11944",parameters);
        }
    }

    public class DataSourceDefinition
    {
        public string Name { get; }
        public string DataSourceId { get; }
        public IReadOnlyList<DataSourceParameter> Parameters { get; }

        public DataSourceDefinition(string name, string dataSourceId, IReadOnlyList<DataSourceParameter> parameters)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            DataSourceId = dataSourceId ?? throw new ArgumentNullException(nameof(dataSourceId));
            Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
        }

        public Dictionary<string, object> ToBody()
        {
            return Parameters
                .Where(p => !p.IsEmpty || p.Required || p.Output)
                .ToDictionary(p => p.Name, p => p.Value, StringComparer.OrdinalIgnoreCase);
        }
    }

    public class DataSourceParameter
    {
        public string Name { get; set; }
        public object OriginalValue { get; set; }
        public bool Required { get; set; }
        public bool Output { get; set; }

        public object Value => NormalizeValue();

        public bool IsEmpty
        {
            get
            {
                if (OriginalValue == null)
                {
                    return true;
                }

                if (OriginalValue is string stringValue)
                {
                    return string.IsNullOrWhiteSpace(stringValue);
                }

                return false;
            }
        }

        public override string ToString()
        {
            return Value?.ToString() ?? string.Empty;
        }
        private object NormalizeValue()
        {
            if (OriginalValue == null)
            {
                return null;
            }

            if (OriginalValue is DateTime dateTime)
            {
                return NormalizeDateValue(dateTime);
            }

            if (OriginalValue is string stringValue)
            {
                if (TryParseDateOnly(stringValue, out var parsedDate))
                {
                    return NormalizeDateValue(parsedDate);
                }

                return stringValue;
            }

            return OriginalValue;
        }
        private string NormalizeDateValue(DateTime value)
        {
            if (Name.EndsWith("_From", StringComparison.OrdinalIgnoreCase))
            {
                value = new DateTime(value.Year, value.Month, value.Day, 0, 0, 0, 0, DateTimeKind.Utc);
                return FormatUtc(value);
            }

            if (Name.EndsWith("_To", StringComparison.OrdinalIgnoreCase))
            {
                value = new DateTime(value.Year, value.Month, value.Day, 23, 59, 59, 999, DateTimeKind.Utc);
                return FormatUtc(value);
            }

            DateTime utcValue;
            if (value.Kind == DateTimeKind.Utc)
            {
                utcValue = value;
            }
            else if (value.Kind == DateTimeKind.Local)
            {
                utcValue = value.ToUniversalTime();
            }
            else
            {
                utcValue = DateTime.SpecifyKind(value, DateTimeKind.Utc);
            }

            return FormatUtc(utcValue);
        }
        private static string FormatUtc(DateTime value)
        {
            return value.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);
        }
        private static bool TryParseDateOnly(string value, out DateTime parsedDate)
        {
            return DateTime.TryParseExact(
                value,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out parsedDate);
        }
    }
}
