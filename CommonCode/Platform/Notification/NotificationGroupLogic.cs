using BFormDomain.CommonCode.Notification;
using BFormDomain.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BFormDomain.CommonCode.Platform.Notification
{
    public class NotificationGroupLogic
    {
        IRepository<NotificationGroup> _repo;

        public NotificationGroupLogic(IRepository<NotificationGroup> repo)
        {
            _repo = repo;
        }

        public void CreateNotificationGroup(List<NotificationContactReference> contactRefs, bool active)
        {
            var notigroup = new NotificationGroup();

            notigroup.Active = active;
            notigroup.Members = contactRefs;

            _repo.Create(notigroup);
        }

        public async Task<List<NotificationGroup>> GetAll()
        {
            var(data, rc) = await _repo.GetAllAsync(nc => true);
            return data;
        }
}
}
