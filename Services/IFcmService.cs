using FirebaseAdmin.Messaging;
using Meeting_Project.Entity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection; // Bunu əlavə edin
using System.Text.Json;

namespace Meeting_Project.Services
{
    public interface IFcmService
    {
        Task SendNotificationAsync(string userId, string title, string body, object data);
    }

    public class FcmService : IFcmService
    {
        // UserManager əvəzinə IServiceScopeFactory istifadə edirik
        private readonly IServiceScopeFactory _scopeFactory;

        public FcmService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task SendNotificationAsync(string userId, string title, string body, object data)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
                var user = await userManager.FindByIdAsync(userId);

                if (user == null || string.IsNullOrEmpty(user.FcmToken))
                {
                    Console.WriteLine($"[FCM] XƏTA: İstifadəçi ID {userId} üçün FCM token tapılmadı.");
                    return;
                }

                try
                {
                    string jsonPayload = JsonSerializer.Serialize(data);

                    var message = new Message()
                    {
                        Token = user.FcmToken,
                        // Notification obyekti sistem tərəfindən avtomatik bildiriş çıxarır.
                        // Biz bunu şərhə alırıq ki, ancaq Data vasitəsilə Flutter tərəfində idarə edək.
                        // Notification = new Notification { Title = title, Body = body }, 

                        Data = new Dictionary<string, string>
                {
                    { "title", title },
                    { "body", body },
                    { "payload", jsonPayload },
                    { "click_action", "FLUTTER_NOTIFICATION_CLICK" }
                },
                        Android = new AndroidConfig()
                        {
                            Priority = Priority.High,
                            Notification = new AndroidNotification()
                            {
                                ChannelId = "meeting_channel_id",
                                ClickAction = "FLUTTER_NOTIFICATION_CLICK"
                            }
                        }
                    };

                    await FirebaseMessaging.DefaultInstance.SendAsync(message);
                    Console.WriteLine($"[FCM] UĞURLU: Bildiriş istifadəçiyə göndərildi: {userId}");
                }
                catch (FirebaseMessagingException ex)
                {
                    // Əgər token artıq etibarsızdırsa (tətbiq silinib və ya token yenilənib)
                    if (ex.MessagingErrorCode == MessagingErrorCode.Unregistered || ex.MessagingErrorCode == MessagingErrorCode.InvalidArgument)
                    {
                        Console.WriteLine($"[FCM] TOKEN SİLİNİR: İstifadəçi {userId} üçün token etibarsızdır.");
                        user.FcmToken = null;
                        await userManager.UpdateAsync(user);
                    }
                    else
                    {
                        Console.WriteLine($"[FCM] KRİTİK XƏTA: {ex.Message}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[FCM] GÖZLƏNİLMƏZ XƏTA: {ex.Message}");
                }
            }
        }
    }
}