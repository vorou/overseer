using Ploeh.SemanticComparison.Fluent;

namespace Overseer.Tests
{
    public static class LikenessExtensions
    {
        public static TSource Ish<TSource>(this TSource obj)
        {
            return obj.AsSource().OfLikeness<TSource>().CreateProxy();
        }
    }
}