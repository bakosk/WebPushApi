using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using WebPush;
using WebPushApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors();
var app = builder.Build();
app.UseCors(b => b
   .AllowAnyOrigin()
   .AllowAnyMethod()
   .AllowAnyHeader()
);

const string fileName = "subscriptions.json";
app.MapGet("/subscriptions", async () => await Helpers.PushSubscriptions(fileName));

app.MapGet("/vapidKey", (IConfiguration config) => config["Vapid:PublicKey"]);

app.MapPost("/subscriptions", async (PushSubscription subscription) =>
{
   var subscriptions = await Helpers.PushSubscriptions(fileName);
   subscriptions.Add(subscription);
   
   File.WriteAllText(fileName,JsonSerializer.Serialize(subscriptions));
});

app.MapPost("/pushNotification", async (NotificationMessage message, IConfiguration configuration) =>
{
   var subscriptions = await Helpers.PushSubscriptions(fileName);
   string subject = configuration["Vapid:Subject"] ?? string.Empty;
   string publicKey = configuration["Vapid:Subject"] ?? string.Empty;
   string privateKey = configuration["Vapid:Subject"] ?? string.Empty;
   var options = new Dictionary<string,object>();
   options["vapidDetails"] = new VapidDetails(subject, publicKey, privateKey);
   //options["gcmAPIKey"] = @"[your key here]";

   var webPushClient = new WebPushClient();

   var angularNotificationString = JsonSerializer.Serialize(new AngularNotification()
   {
      Body = message.Body,
      Title = message.Title
   });
   
   foreach (PushSubscription sub in subscriptions)
   {
      try
      {
         // fire and forget
         webPushClient.SendNotificationAsync(sub, angularNotificationString, options);
      }
      catch (WebPushException exception)
      {
         Console.WriteLine("Http STATUS code" + exception.StatusCode);
      }   
   }
});

app.Run();
