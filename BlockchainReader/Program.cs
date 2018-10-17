using Neo;
using Neo.Core;
using Neo.Cryptography;
using Neo.Implementations.Blockchains.LevelDB;
using Neo.IO;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockchainReader
{
    class Program
    {
        static void Main(string[] args)
        {
            int specified_height = -1;
            if (args.Length > 0)
            {
                if (!int.TryParse(args[0], out specified_height))
                {
                    Console.WriteLine("Please specify the block height");
                    return;
                }
                else
                {
                    Console.WriteLine("specified block height : {0}", specified_height);
                }
            }

            const string path_acc = "chain.acc";
            if (File.Exists(path_acc))
            {
                using (FileStream fs = new FileStream(path_acc, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    ImportBlocksAndSearchNEO(fs, specified_height);
                    //ImportBlocksAndSearchNEP5(fs, specified_height);
                    //ImportBlocksAndArrangeUTXO(fs, specified_height);
                }
            }
            else
            {
                Console.WriteLine("chain.acc not found");
            }
        }

        private static void ImportBlocksAndSearchNEO(Stream stream, int specified_block_height)
        {
            long start_tick = DateTime.Now.Ticks;
            int tx_count = 0;
            //Dictionary<TransactionType, int> types = new Dictionary<TransactionType, int>();
            //Dictionary<TransactionType, int> typesWithOutput = new Dictionary<TransactionType, int>();
            //Dictionary<TransactionType, int> typesWithOutputNEO = new Dictionary<TransactionType, int>();
            //LevelDBBlockchain blockchain = (LevelDBBlockchain)Blockchain.Default;

            INeoAnalyst analyst = new NeoMemAnalyst();

            using (BinaryReader r = new BinaryReader(stream))
            {
                uint start = 0;
                uint count = r.ReadUInt32();
                uint end = start + count - 1;

                if (specified_block_height > end)
                {
                    Console.WriteLine("Specified block height({0}) is larger than the height({1}) of chain.acc.", specified_block_height, end);
                    return;
                }

                Console.WriteLine("total blocks in chain.acc : {0}", count);

                for (uint height = start; height <= specified_block_height; height++)
                {
                    int block_length = r.ReadInt32();
                    byte[] array = r.ReadBytes(block_length);
                    Block block = array.AsSerializable<Block>();
                    tx_count += block.Transactions.Length;
                    int tx_order = 0;
                    foreach (Transaction tx in block.Transactions)
                    {
                        Fixed8 sumOfInput = Fixed8.Zero;
                        foreach (CoinReference input in tx.Inputs)
                        {
                            AddressPair value = analyst.Remove(input.PrevHash, input.PrevIndex);
                            //AddressPair value = analyst.Peek(input.PrevHash, input.PrevIndex);
                            if (value != null)
                            {
                                sumOfInput += value.value;
                            }
                        }

                        Fixed8 sumOfOutput = Fixed8.Zero;
                        for (ushort output_index = 0; output_index < (ushort)tx.Outputs.Length; output_index++)
                        {
                            TransactionOutput output = tx.Outputs[output_index];
                            if (output.AssetId.Equals(Blockchain.GoverningToken.Hash))
                            {
                                analyst.Add(tx.Hash, output_index, output.Value, output.ScriptHash);
                                sumOfOutput += output.Value;
                            }
                        }

                        if (sumOfInput != sumOfOutput && tx.Type != TransactionType.IssueTransaction)
                        {
                            Console.WriteLine("Unbalanced block height {0}, tx order {1} type {2}, input {3}, output {4},\n\ttx {5}", height, tx_order, tx.Type, sumOfInput, sumOfOutput, tx.Hash);
                            Console.WriteLine("-------- Inputs ----------");
                            foreach (CoinReference input in tx.Inputs)
                            {
                                Console.WriteLine("\t{0} : {1}", input.PrevHash, input.PrevIndex);
                                AddressPair value = analyst.Peek(input.PrevHash, input.PrevIndex);
                                if (value != null)
                                {
                                    Console.WriteLine("\t\t{0} : {1}", value.value, value.address);
                                }
                            }

                            Console.WriteLine("-------- Outputs ----------");

                            for (ushort output_index = 0; output_index < (ushort)tx.Outputs.Length; output_index++)
                            {
                                TransactionOutput output = tx.Outputs[output_index];
                                Console.WriteLine("\t{0} : {1} : {2}", output.Value, output.ScriptHash, output_index);
                            }
                            Console.WriteLine("------------------");

                        }

                        tx_order++;
                    }
                    if (height % 1000 == 0)
                    {
                        double progress = height * 100.0 / (specified_block_height + 1);
                        Console.WriteLine("{0:F2}% : block {1} size {2}", progress, height, block_length);

                    }
                }
            }

            Dictionary<UInt160, Fixed8> balanceDictionary = new Dictionary<UInt160, Fixed8>();
            foreach (AddressPair ap in analyst.AsEnumerable())
            {
                if (balanceDictionary.ContainsKey(ap.address))
                {
                    balanceDictionary[ap.address] = balanceDictionary[ap.address] + ap.value;
                }
                else
                {
                    balanceDictionary[ap.address] = ap.value;
                }
            }

            Export(balanceDictionary);

            long stop_tick = DateTime.Now.Ticks;
            long cost = stop_tick - start_tick;
            long seconds = cost / 10000000;
            Console.WriteLine("{0} seconds", seconds);
            Console.WriteLine("...");
        }

        private static void Export(Dictionary<UInt160, Fixed8> balanceDictionary)
        {
            Console.WriteLine("{0} wallets.", balanceDictionary.Keys.Count);
            File.Delete("address_list.csv");
            StreamWriter sw = File.CreateText("address_list.csv");
            foreach (UInt160 key in balanceDictionary.Keys)
            {
                sw.WriteLine("{0},{1}", Wallet.ToAddress(key), balanceDictionary[key]);
                //Console.WriteLine("{0},{1}", key, balanceDictionary[key]);
            }
            sw.Close();
        }

        private static void ImportBlocksAndSearchNEP5(Stream stream, int specified_block_height)
        {
            long start_tick = DateTime.Now.Ticks;
            int tx_count = 0;
            //Dictionary<TransactionType, int> types = new Dictionary<TransactionType, int>();
            //Dictionary<TransactionType, int> typesWithOutput = new Dictionary<TransactionType, int>();
            //Dictionary<TransactionType, int> typesWithOutputNEO = new Dictionary<TransactionType, int>();
            //LevelDBBlockchain blockchain = (LevelDBBlockchain)Blockchain.Default;

            INeoAnalyst analyst = new NeoMemAnalyst();

            using (BinaryReader r = new BinaryReader(stream))
            {
                uint start = 0;
                uint count = r.ReadUInt32();
                uint end = start + count - 1;

                if (specified_block_height > end)
                {
                    Console.WriteLine("Specified block height({0}) is larger than the height({1}) of chain.acc.", specified_block_height, end);
                    return;
                }

                Console.WriteLine("total blocks in chain.acc : {0}", count);
                Console.WriteLine("========================================================");

                for (uint height = start; height <= specified_block_height; height++)
                {
                    int block_length = r.ReadInt32();
                    byte[] array = r.ReadBytes(block_length);
                    if (height < specified_block_height)
                    {
                        continue;
                    }
                    Block block = array.AsSerializable<Block>();
                    tx_count += block.Transactions.Length;
                    int tx_order = 0;
                    foreach (Transaction tx in block.Transactions)
                    {
                        Console.WriteLine(tx.GetType().Name);
                        Console.WriteLine(tx.Hash);
                        if (tx.Hash.ToString() == "0xce08c478f99ed43d238654d3e5309cff354a5fa2507f6d1d6ac5bfeda8553db6")
                        {
                            InvocationTransaction it = (InvocationTransaction)tx;
                            Console.WriteLine(it.ToJson().ToString());
                        }

                        tx_order++;
                        Console.WriteLine("-------------------------------------------------------");
                    }
                    if (height % 1000 == 0)
                    {
                        double progress = height * 100.0 / (specified_block_height + 1);
                        Console.WriteLine("{0:F2}% : block {1} size {2}", progress, height, block_length);

                    }
                }
            }
        }

        private static void ImportBlocksAndArrangeUTXO(Stream stream, int specified_block_height)
        {
            long start_tick = DateTime.Now.Ticks;
            int tx_count = 0;

            INeoAnalyst analyst = new NeoMemAnalyst();
            Dictionary<IndexPair, bool> utxoDictionary = new Dictionary<IndexPair, bool>();

            using (BinaryReader r = new BinaryReader(stream))
            {
                uint start = 0;
                uint count = r.ReadUInt32();
                uint end = start + count - 1;

                if (specified_block_height > end)
                {
                    Console.WriteLine("Specified block height({0}) is larger than the height({1}) of chain.acc.", specified_block_height, end);
                    return;
                } else if (specified_block_height < 0)
                {
                    specified_block_height = (int) end;
                }

                Console.WriteLine("total blocks in chain.acc : {0}", count);

                for (uint height = start; height <= specified_block_height; height++)
                {
                    int block_length = r.ReadInt32();
                    byte[] array = r.ReadBytes(block_length);
                    Block block = array.AsSerializable<Block>();
                    tx_count += block.Transactions.Length;
                    int tx_order = 0;
                    foreach (Transaction tx in block.Transactions)
                    {
                        Fixed8 sumOfInput = Fixed8.Zero;
                        foreach (CoinReference input in tx.Inputs)
                        {
                            AddressPair value = analyst.Remove(input.PrevHash, input.PrevIndex);
                            //AddressPair value = analyst.Peek(input.PrevHash, input.PrevIndex);
                            if (value != null)
                            {
                                sumOfInput += value.value;
                                utxoDictionary[new IndexPair(input.PrevHash, input.PrevIndex)] = false;
                            }
                        }

                        Fixed8 sumOfOutput = Fixed8.Zero;
                        for (ushort output_index = 0; output_index < (ushort)tx.Outputs.Length; output_index++)
                        {
                            TransactionOutput output = tx.Outputs[output_index];
                            //if (output.AssetId.Equals(Blockchain.GoverningToken.Hash))
                            //{
                            analyst.Add(tx.Hash, output_index, output.Value, output.ScriptHash);
                            sumOfOutput += output.Value;
                            utxoDictionary.Add(new IndexPair(tx.Hash, output_index), true);
                            //}
                        }

                        //if (sumOfInput != sumOfOutput && tx.Type != TransactionType.IssueTransaction && tx.Type != TransactionType.ClaimTransaction && tx.Type != TransactionType.EnrollmentTransaction && tx.Type != TransactionType.RegisterTransaction && tx.Type != TransactionType.InvocationTransaction && tx.Type != TransactionType.MinerTransaction)
                        //{
                        //    Console.WriteLine("Unbalanced block height {0}, tx order {1} type {2}, input {3}, output {4},\n\ttx {5}", height, tx_order, tx.Type, sumOfInput, sumOfOutput, tx.Hash);
                        //    Console.WriteLine("-------- Inputs ----------");
                        //    foreach (CoinReference input in tx.Inputs)
                        //    {
                        //        Console.WriteLine("\t{0} : {1}", input.PrevHash, input.PrevIndex);
                        //        AddressPair value = analyst.Peek(input.PrevHash, input.PrevIndex);
                        //        if (value != null)
                        //        {
                        //            Console.WriteLine("\t\t{0} : {1}", value.value, value.address);
                        //        }
                        //    }

                        //    Console.WriteLine("-------- Outputs ----------");

                        //    for (ushort output_index = 0; output_index < (ushort)tx.Outputs.Length; output_index++)
                        //    {
                        //        TransactionOutput output = tx.Outputs[output_index];
                        //        Console.WriteLine("\t{0} : {1} : {2}", output.Value, output.ScriptHash, output_index);
                        //    }
                        //    Console.WriteLine("------------------");

                        //}

                        tx_order++;
                    }
                    if (height % 1000 == 0)
                    {
                        double progress = height * 100.0 / (specified_block_height + 1);
                        Console.WriteLine("{0:F2}% : block {1} size {2}", progress, height, block_length);

                    }
                }
            }

            Console.WriteLine("{0} UTXO entries.", utxoDictionary.Keys.Count);
            File.Delete("utxo_list.csv");
            StreamWriter sw = File.CreateText("utxo_list.csv");
            foreach (IndexPair key in utxoDictionary.Keys)
            {
                sw.WriteLine("{0},{1},{2}", key.hash, key.index, utxoDictionary[key]?"unspent":"spent");
            }
            sw.Close();

            File.Delete("utxo_sample_100k.csv");
            File.Delete("utxo_status_100k.csv");
            StreamWriter sw1 = File.CreateText("utxo_sample_100k.csv");
            StreamWriter sw2 = File.CreateText("utxo_status_100k.csv");
            List<IndexPair> keys = new List<IndexPair>();
            keys.AddRange(utxoDictionary.Keys);
            Random rnd = new Random();
            for (int i = 0; i < 100000; i++)
            {
                int index = rnd.Next(keys.Count);
                var key = keys[index];
                sw1.WriteLine("{0},{1}", key.hash, key.index);
                sw2.WriteLine("{0}", utxoDictionary[key] ? "unspent" : "spent");
            }
            sw1.Close();
            sw2.Close();

            File.Delete("utxo_sample_1m.csv");
            File.Delete("utxo_status_1m.csv");
            StreamWriter sw3 = File.CreateText("utxo_sample_1m.csv");
            StreamWriter sw4 = File.CreateText("utxo_status_1m.csv");
            for (int i = 0; i < 1000000; i++)
            {
                int index = rnd.Next(keys.Count);
                var key = keys[index];
                sw3.WriteLine("{0},{1}", key.hash, key.index);
                sw4.WriteLine("{0}", utxoDictionary[key] ? "unspent" : "spent");
            }
            sw3.Close();
            sw4.Close();

            long stop_tick = DateTime.Now.Ticks;
            long cost = stop_tick - start_tick;
            long seconds = cost / 10000000;
            Console.WriteLine("{0} seconds", seconds);
            Console.WriteLine("...");
        }
    }
}
