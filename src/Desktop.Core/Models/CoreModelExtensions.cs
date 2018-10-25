//----------------------------------------------------------------------- 
// PDS WITSMLstudio StoreSync, 2018.2
// Copyright 2018 PDS Americas LLC
//-----------------------------------------------------------------------

using System.Collections.Generic;
using PDS.WITSMLstudio.Framework;

namespace PDS.WITSMLstudio.Desktop.Core.Models
{
    /// <summary>
    /// Extension Methods used in both the Adapter and StoreSync Configuration Manager Projects
    /// </summary>
    public static class CoreModelExtensions
    {
        /// <summary>
        /// To the name of the unique.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="existingNames">The existing names.</param>
        /// <returns>name</returns>
        public static string ToUniqueName(this string name, string[] existingNames)
        {
            // Remove any non-alpha characters from the end of the string
            while (name.Length > 1 && !char.IsLetter(name[name.Length - 1]))
            {
                name = name.Substring(0, name.Length - 1);
            }

            // Check if there are any existing names that match the new copy
            var startIndex = 1;
            while (existingNames.ContainsIgnoreCase($"{name}_{startIndex}"))
            {
                startIndex++;
            }

            return $"{name}_{startIndex}";
        }

        /// <summary>
        /// Next or default.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="item">The item.</param>
        /// <returns>NextOrDefault</returns>
        public static T NextOrDefault<T>(this IList<T> collection, T item)
        {
            var index = collection.IndexOf(item);
            item = default(T);

            if (index + 1 < collection.Count)
                item = collection[index + 1];
            else if (index - 1 >= 0)
                item = collection[index - 1];

            return item;
        }
    }
}
