using BFormDomain.HelperClasses;
using BFormDomain.Validation;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Platform.Tags
{
    /// <summary>
    /// TagUtil accepts tag information and cleans it up for use
    ///     -References:
    ///         >UserManagementLogic.cs
    ///         >FormLogic.cs
    ///         >KPILogic.cs
    ///         >ReportLogic.cs
    ///         >TableLogic.cs
    ///         >ManagedFileInstance.cs
    ///         >TableRowData.cs
    ///         >Tagger.cs
    ///         >WorkItemLogic.cs
    ///         >WorkSetLogic.cs
    ///     -Functions:
    ///         >OverlapScore
    ///         >MakeTag
    ///         >MakeTags
    /// </summary>
    internal static class TagUtil
    {
        public static int OverlapScore(this ITaggable that, IEnumerable<string> wanted)
        {
            var wantedTags = MakeTags(wanted).Distinct();
            var observedTags = MakeTags(that.Tags).Distinct();
            var common = observedTags.Intersect(wantedTags);
            return common.Count();
        }

        public static string MakeTag(string tag)
        {
            tag.Requires().IsNotNullOrEmpty();
            tag = tag.ToLowerInvariant().Trim();
            tag = tag.Replace(' ', '_');
            tag = tag.Replace('\t', '_');
            return tag;
        }

        public static IEnumerable<string> MakeTags(IEnumerable<string> tags)
        {
            return tags.EmptyIfNull().Select(tag => MakeTag(tag));
        }
    }
}