using FirebaseAdmin.Messaging;
using Meeting_Project.Entity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace Meeting_Project.Services
{
    public interface IFcmService
    {
        Task SendNotificationAsync(
            string userId,
            string title,
            string body,
            object data);
    }

    public class FcmService : IFcmService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public FcmService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task SendNotificationAsync(
            string userId,
            string title,
            string body,
            object data)
        {
            using var scope = _scopeFactory.CreateScope();

            var userManager =
                scope.ServiceProvider
                    .GetRequiredService<UserManager<AppUser>>();

            var user =
                await userManager.FindByIdAsync(userId);

            if (user == null)
            {
                Console.WriteLine(
                    $"[FCM] XƏTA: User tapılmadı: {userId}");

                return;
            }

            if (string.IsNullOrWhiteSpace(user.FcmToken))
            {
                Console.WriteLine(
                    $"[FCM] XƏTA: User üçün FCM token yoxdur: {userId}");

                return;
            }

            try
            {
                var jsonPayload =
                    JsonSerializer.Serialize(data);

                var message = new Message
                {
                    Token = user.FcmToken,

                    // Background / terminated vəziyyətdə
                    // Android sistem notification-u göstərəcək.
                    Notification = new Notification
                    {
                        Title = title,
                        Body = body
                    },

                    // Flutter notification click zamanı
                    // bu məlumatları oxuyacaq.
                    Data = new Dictionary<string, string>
                    {
                        {
                            "title",
                            title
                        },
                        {
                            "body",
                            body
                        },
                        {
                            "payload",
                            jsonPayload
                        },
                        {
                            "click_action",
                            "FLUTTER_NOTIFICATION_CLICK"
                        }
                    },

                    Android = new AndroidConfig
                    {
                        Priority = Priority.High,

                        Notification = new AndroidNotification
                        {
                            ChannelId = "meeting_channel_id",
                            Priority = NotificationPriority.HIGH,
                            DefaultSound = true
                        }
                    }
                };

                var response =
                    await FirebaseMessaging
                        .DefaultInstance
                        .SendAsync(message);

                Console.WriteLine(
                    $"[FCM] UĞURLU: UserId={userId}");

                Console.WriteLine(
                    $"[FCM] MessageId={response}");
            }
            catch (FirebaseMessagingException ex)
            {
                Console.WriteLine(
                    $"[FCM] FirebaseMessagingException: {ex.Message}");

                if (
                    ex.MessagingErrorCode ==
                        MessagingErrorCode.Unregistered ||
                    ex.MessagingErrorCode ==
                        MessagingErrorCode.InvalidArgument
                )
                {
                    Console.WriteLine(
                        $"[FCM] TOKEN SİLİNİR: {userId}");

                    user.FcmToken = null;

                    await userManager.UpdateAsync(user);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[FCM] GÖZLƏNİLMƏZ XƏTA: {ex}");

                throw;
            }
        }
    }
}