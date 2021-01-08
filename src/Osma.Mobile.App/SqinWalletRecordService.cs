using Hyperledger.Aries.Extensions;
using Hyperledger.Aries.Features.PresentProof;
using Hyperledger.Aries.Storage;
using Hyperledger.Indy.NonSecretsApi;
using Hyperledger.Indy.WalletApi;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Osma.Mobile.App
{
    /// <inheritdoc />
    public class SqinWalletRecordService : IWalletRecordService
    {
        private readonly JsonSerializerSettings _jsonSettings;

        /// <summary>Initializes a new instance of the <see cref="DefaultWalletRecordService"/> class.</summary>
        public SqinWalletRecordService()
        {
            _jsonSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                Converters = new List<JsonConverter>
                {
                    //new AgentEndpointJsonConverter(),
                    new AttributeFilterConverter()
                }
            };
        }

        /// <inheritdoc />
        public virtual Task AddAsync<T>(Wallet wallet, T record)
            where T : RecordBase, new()
        {
            Debug.WriteLine($"Adding record of type {record.TypeName} with Id {record.Id}");

            SetCreatedAtUtc(record);

            return NonSecrets.AddRecordAsync(wallet,
                record.TypeName,
                record.Id,
                Base64Encode(record.ToJson(_jsonSettings)),
                GetTags(record).ToJson());
        }

        /// <inheritdoc />
        public virtual async Task<List<T>> SearchAsync<T>(Wallet wallet, ISearchQuery query, SearchOptions options, int count)
            where T : RecordBase, new()
        {
            using (var search = await NonSecrets.OpenSearchAsync(wallet, new T().TypeName,
                (query ?? SearchQuery.Empty).ToJson(),
                (options ?? new SearchOptions()).ToJson()))
            {
                var result = JsonConvert.DeserializeObject<SearchResult>(await search.NextAsync(wallet, count), _jsonSettings);
                // TODO: Add support for pagination

                return result.Records?
                           .Select(x =>
                           {
                               var record = JsonConvert.DeserializeObject<T>(Base64Decode(x.Value), _jsonSettings);
                               foreach (var tag in x.Tags)
                                   GetTags(record)[tag.Key] = tag.Value;
                               return record;
                           })
                           .ToList()
                       ?? new List<T>();
            }
        }

        /// <inheritdoc />
        public virtual async Task UpdateAsync(Wallet wallet, RecordBase record)
        {
            SetUpdatedAtUtc(record);

            await NonSecrets.UpdateRecordValueAsync(wallet,
                record.TypeName,
                record.Id,
                Base64Encode(record.ToJson(_jsonSettings)));

            await NonSecrets.UpdateRecordTagsAsync(wallet,
                record.TypeName,
                record.Id,
                GetTags(record).ToJson(_jsonSettings));
        }

        private Dictionary<string, string> GetTags(RecordBase record)
        {
            Type recordType = record.GetType();
            PropertyInfo pi = recordType.GetProperty("Tags", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetProperty);
            return pi.GetValue(record, null) as Dictionary<string, string>;
        }

        private void SetCreatedAtUtc(RecordBase record)
        {
            Type recordType = record.GetType();
            PropertyInfo pi = recordType.GetProperty("CreatedAtUtc", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty);
            //pi = recordType.GetProperties()[11];
            pi.SetValue(record, DateTime.UtcNow);
        }

        private void SetUpdatedAtUtc(RecordBase record)
        {
            Type recordType = record.GetType();
            PropertyInfo pi = recordType.GetProperty("CreatedAtUtc", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty);
            //pi = recordType.GetProperties()[12];
            pi.SetValue(record, DateTime.UtcNow);
        }

        /// <inheritdoc />
        public virtual async Task<T> GetAsync<T>(Wallet wallet, string id) where T : RecordBase, new()
        {
            try
            {
                var recordJson = await NonSecrets.GetRecordAsync(wallet,
                    new T().TypeName,
                    id,
                    new SearchOptions().ToJson());

                if (recordJson == null) return null;

                var item = JsonConvert.DeserializeObject<SearchItem>(recordJson, _jsonSettings);

                var record = JsonConvert.DeserializeObject<T>(Base64Decode(item.Value), _jsonSettings);

                foreach (var tag in item.Tags)
                    GetTags(record)[tag.Key] = tag.Value;

                return record;
            }
            catch (WalletItemNotFoundException)
            {
                return null;
            }
        }

        /// <inheritdoc />
        public virtual async Task<bool> DeleteAsync<T>(Wallet wallet, string id) where T : RecordBase, new()
        {
            try
            {
                var record = await GetAsync<T>(wallet, id);
                var typeName = new T().TypeName;

                await NonSecrets.DeleteRecordTagsAsync(
                    wallet: wallet,
                    type: typeName,
                    id: id,
                    tagsJson: GetTags(record).Select(x => x.Key).ToArray().ToJson());
                await NonSecrets.DeleteRecordAsync(
                    wallet: wallet,
                    type: typeName,
                    id: id);

                return true;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Couldn't delete record: {e}");
                return false;
            }
        }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }
    }
}