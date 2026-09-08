using Humanizer;
using meeting_app.Hubs;
using Meeting_Project.Data;
using Meeting_Project.Dtos.ContactDtos;
using Meeting_Project.Dtos.ContactDtos;
using Meeting_Project.Entity;
using Meeting_Project.Helper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace Meeting_Project.Contollers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContactController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly AppDbContext _context;
        private readonly IHubContext<UserHub> _hubContext;

        public ContactController(IHubContext<UserHub> hubContext, AppDbContext context, UserManager<AppUser> userManager)
        {
            _hubContext = hubContext;
            _context = context;
            _userManager = userManager;
        }


        [Authorize]
        [HttpPost("sendMessage")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageDto dto) 
        {
            if (string.IsNullOrWhiteSpace(dto.Message))
                return BadRequest("Mesaj mətni boş ola bilməz.");

            var senderId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(senderId)) return Unauthorized("İstifadəçi tanınmadı.");
            var existSenderUser=await _userManager.FindByIdAsync(senderId);
            if (existSenderUser == null) return NotFound("Gonderen istifadəçi tapılmadı.");


            var existToUser = await _userManager.FindByIdAsync(dto.ToUserId);
            if (existToUser == null) return NotFound("Qəbuledici istifadəçi tapılmadı.");

         
            Contact newContact = new()
            {
                Message = dto.Message,
                FromUserId = senderId,
                ToUserId = dto.ToUserId,
                MessageStatus = ContactStatus.Unread.ToString(),
                CreatedTime = DateTime.Now.ToString("MM/dd/yyyy")

            };

            await _context.ContactMessages.AddAsync(newContact);
            await _context.SaveChangesAsync();
            await _hubContext.Clients.User(dto.ToUserId).SendAsync("ReceiveMessage", new
            {
                fromUser = senderId,
                message = newContact.Message,
                text = dto.Message,
                toUserId=dto.ToUserId,
                toUserName=existToUser.UserName,
                fromUserName=existSenderUser.FullName,
                createdAt=newContact.CreatedTime,
                messageStatus=newContact.MessageStatus
            });
            return Ok(new { message = "Məktubunuz uğurla göndərildi." });
        }

        [Authorize, HttpGet("getallUserMessage")]
        public async Task<IActionResult> GetAll()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var result = await _context.ContactMessages
          .Where(m => m.ToUserId == currentUserId && !m.isDeleted)
                .Join(_context.Users,
                    contact => contact.FromUserId,
                    sender => sender.Id,
                    (contact, sender) => new { contact, sender })
                .Join(_context.Users,
                    combined => combined.contact.ToUserId,
                    receiver => receiver.Id,
                    (combined, receiver) => new ReturnContactDto
                    {
                        Id = combined.contact.Id,
                        Message = combined.contact.Message,
                        MessageStatus = combined.contact.MessageStatus,
                        CreatedTime = combined.contact.CreatedTime,
                        FromUserName = combined.sender.UserName,
                        FromUserId = combined.sender.Id,
                        ToUserId = combined.contact.ToUserId,
                        ToUseName = receiver.UserName
                    })
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            return Ok(result);
        }

        [Authorize, HttpDelete("deleteMessage/{id}")]
        public async Task<IActionResult> DeleteMessage(int id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserId == null) return Unauthorized("İstifadəçi tapılmadı.");

            var message = await _context.ContactMessages
                .FirstOrDefaultAsync(m => m.Id == id && m.ToUserId == currentUserId);

            if (message == null)
            {
                return NotFound("Mesaj tapılmadı və ya bu mesajı silmək üçün icazəniz yoxdur.");
            }

            message.isDeleted = true;

          
            _context.ContactMessages.Update(message);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Mesaj uğurla silindi." });
        }

        [Authorize,HttpPut("changeStatus")]
        public async Task<IActionResult> ChangeStatus(int id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserId == null) return Unauthorized("İstifadəçi tapılmadı.");

            var message = await _context.ContactMessages
                .FirstOrDefaultAsync(m => m.Id == id && m.ToUserId == currentUserId);

            if (message == null)
            {
                return NotFound("Mesaj tapılmadı və ya bu mesajı silmək üçün icazəniz yoxdur.");
            }
            message.MessageStatus = ContactStatus.Read.ToString();
            _context.ContactMessages.Update(message); 
            await _context.SaveChangesAsync();
            return Ok(new { message = "Mesaj uğurla deyisdirildi." });

        }
    }
}
