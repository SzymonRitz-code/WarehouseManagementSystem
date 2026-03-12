using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.Domain.Interfaces;

namespace WarehouseManagementSystem.API.Controllers
{
    [Route("api/Stocks/{stockId}/[controller]")]
    [ApiController]
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
        /// Pobiera wszystkie rezerwacje dla danego stocka
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<StockReservationDto>>> GetStockReservations(Guid stockId)
        {
            var reservations = await _unitOfWork.Stocks.FindReservationsByStockIdAsync(stockId);

            return Ok(_mapper.Map<IEnumerable<StockReservationDto>>(reservations));
        }

        /// <summary>
        /// Pobiera konkretną rezerwację dla danego stocka
        /// </summary>
        [HttpGet("{reservationId}")]
        public async Task<ActionResult<StockReservationDto>> GetStockReservation(
            Guid stockId,
            Guid reservationId)
        {
            var reservation = (await _unitOfWork.Stocks.FindReservationsByStockIdAsync(stockId)).First(r => r.Id == reservationId );

            if (reservation == null || reservation.StockId != stockId)
                return NotFound();

            return Ok(_mapper.Map<StockReservationDto>(reservation));
        }
    }
}