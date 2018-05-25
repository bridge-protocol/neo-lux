﻿using Neo.Lux.Cryptography;
using Neo.Lux.Debugger;
using Neo.Lux.Utils;
using Neo.Lux.VM;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.Lux.Core
{
    public class VirtualChain : Chain
    {
        public uint Time { get; set; }

        private DebugClient _debugger;

        public VirtualChain(NeoAPI api, KeyPair owner)
        {
            this.Time = DateTime.UtcNow.ToTimestamp();

            var scripthash = new UInt160(owner.signatureHash.ToArray());

            var txs = new List<Transaction>();
            foreach (var entry in NeoAPI.Assets)
            {
                var symbol = entry.Key;
                var assetID = NeoAPI.GetAssetID(symbol);
                _assetMap[assetID] = new Asset() { hash = new UInt256(assetID), name = symbol };

                var tx = new Transaction();
                tx.outputs = new Transaction.Output[] { new Transaction.Output() { assetID = assetID, scriptHash = scripthash, value = 1000000000 } };
                tx.inputs = new Transaction.Input[] { };
                txs.Add(tx);
            }
            GenerateBlock(txs);
        }

        public void AttachDebugger(DebugClient debugger)
        {
            this._debugger = debugger;
        }

        public void DettachDebugger()
        {
            this._debugger = null;
        }

        internal override void OnLoadScript(ExecutionEngine vm, byte[] script)
        {
            base.OnLoadScript(vm, script);

            if (_debugger != null)
            {
                _debugger.SendScript(script);
            }
        }

        protected override void OnVMStep(ExecutionEngine vm)
        {
            base.OnVMStep(vm);

            if (_debugger != null)
            {
                _debugger.Step(vm);
            }
        }

        protected override uint GetTime()
        {
            return this.Time;
        }

        public void GenerateBlock(IEnumerable<Transaction> transactions)
        {
            var block = new Block();

            var rnd = new Random();
            block.ConsensusData = ((long)rnd.Next() << 32) + rnd.Next();
            block.Height = (uint)_blocks.Count;
            block.PreviousHash = _blocks.Count > 0 ? _blocks[(uint)(_blocks.Count - 1)].Hash : null;
            //block.MerkleRoot = 
            block.Timestamp = this.Time.ToDateTime();
            //block.Validator = 0;
            block.Version = 0;
            block.transactions = new Transaction[transactions.Count()];

            int index = 0;
            foreach (var tx in transactions)
            {
                block.transactions[index] = tx;
                index++;
            }

            AddBlock(block);

            this.Time += 15 * 1000;
        }

    }
}