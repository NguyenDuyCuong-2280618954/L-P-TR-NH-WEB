﻿// Trong Services/IEmailSender.cs
namespace WebThoiTrang.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string toEmail, string subject, string message);
    }
}