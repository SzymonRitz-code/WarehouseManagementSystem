using System.ComponentModel.DataAnnotations;

namespace WarehouseManagementSystem.API.DTO
{
    public class CreateWarehouseDto
    {
        [property: Required, StringLength(30)]
        public string Code { get; set; }


        [property: Required, StringLength(200)]
        public string Name {get; set;}

        [property: Required, StringLength(200)]
        public string Country {get; set;}

        [property: Required, StringLength(200)]
        public string City {get; set;}

        [property: Required, StringLength(200)]
        public string Address {get; set;}
    }
}
