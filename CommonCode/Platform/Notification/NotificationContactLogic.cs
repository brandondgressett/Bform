using BFormDomain.CommonCode.Notification;
using BFormDomain.HelperClasses;
using BFormDomain.Repository;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Platform.Notification
{
    public class NotificationContactLogic
    {
        IRepository<NotificationContact> _repo;
        ILogger<NotificationContactLogic> _logger;
        public NotificationContactLogic(IRepository<NotificationContact> repo, ILogger<NotificationContactLogic> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public void CreateContact(Guid userRef, NotificationTimeSeverityTable timeTable, string contactTitle,
            string contactNotes, string timeZoneID, bool active, 
            string emailAddress = "", string textNumber = "", string callNumber = "")
        {
            try
            {
                var notiContact = new NotificationContact();

                notiContact.UserRef = userRef;
                notiContact.TimeSeverityTable = timeTable;
                notiContact.ContactTitle = contactTitle;
                notiContact.ContactNotes = contactNotes;
                notiContact.TimeZoneInfoId = timeZoneID;
                notiContact.Active = active;
                notiContact.EmailAddress = emailAddress;
                notiContact.TextNumber = textNumber;
                notiContact.CallNumber = callNumber;

                _repo.Create(notiContact);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("{trace}", ex.TraceInformation());
            }
        }

        public void CreateContact(NotificationContact contact)
        {
            try
            {
                _repo.Create(contact);
            }
            catch(Exception ex)
            {
                _logger.LogWarning("{trace}", ex.TraceInformation());
            }
        }

        public async Task<List<NotificationContact>> GetAll()
        {
            var (data, rc) = await _repo.GetAllAsync(nc => true);
            return data;
        }

        public async Task<NotificationContact> GetContact(Guid ID)
        {
            try
            {

                var notiContact = await _repo.LoadAsync(ID);
                return notiContact.Item1;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("{trace}", ex.TraceInformation());
                throw;
            }
        }

        public async void DeleteContact(Guid ID)
        {
            try
            {
                var notiContact = await GetContact(ID);

                _repo.Delete(notiContact);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("{trace}", ex.TraceInformation());
            }
        }

        public async void EditContact(Guid ID, NotificationContact contact)
        {
            try
            {
                var notiContact = await GetContact(ID);

                notiContact = contact;
                notiContact.Id = ID;
                
                _repo.Update(notiContact);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("{trace}", ex.TraceInformation());
            }
        }
    }
}
