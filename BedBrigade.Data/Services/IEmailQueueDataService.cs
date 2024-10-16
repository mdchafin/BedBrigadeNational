﻿using BedBrigade.Common.Enums;
using BedBrigade.Common.Models;

namespace BedBrigade.Data.Services
{
    public interface IEmailQueueDataService : IRepository<EmailQueue>
    {
        Task<List<EmailQueue>> GetLockedEmails();
        Task<List<EmailQueue>> GetEmailsSentToday();
        Task ClearEmailQueueLock();
        Task<List<EmailQueue>> GetEmailsToProcess(int maxPerChunk);
        Task DeleteOldEmailQueue(int daysOld);
        Task LockEmailsToProcess(List<EmailQueue> emailsToProcess);
        Task<ServiceResponse<string>> GetSendPlanMessage(int locationId, EmailRecipientOption option, int scheduleId);
        Task<ServiceResponse<List<string>>> GetEmailsToSend(int locationId, EmailRecipientOption option, int scheduleId);
        Task<ServiceResponse<string>> QueueEmail(EmailQueue email);
        Task<ServiceResponse<string>> QueueBulkEmail(List<string> emaiList, string subject, string body);
    }
}
