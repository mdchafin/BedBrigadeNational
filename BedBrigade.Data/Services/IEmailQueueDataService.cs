﻿using BedBrigade.Data.Models;

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
    }
}