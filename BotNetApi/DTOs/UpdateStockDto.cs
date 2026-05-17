using System.ComponentModel.DataAnnotations;
using BotNetApi.Models;

namespace BotNetApi.DTOs;

public class UpdateStockDto
{
    [Required]
    public StockLevel StockLevel { get; set; }
}
