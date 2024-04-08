using System;
using ScrumBoard.Models.Entities;

namespace ScrumBoard.Models.Messages;

public class AvatarToken : IMessageToken
{
    public Type Component => typeof(Shared.Widgets.Messages.AvatarToken);
    
    public User User { get; }
    
    public double Size { get; }

    public AvatarToken(User user, double size)
    {
        User = user;
        Size = size;
    }
}