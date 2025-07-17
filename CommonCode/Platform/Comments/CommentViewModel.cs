using BFormDomain.CommonCode.Authorization;

namespace BFormDomain.CommonCode.Platform.Comments;

public class CommentViewModel
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = "";
    public string CommentText { get; set; } = "";
    public bool IsChildComment { get; set; }
    public Guid? ParentComment { get; set; }
    public DateTime PostDate { get; set; }


    public static async Task<List<CommentViewModel>> Convert(
        IEnumerable<Comment> data, 
        UserInformationCache cache, 
        string timeZoneId)
    {
        var retval = new List<CommentViewModel>();

        foreach(var comment in data)
        {
            var userInfo = await cache.Fetch(comment.UserID);
            var userName = userInfo?.UserName ?? "unknown";
        
            var localTz =  TimeZoneInfo.FromSerializedString(timeZoneId);
            var postDate = TimeZoneInfo.ConvertTimeFromUtc(comment.PostDate, localTz);

            retval.Add(new CommentViewModel
            {
                Id = comment.Id,
                CommentText = comment.CommentText,
                IsChildComment = comment.IsChildComment,
                ParentComment = comment.ParentComment,
                UserName = userName,
                PostDate = postDate
            });
        }

        return retval;
    }
}
