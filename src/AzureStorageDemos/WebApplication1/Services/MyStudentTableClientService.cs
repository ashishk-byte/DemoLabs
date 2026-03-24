using Azure.Data.Tables;
using WebApplication1.Entities;


namespace WebApplication1.Services;


public class MyStudentTableClientService
{

    private readonly TableClient _tableClient;


    public MyStudentTableClientService(IConfiguration config)
    {
        var connectionString = config["AzureTableStorage:ConnectionString"];
        var tableName = config["AzureTableStorage:TableName"];

        _tableClient = new TableClient(connectionString, tableName);
        _tableClient.CreateIfNotExists();
    }


    public async Task<List<StudentEntity>> GetAllStudentsAsync()
    {
        var students = new List<StudentEntity>();

        await foreach (var student in _tableClient.QueryAsync<StudentEntity>())
        {
            students.Add(student);
        }

        return students;
    }


    public async Task<StudentEntity?> GetStudentAsync(string rowKey)
    {
        try
        {
            var response = await _tableClient.GetEntityAsync<StudentEntity>(StudentEntity.TableNAME, rowKey);
            return response.Value;
        }
        catch
        {
            return null;
        }
    }


    public async Task AddStudentAsync(StudentEntity student)
    {
        await _tableClient.AddEntityAsync(student);
    }


    public async Task UpdateStudentAsync(StudentEntity student)
    {
        await _tableClient.UpdateEntityAsync(student, student.ETag, TableUpdateMode.Replace);
    }


    public async Task DeleteStudentAsync(string rowKey)
    {
        await _tableClient.DeleteEntityAsync(StudentEntity.TableNAME, rowKey);
    }

}
