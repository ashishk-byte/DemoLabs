using Azure;
using Azure.Data.Tables;

namespace WebApplication1.Entities;


public class StudentEntity : ITableEntity
{
    public const string TableNAME = "STUDENT";

    #region ITableEntity members

    public string PartitionKey { get; set; } = TableNAME;
    public string RowKey { get; set; } = Guid.NewGuid().ToString();
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    #endregion


    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Age { get; set; }

}