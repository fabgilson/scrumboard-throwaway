using ScrumBoard.Models.Entities.Announcements;
using ScrumBoard.Models.Forms;
using ScrumBoard.Models.Shapes;

namespace ScrumBoard.Extensions
{
    public static class AnnouncementExtensions
    {
        public static Announcement ToAnnouncement(this IAnnouncementShape shape)
        {
            return shape.ToConcreteImplementation<Announcement>();
        }

        public static AnnouncementForm ToAnnouncementForm(this IAnnouncementShape shape)
        {
            return shape.ToConcreteImplementation<AnnouncementForm>();
        }

        private static T ToConcreteImplementation<T>(this IAnnouncementShape shape) where T : IAnnouncementShape, new()
        {
            return new T()
            {
                CanBeHidden = shape.CanBeHidden,
                Content = shape.Content,
                End = shape.End,
                ManuallyArchived = shape.ManuallyArchived,
                Start = shape.Start,
            };
        }
    }
}