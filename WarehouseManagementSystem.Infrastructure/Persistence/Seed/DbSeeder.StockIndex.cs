using WarehouseManagementSystem.Domain.Model.InventoryDomain;

namespace WarehouseManagementSystem.Infrastructure.Persistence.Seed;

public static partial class DbSeeder
{
    #region Stock Seed State Model

    private readonly record struct StockSeedKey(
        Guid ProductId,
        Guid WarehouseId,
        Guid WarehouseZoneId,
        Guid? ProductBatchId);

    private sealed class StockSeedState
    {
        public StockSeedState(
            Guid productId,
            Guid warehouseId,
            Guid warehouseZoneId,
            Guid? productBatchId,
            decimal available)
        {
            ProductId = productId;
            WarehouseId = warehouseId;
            WarehouseZoneId = warehouseZoneId;
            ProductBatchId = productBatchId;
            Available = available;
        }

        public Guid ProductId { get; }
        public Guid WarehouseId { get; }
        public Guid WarehouseZoneId { get; }
        public Guid? ProductBatchId { get; }
        public decimal Available { get; private set; }
        public int AvailableListIndex { get; set; } = -1;
        public bool IsAvailable => Available > 0;

        public void Increase(decimal quantity)
        {
            Available += quantity;
        }

        public void Decrease(decimal quantity)
        {
            Available -= quantity;
        }
    }

    #endregion

    #region Stock Seed Index

    private sealed class StockSeedIndex
    {
        private readonly Dictionary<StockSeedKey, StockSeedState> _statesByKey;
        private readonly Dictionary<Guid, List<StockSeedState>> _availableByWarehouse = new();
        private readonly Dictionary<Guid, int> _availableWarehouseIndexes = new();
        private readonly List<Guid> _availableWarehouseIds = new();

        public StockSeedIndex(IEnumerable<Stock> stocks)
        {
            _statesByKey = stocks
                .Select(x => new StockSeedState(
                    x.ProductId,
                    x.WarehouseId,
                    x.WarehouseZoneId,
                    x.ProductBatchId,
                    x.Available))
                .ToDictionary(
                    x => new StockSeedKey(x.ProductId, x.WarehouseId, x.WarehouseZoneId, x.ProductBatchId),
                    x => x);

            foreach (var state in _statesByKey.Values.Where(x => x.IsAvailable))
            {
                AddAvailableState(state);
            }
        }

        public bool HasAvailableStock => _availableWarehouseIds.Count > 0;

        public int GetAvailableStockCount(Guid warehouseId)
        {
            return _availableByWarehouse.TryGetValue(warehouseId, out var states)
                ? states.Count
                : 0;
        }

        public Guid PickWarehouseIdWithAvailableStock(Random random)
        {
            return _availableWarehouseIds.Count == 0
                ? throw new InvalidOperationException("Cannot pick warehouse without available stock.")
                : _availableWarehouseIds[random.Next(_availableWarehouseIds.Count)];
        }

        public StockSeedState PickAvailableStock(Random random, Guid warehouseId)
        {
            return !_availableByWarehouse.TryGetValue(warehouseId, out var states) || states.Count == 0
                ? throw new InvalidOperationException("Cannot generate outbound document item without available stock.")
                : states[random.Next(states.Count)];
        }

        public void Increase(
            Guid productId,
            Guid warehouseId,
            Guid warehouseZoneId,
            Guid? productBatchId,
            decimal quantity)
        {
            var key = new StockSeedKey(productId, warehouseId, warehouseZoneId, productBatchId);
            if (!_statesByKey.TryGetValue(key, out var state))
            {
                state = new StockSeedState(productId, warehouseId, warehouseZoneId, productBatchId, 0);
                _statesByKey.Add(key, state);
            }

            var wasAvailable = state.IsAvailable;
            state.Increase(quantity);

            if (!wasAvailable && state.IsAvailable)
            {
                AddAvailableState(state);
            }
        }

        public void Decrease(StockSeedState state, decimal quantity)
        {
            var wasAvailable = state.IsAvailable;
            state.Decrease(quantity);

            if (wasAvailable && !state.IsAvailable)
            {
                RemoveAvailableState(state);
            }
        }

        private void AddAvailableState(StockSeedState state)
        {
            if (!_availableByWarehouse.TryGetValue(state.WarehouseId, out var states))
            {
                states = new List<StockSeedState>();
                _availableByWarehouse.Add(state.WarehouseId, states);
                _availableWarehouseIndexes[state.WarehouseId] = _availableWarehouseIds.Count;
                _availableWarehouseIds.Add(state.WarehouseId);
            }

            state.AvailableListIndex = states.Count;
            states.Add(state);
        }

        private void RemoveAvailableState(StockSeedState state)
        {
            if (!_availableByWarehouse.TryGetValue(state.WarehouseId, out var states))
            {
                return;
            }

            var index = state.AvailableListIndex;
            if (index < 0 || index >= states.Count)
            {
                return;
            }

            var lastIndex = states.Count - 1;
            var lastState = states[lastIndex];
            states[index] = lastState;
            lastState.AvailableListIndex = index;
            states.RemoveAt(lastIndex);
            state.AvailableListIndex = -1;

            if (states.Count == 0)
            {
                _availableByWarehouse.Remove(state.WarehouseId);
                RemoveAvailableWarehouse(state.WarehouseId);
            }
        }

        private void RemoveAvailableWarehouse(Guid warehouseId)
        {
            if (!_availableWarehouseIndexes.TryGetValue(warehouseId, out var index))
            {
                return;
            }

            var lastIndex = _availableWarehouseIds.Count - 1;
            var lastWarehouseId = _availableWarehouseIds[lastIndex];
            _availableWarehouseIds[index] = lastWarehouseId;
            _availableWarehouseIndexes[lastWarehouseId] = index;
            _availableWarehouseIds.RemoveAt(lastIndex);
            _availableWarehouseIndexes.Remove(warehouseId);
        }
    }

    #endregion
}
