// Request body for POST /api/orders/{id}/ask — a customer's free-text question
// about their order, answered by the AI concierge (#43).
using System.ComponentModel.DataAnnotations;

namespace OrderService.DTOs;

public class AskQuestionDto
{
    [Required]
    [MaxLength(500)]
    public string Question { get; set; } = string.Empty;
}
