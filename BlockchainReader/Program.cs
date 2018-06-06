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
                    ImportBlocks(fs, specified_height);
                }
            }
            else
            {
                Console.WriteLine("chain.acc not found");
            }
        }

        private static void ImportBlocks(Stream stream, int specified_block_height)
        {
            long start_tick = DateTime.Now.Ticks;
            int tx_count = 0;
            //Dictionary<TransactionType, int> types = new Dictionary<TransactionType, int>();
            //Dictionary<TransactionType, int> typesWithOutput = new Dictionary<TransactionType, int>();
            //Dictionary<TransactionType, int> typesWithOutputNEO = new Dictionary<TransactionType, int>();
            //UInt256 dup_tx_id = UInt256.Parse("0xd46e2df37f43fe3d1dd6f2bb0fddfac3c1821a22215df3df31050cf4f482001c");
            //LevelDBBlockchain blockchain = (LevelDBBlockchain)Blockchain.Default;

            NeoAnalyst analyst = new NeoMemAnalyst();

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
                    foreach(Transaction tx in block.Transactions)
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

                        if (sumOfInput!=sumOfOutput && tx.Type != TransactionType.IssueTransaction)
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

                        //if (tx.Hash.Equals(dup_tx_id))
                        //{
                        //    Console.WriteLine("Block height : {0}, tx order : {1}", height, tx_order);
                        //    Console.WriteLine("tx type {0}, version {1}, length {2}", tx.Type, tx.Version, tx.Size);
                        //    var hex = Helper.ToHexString(tx);
                        //    Console.WriteLine(hex);
                        //}
                        //if (types.ContainsKey(tx.Type))
                        //{
                        //    types[tx.Type] = types[tx.Type] + 1;
                        //}
                        //else
                        //{
                        //    types[tx.Type] = 1;
                        //}
                        //if (tx.Outputs.Length > 0)
                        //{
                        //    if (typesWithOutput.ContainsKey(tx.Type))
                        //    {
                        //        typesWithOutput[tx.Type] = typesWithOutput[tx.Type] + 1;
                        //    }
                        //    else
                        //    {
                        //        typesWithOutput[tx.Type] = 1;
                        //    }
                        //    foreach (TransactionOutput to in tx.Outputs)
                        //    {
                        //        if (to.AssetId.Equals(Blockchain.GoverningToken.Hash))
                        //        {
                        //            if (typesWithOutputNEO.ContainsKey(tx.Type))
                        //            {
                        //                typesWithOutputNEO[tx.Type] = typesWithOutputNEO[tx.Type] + 1;
                        //            }
                        //            else
                        //            {
                        //                typesWithOutputNEO[tx.Type] = 1;
                        //            }
                        //            //if (tx.Type == TransactionType.InvocationTransaction)
                        //            //{
                        //            //    Console.WriteLine("{0}", tx.Hash);
                        //            //    foreach (CoinReference input in tx.Inputs)
                        //            //    {
                        //            //        Console.WriteLine("{0} : {1}", input.PrevHash, input.PrevIndex);
                        //            //    }
                        //            //    //goto Exit;
                        //            //}
                        //        }
                        //    }
                        //}
                        tx_order++;
                    }
                    //blockchain.AddBlockDirectly(block);
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

            export(balanceDictionary);

            long stop_tick = DateTime.Now.Ticks;
            long cost = stop_tick - start_tick;
            long seconds = cost / 10000000;
            Console.WriteLine("{0} seconds", seconds);
            //Console.WriteLine("{0} transactions", tx_count);
            //foreach (TransactionType tt in types.Keys)
            //{
            //    Console.WriteLine("{0} : {1}", tt, types[tt]);
            //}
            //Console.WriteLine("----------------------------------");
            //foreach (TransactionType tt in typesWithOutput.Keys)
            //{
            //    Console.WriteLine("{0} : {1}", tt, typesWithOutput[tt]);
            //}
            //Console.WriteLine("----------------------------------");
            //foreach (TransactionType tt in typesWithOutputNEO.Keys)
            //{
            //    Console.WriteLine("{0} : {1}", tt, typesWithOutputNEO[tt]);
            //}
            Console.WriteLine("...");
        }

        private static void export(Dictionary<UInt160, Fixed8> balanceDictionary)
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
    }
}
