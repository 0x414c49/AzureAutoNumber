using AutoNumber.Exceptions;
using AutoNumber.Extensions;
using AutoNumber.Interfaces;
using AutoNumber.Options;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace AutoNumber
{
    public class UniqueIdGenerator : IUniqueIdGenerator
    {
        private readonly IOptimisticDataStore optimisticDataStore;

        private readonly IDictionary<string, ScopeState> states = new Dictionary<string, ScopeState>();
        private readonly object statesLock = new object();
        private int maxWriteAttempts = 25;

        public UniqueIdGenerator(IOptimisticDataStore optimisticDataStore)
        {
            this.optimisticDataStore = optimisticDataStore;
            optimisticDataStore.Init()
                .GetAwaiter()
                .GetResult();
        }

        public UniqueIdGenerator(IOptimisticDataStore optimisticDataStore, IOptions<HailstoneOptions> options)
            : this(optimisticDataStore)
        {
            BatchSize = options.Value.BatchSize;
            MaxWriteAttempts = options.Value.MaxWriteAttempts;
        }

        public int BatchSize { get; set; } = 100;

        public int MaxWriteAttempts
        {
            get { return maxWriteAttempts; }
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException(nameof(value), maxWriteAttempts, "MaxWriteAttempts must be a positive number.");

                maxWriteAttempts = value;
            }
        }

        public long NextId(string scopeName)
        {
            var state = GetScopeState(scopeName);

            lock (state.IdGenerationLock)
            {
                if (state.LastId == state.HighestIdAvailableInBatch)
                    UpdateFromSyncStore(scopeName, state);

                return Interlocked.Increment(ref state.LastId);
            }
        }

        private ScopeState GetScopeState(string scopeName)
        {
            return states.GetValue(
                scopeName,
                statesLock,
                () => new ScopeState());
        }

        private void UpdateFromSyncStore(string scopeName, ScopeState state)
        {
            var writesAttempted = 0;

            while (writesAttempted < maxWriteAttempts)
            {
                var data = optimisticDataStore.GetData(scopeName);

                long nextId;
                if (!long.TryParse(data, out nextId))
                    throw new UniqueIdGenerationException($"The id seed returned from storage for scope '{scopeName}' was corrupt, and could not be parsed as a long. The data returned was: {data}");

                state.LastId = nextId - 1;
                state.HighestIdAvailableInBatch = nextId - 1 + BatchSize;
                var firstIdInNextBatch = state.HighestIdAvailableInBatch + 1;

                if (optimisticDataStore.TryOptimisticWrite(scopeName, firstIdInNextBatch.ToString(CultureInfo.InvariantCulture)))
                    return;

                writesAttempted++;
            }

            throw new UniqueIdGenerationException($"Failed to update the data store after {writesAttempted} attempts. This likely represents too much contention against the store. Increase the batch size to a value more appropriate to your generation load.");
        }
    }
}
