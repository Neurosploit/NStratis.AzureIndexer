using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using NBitcoin.Indexer.Converters;
using NBitcoin.Protocol;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Indexer
{
    public class IndexerConfiguration
    {
        public static IndexerConfiguration FromConfiguration(string configurationFile)
        {
            IndexerConfiguration config = new IndexerConfiguration();
            Fill(config, configurationFile);
            return config;
        }

        public Task EnsureSetupAsync()
        {
            var tasks = EnumerateTables()
                .Select(t => t.CreateIfNotExistsAsync())
                .OfType<Task>()
                .ToList();
            tasks.Add(GetBlocksContainer().CreateIfNotExistsAsync());
            return Task.WhenAll(tasks.ToArray());
        }
        public void EnsureSetup()
        {
            try
            {
                EnsureSetupAsync().Wait();
            }
            catch (AggregateException aex)
            {
                ExceptionDispatchInfo.Capture(aex).Throw();
                throw;
            }
        }

        protected static void Fill(IndexerConfiguration config, string configurationFile)
        {
            var account = GetValue("Azure.AccountName", true, configurationFile);
            var key = GetValue("Azure.Key", true, configurationFile);
            config.StorageCredentials = new StorageCredentials(account, key);
            config.StorageNamespace = GetValue("StorageNamespace", false, configurationFile);
            var network = GetValue("Bitcoin.Network", false, configurationFile) ?? "Main";            
            if (network.ToLowerInvariant() == "stratismain")
                config.Network = Network.StratisMain;
            else
                config.Network = Network.GetNetwork(network);
            if (config.Network == null)
                throw new InvalidOperationException("Invalid value " + network + " in appsettings (expecting Main, Test or Seg)");
            config.Node = GetValue("Node", false, configurationFile);
            config.CheckpointSetName = GetValue("CheckpointSetName", false, configurationFile);
            if (string.IsNullOrWhiteSpace(config.CheckpointSetName))
                config.CheckpointSetName = "default";

            var emulator = GetValue("AzureStorageEmulatorUsed", false, configurationFile);
            if(!string.IsNullOrWhiteSpace(emulator))
                config.AzureStorageEmulatorUsed = bool.Parse(emulator);
        }

        protected static string GetValue(string config, bool required, string configurationFile)
        {
            if (!File.Exists(configurationFile))
            {
                throw new InvalidOperationException("Configuration file not found.");
            }

            var text = File.ReadAllText(configurationFile);
            var dict = JsonConvert.DeserializeObject<Dictionary<string,string>>(text);

            dict.TryGetValue(config, out string result);            
            result = String.IsNullOrWhiteSpace(result) ? null : result;
            if (result == null && required)
                throw new InvalidOperationException("AppSetting " + config + " not found");
            return result;
        }
        public IndexerConfiguration()
        {
            Network = Network.Main;            
        }
        public Network Network
        {
            get;
            set;
        }

        public bool AzureStorageEmulatorUsed
        {
            get;
            set;
        }

        public AzureIndexer CreateIndexer()
        {
            return new AzureIndexer(this);
        }

        public Node ConnectToNode(bool isRelay)
        {
            if (String.IsNullOrEmpty(Node))
                throw new InvalidOperationException("Node setting is not configured");
            var node = NBitcoin.Protocol.Node.Connect(Network, Node, isRelay: isRelay);

            // TODO: fullnode doesn't handle requesting data with TransactionOptions.All yet in their GetDataPayload methods. 
            // i.e. going from InventoryType.MSG_TX etc. to InventoryType.MSG_WITNESS_TX.
            // This option disables that for the node connection.
            node.PreferredTransactionOptions = TransactionOptions.None;
            return node;
        }

        public string Node
        {
            get;
            set;
        }

        public string CheckpointSetName
        {
            get;
            set;
        }

        string _Container = "indexer";
        string _TransactionTable = "transactions";
        string _BalanceTable = "balances";
        string _ChainTable = "chain";
        string _WalletTable = "wallets";

        public StorageCredentials StorageCredentials
        {
            get;
            set;
        }
        public CloudBlobClient CreateBlobClient()
        {
            return new CloudBlobClient(MakeUri("blob", AzureStorageEmulatorUsed), StorageCredentials);
        }
        public IndexerClient CreateIndexerClient()
        {
            return new IndexerClient(this);
        }
        public CloudTable GetTransactionTable()
        {
            return CreateTableClient().GetTableReference(GetFullName(_TransactionTable));
        }
        public CloudTable GetWalletRulesTable()
        {
            return CreateTableClient().GetTableReference(GetFullName(_WalletTable));
        }

        public CloudTable GetTable(string tableName)
        {
            return CreateTableClient().GetTableReference(GetFullName(tableName));
        }
        private string GetFullName(string storageObjectName)
        {
            return (StorageNamespace + storageObjectName).ToLowerInvariant();
        }
        public CloudTable GetBalanceTable()
        {
            return CreateTableClient().GetTableReference(GetFullName(_BalanceTable));
        }
        public CloudTable GetChainTable()
        {
            return CreateTableClient().GetTableReference(GetFullName(_ChainTable));
        }

        public CloudBlobContainer GetBlocksContainer()
        {
            return CreateBlobClient().GetContainerReference(GetFullName(_Container));
        }

        private Uri MakeUri(string clientType, bool azureStorageEmulatorUsed = false)
        {
            if (!azureStorageEmulatorUsed)
            {
                return new Uri(String.Format("http://{0}.{1}.core.windows.net/", StorageCredentials.AccountName,
                    clientType), UriKind.Absolute);
            }
            else
            {
                if (clientType.Equals("blob"))
                {
                    return new Uri("http://127.0.0.1:10000/devstoreaccount1");
                }
                else
                {
                    if (clientType.Equals("table"))
                    {
                        return new Uri("http://127.0.0.1:10002/devstoreaccount1");
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }


        public CloudTableClient CreateTableClient()
        {
            return new CloudTableClient(MakeUri("table", AzureStorageEmulatorUsed), StorageCredentials);
        }


        public string StorageNamespace
        {
            get;
            set;
        }

        public IEnumerable<CloudTable> EnumerateTables()
        {
            yield return GetTransactionTable();
            yield return GetBalanceTable();
            yield return GetChainTable();
            yield return GetWalletRulesTable();
        }
    }
}
