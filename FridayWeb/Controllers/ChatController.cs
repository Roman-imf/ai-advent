using FridayWeb.Entities;
using FridayWeb.Services;
using Microsoft.AspNetCore.Mvc;

namespace FridayWeb.Controllers;

[ApiController]
[Route("api/friday/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IFridayChatService chatService;

    public ChatController(IFridayChatService chatService)
    {
        this.chatService = chatService;
    }

    [HttpPost("message/send")]
    public async Task<IActionResult> SendMessageAsync([FromQuery] string message)
    {
        var response = await chatService.SendMessageAsync(message);
        return Ok(response);
    }

    [HttpPost("settings/save")]
    public async Task<IActionResult> SaveSettingsAsync([FromBody] FridaySettings settings)
    {
        await chatService.SaveSettingsAsync(settings);
        return Ok();
    }
}