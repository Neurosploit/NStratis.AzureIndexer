using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace NBitcoin.Indexer.Console
{
    class IndexerOptions
    {
        [Option('s', "settingsFile", Required = true, HelpText = "Path to LocalSettings.config file.")]
        public string SettingsFile
        {
            get;
            set;
        }

        [Option('b', "IndexBlocks", Default = false, Required = false, HelpText = "Index blocks into azure blob container")]
        public bool IndexBlocks
        {
            get;
            set;
        }

        [Option("IgnoreCheckpoints", HelpText = "Ignore checkpoints (Do not save them, nor load them)", Required = false, Default = false)]
        public bool IgnoreCheckpoints
        {
            get;
            set;
        }

        [Option("ListCheckpoints", HelpText = "list checkpoints", Required = false, Default = false)]
        public bool ListCheckpoints
        {
            get;
            set;
        }
        [Option("AddCheckpoint", HelpText = "add/set checkpoint (format : \"CheckpointName:Height\")", Required = false, Default = null)]
        public string AddCheckpoint
        {
            get;
            set;
        }

        [Option("DeleteCheckpoint", HelpText = "delete checkpoint (format : checkpoint name)", Required = false, Default = null)]
        public string DeleteCheckpoint
        {
            get;
            set;
        }

        [Option('?', "help", HelpText = "Display this help screen.", Required = false)]
        public string Help
        {
            get; set;
        }

        string _Usage;
        public string GetUsage(ParserResult<IndexerOptions> parserResult)
        {
            if (_Usage == null)
            {
                _Usage = HelpText.AutoBuild<IndexerOptions>(parserResult);
                _Usage = _Usage.Replace("NBitcoin.Indexer 1.0.0.0", "NBitcoin.Indexer " + typeof(IndexerClient).GetTypeInfo().Assembly.GetName().Version);
            }
            return _Usage;
            //
        }

        [Option('c', "CountBlkFiles", HelpText = "Count the number of blk file downloaded by bitcoinq", Default = false, Required = false)]
        public bool CountBlkFiles
        {
            get;
            set;
        }

        [Option("From",
            HelpText = "The height of the first block to index",
            Default = 0,
            Required = false)]
        public int From
        {
            get;
            set;
        }
        [Option("To",
            HelpText = "The height of the last block (included)",
            Default = 99999999,
            Required = false)]
        public int To
        {
            get;
            set;
        }


        [Option('t', "IndexTransactions", Default = false, Required = false, HelpText = "Index transactions into azure table")]
        public bool IndexTransactions
        {
            get;
            set;
        }

        [Option('w', "IndexWallets", Default = false, Required = false, HelpText = "Index wallets into azure table")]
        public bool IndexWallets
        {
            get;
            set;
        }


        [Option('a', "IndexAddresses", Default = false, Required = false, HelpText = "Index bitcoin addresses into azure table")]
        public bool IndexAddresses
        {
            get;
            set;
        }

        [Option('m', "IndexMainChain", Default = false, Required = false, HelpText = "Index the main chain into azure table")]
        public bool IndexChain
        {
            get;
            set;
        }

        [Option("All", Default = false, Required = false, HelpText = "Index all objects, equivalent to -m -a -b -t -w")]
        public bool All
        {
            get;
            set;
        }

        [Option("CheckpointInterval", Default = "00:15:00", Required = false, HelpText = "Interval after which the indexer flush its progress to azure tables and save a checkpoint")]
        public string CheckpointInterval
        {
            get;
            set;
        }
    }
}
