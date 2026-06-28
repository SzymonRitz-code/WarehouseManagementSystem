using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.API.Services.Stocks.Query;

namespace WarehouseManagementSystem.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/Stocks/{stockId}/[controller]")]
    public class StockReservationsController : ControllerBase
    {
        private readonly IStockQueryService _stockQueryService;

        public StockReservationsController(IStockQueryService stockQueryService)
        {
            _stockQueryService = stockQueryService;
        }

        /// <summary>
        /// Gets all reservations for the specified stock record.
        /// </summary>
        /// <param name="stockId">Unique stock record identifier.</param>
        /// <returns>List of reservations assigned to the specified stock record.</returns>
        [HttpHead]
        [HttpGet]
        [ResponseCache(CacheProfileName = HttpCacheProfiles.VolatileData)]
        public async Task<ActionResult<IEnumerable<StockReservationDto>>> GetStockReservations(Guid stockId, CancellationToken ct)
        {
            var reservations = await _stockQueryService.GetReservationsAsync(stockId, ct);
            return Ok(reservations);
        }

        /// <summary>
        /// Gets a specific reservation for the specified stock record.
        /// </summary>
        /// <param name="stockId">Unique stock record identifier.</param>
        /// <param name="reservationId">Unique reservation identifier.</param>
        /// <returns>The reservation with the specified identifier, or a 404 response if it does not exist for the given stock record.</returns>
        [HttpHead("{reservationId}")]
        [HttpGet("{reservationId}")]
        [ResponseCache(CacheProfileName = HttpCacheProfiles.VolatileData)]
        public async Task<ActionResult<StockReservationDto>> GetStockReservation(
            Guid stockId,
            Guid reservationId,
            CancellationToken ct)
        {
            var reservation = await _stockQueryService.GetReservationAsync(stockId, reservationId, ct);
            return reservation == null ? (ActionResult<StockReservationDto>)NotFound() : (ActionResult<StockReservationDto>)Ok(reservation);
        }

        /// <summary>
        /// Returns the available HTTP methods supported by the stock reservations controller.
        /// </summary>
        /// <returns>Response with the Allow header containing the list of available HTTP methods.</returns>
        [HttpOptions]
        public IActionResult GetOptions()
        {
            Response.Headers.Append("Allow", "GET, HEAD, OPTIONS");
            return Ok();
        }
    }
}
