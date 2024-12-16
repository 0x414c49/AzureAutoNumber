using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using AutoNumber.Exceptions;
using AutoNumber.Extensions;
using AutoNumber.Interfaces;
using AutoNumber.Options;
using Microsoft.Extensions.Options;

namespace AutoNumber
{
    /// <summary>
    ///     Generate a new incremental id regards the scope name
    /// </summary>
    internal class UniqueIdGenerator : IUniqueIdGenerator
    {
        /// <summary>
        ///     Generate a new incremental id regards the scope name
        /// </summary>
        /// <param name="scopeName"></param>
        /// <returns></returns>
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

            while (writesAttempted < MaxWriteAttempts)
            {
                var data = optimisticDataStore.GetDataWithConcurrencyCheck(scopeName);

                if (!long.TryParse(data.Value, out var nextId))
                    throw new UniqueIdGenerationException(
                        $"The id seed returned from storage for scope '{scopeName}' was corrupt, and could not be parsed as a long. The data returned was: {data}");

                state.LastId = nextId - 1;
                state.HighestIdAvailableInBatch = nextId - 1 + BatchSize;
                var firstIdInNextBatch = state.HighestIdAvailableInBatch + 1;

                if (optimisticDataStore.TryOptimisticWriteWithConcurrencyCheck(scopeName,
                    firstIdInNextBatch.ToString(CultureInfo.InvariantCulture), data.ETag))
                    return;

                writesAttempted++;
            }

            throw new UniqueIdGenerationException(
                $"Failed to update the data store after {writesAttempted} attempts. This likely represents too much contention against the store. Increase the batch size to a value more appropriate to your generation load.");
        }

        #region fields

        private readonly IOptimisticDataStore optimisticDataStore;
        private readonly IDictionary<string, ScopeState> states = new Dictionary<string, ScopeState>();
        private readonly object statesLock = new object();
        private int maxWriteAttempts = 25;

        #endregion

        #region properties

        public int BatchSize { get; set; } = 100;

        public int MaxWriteAttempts
        {
            get => maxWriteAttempts;
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException(nameof(value), value,
                        "MaxWriteAttempts must be a positive number.");

                maxWriteAttempts = value;
            }
        }

        #endregion

        #region ctor

        public UniqueIdGenerator(IOptimisticDataStore optimisticDataStore)
        {
            this.optimisticDataStore = optimisticDataStore;
            optimisticDataStore.Init();
        }

        public UniqueIdGenerator(IOptimisticDataStore optimisticDataStore, IOptions<AutoNumberOptions> options)
            : this(optimisticDataStore)
        {
            BatchSize = options.Value.BatchSize;
            MaxWriteAttempts = options.Value.MaxWriteAttempts;
        }

        public UniqueIdGenerator(IOptimisticDataStore optimisticDataStore, AutoNumberOptions options)
            : this(optimisticDataStore)
        {
            BatchSize = options.BatchSize;
            MaxWriteAttempts = options.MaxWriteAttempts;
        }

        #endregion
    }
}