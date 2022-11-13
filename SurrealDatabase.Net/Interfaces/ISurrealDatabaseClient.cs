using System.Threading.Tasks;
using SurrealDatabase.Net.DTOs;

namespace SurrealDatabase.Net.Interfaces
{
	public interface ISurrealDatabaseClient
	{
		Task<SurrealDatabaseResponse> Query(string query);
		Task<SurrealDatabaseResponse> SelectRecord(string tableName, string recordId);
		Task<SurrealDatabaseResponse> SelectRecords(string tableName);
		Task<SurrealDatabaseResponse> CreateRecord(string tableName, string recordId, object record);
		Task<SurrealDatabaseResponse> CreateRecords(string tableName, object records);
		Task<SurrealDatabaseResponse> UpdateRecord(string tableName, string recordId, object record);
		Task<SurrealDatabaseResponse> ModifyRecord(string tableName, string recordId, object record);
		Task<SurrealDatabaseResponse> DeleteRecord(string tableName, string recordId);
		Task<SurrealDatabaseResponse> DeleteRecords(string tableName);
	}
}
