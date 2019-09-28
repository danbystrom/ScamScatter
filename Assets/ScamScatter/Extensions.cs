using UnityEngine;

namespace ScamScatter
{
    public static class Extensions
    {
        public static T GetComponentInChildrenPure<T>(this GameObject gameObject) where T : class
        {
            var component = gameObject.GetComponentInChildren<T>();
            // ReSharper disable once MergeConditionalExpression
#pragma warning disable IDE0029 // Use coalesce expression
            return component == null ? null : component;
#pragma warning restore IDE0029 // Use coalesce expression
        }

    }

}
