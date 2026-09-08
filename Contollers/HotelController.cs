using Humanizer;
using Meeting_Project.Data;
using Meeting_Project.Dtos.HotelDtos;
using Meeting_Project.Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Meeting_Project.Contollers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HotelController : ControllerBase
    {
        private readonly AppDbContext _context;

        public HotelController(AppDbContext context)
        {
            _context = context;
        }
        [HttpGet("getallHotel")]
        [Authorize]
        public IActionResult getAll()
        {
            var hotels=_context.Hotels.Include(p=>p.Rooms).ToList();
            var returnHotels = hotels.Select(h => new GetOtelDto()
            {
                CreatedTime = h.CreatedTime,
                Name = h.Name,
                Description=h.Description== null ? null : h.Description ,
                DeletedTime = h.DeletedTime,
                Email = h.Email==null ?null:h.Email,
                Phone = h.Phone == null ? null : h.Phone,
                Id = h.Id,
                Location=h.Location==null ?null :h.Location,
                IsMain=h.IsMain,
                isDeleted = h.isDeleted,
                RoomDtos = h.Rooms.Select(p => new GetRoomDto()
                {
                    Id = p.Id,
                    RoomNumber = p.RoomNumber,
                    CreatedTime = p.CreatedTime,
                    isDeleted = p.isDeleted,
                    DeletedTime = p.DeletedTime,
                    isFree = p.isFree
                }).ToList()
            });
            return Ok(returnHotels);
        }

        [HttpGet("getById")]
        [Authorize]

        public IActionResult getById(int id)
        {
            if (id == null) return BadRequest("Id not found");
            var existHotel=_context.Hotels.Include(p=>p.Rooms).FirstOrDefault(x=>x.Id == id);
            if (existHotel == null) return BadRequest("Hotel not found");
            var returnHotel = new GetOtelDto()
            {
                CreatedTime = existHotel.CreatedTime,
                isDeleted = existHotel.isDeleted,
                Id=existHotel.Id,
                Location=existHotel.Location==null ?null :existHotel.Location,
                Email = existHotel.Email == null ? null : existHotel.Email,
                Phone = existHotel.Phone == null ? null : existHotel.Phone,
                IsMain=existHotel.IsMain,
                Name = existHotel.Name,
                DeletedTime = existHotel.DeletedTime,
                RoomDtos = existHotel.Rooms.Select(p => new GetRoomDto()
                {
                    Id = p.Id,
                    RoomNumber = p.RoomNumber,
                    CreatedTime = p.CreatedTime,
                    isDeleted = p.isDeleted,
                    DeletedTime= p.DeletedTime,
                    isFree=p.isFree
                }).ToList()
            };
            return Ok(returnHotel);
        }
        [Authorize(Roles ="SuperAdmin")]


        [HttpPost("CreateHotel")]
        public IActionResult Create([FromForm]CreateHotelDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existHotel = _context.Hotels
                .FirstOrDefault(x => x.Name.ToLower() == dto.Name.ToLower());

            if (existHotel != null)
                return BadRequest("Bu adda məkan mövcuddur");

            var hotel = new Hotel
            {
                Name = dto.Name,
                Email = dto.Email == null ? null : dto.Email,
                Phone = dto.Phone == null ? null : dto.Phone,
                IsMain=dto.IsMain,
                Description=dto.Description==null ? null :dto.Description,
                Location=dto.Location==null? null :dto.Location,
                CreatedTime = DateTime.Now.ToString("MM/dd/yyyy"),
                isDeleted = false
            };

            _context.Hotels.Add(hotel);
            _context.SaveChanges();

            return Ok(hotel);
        }
        [Authorize(Roles = "SuperAdmin")]


        [HttpPut("updateHotel")]
        public IActionResult UpdateHotel([FromForm]UpdateHotelDto updateDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existHotel = _context.Hotels
                .Include(x => x.Rooms)
                .FirstOrDefault(x => x.Id == updateDto.Id && !x.isDeleted);

            if (existHotel == null)
                return NotFound("Hotel not found");

            // 🔥 Name check (özünü exclude edir)
            var nameExists = _context.Hotels.Any(x =>
                x.Id != updateDto.Id &&
                !x.isDeleted &&
                x.Name.ToLower() == updateDto.Name.ToLower());

            if (nameExists)
                return BadRequest("Bu adda məkan mövcuddur");

            // 🔥 HOTEL UPDATE
            existHotel.Name = updateDto.Name;
            existHotel.Email = updateDto.Email;
            existHotel.Phone = updateDto.Phone;
            existHotel.IsMain=updateDto.IsMain; 
            existHotel.UpdatedTime = DateTime.Now.ToString("MM/dd/yyyy");
            existHotel.Description = updateDto.Description;
            existHotel.Location = updateDto.Location;

            _context.SaveChanges();

            return Ok();
        }
        [Authorize(Roles = "SuperAdmin")]

        [HttpDelete("deleteHotel")]
        public IActionResult DeleteHotel(int id)
        {
            var existHotel = _context.Hotels
                .Include(x => x.Rooms)
                .FirstOrDefault(x => x.Id == id && !x.isDeleted);

            if (existHotel == null)
                return NotFound("Hotel not found");

            _context.Hotels.Remove(existHotel);

            foreach (var room in existHotel.Rooms)
            {
                _context.HotelRooms.Remove(room);
            }

            _context.SaveChanges();

            return Ok("Hotel və bütün otaqlar silindi");
        }
        [HttpGet("search")]
        public IActionResult SearchHotel(string? searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                var allHotels = _context.Hotels
                    .Include(p => p.Rooms)
                    .Select(h => new GetOtelDto
                    {
                        Id = h.Id,
                        Name = h.Name,
                        Email = h.Email,
                        Phone = h.Phone,
                        CreatedTime = h.CreatedTime,
                        isDeleted = h.isDeleted,
                        RoomDtos = h.Rooms.Select(r => new GetRoomDto
                        {
                            Id = r.Id,
                            RoomNumber = r.RoomNumber,
                            isFree = r.isFree,
                            CreatedTime = r.CreatedTime,
                            isDeleted = r.isDeleted
                        }).ToList()
                    })
                    .ToList();

                return Ok(allHotels);
            }

            var result = _context.Hotels
                .Include(p => p.Rooms)
                .Where(p => p.Name.ToLower().Contains(searchText.ToLower()))
                .Select(h => new GetOtelDto
                {
                    Id = h.Id,
                    Name = h.Name,
                    Email = h.Email,
                    Phone = h.Phone,
                    CreatedTime = h.CreatedTime,
                    isDeleted = h.isDeleted,
                    RoomDtos = h.Rooms.Select(r => new GetRoomDto
                    {
                        Id = r.Id,
                        RoomNumber = r.RoomNumber,
                        isFree = r.isFree,
                        CreatedTime = r.CreatedTime,
                        isDeleted = r.isDeleted
                    }).ToList()
                })
                .ToList();

            if (!result.Any())
            {
                return NotFound(new
                {
                    message = "No hotels found matching your search",
                    data = new List<object>()
                });
            }

            return Ok(result);
        }


        [HttpPost("createRoom")]
        [Authorize(Roles ="SuperAdmin")]
        public IActionResult CreateRoom([FromForm]CreateRoomDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var hotel = _context.Hotels.FirstOrDefault(x => x.Id == dto.HotelId);
            if (hotel == null)
                return BadRequest("Hotel not found");

            var existRoom = _context.HotelRooms.Include(p=>p.Hotel)
                .FirstOrDefault(x =>
                    x.HotelId == dto.HotelId &&
                    !x.isDeleted &&
                    x.RoomNumber.ToLower() == dto.RoomNumber.ToLower()
                );

            if (existRoom != null)
                return BadRequest("Bu adda otaq movcuddur");

            var room = new HotelRoom
            {
                RoomNumber = dto.RoomNumber,
                isFree = true,
                isDeleted = false,
                CreatedTime = DateTime.Now.ToString("MM/dd/yyyy"),
                HotelId = dto.HotelId
            };

            _context.HotelRooms.Add(room);
            _context.SaveChanges();

            return Ok();
        }
        [HttpDelete("DeleteRoom")]
        [Authorize(Roles = "SuperAdmin")]
        public IActionResult DeleteRoom(int id)
        {
            if (id == null) return BadRequest("Id not found");
            var existRoom = _context.HotelRooms.FirstOrDefault(p => p.Id == id);
            if (existRoom == null) return BadRequest("Room not found");
            _context.HotelRooms.Remove(existRoom);
            _context.SaveChanges();
            return Ok();
        }

        [Authorize(Roles ="SuperAdmin")]
        [HttpPut("UpdateRoom")]
        public IActionResult UpdateRoom([FromForm]UpdateRoomDto dto)
        {
            if(!ModelState.IsValid) return BadRequest(ModelState);
            var existRoom = _context.HotelRooms.FirstOrDefault(x => x.Id == dto.Id);
            if (existRoom == null) return BadRequest("Room yoxdur");
            var existroomWithName=_context.HotelRooms.FirstOrDefault(x=>x.RoomNumber.ToLower()==dto.RoomNumber.ToLower());
            if (existroomWithName != null) return BadRequest("Bu adda otaq movcuddur");
            existRoom.RoomNumber = dto.RoomNumber;
            existRoom.UpdatedTime = DateTime.Now.ToString("MM/dd/yyyy");
            _context.SaveChanges();
            return Ok();
        }

        [HttpGet("getAllMeetingRoom")]
        [Authorize]
        public IActionResult GetAllMeetingRoom()
        {
            var rooms = _context.HotelRooms
                .Include(x => x.Hotel)
                .Where(x => !x.isDeleted)
                .OrderBy(x => x.Hotel.Name)
                .ThenBy(x => x.RoomNumber)
                .Select(x => new GetRoomDto
                {
                    Id = x.Id,

                    RoomNumber = x.RoomNumber,

                    isFree = x.isFree,

                    Description = $"{x.Hotel.Name} - {x.RoomNumber} nömrəli otağı",

                    CreatedTime = x.CreatedTime,

                    isDeleted = x.isDeleted,

                    DeletedTime = x.DeletedTime,

                    HotelName = x.Hotel != null
                        ? x.Hotel.Name
                        : ""
                })
                .ToList();

            return Ok(rooms);
        }

    }
}
