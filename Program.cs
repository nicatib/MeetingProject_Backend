using Meeting_Project;
using Meeting_Project.Dtos.AccountDtos;
using Meeting_Project.Hubs;
using FluentValidation;
using FluentValidation.AspNetCore;
using meeting_app.Hubs;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Meeting_Project.Services;

var builder = WebApplication.CreateBuilder(args);
string pathToKey = Path.Combine(Directory.GetCurrentDirectory(), "service-account-key.json");
if (FirebaseApp.DefaultInstance == null)
{
    FirebaseApp.Create(new AppOptions()
    {
        Credential = GoogleCredential.FromFile(pathToKey)
    });
}
builder.WebHost.UseUrls("http://0.0.0.0:5137");

var configuration = builder.Configuration;
// Program.cs faylının içərisində, builder hissəsində:

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Register(configuration);

builder.Services.AddValidatorsFromAssemblyContaining<RegisterDtoValidator>();
builder.Services.AddScoped<IFcmService, FcmService>();
builder.Services.AddFluentValidationAutoValidation();

builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(origin => true) 
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
builder.Services.AddHostedService<MeetingExpirationService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}



app.UseStaticFiles();

app.UseRouting();

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHub<UserHub>("/userHub");

app.Run();