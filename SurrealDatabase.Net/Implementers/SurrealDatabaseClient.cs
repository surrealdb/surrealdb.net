using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using SurrealDatabase.Net.DTOs;
using SurrealDatabase.Net.Helpers;
using SurrealDatabase.Net.Interfaces;

namespace SurrealDatabase.Net.Implementers
{
	public class SurrealDatabaseClient : ISurrealDatabaseClient
	{
		private readonly HttpClient _httpClient;

		public SurrealDatabaseClient(HttpClient httpClient) =>
			_httpClient = httpClient;

		public async Task<SurrealDatabaseResponse> Query(string query)
		{
			try
			{
				var content = new StringContent(query, Encoding.UTF8);
				var surrealResponse = await _httpClient.PostAsync("/sql",content);
				var responseContent = await surrealResponse.Content.ReadAsStringAsync();
				var response = JsonSerializer.Deserialize<SurrealDatabaseResponse>(responseContent);
				if (response is null)
					return SurrealDatabaseResponseHelper.GenerateErrorResponse("Response is null");
				return response;
			}
			catch (Exception e)
			{
				return SurrealDatabaseResponseHelper.GenerateErrorResponse(e.Message);
			}
		}
		public async Task<SurrealDatabaseResponse> SelectRecord(string tableName, string recordId)
		{
			try
			{
				var surrealResponse = await _httpClient.GetAsync($"/key/{tableName}/{recordId}");
				var responseContent = await surrealResponse.Content.ReadAsStringAsync();
				var response = JsonSerializer.Deserialize<SurrealDatabaseResponse>(responseContent);
				if (response is null)
					return SurrealDatabaseResponseHelper.GenerateErrorResponse("Response is null");
				return response;
			}
			catch (Exception e)
			{
				return SurrealDatabaseResponseHelper.GenerateErrorResponse(e.Message);
			}
		}
		public async Task<SurrealDatabaseResponse> SelectRecords(string tableName)
		{
			try
			{
				var surrealResponse = await _httpClient.GetAsync($"/key/{tableName}");
				var responseContent = await surrealResponse.Content.ReadAsStringAsync();
				var response = JsonSerializer.Deserialize<SurrealDatabaseResponse>(responseContent);
				if (response is null)
					return SurrealDatabaseResponseHelper.GenerateErrorResponse("Response is null");
				return response;
			}
			catch (Exception e)
			{
				return SurrealDatabaseResponseHelper.GenerateErrorResponse(e.Message);
			}
		}

		public async Task<SurrealDatabaseResponse> CreateRecord(string tableName, string recordId, object record)
		{
			try
			{
				var serializedRecords = JsonSerializer.Serialize(record);
				var content = new StringContent(serializedRecords, Encoding.UTF8);
				var surrealResponse = await _httpClient.PostAsync($"/key/{tableName}/{recordId}",content);
				var responseContent = await surrealResponse.Content.ReadAsStringAsync();
				var response = JsonSerializer.Deserialize<SurrealDatabaseResponse>(responseContent);
				if (response is null)
					return SurrealDatabaseResponseHelper.GenerateErrorResponse("Response is null");
				return response;
			}
			catch (Exception e)
			{
				return SurrealDatabaseResponseHelper.GenerateErrorResponse(e.Message);
			}
		}
		public async Task<SurrealDatabaseResponse> CreateRecords(string tableName, object records)
		{
			try
			{
				var serializedRecords = JsonSerializer.Serialize(records);
				var content = new StringContent(serializedRecords, Encoding.UTF8);
				var surrealResponse = await _httpClient.PostAsync($"/key/{tableName}",content);
				var responseContent = await surrealResponse.Content.ReadAsStringAsync();
				var response = JsonSerializer.Deserialize<SurrealDatabaseResponse>(responseContent);
				if (response is null)
					return SurrealDatabaseResponseHelper.GenerateErrorResponse("Response is null");
				return response;
			}
			catch (Exception e)
			{
				return SurrealDatabaseResponseHelper.GenerateErrorResponse(e.Message);
			}
		}
		public async Task<SurrealDatabaseResponse> UpdateRecord(string tableName, string recordId, object record)
		{
			try
			{
				var serializedRecords = JsonSerializer.Serialize(record);
				var content = new StringContent(serializedRecords, Encoding.UTF8);
				var surrealResponse = await _httpClient.PutAsync($"/key/{tableName}/{recordId}",content);
				var responseContent = await surrealResponse.Content.ReadAsStringAsync();
				var response = JsonSerializer.Deserialize<SurrealDatabaseResponse>(responseContent);
				if (response is null)
					return SurrealDatabaseResponseHelper.GenerateErrorResponse("Response is null");
				return response;
			}
			catch (Exception e)
			{
				return SurrealDatabaseResponseHelper.GenerateErrorResponse(e.Message);
			}
		}
		public async Task<SurrealDatabaseResponse> ModifyRecord(string tableName, string recordId, object record)
		{
			try
			{
				var serializedRecords = JsonSerializer.Serialize(record);
				var content = new StringContent(serializedRecords, Encoding.UTF8);
				var surrealResponse = await _httpClient.PatchAsync($"/key/{tableName}/{recordId}",content);
				var responseContent = await surrealResponse.Content.ReadAsStringAsync();
				var response = JsonSerializer.Deserialize<SurrealDatabaseResponse>(responseContent);
				if (response is null)
					return SurrealDatabaseResponseHelper.GenerateErrorResponse("Response is null");
				return response;
			}
			catch (Exception e)
			{
				return SurrealDatabaseResponseHelper.GenerateErrorResponse(e.Message);
			}
		}
		public async Task<SurrealDatabaseResponse> DeleteRecord(string tableName, string recordId)
		{
			try
			{
				var surrealResponse = await _httpClient.DeleteAsync($"/key/{tableName}/{recordId}");
				var responseContent = await surrealResponse.Content.ReadAsStringAsync();
				var response = JsonSerializer.Deserialize<SurrealDatabaseResponse>(responseContent);
				if (response is null)
					return SurrealDatabaseResponseHelper.GenerateErrorResponse("Response is null");
				return response;
			}
			catch (Exception e)
			{
				return SurrealDatabaseResponseHelper.GenerateErrorResponse(e.Message);
			}
		}
		public async Task<SurrealDatabaseResponse> DeleteRecords(string tableName)
		{
			try
			{
				var surrealResponse = await _httpClient.DeleteAsync($"/key/{tableName}");
				var responseContent = await surrealResponse.Content.ReadAsStringAsync();
				var response = JsonSerializer.Deserialize<SurrealDatabaseResponse>(responseContent);
				if (response is null)
					return SurrealDatabaseResponseHelper.GenerateErrorResponse("Response is null");
				return response;
			}
			catch (Exception e)
			{
				return SurrealDatabaseResponseHelper.GenerateErrorResponse(e.Message);
			}
		}

	}
}
