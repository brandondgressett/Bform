using BFormDomain.CommonCode.Utility;
using BFormDomain.HelperClasses;
using BFormDomain.MessageBus;
using BFormDomain.Repository;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Notification;

public class UserToastLogic
{
    private readonly IRepository<UserToast> _userToastRepo;
    private readonly IMessageBusSpecifier _messageBusSpecifier;
    private readonly IMessagePublisher _messagePublisher;

    public UserToastLogic(
        IRepository<UserToast> userToastRepo,
        KeyInject<string, IMessagePublisher>.ServiceResolver messagePublisher,
        KeyInject<string, IMessageBusSpecifier>.ServiceResolver busSpecifier)
    {
        _userToastRepo = userToastRepo;
        _messageBusSpecifier = busSpecifier(MessageBusTopology.Distributed.EnumName());
        _messagePublisher = messagePublisher(MessageBusTopology.Distributed.EnumName());
    }

    public async Task<IList<UserToast>> FetchToasts(Guid userId)
    {
        var (toasts, _) = await _userToastRepo.GetOrderedAsync(it => it.Created, true, 0, 100,
            it => it.UserId == userId);

        await _userToastRepo.DeleteFilterAsync(it=>it.UserId == userId);

        return toasts;
    }

    public async Task AddToast(UserToast userToast)
    {
        RunOnce.ThisCode(() =>
        {
            _messageBusSpecifier.DeclareExchange(ToastMessages.ToastExchange, ExchangeTypes.Fanout)
                            .SpecifyExchange(ToastMessages.ToastExchange)
                            .DeclareQueue(ToastMessages.ToastNotify, ToastMessages.ToastNotify.EnumName());
        });

        await _userToastRepo.CreateAsync(userToast);
        await _messagePublisher.SendAsync(new WebNotification
            {
                ForUserId = userToast.UserId.ToString(),
                Kind = WebNotificationKind.Toast
            }, 
            ToastMessages.ToastNotify.EnumName());

    }





}
