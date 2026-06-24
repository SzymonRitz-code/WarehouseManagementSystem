using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.Domain.Interfaces;

namespace WarehouseManagementSystem.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/Stocks/{stockId}/[controller]")]
    public class StockReservationsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public StockReservationsController(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        /// <summary>
        /// Gets all reservations for the specified stock record.
        /// </summary>
        /// <param name="stockId">Unique stock record identifier.</param>
        /// <returns>List of reservations assigned to the specified stock record.</returns>
        [HttpHead]
        [HttpGet]
        [ResponseCache(CacheProfileName = HttpCacheProfiles.VolatileData)]
        public async Task<ActionResult<IEnumerable<StockReservationDto>>> GetStockReservations(Guid stockId)
        {
            var reservations = await _unitOfWork.Stocks.FindReservationsByStockIdAsync(stockId);

            return Ok(_mapper.Map<IEnumerable<StockReservationDto>>(reservations));
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
            Guid reservationId)
        {
            var reservation = (await _unitOfWork.Stocks.FindReservationsByStockIdAsync(stockId))
                .FirstOrDefault(r => r.Id == reservationId);

            if (reservation == null || reservation.StockId != stockId)
                return NotFound();

            return Ok(_mapper.Map<StockReservationDto>(reservation));
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
