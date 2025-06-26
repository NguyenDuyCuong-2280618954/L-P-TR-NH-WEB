namespace WebThoiTrang.Models.Entity
{
    public class EmailConfirmation
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string Email { get; set; }
        public string Code { get; set; }
        public DateTime Expiration { get; set; }
        public bool IsUsed { get; set; } = false;
    }
}
